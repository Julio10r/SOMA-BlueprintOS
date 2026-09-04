using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>B3 — Bloco 5A/5A.9: sincronização de Referências de Item Fiscal por Fornecedor
/// (`ITEM_FISCAL_REF_FORNECEDOR`). A resolução de identidade do Fornecedor já acontece no reader
/// (<see cref="IItemFiscalReferenciaFornecedorErpReader"/>, cadeia validada com dado real: `FORNECEDOR`
/// (=`NOME_CLIFOR`) -> `CLIFOR` -> `FORNECEDORES.COD_FORNECEDOR`); este caso de uso nunca tenta uma
/// resolução alternativa (sem fuzzy/LIKE/contains/escolha arbitrária) — só age quando
/// <see cref="ItemFiscalReferenciaFornecedorErpDto.FornecedoresResolvidos"/> é exatamente 1.
///
/// Bloco 5A.9 (GAP PLATINUM): a resolução do Fornecedor local passa a ser por QUALQUER
/// <see cref="BlueprintOS.Domain.Procurement.Suppliers.FornecedorLinxVinculo"/> conhecido (`ErpSistema` +
/// `CLIFOR/COD_FORNECEDOR`), nunca mais por `Fornecedor.ErpFornecedorId` (campo legado, espelha só o
/// Principal atual) — o vínculo resolvido NÃO precisa ser Principal; nunca usa CNPJ como fallback para
/// inferir a identidade Linx da referência.
///
/// Diferente do Item Fiscal (Bloco 5A.3, LWW ainda não implementado): esta tabela já tem regra de conflito
/// homologada sem timestamp confiável (`ADR-0024` — Linx prevalece), então uma referência local já
/// existente com <c>CodigoItemFornecedor</c> divergente É atualizada para o valor do Linx.</summary>
public sealed class SincronizarItemFiscalReferenciasFornecedorErpUseCase(
    IItemFiscalReferenciaFornecedorErpReader reader,
    IItemFiscalRepository itensFiscais,
    IFornecedorLinxVinculoRepository vinculos,
    IItemFiscalReferenciaFornecedorRepository referencias,
    ICurrentIdentity identity,
    ILogger<SincronizarItemFiscalReferenciasFornecedorErpUseCase> logger) : ISincronizarItemFiscalReferenciasFornecedorErpUseCase
{
    private const int TamanhoPaginaPadrao = 200;
    private const string ErpSistemaLinx = "SOMA_DESENV";

    public async Task<SincronizacaoItemFiscalReferenciasFornecedorErpResumo> ExecuteAsync(SincronizarItemFiscalReferenciasFornecedorErpDto dto, CancellationToken cancellationToken = default)
    {
        // Onda 2 (Multi-BU/Multi-ERP, 03/09/2026): FornecedorLinxVinculo agora é escopado por Business
        // Unit — mesmo fail-closed já usado por SincronizarItensFiscaisErpUseCase/SincronizarFornecedoresErpUseCase.
        var identidadeAtual = identity.GetRequired();
        if (identidadeAtual.UnidadeNegocioId is null || identidadeAtual.UnidadeNegocioId == Guid.Empty)
        {
            throw new InvalidOperationException("A sessão atual não possui Unidade de Negócio resolvida; sincronização de Referências de Item Fiscal por Fornecedor não pode ser iniciada.");
        }
        var unidadeNegocioId = identidadeAtual.UnidadeNegocioId.Value;
        var correlationId = string.IsNullOrWhiteSpace(dto.CorrelationId) ? Guid.NewGuid().ToString("N") : dto.CorrelationId.Trim()[..Math.Min(dto.CorrelationId.Trim().Length, 100)];
        var limiteExplicito = dto.Limite > 0 ? dto.Limite : (int?)null;
        var dryRun = dto.DryRun;

        var inicio = DateTimeOffset.UtcNow;
        int consultados = 0, incluidos = 0, atualizados = 0, semAlteracao = 0, erros = 0;
        var conflitos = new List<ItemFiscalReferenciaFornecedorErpConflito>();
        var skip = 0;
        var possivelmenteTruncado = false;

        logger.LogInformation(
            "Sincronizacao de referencias de item fiscal por fornecedor ERP iniciada. LimiteExplicito {LimiteExplicito}. DryRun {DryRun}. CorrelationId {CorrelationId}",
            limiteExplicito, dryRun, correlationId);

        while (true)
        {
            int tamanhoPagina;
            if (limiteExplicito.HasValue)
            {
                var restante = limiteExplicito.Value - consultados;
                if (restante <= 0) break;
                tamanhoPagina = Math.Min(TamanhoPaginaPadrao, restante);
            }
            else
            {
                tamanhoPagina = TamanhoPaginaPadrao;
            }

            var lote = await reader.BuscarReferenciasAsync(skip, tamanhoPagina, cancellationToken);
            if (lote.Count == 0) break;

            foreach (var externo in lote)
            {
                consultados++;
                try
                {
                    var (resultado, conflito) = await ProcessarAsync(externo, unidadeNegocioId, dryRun, cancellationToken);
                    switch (resultado)
                    {
                        case ResultadoProcessamento.Incluido: incluidos++; break;
                        case ResultadoProcessamento.Atualizado: atualizados++; break;
                        case ResultadoProcessamento.SemAlteracao: semAlteracao++; break;
                        case ResultadoProcessamento.Conflito: conflitos.Add(conflito!); break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    erros++;
                    logger.LogError(ex, "Erro parcial na sincronizacao de referencia de item fiscal por fornecedor ERP. CodigoItem {CodigoItem}. CodigoItemFornecedor {CodigoItemFornecedor}",
                        externo.CodigoItem, externo.CodigoItemFornecedor);
                }
            }

            skip += lote.Count;
            if (limiteExplicito.HasValue && consultados >= limiteExplicito.Value)
            {
                possivelmenteTruncado = lote.Count == tamanhoPagina;
                break;
            }
        }

        var fim = DateTimeOffset.UtcNow;
        var status = dryRun ? "DryRunConcluido" : "Concluida";
        logger.LogInformation(
            "Sincronizacao de referencias de item fiscal por fornecedor ERP finalizada. Status {Status}. Consultados {Consultados}. Incluidos {Incluidos}. Atualizados {Atualizados}. SemAlteracao {SemAlteracao}. Conflitos {Conflitos}. Erros {Erros}.",
            status, consultados, incluidos, atualizados, semAlteracao, conflitos.Count, erros);

        return new SincronizacaoItemFiscalReferenciasFornecedorErpResumo(
            status, inicio, fim, consultados, incluidos, atualizados, semAlteracao, erros,
            (long)(fim - inicio).TotalMilliseconds, correlationId, possivelmenteTruncado, conflitos);
    }

    private enum ResultadoProcessamento { Incluido, Atualizado, SemAlteracao, Conflito }

    private async Task<(ResultadoProcessamento, ItemFiscalReferenciaFornecedorErpConflito?)> ProcessarAsync(
        ItemFiscalReferenciaFornecedorErpDto externo, Guid unidadeNegocioId, bool dryRun, CancellationToken ct)
    {
        if (externo.FornecedoresResolvidos != 1 || externo.ErpFornecedorId is null)
        {
            return (ResultadoProcessamento.Conflito, new ItemFiscalReferenciaFornecedorErpConflito(
                externo.CodigoItem, externo.CodigoItemFornecedor, null, ItemFiscalReferenciaFornecedorErpConflitoMotivo.NomeFornecedorNaoResolvidoOuAmbiguo));
        }

        var itemFiscal = await itensFiscais.ObterPorCodigoSemRastreamentoAsync(externo.CodigoItem, ct);
        if (itemFiscal is null)
        {
            return (ResultadoProcessamento.Conflito, new ItemFiscalReferenciaFornecedorErpConflito(
                externo.CodigoItem, externo.CodigoItemFornecedor, externo.ErpFornecedorId, ItemFiscalReferenciaFornecedorErpConflitoMotivo.ItemFiscalAindaNaoSincronizadoLocalmente));
        }

        var vinculo = await vinculos.ObterPorErpSistemaECodigoAsync(ErpSistemaLinx, externo.ErpFornecedorId, unidadeNegocioId, ct);
        if (vinculo is null)
        {
            return (ResultadoProcessamento.Conflito, new ItemFiscalReferenciaFornecedorErpConflito(
                externo.CodigoItem, externo.CodigoItemFornecedor, externo.ErpFornecedorId, ItemFiscalReferenciaFornecedorErpConflitoMotivo.FornecedorAindaNaoSincronizadoLocalmente));
        }
        var fornecedorId = vinculo.FornecedorId;

        var existente = await referencias.ObterPorItemEFornecedorAsync(itemFiscal.Id, fornecedorId, ct);
        if (existente is not null)
        {
            if (existente.CodigoItemFornecedor == externo.CodigoItemFornecedor.Trim())
            {
                return (ResultadoProcessamento.SemAlteracao, null);
            }

            // ADR-0024 — sem timestamp confiável nesta tabela: Linx prevalece em divergência.
            if (await referencias.ExisteCodigoParaFornecedorAsync(fornecedorId, externo.CodigoItemFornecedor, excluirId: existente.Id, ct))
            {
                return (ResultadoProcessamento.Conflito, new ItemFiscalReferenciaFornecedorErpConflito(
                    externo.CodigoItem, externo.CodigoItemFornecedor, externo.ErpFornecedorId, ItemFiscalReferenciaFornecedorErpConflitoMotivo.CodigoItemFornecedorJaAssociadoAOutroItem));
            }

            if (dryRun) return (ResultadoProcessamento.Atualizado, null);
            existente.Atualizar(externo.CodigoItemFornecedor, DateTimeOffset.UtcNow);
            await referencias.SalvarAlteracoesAsync(ct);
            return (ResultadoProcessamento.Atualizado, null);
        }

        if (await referencias.ExisteCodigoParaFornecedorAsync(fornecedorId, externo.CodigoItemFornecedor, excluirId: null, ct))
        {
            return (ResultadoProcessamento.Conflito, new ItemFiscalReferenciaFornecedorErpConflito(
                externo.CodigoItem, externo.CodigoItemFornecedor, externo.ErpFornecedorId, ItemFiscalReferenciaFornecedorErpConflitoMotivo.CodigoItemFornecedorJaAssociadoAOutroItem));
        }

        if (dryRun) return (ResultadoProcessamento.Incluido, null);

        var nova = new ItemFiscalReferenciaFornecedor(itemFiscal.Id, fornecedorId, externo.CodigoItemFornecedor, DateTimeOffset.UtcNow);
        await referencias.AdicionarAsync(nova, ct);
        await referencias.SalvarAlteracoesAsync(ct);
        return (ResultadoProcessamento.Incluido, null);
    }
}
