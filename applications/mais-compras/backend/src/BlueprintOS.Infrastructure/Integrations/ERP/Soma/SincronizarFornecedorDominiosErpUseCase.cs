using BlueprintOS.Application.Integrations.Contracts;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Domain.Integrations.Occurrences;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed record SincronizacaoFornecedorDominiosErpResultado(
    Guid ExecucaoRawId, bool DryRun, bool Aplicado, int TotalRaw, int Inseridos, int Atualizados, int SemAlteracao, int Rejeitados, int OcorrenciasPersistidas)
{
    /// <summary>Catálogo (TipoDominio, CodigoErp) -> Id resultante desta sincronização — inclui os
    /// registros já existentes E os recém-decididos (mesmo em dry-run, quando nada é persistido: o Id de um
    /// Insert em dry-run é um placeholder gerado só para permitir a PRÉVIA de vinculação simular a resolução
    /// sem depender de uma escrita real). Nunca use este dicionário para nada além de prever/vincular na
    /// mesma execução lógica — depois de um apply real, releia do banco.</summary>
    public IReadOnlyDictionary<(string TipoDominio, string CodigoErp), Guid> CatalogoResultante { get; init; } = new Dictionary<(string, string), Guid>();
}

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): RAW→REFINED→DOMÍNIO para os 3 catálogos que alimentam
/// <c>FornecedorDominioErp</c> (FORNECEDOR_TIPOS, FORNECEDOR_SUBTIPO, COND_ENT_PGTOS). Estratégia FULL
/// apenas (decisão do PO) — sem máquina de baseline/incremental, mesmo padrão de Unidade de Medida.
/// <see cref="BusinessUnitPadrao"/> = "DEFAULT" reaproveita o mesmo sentinela já usado em
/// <c>FornecedorLinxVinculoUseCases.cs</c> (<c>fornecedor.BusinessUnit ?? "DEFAULT"</c>) para o mesmo
/// problema estrutural: estes catálogos Linx são globais, sem dimensão de Unidade de Negócio própria —
/// nunca inventamos uma BU nova, reaproveitamos a convenção já homologada no código existente.
/// </summary>
public sealed class SincronizarFornecedorDominiosErpUseCase(
    BlueprintOSDbContext context, IIntegrationOccurrenceRepository occurrenceRepository, ILogger<SincronizarFornecedorDominiosErpUseCase> logger)
{
    public const string ErpSistema = "SOMA_DESENV";
    public const string BusinessUnitPadrao = "DEFAULT";
    private const int TamanhoDoLote = 2000;

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026): <paramref name="unidadeNegocioId"/> é a Business
    /// Unit explícita da execução — obrigatória, nunca inferida. <c>FornecedorDominioErp</c> em si
    /// permanece um catálogo GLOBAL (decisão já documentada nesta classe — sem dimensão de BU própria);
    /// a Business Unit aqui identifica apenas a execução para <see cref="IntegrationOccurrence"/> (toda
    /// ocorrência precisa de uma BU, mesmo quando o dado subjacente é global).</summary>
    public async Task<SincronizacaoFornecedorDominiosErpResultado> ExecutarAsync(bool dryRun, Guid unidadeNegocioId, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        if (unidadeNegocioId == Guid.Empty)
            throw new ArgumentException("Business Unit é obrigatória e não pode ser Guid.Empty — pipeline headless nunca lê/escreve domínio sem uma Unidade de Negócio explícita e válida (fail closed).", nameof(unidadeNegocioId));

        var execucaoRaw = await context.RawLinxFornecedoresSnapshotExecucoes
            .AsNoTracking()
            .Where(e => e.Dataset == LinxReadDatasetCatalog.FornecedorDominiosSnapshot && e.Completa)
            .OrderByDescending(e => e.ConcluidoEm)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Nenhuma execução RAW completa encontrada para o dataset '{LinxReadDatasetCatalog.FornecedorDominiosSnapshot}'.");

        var agora = timeProvider.GetUtcNow();

        var rawRows = await context.RawLinxFornecedorDominiosSnapshot.AsNoTracking().ToListAsync(cancellationToken);
        var itens = rawRows.Select(r => new FornecedorDominioErpRefinedItem(r.TipoDominio, r.CodigoErp, r.Descricao ?? r.CodigoErp, r.UltimaAlteracao, r.Id)).ToList();

        var existentes = await context.FornecedoresDominiosErp.AsNoTracking()
            .Where(f => f.ErpSistema == ErpSistema && f.BusinessUnit == BusinessUnitPadrao)
            .Select(f => new { f.Id, f.Tipo, f.CodigoERP, f.Descricao })
            .ToListAsync(cancellationToken);
        var existentesPorChave = existentes.ToDictionary(
            e => (e.Tipo, e.CodigoERP),
            e => new FornecedorDominioErpExistente(e.Id, e.Descricao));

        var plano = FornecedorDominioErpRefinedProjector.Projetar(itens, existentesPorChave);
        logger.LogInformation("Sincronização de domínios ERP de Fornecedor: {Total} lidos, {Inseridos} a inserir, {Atualizados} a atualizar, {SemAlteracao} sem alteração.",
            itens.Count, plano.Inseridos, plano.Atualizados, plano.SemAlteracao);

        // Catálogo resultante (existentes + decisões) — construído sempre, mesmo em dry-run, para que a
        // vinculação de Fornecedores possa prever a resolução sem depender de uma escrita real ainda não
        // aplicada (nunca lê o banco de volta esperando encontrar o que só existe em memória nesta chamada).
        var catalogoResultante = new Dictionary<(string, string), Guid>(existentesPorChave.Select(kv => new KeyValuePair<(string, string), Guid>(kv.Key, kv.Value.Id)));
        foreach (var decisao in plano.Decisoes.Where(d => d.Action == FornecedorDominioErpRefinedAction.Insert))
        {
            catalogoResultante[(decisao.TipoDominio, decisao.CodigoErp)] = Guid.NewGuid();
        }

        var ocorrenciasPersistidas = 0;
        if (plano.Rejeicoes.Count > 0 && !dryRun)
        {
            // Mesmo execucaoRawId é reaproveitado por VincularFornecedorDominiosErpUseCase (Stage=Domain) —
            // filtrar por Stage=Refined evita que a ocorrência da vinculação seja lida como se já cobrisse
            // esta persistência (ver comentário simétrico em VincularFornecedorDominiosErpUseCase).
            var jaPersistidas = (await occurrenceRepository.ListarPorExecucaoAsync(execucaoRaw.Id, cancellationToken))
                .Where(o => o.Stage == IntegrationStage.Refined).ToList();
            if (jaPersistidas.Count == 0)
            {
                var ocorrencias = plano.Rejeicoes.Select(r => IntegrationOccurrence.Registrar(
                    execucaoRaw.Id, unidadeNegocioId, LinxReadDatasetCatalog.FornecedorDominiosSnapshot, IntegrationStage.Refined, IntegrationOccurrenceSeverity.Error,
                    r.Code, r.Mensagem, r.OriginRecordKey, agora)).ToList();
                await occurrenceRepository.AdicionarLoteAsync(ocorrencias, cancellationToken);
                ocorrenciasPersistidas = ocorrencias.Count;
            }
            else
            {
                ocorrenciasPersistidas = jaPersistidas.Count;
                logger.LogInformation("Ocorrências já persistidas para a execução {ExecucaoId} — reprocessamento idempotente.", execucaoRaw.Id);
            }
        }

        if (dryRun)
        {
            return new(execucaoRaw.Id, dryRun, Aplicado: false, itens.Count, plano.Inseridos, plano.Atualizados, plano.SemAlteracao, plano.Rejeicoes.Count, OcorrenciasPersistidas: 0) { CatalogoResultante = catalogoResultante };
        }

        var idsReaisPorChave = new Dictionary<(string, string), Guid>();
        foreach (var lote in plano.Decisoes.Where(d => d.Action != FornecedorDominioErpRefinedAction.NoChange).Chunk(TamanhoDoLote))
        {
            var idsParaAtualizar = lote.Where(d => d.Action == FornecedorDominioErpRefinedAction.Update).Select(d => d.ExistenteId!.Value).ToList();
            var tracked = idsParaAtualizar.Count == 0
                ? []
                : await context.FornecedoresDominiosErp.Where(f => idsParaAtualizar.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken);

            foreach (var decisao in lote)
            {
                if (decisao.Action == FornecedorDominioErpRefinedAction.Update)
                {
                    tracked[decisao.ExistenteId!.Value].Atualizar(decisao.Descricao, "Ativo", agora);
                }
                else
                {
                    var novoId = Guid.NewGuid();
                    context.FornecedoresDominiosErp.Add(new FornecedorDominioErp(
                        novoId, decisao.TipoDominio, decisao.CodigoErp, decisao.Descricao, BusinessUnitPadrao, ErpSistema, "Ativo", agora));
                    idsReaisPorChave[(decisao.TipoDominio, decisao.CodigoErp)] = novoId;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }

        foreach (var (chave, id) in idsReaisPorChave) catalogoResultante[chave] = id;

        return new(execucaoRaw.Id, dryRun, Aplicado: true, itens.Count, plano.Inseridos, plano.Atualizados, plano.SemAlteracao, plano.Rejeicoes.Count, ocorrenciasPersistidas) { CatalogoResultante = catalogoResultante };
    }
}
