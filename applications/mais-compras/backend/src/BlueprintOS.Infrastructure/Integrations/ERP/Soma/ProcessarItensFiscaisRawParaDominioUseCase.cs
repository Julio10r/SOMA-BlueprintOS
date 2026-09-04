using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Integrations.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Domain.Integrations.Occurrences;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed record ReconciliacaoItensFiscaisResultado(int CodigosValidosRaw, int ItensCorrespondentesNoDominio, IReadOnlyList<string> Divergencias)
{
    public bool Aprovada => Divergencias.Count == 0 && CodigosValidosRaw == ItensCorrespondentesNoDominio;
}

public sealed record ProcessamentoItensFiscaisResultado(
    Guid ExecucaoRawId, bool DryRun, bool Aplicado, int TotalRaw, int NovosCriados, int AtualizadosDeErp, int PreservadosLocal, int SemAlteracao, int Rejeitados,
    ReconciliacaoItensFiscaisResultado? Reconciliacao, int OcorrenciasPersistidas,
    bool BaselineHomologada, bool CargaFullInicialValidada, bool IncrementalLiberado, DateTimeOffset? WatermarkInicial, bool WatermarkAvancado);

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): RAW→REFINED→DOMÍNIO→RECONCILIAÇÃO→BASELINE para Itens
/// Fiscais, mesmo padrão arquitetural já homologado para Fornecedor — mas reproduzindo o algoritmo LWW já
/// homologado em <c>SincronizarItensFiscaisErpUseCase</c> (ver <see cref="ItemFiscalRefinedProjector"/>) em
/// vez do modelo Principal/vínculos, que não se aplica aqui. Batches de <see cref="TamanhoDoLote"/> (nunca 1
/// SaveChanges por item).
///
/// GAP ARQUITETURAL RESOLVIDO (Onda 2 — rodada Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner
/// registrada em <c>applications/mais-compras/docs/cadernos/Onda-2.md</c>): <see cref="ItemFiscal.CriarDeErp"/>
/// exige um <c>UnidadeNegocioId</c> (conceito multi-tenant real do +Compras). Este pipeline é um processo CLI
/// headless, sem sessão de usuário — por isso ele NUNCA infere a Unidade de Negócio a partir de
/// <c>ICurrentIdentity</c> (isso seria dado inventado). Em vez disso, <paramref name="unidadeNegocioId"/> é
/// SEMPRE recebida explicitamente de quem dispara a execução (evidência da execução, não invenção) — o
/// chamador (<c>ItensFiscaisRefinedCliHandler</c>) resolve o argumento <c>--business-unit</c> contra o
/// cadastro real de <c>UnidadeNegocio</c> antes de chamar este método; uma Business Unit inválida/ausente
/// falha fechado ANTES deste método ser invocado. Um código Linx NOVO (sem Item Fiscal local correspondente)
/// agora É criado por este use case, com <see cref="ItemFiscal.UnidadeNegocioId"/> = <paramref name="unidadeNegocioId"/>
/// da execução — nunca um Guid diferente, nunca inferido. Update/PreservarLocal/SemAlteracao continuam
/// funcionando normalmente para códigos que JÁ têm Item Fiscal local (o UnidadeNegocioId já existe, nunca é
/// alterado por este pipeline).
/// </summary>
public sealed class ProcessarItensFiscaisRawParaDominioUseCase(
    BlueprintOSDbContext context,
    IIntegrationOccurrenceRepository occurrenceRepository,
    ILogger<ProcessarItensFiscaisRawParaDominioUseCase> logger)
{
    private const int TamanhoDoLote = 2000;
    private const string Dataset = LinxReadDatasetCatalog.ItensFiscaisSnapshot;

    /// <summary><paramref name="unidadeNegocioId"/> é a Business Unit explícita da execução — obrigatória,
    /// nunca <see cref="Guid.Empty"/>. Fail closed: este método nunca lê nem escreve domínio sem uma
    /// Business Unit válida (defesa em profundidade; o chamador headless já deve ter resolvido e validado
    /// o argumento antes de chegar aqui).</summary>
    public async Task<ProcessamentoItensFiscaisResultado> ExecutarAsync(bool dryRun, Guid unidadeNegocioId, TimeProvider timeProvider, CancellationToken cancellationToken = default)
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

        var rawRows = await context.RawLinxItensFiscaisSnapshot.AsNoTracking().ToListAsync(cancellationToken);
        var itens = rawRows.Select(r => new ItemFiscalRefinedItem(r.CodigoErp, r.Descricao, r.UnidadeErp, r.ContaContabilErp, r.InativoErp, ConverterParaDateTimeOffset(r.UltimaAlteracao), r.Id)).ToList();

        var existentes = await context.ItensFiscais.AsNoTracking()
            .Select(f => new { f.Id, f.Codigo, f.Descricao, f.UnidadeMedidaCodigoErp, f.ContaContabilCodigoErp, f.Ativo, f.UltimaAlteracaoLocalEm })
            .ToListAsync(cancellationToken);
        var existentesPorCodigo = existentes.ToDictionary(
            e => e.Codigo,
            e => new ItemFiscalExistente(e.Id, e.Descricao, e.UnidadeMedidaCodigoErp, e.ContaContabilCodigoErp, e.Ativo, e.UltimaAlteracaoLocalEm));

        var plano = ItemFiscalRefinedProjector.Projetar(itens, existentesPorCodigo);
        logger.LogInformation("REFINED Itens Fiscais: {Total} lidos, {Novos} novos-criados (UnidadeNegocioId={UnidadeNegocioId}), {AtualizarErp} atualizar-de-erp, {PreservarLocal} preservar-local, {SemAlteracao} sem-alteracao, {Rejeitados} rejeitados.",
            itens.Count, plano.Inseridos, unidadeNegocioId, plano.AtualizadosDeErp, plano.PreservadosLocal, plano.SemAlteracao, plano.Rejeicoes.Count);

        // Ocorrências: apenas rejeições reais (código vazio, Error) — a criação de Item Fiscal novo deixou
        // de ser um GAP/Warning (ver doc-comment da classe): agora é aplicada normalmente abaixo, com a
        // Business Unit explícita da execução.
        var todasOcorrencias = plano.Rejeicoes
            .Select(r => (Code: r.Code, Mensagem: r.Mensagem, Chave: r.OriginRecordKey, Severidade: IntegrationOccurrenceSeverity.Error))
            .ToList();

        var ocorrenciasPersistidas = 0;
        if (todasOcorrencias.Count > 0)
        {
            var jaPersistidas = (await occurrenceRepository.ListarPorExecucaoAsync(execucaoRaw.Id, cancellationToken))
                .Where(o => o.Stage == IntegrationStage.Refined).ToList();
            if (jaPersistidas.Count == 0)
            {
                if (!dryRun)
                {
                    var ocorrencias = todasOcorrencias.Select(o => IntegrationOccurrence.Registrar(
                        execucaoRaw.Id, unidadeNegocioId, Dataset, IntegrationStage.Refined, o.Severidade, o.Code, o.Mensagem, o.Chave, agora)).ToList();
                    await occurrenceRepository.AdicionarLoteAsync(ocorrencias, cancellationToken);
                }
                ocorrenciasPersistidas = todasOcorrencias.Count;
            }
            else
            {
                ocorrenciasPersistidas = jaPersistidas.Count;
            }
        }

        if (dryRun)
        {
            return new(execucaoRaw.Id, dryRun, Aplicado: false, itens.Count, plano.Inseridos, plano.AtualizadosDeErp, plano.PreservadosLocal, plano.SemAlteracao, plano.Rejeicoes.Count,
                Reconciliacao: null, ocorrenciasPersistidas, BaselineHomologada: false, CargaFullInicialValidada: false, IncrementalLiberado: false, WatermarkInicial: null, WatermarkAvancado: false);
        }

        // Item Fiscal novo do Linx: criado com a Business Unit explícita da execução (nunca inferida) —
        // ver doc-comment da classe. UnidadeNegocioId já validada != Guid.Empty no início deste método.
        foreach (var lote in plano.Decisoes.Where(d => d.Action == ItemFiscalRefinedAction.Insert).Chunk(TamanhoDoLote))
        {
            foreach (var decisao in lote)
            {
                var novo = ItemFiscal.CriarDeErp(decisao.CodigoErp, decisao.Descricao, decisao.UnidadeErp, decisao.ContaContabilErp, decisao.Ativo, unidadeNegocioId, decisao.UltimaAlteracaoErp, agora);
                context.ItensFiscais.Add(novo);
            }

            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }

        foreach (var lote in plano.Decisoes.Where(d => d.Action == ItemFiscalRefinedAction.AtualizarDeErp).Chunk(TamanhoDoLote))
        {
            var idsParaAtualizar = lote.Select(d => d.ExistenteId!.Value).ToList();
            var tracked = await context.ItensFiscais.Where(f => idsParaAtualizar.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken);

            foreach (var decisao in lote)
            {
                tracked[decisao.ExistenteId!.Value].AtualizarDeErp(decisao.Descricao, decisao.UnidadeErp, decisao.ContaContabilErp, decisao.Ativo, decisao.UltimaAlteracaoErp, agora);
            }

            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }

        var reconciliacao = await ReconciliarAsync(existentesPorCodigo.Keys.ToList(), cancellationToken);

        var execucaoTracked = await context.RawLinxFornecedoresSnapshotExecucoes.SingleAsync(e => e.Id == execucaoRaw.Id, cancellationToken);
        execucaoTracked.RegistrarReconciliacao(reconciliacao.Aprovada ? RawReconciliacaoStatus.Aprovada : RawReconciliacaoStatus.Reprovada, agora);

        var baselineHomologada = false;
        var watermarkAvancado = false;
        DateTimeOffset? watermarkInicial = null;
        var acao = DecisaoPosReconciliacao.Decidir(execucaoTracked.Modo, execucaoTracked.ReconciliacaoStatus);
        switch (acao)
        {
            case ProximaAcaoBaseline.HomologarBaseline:
            {
                var estado = await context.LinxDatasetLoadStates.SingleOrDefaultAsync(s => s.UnidadeNegocioId == unidadeNegocioId && s.Dataset == Dataset, cancellationToken);
                if (estado is null)
                {
                    estado = LinxDatasetLoadState.Novo(unidadeNegocioId, Dataset);
                    context.LinxDatasetLoadStates.Add(estado);
                }
                watermarkInicial = execucaoTracked.IniciadoEm;
                estado.HomologarBaseline(execucaoTracked, agora, watermarkInicial);
                baselineHomologada = true;
                break;
            }
            case ProximaAcaoBaseline.AvancarWatermark:
            {
                var estado = await context.LinxDatasetLoadStates.SingleAsync(s => s.UnidadeNegocioId == unidadeNegocioId && s.Dataset == Dataset, cancellationToken);
                estado.AvancarWatermark(execucaoTracked);
                watermarkInicial = estado.UltimoWatermarkValido;
                watermarkAvancado = true;
                break;
            }
            case ProximaAcaoBaseline.Nenhuma:
                logger.LogWarning("Nenhuma ação de baseline/watermark para dataset {Dataset}: Modo {Modo}, Reconciliação {Status}.", Dataset, execucaoTracked.Modo, execucaoTracked.ReconciliacaoStatus);
                break;
        }

        await context.SaveChangesAsync(cancellationToken);

        var estadoFinal = await context.LinxDatasetLoadStates.AsNoTracking().SingleOrDefaultAsync(s => s.UnidadeNegocioId == unidadeNegocioId && s.Dataset == Dataset, cancellationToken);

        return new(
            execucaoRaw.Id, dryRun, Aplicado: true, itens.Count, plano.Inseridos, plano.AtualizadosDeErp, plano.PreservadosLocal, plano.SemAlteracao, plano.Rejeicoes.Count,
            reconciliacao, ocorrenciasPersistidas, baselineHomologada, estadoFinal?.CargaFullInicialValidada ?? false, estadoFinal?.IncrementalLiberado ?? false, watermarkInicial, watermarkAvancado);
    }

    /// <summary>Reconciliação real (item de gate do PO: contagem igual não prova igualdade) — escopo
    /// deliberadamente restrito aos códigos que JÁ existiam localmente antes desta execução: nenhum deles
    /// pode ter desaparecido do domínio (nada aqui exclui Item Fiscal). Códigos novos do Linx NUNCA são
    /// criados por este pipeline (ver GAP arquitetural documentado na classe) — por isso não entram no
    /// universo esperado; contá-los como divergência produziria falso-negativo permanente, mascarando o
    /// verdadeiro estado (decisão pendente do PO), não uma falha de reconciliação real.</summary>
    private async Task<ReconciliacaoItensFiscaisResultado> ReconciliarAsync(IReadOnlyCollection<string> codigosPreExistentes, CancellationToken cancellationToken)
    {
        if (codigosPreExistentes.Count == 0) return new(0, 0, []);

        var existentesNoDominio = await context.ItensFiscais.AsNoTracking().Where(f => codigosPreExistentes.Contains(f.Codigo)).Select(f => f.Codigo).ToListAsync(cancellationToken);

        var divergencias = new List<string>();
        var faltantes = codigosPreExistentes.Except(existentesNoDominio).ToList();
        if (faltantes.Count > 0) divergencias.Add($"{faltantes.Count} código(s) que já existiam localmente desapareceram do domínio — divergência real.");

        return new(codigosPreExistentes.Count, existentesNoDominio.Count, divergencias);
    }

    private static DateTimeOffset? ConverterParaDateTimeOffset(DateTime? valor)
    {
        if (valor is null) return null;
        var local = DateTime.SpecifyKind(valor.Value, DateTimeKind.Unspecified);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone));
    }
}
