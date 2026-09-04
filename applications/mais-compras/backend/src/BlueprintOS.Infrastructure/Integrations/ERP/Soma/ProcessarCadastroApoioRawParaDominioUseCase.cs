using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Integrations.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Domain.Integrations.Occurrences;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>Reconciliação pós-escrita (item de gate do PO: contagem igual não prova igualdade) — para um
/// cadastro de apoio simples, a única invariante possível é: todo código RAW que o Linx sinaliza inativo E
/// que já tinha metadado local deve, após a aplicação, estar inativo no domínio. <see cref="Aprovada"/>
/// exige zero divergências dessa invariante.</summary>
public sealed record ReconciliacaoCadastroApoioResultado(int VerificadosParaInativacao, int DivergenciasPosAplicacao)
{
    public bool Aprovada => DivergenciasPosAplicacao == 0;
}

/// <summary><see cref="SemMetadadoLocal"/> é só uma estatística informativa da execução (quantos códigos do
/// Linx ainda não têm metadado local) — PO (revisão B3/Bloco 5A pós-certificação): isso é comportamento BY
/// DESIGN, nunca vira <c>IntegrationOccurrence</c>. Só <see cref="OcorrenciasPersistidas"/> reflete exceções
/// reais (hoje: código Linx ambíguo).</summary>
public sealed record ProcessamentoCadastroApoioResultado(
    Guid ExecucaoRawId,
    bool DryRun,
    bool Aplicado,
    int TotalRaw,
    int Inativados,
    int SemMetadadoLocal,
    int OcorrenciasPersistidas,
    ReconciliacaoCadastroApoioResultado? Reconciliacao,
    bool BaselineHomologada,
    bool CargaFullInicialValidada,
    bool IncrementalLiberado,
    DateTimeOffset? WatermarkInicial,
    bool WatermarkAvancado = false);

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): orquestrador GENÉRICO de RAW→REFINED→DOMÍNIO→
/// RECONCILIAÇÃO→BASELINE compartilhado pelos cadastros de apoio estruturalmente idênticos (Conta Contábil,
/// Unidade de Medida, Centro de Custo, Filial) — mesmo padrão arquitetural já homologado para Fornecedor
/// (<c>ProcessarFornecedoresRawParaDominioUseCase</c>), mas sem LWW/Principal/vínculos: aqui a única decisão
/// possível é inativar um metadado local quando o Linx sinaliza inatividade (nunca criar, nunca reativar).
/// PO (revisão B3/Bloco 5A pós-certificação): um código RAW sem metadado local NUNCA gera
/// <c>IntegrationOccurrence</c> — é estado normal/lazy, não uma exceção (ver <see cref="CadastroApoioRefinedProjector"/>).
/// Reutiliza <see cref="RawLinxFornecedorSnapshotExecucao"/> como cabeçalho de execução e
/// <see cref="LinxDatasetLoadState"/> como estado de bootstrap/baseline — seus campos <c>Dataset</c> já
/// discriminam por dataset desde o Gate A; nada ali é específico de Fornecedor apesar do nome da classe.
/// <paramref name="suportaIncremental"/> só é <c>true</c> para os datasets cuja EstrategiaNormal é
/// Incremental (Conta Contábil, Centro de Custo) — decisão do PO: a máquina de baseline
/// (CargaFullInicialValidada/IncrementalLiberado) só existe para gatear incremental; um dataset
/// exclusivamente Full (Unidade de Medida) não precisa dela.
/// </summary>
public sealed class ProcessarCadastroApoioRawParaDominioUseCase<TRaw, TMetadado>(
    BlueprintOSDbContext context,
    IIntegrationOccurrenceRepository occurrenceRepository,
    ILogger<ProcessarCadastroApoioRawParaDominioUseCase<TRaw, TMetadado>> logger,
    string dataset,
    IntegrationStage ocorrenciaStage,
    bool suportaIncremental,
    Func<TRaw, CadastroApoioRefinedItem> mapear)
    where TRaw : class
    where TMetadado : class, ICadastroApoioMetadado
{
    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026): <paramref name="unidadeNegocioId"/> é a Business
    /// Unit explícita da execução — obrigatória, nunca inferida. Metadados existentes lidos aqui são
    /// sempre escopados por ela — sem isso, dois metadados de BUs diferentes com o mesmo CodigoErp
    /// colidiriam no dicionário de projeção (`existentesPorCodigo` é indexado por CodigoErp).</summary>
    public async Task<ProcessamentoCadastroApoioResultado> ExecutarAsync(bool dryRun, Guid unidadeNegocioId, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        if (unidadeNegocioId == Guid.Empty)
            throw new ArgumentException("Business Unit é obrigatória e não pode ser Guid.Empty — pipeline headless nunca lê/escreve domínio sem uma Unidade de Negócio explícita e válida (fail closed).", nameof(unidadeNegocioId));

        var execucaoRaw = await context.RawLinxFornecedoresSnapshotExecucoes
            .AsNoTracking()
            .Where(e => e.Dataset == dataset && e.Completa)
            .OrderByDescending(e => e.ConcluidoEm)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Nenhuma execução RAW completa encontrada para o dataset '{dataset}'. REFINED nunca processa uma carga incompleta.");

        var agora = timeProvider.GetUtcNow();

        var rawRows = await context.Set<TRaw>().AsNoTracking().ToListAsync(cancellationToken);
        var itens = rawRows.Select(mapear).ToList();

        var existentes = await context.Set<TMetadado>().AsNoTracking()
            .Where(m => m.UnidadeNegocioId == unidadeNegocioId)
            .Select(m => new CadastroApoioExistenteProjecao(m.Id, m.CodigoErp, m.AtivoNoMaisCompras))
            .ToListAsync(cancellationToken);
        var existentesPorCodigo = existentes.ToDictionary(
            e => e.CodigoErp,
            e => new CadastroApoioExistente(e.Id, e.AtivoNoMaisCompras));

        var plano = CadastroApoioRefinedProjector.Projetar(itens, existentesPorCodigo);

        // PO (revisão B3/Bloco 5A pós-certificação): só plano.Ocorrencias (exceções reais, ex. código Linx
        // ambíguo) vira IntegrationOccurrence. plano.CodigosSemMetadadoLocal nunca é persistido aqui — ver
        // ProcessamentoCadastroApoioResultado.SemMetadadoLocal para a contagem informativa.
        var ocorrencias = plano.Ocorrencias
            .Select(o => IntegrationOccurrence.Registrar(
                execucaoRaw.Id, unidadeNegocioId, dataset, ocorrenciaStage, IntegrationOccurrenceSeverity.Warning,
                o.Code, o.Mensagem, o.CodigoErp, agora))
            .ToList();

        var ocorrenciasPersistidas = 0;
        if (ocorrencias.Count > 0)
        {
            var jaPersistidas = await occurrenceRepository.ListarPorExecucaoAsync(execucaoRaw.Id, cancellationToken);
            if (jaPersistidas.Count == 0)
            {
                await occurrenceRepository.AdicionarLoteAsync(ocorrencias, cancellationToken);
                ocorrenciasPersistidas = ocorrencias.Count;
            }
            else
            {
                logger.LogInformation("Ocorrências já persistidas para a execução {ExecucaoId} ({Quantidade}) — reprocessamento idempotente, nenhuma nova inserida.", execucaoRaw.Id, jaPersistidas.Count);
                ocorrenciasPersistidas = jaPersistidas.Count;
            }
        }

        if (dryRun)
        {
            return new(execucaoRaw.Id, dryRun, Aplicado: false, itens.Count, Inativados: plano.Decisoes.Count, plano.CodigosSemMetadadoLocal.Count, ocorrenciasPersistidas,
                Reconciliacao: null, BaselineHomologada: false, CargaFullInicialValidada: false, IncrementalLiberado: false, WatermarkInicial: null);
        }

        if (plano.Decisoes.Count > 0)
        {
            var idsParaInativar = plano.Decisoes.Select(d => d.MetadadoId).ToList();
            var tracked = await context.Set<TMetadado>().Where(m => idsParaInativar.Contains(m.Id)).ToListAsync(cancellationToken);
            foreach (var metadado in tracked)
            {
                metadado.Inativar(agora);
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        var reconciliacao = await ReconciliarAsync(itens, unidadeNegocioId, cancellationToken);

        var execucaoTracked = await context.RawLinxFornecedoresSnapshotExecucoes.SingleAsync(e => e.Id == execucaoRaw.Id, cancellationToken);
        execucaoTracked.RegistrarReconciliacao(reconciliacao.Aprovada ? RawReconciliacaoStatus.Aprovada : RawReconciliacaoStatus.Reprovada, agora);

        var baselineHomologada = false;
        var watermarkAvancado = false;
        DateTimeOffset? watermarkInicial = null;
        var acao = suportaIncremental ? DecisaoPosReconciliacao.Decidir(execucaoTracked.Modo, execucaoTracked.ReconciliacaoStatus) : ProximaAcaoBaseline.Nenhuma;
        switch (acao)
        {
            case ProximaAcaoBaseline.HomologarBaseline:
            {
                var estado = await context.LinxDatasetLoadStates.SingleOrDefaultAsync(s => s.UnidadeNegocioId == unidadeNegocioId && s.Dataset == dataset, cancellationToken);
                if (estado is null)
                {
                    estado = LinxDatasetLoadState.Novo(unidadeNegocioId, dataset);
                    context.LinxDatasetLoadStates.Add(estado);
                }

                // Mesma regra definitiva de watermark já corrigida para Fornecedor: instante de INÍCIO da
                // execução que estabelece a baseline, nunca o de conclusão.
                watermarkInicial = execucaoTracked.IniciadoEm;
                estado.HomologarBaseline(execucaoTracked, agora, watermarkInicial);
                baselineHomologada = true;
                break;
            }
            case ProximaAcaoBaseline.AvancarWatermark:
            {
                var estado = await context.LinxDatasetLoadStates.SingleAsync(s => s.UnidadeNegocioId == unidadeNegocioId && s.Dataset == dataset, cancellationToken);
                estado.AvancarWatermark(execucaoTracked);
                watermarkInicial = estado.UltimoWatermarkValido;
                watermarkAvancado = true;
                break;
            }
            case ProximaAcaoBaseline.Nenhuma when !suportaIncremental:
                logger.LogInformation("Dataset {Dataset} é exclusivamente FULL — sem máquina de baseline/incremental a homologar.", dataset);
                break;
            case ProximaAcaoBaseline.Nenhuma:
                logger.LogWarning("Nenhuma ação de baseline/watermark para dataset {Dataset}: Modo {Modo}, Reconciliação {Status}.", dataset, execucaoTracked.Modo, execucaoTracked.ReconciliacaoStatus);
                break;
        }

        await context.SaveChangesAsync(cancellationToken);

        var estadoFinal = suportaIncremental
            ? await context.LinxDatasetLoadStates.AsNoTracking().SingleOrDefaultAsync(s => s.UnidadeNegocioId == unidadeNegocioId && s.Dataset == dataset, cancellationToken)
            : null;

        return new(
            execucaoRaw.Id, dryRun, Aplicado: true, itens.Count, plano.Decisoes.Count, plano.CodigosSemMetadadoLocal.Count, ocorrenciasPersistidas,
            reconciliacao, baselineHomologada, estadoFinal?.CargaFullInicialValidada ?? false, estadoFinal?.IncrementalLiberado ?? false, watermarkInicial, watermarkAvancado);
    }

    private async Task<ReconciliacaoCadastroApoioResultado> ReconciliarAsync(List<CadastroApoioRefinedItem> itens, Guid unidadeNegocioId, CancellationToken cancellationToken)
    {
        var codigosParaVerificar = itens.Where(i => i.InativoErp == true).Select(i => i.CodigoErp.Trim()).ToList();
        if (codigosParaVerificar.Count == 0) return new(0, 0);

        var estadoAtual = await context.Set<TMetadado>().AsNoTracking()
            .Where(m => m.UnidadeNegocioId == unidadeNegocioId && codigosParaVerificar.Contains(m.CodigoErp))
            .Select(m => new { m.CodigoErp, m.AtivoNoMaisCompras })
            .ToListAsync(cancellationToken);

        var divergencias = estadoAtual.Count(m => m.AtivoNoMaisCompras);
        return new(codigosParaVerificar.Count, divergencias);
    }

    private sealed record CadastroApoioExistenteProjecao(Guid Id, string CodigoErp, bool AtivoNoMaisCompras);
}
