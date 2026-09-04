using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>B3 — Bloco 5A/5A.7: sincronização de leitura/importação Linx -> +Compras de Item Fiscal
/// (`CADASTRO_ITEM_FISCAL`). Mesmo padrão de paginação/limite de <c>SincronizarFornecedoresErpUseCase</c>,
/// deliberadamente SEM os mecanismos daquele use case que não se aplicam aqui: sem guarda de inativação em
/// massa (este caso de uso nunca inativa nada automaticamente por conta própria — só reflete a situação
/// cadastral real vinda do Linx). Itens sem Conta Contábil/Unidade no Linx são criados/atualizados como
/// estão — nunca inventados nem descartados (decisão do Product Owner, pré-validação real: 144+2+2 casos
/// reais).
///
/// Last Write Wins (Bloco 5A.7, decisão do Product Owner): quando um Item Fiscal já existe localmente e seu
/// conteúdo diverge do Linx, a resolução segue o algoritmo abaixo — nunca uma escolha manual. Compara
/// `CADASTRO_ITEM_FISCAL.DATA_PARA_TRANSFERENCIA` (<c>externo.UltimaAlteracaoEm</c>) com o timestamp de
/// negócio LOCAL relevante (<c>ItemFiscal.UltimaAlteracaoLocalEm</c> — nunca o horário desta própria
/// sincronização):
///
/// A) registro só existe no Linx → incluído (sem comparação, caminho já existente).
/// B) existe nos dois, conteúdo diverge, Linx mais novo → Linx prevalece, +Compras atualizado.
/// C) existe nos dois, conteúdo diverge, +Compras mais novo → +Compras preservado, nada aplicado.
/// D) conteúdo equivalente (independentemente dos timestamps) → sem alteração.
/// E) conteúdo diverge, timestamps equivalentes → ADR-0024 (Linx prevalece), registrado como ocorrência
///    distinta de uma comparação real (empate, não "mais novo").
/// F) conteúdo diverge, timestamp local e/ou do Linx indisponível para comparação → nenhum timestamp é
///    inventado; aplica-se a regra de autoridade homologada (ADR-0024, Linx prevalece), registrado como
///    ocorrência distinta de B/C/E.</summary>
public sealed class SincronizarItensFiscaisErpUseCase(
    IItemFiscalErpReader reader,
    IItemFiscalRepository repository,
    ICurrentIdentity identity,
    ILogger<SincronizarItensFiscaisErpUseCase> logger) : ISincronizarItensFiscaisErpUseCase
{
    private const int TamanhoPaginaPadrao = 500;

    public async Task<SincronizacaoItensFiscaisErpResumo> ExecuteAsync(SincronizarItensFiscaisErpDto dto, CancellationToken cancellationToken = default)
    {
        var identidadeAtual = identity.GetRequired();
        if (identidadeAtual.UnidadeNegocioId is null || identidadeAtual.UnidadeNegocioId == Guid.Empty)
        {
            throw new InvalidOperationException("A sessão atual não possui Unidade de Negócio resolvida; sincronização de Item Fiscal ERP não pode ser iniciada.");
        }
        var unidadeNegocioId = identidadeAtual.UnidadeNegocioId.Value;

        var correlationId = string.IsNullOrWhiteSpace(dto.CorrelationId) ? Guid.NewGuid().ToString("N") : dto.CorrelationId.Trim()[..Math.Min(dto.CorrelationId.Trim().Length, 100)];
        var limiteExplicito = dto.Limite > 0 ? dto.Limite : (int?)null;
        var dryRun = dto.DryRun;

        var inicio = DateTimeOffset.UtcNow;
        int consultados = 0, incluidos = 0, semAlteracao = 0, atualizadosLinxMaisNovo = 0, preservadosLocalMaisNovo = 0,
            atualizadosEmpate = 0, atualizadosTimestampIndisponivel = 0, erros = 0;
        var ocorrencias = new List<ItemFiscalErpOcorrenciaLww>();
        var skip = 0;
        var possivelmenteTruncado = false;

        logger.LogInformation(
            "Sincronizacao de itens fiscais ERP iniciada. LimiteExplicito {LimiteExplicito}. DryRun {DryRun}. CorrelationId {CorrelationId}",
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

            var lote = await reader.BuscarItensFiscaisAsync(skip, tamanhoPagina, cancellationToken);
            if (lote.Count == 0) break;

            foreach (var externo in lote)
            {
                consultados++;
                try
                {
                    var resultado = await ProcessarAsync(externo, unidadeNegocioId, dryRun, cancellationToken);
                    switch (resultado.Decisao)
                    {
                        case ResultadoProcessamento.Incluido:
                            incluidos++;
                            break;
                        case ResultadoProcessamento.SemAlteracao:
                            semAlteracao++;
                            break;
                        case ResultadoProcessamento.AtualizadoLinxMaisNovo:
                            atualizadosLinxMaisNovo++;
                            ocorrencias.Add(resultado.Ocorrencia!);
                            break;
                        case ResultadoProcessamento.PreservadoLocalMaisNovo:
                            preservadosLocalMaisNovo++;
                            ocorrencias.Add(resultado.Ocorrencia!);
                            break;
                        case ResultadoProcessamento.AtualizadoEmpateAdr0024:
                            atualizadosEmpate++;
                            ocorrencias.Add(resultado.Ocorrencia!);
                            break;
                        case ResultadoProcessamento.AtualizadoTimestampIndisponivelAdr0024:
                            atualizadosTimestampIndisponivel++;
                            ocorrencias.Add(resultado.Ocorrencia!);
                            break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    erros++;
                    logger.LogError(ex, "Erro parcial na sincronizacao de item fiscal ERP. CodigoItem {CodigoItem}", externo.CodigoItem);
                }
            }

            logger.LogInformation(
                "Lote de itens fiscais ERP processado. Skip {Skip}. ProcessadosNoLote {ProcessadosNoLote}. Consultados {Consultados}. Erros {Erros}",
                skip, lote.Count, consultados, erros);
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
            "Sincronizacao de itens fiscais ERP finalizada. Status {Status}. Consultados {Consultados}. Incluidos {Incluidos}. SemAlteracao {SemAlteracao}. " +
            "AtualizadosLinxMaisNovo {AtualizadosLinxMaisNovo}. PreservadosLocalMaisNovo {PreservadosLocalMaisNovo}. AtualizadosEmpateAdr0024 {AtualizadosEmpateAdr0024}. " +
            "AtualizadosTimestampIndisponivelAdr0024 {AtualizadosTimestampIndisponivelAdr0024}. Erros {Erros}.",
            status, consultados, incluidos, semAlteracao, atualizadosLinxMaisNovo, preservadosLocalMaisNovo, atualizadosEmpate, atualizadosTimestampIndisponivel, erros);

        return new SincronizacaoItensFiscaisErpResumo(
            status, inicio, fim, consultados, incluidos, semAlteracao, atualizadosLinxMaisNovo, preservadosLocalMaisNovo,
            atualizadosEmpate, atualizadosTimestampIndisponivel, erros,
            (long)(fim - inicio).TotalMilliseconds, correlationId, possivelmenteTruncado, ocorrencias);
    }

    private enum ResultadoProcessamento
    {
        Incluido,
        SemAlteracao,
        AtualizadoLinxMaisNovo,
        PreservadoLocalMaisNovo,
        AtualizadoEmpateAdr0024,
        AtualizadoTimestampIndisponivelAdr0024,
    }

    private readonly record struct Resultado(ResultadoProcessamento Decisao, ItemFiscalErpOcorrenciaLww? Ocorrencia);

    private async Task<Resultado> ProcessarAsync(ItemFiscalErpDto externo, Guid unidadeNegocioId, bool dryRun, CancellationToken ct)
    {
        var local = await repository.ObterPorCodigoSemRastreamentoAsync(externo.CodigoItem, ct);
        if (local is null)
        {
            if (!dryRun)
            {
                var agora = DateTimeOffset.UtcNow;
                var novo = ItemFiscal.CriarDeErp(
                    externo.CodigoItem, externo.Descricao, externo.UnidadeErp, externo.ContaContabilErp,
                    ativo: !externo.Inativo, unidadeNegocioId, externo.UltimaAlteracaoEm, agora);
                await repository.AdicionarAsync(novo, ct);
                await repository.SalvarAlteracoesAsync(ct);
            }
            return new Resultado(ResultadoProcessamento.Incluido, null);
        }

        var camposDivergentes = ObterCamposDivergentes(local, externo);
        if (camposDivergentes.Count == 0)
        {
            return new Resultado(ResultadoProcessamento.SemAlteracao, null);
        }

        var timestampLocal = local.UltimaAlteracaoLocalEm;
        var timestampLinx = externo.UltimaAlteracaoEm;
        var decisao = DecidirLww(timestampLocal, timestampLinx);

        if (decisao == ResultadoProcessamento.PreservadoLocalMaisNovo)
        {
            return new Resultado(decisao, new ItemFiscalErpOcorrenciaLww(externo.CodigoItem, timestampLinx, timestampLocal, camposDivergentes, ItemFiscalErpDecisaoLww.PreservadoLocalMaisNovo));
        }

        if (!dryRun)
        {
            var atualizavel = await repository.ObterPorCodigoAsync(externo.CodigoItem, ct)
                ?? throw new InvalidOperationException($"Item Fiscal '{externo.CodigoItem}' encontrado na classificação, mas não na leitura rastreada — estado inconsistente.");
            atualizavel.AtualizarDeErp(externo.Descricao, externo.UnidadeErp, externo.ContaContabilErp, ativo: !externo.Inativo, externo.UltimaAlteracaoEm, DateTimeOffset.UtcNow);
            await repository.SalvarAlteracoesAsync(ct);
        }

        var decisaoLww = decisao switch
        {
            ResultadoProcessamento.AtualizadoLinxMaisNovo => ItemFiscalErpDecisaoLww.AtualizadoLinxMaisNovo,
            ResultadoProcessamento.AtualizadoEmpateAdr0024 => ItemFiscalErpDecisaoLww.AtualizadoEmpateAdr0024,
            ResultadoProcessamento.AtualizadoTimestampIndisponivelAdr0024 => ItemFiscalErpDecisaoLww.AtualizadoTimestampIndisponivelAdr0024,
            _ => throw new InvalidOperationException($"Decisão de LWW inesperada: {decisao}."),
        };
        return new Resultado(decisao, new ItemFiscalErpOcorrenciaLww(externo.CodigoItem, timestampLinx, timestampLocal, camposDivergentes, decisaoLww));
    }

    /// <summary>Casos B/C/E/F do algoritmo de LWW homologado (Bloco 5A.7) — só chamado quando já se sabe que
    /// o conteúdo diverge (caso D já foi descartado antes). Nunca inventa timestamp: ausência em qualquer um
    /// dos lados cai no caso F (autoridade homologada ADR-0024), nunca em comparação.</summary>
    private static ResultadoProcessamento DecidirLww(DateTimeOffset? timestampLocal, DateTimeOffset? timestampLinx)
    {
        if (timestampLocal is null || timestampLinx is null)
        {
            return ResultadoProcessamento.AtualizadoTimestampIndisponivelAdr0024;
        }

        var comparacao = timestampLinx.Value.ToUniversalTime().CompareTo(timestampLocal.Value.ToUniversalTime());
        if (comparacao > 0) return ResultadoProcessamento.AtualizadoLinxMaisNovo;
        if (comparacao < 0) return ResultadoProcessamento.PreservadoLocalMaisNovo;
        return ResultadoProcessamento.AtualizadoEmpateAdr0024;
    }

    private static IReadOnlyList<string> ObterCamposDivergentes(ItemFiscal local, ItemFiscalErpDto externo)
    {
        var campos = new List<string>();
        if (local.Descricao != externo.Descricao.Trim()) campos.Add(nameof(ItemFiscal.Descricao));
        if (!string.Equals(local.ContaContabilCodigoErp, NormalizarErp(externo.ContaContabilErp), StringComparison.Ordinal)) campos.Add(nameof(ItemFiscal.ContaContabilCodigoErp));
        if (!string.Equals(local.UnidadeMedidaCodigoErp, NormalizarErp(externo.UnidadeErp), StringComparison.Ordinal)) campos.Add(nameof(ItemFiscal.UnidadeMedidaCodigoErp));
        if (local.Ativo != !externo.Inativo) campos.Add(nameof(ItemFiscal.Ativo));
        return campos;
    }

    private static string? NormalizarErp(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
