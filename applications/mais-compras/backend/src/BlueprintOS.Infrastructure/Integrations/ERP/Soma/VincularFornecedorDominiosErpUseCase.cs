using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Integrations.Contracts;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Domain.Integrations.Occurrences;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed record VinculacaoFornecedorDominiosErpResultado(
    bool DryRun, bool Aplicado, int FornecedoresAnalisados, int FornecedoresAtualizados, int ValoresNaoResolvidos, int OcorrenciasPersistidas);

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): conecta a infraestrutura já existente — mas nunca
/// chamada — de <c>FornecedorDominioErp</c>/<c>Fornecedor.VincularDominios</c> ao pipeline real. Para cada
/// Fornecedor com pelo menos um dos 3 valores livres (Tipo/Subtipo/CondiçãoPagamento) já sincronizados do
/// Linx, resolve contra o catálogo <c>FornecedorDominioErp</c> (populado por
/// <see cref="SincronizarFornecedorDominiosErpUseCase"/>) e vincula pelo Guid correspondente — nunca
/// inventa um vínculo quando o valor livre não resolve (fica registrado como
/// <see cref="IntegrationOccurrenceSeverity.Warning"/>) e nunca regride um vínculo já resolvido em execução
/// anterior. Processamento em lotes (nunca 1 SaveChanges por Fornecedor).
/// </summary>
public sealed class VincularFornecedorDominiosErpUseCase(
    BlueprintOSDbContext context,
    IIntegrationOccurrenceRepository occurrenceRepository,
    ILogger<VincularFornecedorDominiosErpUseCase> logger)
{
    private const int TamanhoDoLote = 2000;

    /// <summary>
    /// <paramref name="catalogo"/> é sempre fornecido pelo chamador (tipicamente
    /// <see cref="SincronizarFornecedorDominiosErpUseCase.ExecutarAsync"/>'s
    /// <c>CatalogoResultante</c>) — nunca reconsultado aqui, para que uma prévia (dry-run) consiga simular a
    /// resolução mesmo quando a sincronização ainda não gravou nada real no banco.
    /// </summary>
    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026): <paramref name="unidadeNegocioId"/> é a Business
    /// Unit explícita da execução — obrigatória, nunca inferida. Só Fornecedores dessa BU são analisados.</summary>
    public async Task<VinculacaoFornecedorDominiosErpResultado> ExecutarAsync(
        Guid execucaoRawId, Guid unidadeNegocioId, IReadOnlyDictionary<(string TipoDominio, string CodigoErp), Guid> catalogo, bool dryRun, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        if (unidadeNegocioId == Guid.Empty)
            throw new ArgumentException("Business Unit é obrigatória e não pode ser Guid.Empty — pipeline headless nunca lê/escreve domínio sem uma Unidade de Negócio explícita e válida (fail closed).", nameof(unidadeNegocioId));

        var agora = timeProvider.GetUtcNow();

        var totalAnalisados = 0;
        var totalAtualizados = 0;
        var todasOcorrencias = new List<IntegrationOccurrence>();

        // Achado real: sincronização (Stage=Refined) e vinculação (Stage=Domain) reaproveitam o MESMO
        // execucaoRawId, e ListarPorExecucaoAsync retorna ocorrências de QUALQUER estágio dessa execução —
        // sem filtrar por Stage aqui, a checagem de idempotência da vinculação encontrava a ocorrência da
        // sincronização (já persistida antes, na mesma chamada) e concluía, incorretamente, que a própria
        // já tinha rodado, pulando sua própria persistência. O índice de deduplicação real já diferencia por
        // Stage — esta checagem em memória precisa fazer o mesmo.
        var jaPersistidas = (await occurrenceRepository.ListarPorExecucaoAsync(execucaoRawId, cancellationToken))
            .Where(o => o.Stage == IntegrationStage.Domain).ToList();
        var podePersistirOcorrencias = jaPersistidas.Count == 0;

        var skip = 0;
        while (true)
        {
            var lote = await context.Fornecedores
                .Where(f => f.UnidadeNegocioId == unidadeNegocioId && (f.TipoFornecedor != null || f.SubtipoFornecedor != null || f.CondicaoPagamento != null))
                .OrderBy(f => f.Id)
                .Skip(skip)
                .Take(TamanhoDoLote)
                .ToListAsync(cancellationToken);
            if (lote.Count == 0) break;

            foreach (var fornecedor in lote)
            {
                totalAnalisados++;
                var resultado = FornecedorVinculoDominioResolver.Resolver(
                    fornecedor.TipoFornecedor, fornecedor.SubtipoFornecedor, fornecedor.CondicaoPagamento,
                    fornecedor.TipoFornecedorDominioId, fornecedor.SubtipoFornecedorDominioId, fornecedor.CondicaoPagamentoDominioId,
                    catalogo);

                if (resultado.Mudou(fornecedor.TipoFornecedorDominioId, fornecedor.SubtipoFornecedorDominioId, fornecedor.CondicaoPagamentoDominioId))
                {
                    totalAtualizados++;
                    if (!dryRun)
                    {
                        fornecedor.VincularDominios(resultado.CondicaoId, resultado.TipoId, resultado.SubtipoId, agora);
                    }
                }

                foreach (var naoResolvido in resultado.NaoResolvidos)
                {
                    todasOcorrencias.Add(IntegrationOccurrence.Registrar(
                        execucaoRawId, unidadeNegocioId, LinxReadDatasetCatalog.FornecedorDominiosSnapshot, IntegrationStage.Domain, IntegrationOccurrenceSeverity.Warning,
                        "FORNECEDOR_DOMINIO_NAO_RESOLVIDO",
                        $"Valor '{naoResolvido}' presente no Fornecedor mas sem correspondência exata no catálogo FornecedorDominioErp — vínculo não alterado, nunca inventado.",
                        fornecedor.Cnpj_Cpf, agora));
                }
            }

            if (!dryRun) await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            skip += lote.Count;
        }

        var ocorrenciasPersistidas = 0;
        if (todasOcorrencias.Count > 0 && podePersistirOcorrencias && !dryRun)
        {
            await occurrenceRepository.AdicionarLoteAsync(todasOcorrencias, cancellationToken);
            ocorrenciasPersistidas = todasOcorrencias.Count;
        }
        else if (!podePersistirOcorrencias)
        {
            ocorrenciasPersistidas = jaPersistidas.Count;
            logger.LogInformation("Ocorrências de vinculação de domínios já persistidas para a execução {ExecucaoId} — reprocessamento idempotente.", execucaoRawId);
        }

        logger.LogInformation("Vinculação de domínios ERP de Fornecedor: {Analisados} analisados, {Atualizados} atualizados, {NaoResolvidos} valores não resolvidos.",
            totalAnalisados, totalAtualizados, todasOcorrencias.Count);

        return new(dryRun, Aplicado: !dryRun, totalAnalisados, totalAtualizados, todasOcorrencias.Count, ocorrenciasPersistidas);
    }
}
