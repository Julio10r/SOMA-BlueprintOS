using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Integrations.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Domain.Integrations.Occurrences;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed record ProcessamentoItensFiscaisReferenciasFornecedorResultado(
    Guid ExecucaoRawId, bool DryRun, bool Aplicado, int TotalRaw, int Inseridos, int Atualizados, int SemAlteracao, int Conflitos, int OcorrenciasPersistidas);

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): RAW→REFINED→DOMÍNIO para Item Fiscal Referência por
/// Fornecedor. Estratégia FULL (decisão do PO — sem watermark/trigger disponíveis nesta tabela). Sem
/// máquina de baseline/incremental (mesmo padrão de Unidade de Medida/domínios de Fornecedor).
/// </summary>
public sealed class ProcessarItensFiscaisReferenciasFornecedorRawParaDominioUseCase(
    BlueprintOSDbContext context,
    IIntegrationOccurrenceRepository occurrenceRepository,
    ILogger<ProcessarItensFiscaisReferenciasFornecedorRawParaDominioUseCase> logger)
{
    private const string ErpSistema = "SOMA_DESENV";
    private const int TamanhoDoLote = 2000;
    private const string Dataset = LinxReadDatasetCatalog.ItensFiscaisReferenciasFornecedorSnapshot;

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026): <paramref name="unidadeNegocioId"/> é a Business
    /// Unit explícita da execução — obrigatória, nunca inferida. Escopa a leitura de
    /// <c>FornecedorLinxVinculo</c> (identidade agora inclui BU) — sem isso, dois vínculos de BUs
    /// diferentes com o mesmo CodigoErp colidiriam no dicionário de resolução.</summary>
    public async Task<ProcessamentoItensFiscaisReferenciasFornecedorResultado> ExecutarAsync(bool dryRun, Guid unidadeNegocioId, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        if (unidadeNegocioId == Guid.Empty)
            throw new ArgumentException("Business Unit é obrigatória e não pode ser Guid.Empty — pipeline headless nunca lê/escreve domínio sem uma Unidade de Negócio explícita e válida (fail closed).", nameof(unidadeNegocioId));

        var execucaoRaw = await context.RawLinxFornecedoresSnapshotExecucoes
            .AsNoTracking()
            .Where(e => e.Dataset == Dataset && e.Completa)
            .OrderByDescending(e => e.ConcluidoEm)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Nenhuma execução RAW completa encontrada para o dataset '{Dataset}'.");

        var agora = timeProvider.GetUtcNow();

        var rawRows = await context.RawLinxItensFiscaisReferenciasFornecedorSnapshot.AsNoTracking().ToListAsync(cancellationToken);
        var itens = rawRows.Select(r => new ItemFiscalReferenciaFornecedorRefinedItem(r.CodigoItem, r.CodigoItemFornecedor, r.ErpFornecedorId, r.FornecedoresResolvidos)).ToList();

        var itensFiscaisPorCodigo = await context.ItensFiscais.AsNoTracking().Select(f => new { f.Id, f.Codigo }).ToDictionaryAsync(f => f.Codigo, f => f.Id, cancellationToken);
        var fornecedorIdPorCodigoErpVinculo = await context.FornecedorLinxVinculos.AsNoTracking()
            .Where(v => v.ErpSistema == ErpSistema && v.UnidadeNegocioId == unidadeNegocioId)
            .Select(v => new { v.CodigoErp, v.FornecedorId })
            .ToDictionaryAsync(v => v.CodigoErp, v => v.FornecedorId, cancellationToken);

        var existentesLista = await context.ItensFiscaisReferenciasFornecedor.AsNoTracking()
            .Select(r => new { r.Id, r.ItemFiscalId, r.FornecedorId, r.CodigoItemFornecedor })
            .ToListAsync(cancellationToken);
        var existentes = existentesLista.ToDictionary(
            r => (r.ItemFiscalId, r.FornecedorId),
            r => new ItemFiscalReferenciaFornecedorExistente(r.Id, r.CodigoItemFornecedor));
        var itemFiscalIdPorCodigoNoFornecedor = existentesLista.ToDictionary(r => (r.FornecedorId, r.CodigoItemFornecedor), r => r.ItemFiscalId);

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(itens, itensFiscaisPorCodigo, fornecedorIdPorCodigoErpVinculo, existentes, itemFiscalIdPorCodigoNoFornecedor);
        var inseridos = plano.Decisoes.Count(d => d.Action == ItemFiscalReferenciaFornecedorRefinedAction.Insert);
        var atualizados = plano.Decisoes.Count(d => d.Action == ItemFiscalReferenciaFornecedorRefinedAction.Update);
        var semAlteracao = plano.Decisoes.Count(d => d.Action == ItemFiscalReferenciaFornecedorRefinedAction.NoChange);
        logger.LogInformation("REFINED Item Fiscal Ref. Fornecedor: {Total} lidos, {Inseridos} inserir, {Atualizados} atualizar, {SemAlteracao} sem-alteracao, {Conflitos} conflitos.",
            itens.Count, inseridos, atualizados, semAlteracao, plano.Conflitos.Count);

        var ocorrenciasPersistidas = 0;
        if (plano.Conflitos.Count > 0)
        {
            var jaPersistidas = await occurrenceRepository.ListarPorExecucaoAsync(execucaoRaw.Id, cancellationToken);
            if (jaPersistidas.Count == 0)
            {
                if (!dryRun)
                {
                    var ocorrencias = plano.Conflitos.Select(c => IntegrationOccurrence.Registrar(
                        execucaoRaw.Id, unidadeNegocioId, Dataset, IntegrationStage.Refined, IntegrationOccurrenceSeverity.Conflict, c.Code, c.Mensagem, c.OriginRecordKey, agora)).ToList();
                    await occurrenceRepository.AdicionarLoteAsync(ocorrencias, cancellationToken);
                }
                ocorrenciasPersistidas = plano.Conflitos.Count;
            }
            else
            {
                ocorrenciasPersistidas = jaPersistidas.Count;
            }
        }

        if (dryRun)
        {
            return new(execucaoRaw.Id, dryRun, Aplicado: false, itens.Count, inseridos, atualizados, semAlteracao, plano.Conflitos.Count, ocorrenciasPersistidas);
        }

        foreach (var lote in plano.Decisoes.Where(d => d.Action != ItemFiscalReferenciaFornecedorRefinedAction.NoChange).Chunk(TamanhoDoLote))
        {
            var idsParaAtualizar = lote.Where(d => d.ExistenteId.HasValue).Select(d => d.ExistenteId!.Value).ToList();
            var tracked = idsParaAtualizar.Count == 0
                ? []
                : await context.ItensFiscaisReferenciasFornecedor.Where(r => idsParaAtualizar.Contains(r.Id)).ToDictionaryAsync(r => r.Id, cancellationToken);

            foreach (var decisao in lote)
            {
                if (decisao.Action == ItemFiscalReferenciaFornecedorRefinedAction.Update)
                {
                    tracked[decisao.ExistenteId!.Value].Atualizar(decisao.CodigoItemFornecedor, agora);
                }
                else
                {
                    context.ItensFiscaisReferenciasFornecedor.Add(new ItemFiscalReferenciaFornecedor(decisao.ItemFiscalId, decisao.FornecedorId, decisao.CodigoItemFornecedor, agora));
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }

        return new(execucaoRaw.Id, dryRun, Aplicado: true, itens.Count, inseridos, atualizados, semAlteracao, plano.Conflitos.Count, ocorrenciasPersistidas);
    }
}
