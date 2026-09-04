using BlueprintOS.Application.Integrations.Contracts;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Integrations.Occurrences;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed record ReconciliacaoResultado(
    int CnpjsDistintosRaw,
    int FornecedoresCorrespondentesNoDominio,
    int VinculosEsperados,
    int VinculosReaisNoDominio,
    int AtivosEsperados,
    int AtivosReaisNoDominio,
    int PrincipaisEsperados,
    int PrincipaisReaisNoDominio,
    IReadOnlyList<string> Divergencias)
{
    /// <summary>Item de gate do PO: "Não validar apenas COUNT(*)... separar diferença esperada por
    /// transformação do modelo de divergência real." Aprovada só quando NENHUMA das contagens-chave diverge
    /// e nenhuma divergência de amostra foi registrada.</summary>
    public bool Aprovada => Divergencias.Count == 0
        && CnpjsDistintosRaw == FornecedoresCorrespondentesNoDominio
        && VinculosEsperados == VinculosReaisNoDominio
        && AtivosEsperados == AtivosReaisNoDominio
        && PrincipaisEsperados == PrincipaisReaisNoDominio;
}

public sealed record ProcessamentoRawParaDominioResultado(
    Guid ExecucaoRawId,
    bool DryRun,
    bool Aplicado,
    RefinedPlanResumo Resumo,
    IReadOnlyList<RefinedOcorrencia> Conflitos,
    IReadOnlyList<RefinedOcorrencia> Erros,
    bool LimiarInativacaoExcedido,
    ReconciliacaoResultado? Reconciliacao,
    TimeSpan DuracaoRefined,
    TimeSpan DuracaoAplicacao,
    TimeSpan DuracaoReconciliacao,
    int BatchesAplicados,
    int OcorrenciasPersistidas,
    bool BaselineHomologada,
    bool CargaFullInicialValidada,
    bool IncrementalLiberado,
    DateTimeOffset? WatermarkInicial,
    bool WatermarkAvancado = false);

/// <summary>
/// B3 — Bloco 5A.9, Gate RAW→REFINED→DOMÍNIO (autorizado pelo PO em 2026-09-03): orquestra a leitura do RAW
/// mais recente e completo, delega a decisão pura a <see cref="FornecedorRefinedProjector"/>, e — quando não
/// é dry-run — aplica o plano ao domínio em lotes (nunca 1 SaveChanges por Fornecedor, decisão do PO). Uma
/// falha no meio de um lote não compromete os lotes já commitados (idempotente: reprocessar os mesmos CNPJs
/// produz NoChange na segunda vez), mas a execução inteira só é elegível a reconciliação/baseline se todos os
/// lotes completarem.
/// </summary>
public sealed class ProcessarFornecedoresRawParaDominioUseCase(
    BlueprintOSDbContext context,
    IIntegrationOccurrenceRepository occurrenceRepository,
    ILogger<ProcessarFornecedoresRawParaDominioUseCase> logger)
{
    public const decimal LimiarInativacaoAnormal = 0.30m;
    private const int TamanhoDoLote = 2000;

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): <paramref name="unidadeNegocioId"/>
    /// é a Business Unit explícita da execução — obrigatória, nunca inferida, mesmo padrão fail-closed já
    /// aplicado a Item Fiscal. Fornecedores/vínculos existentes lidos e escritos aqui são sempre escopados
    /// por ela — dois CNPJs iguais em BUs diferentes nunca colidem nem se misturam.</summary>
    public async Task<ProcessamentoRawParaDominioResultado> ExecutarAsync(string dataset, Guid unidadeNegocioId, bool dryRun, TimeProvider timeProvider, CancellationToken cancellationToken = default)
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
        var cronometroRefined = System.Diagnostics.Stopwatch.StartNew();

        var raw = await context.RawLinxFornecedoresSnapshot.AsNoTracking().ToListAsync(cancellationToken);

        var vinculosExistentes = await context.FornecedorLinxVinculos.AsNoTracking()
            .Where(v => v.UnidadeNegocioId == unidadeNegocioId)
            .Select(v => new { v.Id, v.FornecedorId, v.CodigoErp, v.NomeClifor, v.InativoFornecedores, v.InativoCadastroCliFor, v.DataParaTransferencia, v.Principal })
            .ToListAsync(cancellationToken);
        var vinculosPorFornecedorId = vinculosExistentes.GroupBy(v => v.FornecedorId).ToDictionary(g => g.Key, g => g.ToList());

        var fornecedoresExistentes = await context.Fornecedores.AsNoTracking()
            .Where(f => f.UnidadeNegocioId == unidadeNegocioId)
            .Select(f => new { f.Id, f.Cnpj_Cpf, f.Status, f.RazaoSocial, f.NomeFantasia, f.TipoPessoa })
            .ToListAsync(cancellationToken);

        var existentesPorCnpj = fornecedoresExistentes.ToDictionary(
            f => f.Cnpj_Cpf,
            f => new FornecedorExistente(
                f.Id, f.Cnpj_Cpf, f.Status, f.RazaoSocial, f.NomeFantasia, f.TipoPessoa,
                vinculosPorFornecedorId.TryGetValue(f.Id, out var vs)
                    ? vs.Select(v => new VinculoExistente(v.Id, v.CodigoErp, v.NomeClifor, v.InativoFornecedores, v.InativoCadastroCliFor, v.DataParaTransferencia, v.Principal)).ToList()
                    : []));

        var fornecedoresAtivosAntes = fornecedoresExistentes.Count(f => f.Status == "Ativo");

        var plano = FornecedorRefinedProjector.Projetar(raw, existentesPorCnpj, agora);
        var resumo = plano.Resumir(fornecedoresAtivosAntes);
        cronometroRefined.Stop();

        var limiarExcedido = resumo.PercentualInativacao > LimiarInativacaoAnormal;
        if (limiarExcedido)
        {
            logger.LogWarning("REFINED: limiar de inativação anormal excedido ({Percentual:P1} > {Limiar:P0}) — aplicação bloqueada.", resumo.PercentualInativacao, LimiarInativacaoAnormal);
        }

        // Item de gate do PO ("PERSISTÊNCIA DE OCORRÊNCIAS/ERROS DE INTEGRAÇÃO"): toda ocorrência relevante
        // (rejeição, conflito) fica persistida individualmente e vinculada à execução — nunca só em log
        // técnico/console. Persistido apenas em execução real (dry-run permanece sem qualquer escrita).
        // Idempotente por ExecutionId: reprocessar a MESMA execução RAW nunca duplica ocorrências já
        // persistidas (mesmo índice único que protege a tabela — checado aqui primeiro para não depender de
        // capturar uma exceção de violação de constraint).
        var ocorrenciasPersistidas = 0;
        if (!dryRun)
        {
            var jaPersistidas = await occurrenceRepository.ListarPorExecucaoAsync(execucaoRaw.Id, cancellationToken);
            if (jaPersistidas.Count == 0)
            {
                var ocorrencias = plano.Erros.Concat(plano.ConflitosPrincipal)
                    .Select(o => IntegrationOccurrence.Registrar(execucaoRaw.Id, unidadeNegocioId, dataset, IntegrationStage.Refined, o.Severidade, o.Code, o.Mensagem, o.OriginRecordKey, agora))
                    .ToList();
                await occurrenceRepository.AdicionarLoteAsync(ocorrencias, cancellationToken);
                ocorrenciasPersistidas = ocorrencias.Count;
            }
            else
            {
                logger.LogInformation("Ocorrências já persistidas para a execução {ExecucaoId} ({Quantidade}) — reprocessamento idempotente, nenhuma nova inserida.", execucaoRaw.Id, jaPersistidas.Count);
                ocorrenciasPersistidas = jaPersistidas.Count;
            }
        }

        if (dryRun || limiarExcedido)
        {
            return new(execucaoRaw.Id, dryRun, Aplicado: false, resumo, plano.ConflitosPrincipal, plano.Erros, limiarExcedido,
                Reconciliacao: null, cronometroRefined.Elapsed, TimeSpan.Zero, TimeSpan.Zero, BatchesAplicados: 0, ocorrenciasPersistidas,
                BaselineHomologada: false, CargaFullInicialValidada: false, IncrementalLiberado: false, WatermarkInicial: null);
        }

        var cronometroAplicacao = System.Diagnostics.Stopwatch.StartNew();
        var batches = 0;
        var alterados = plano.Fornecedores.Where(f => f.Action != RefinedAction.NoChange).ToList();
        foreach (var lote in Chunk(alterados, TamanhoDoLote))
        {
            await AplicarLoteAsync(lote, unidadeNegocioId, agora, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            batches++;
        }
        cronometroAplicacao.Stop();

        var cronometroReconciliacao = System.Diagnostics.Stopwatch.StartNew();
        var reconciliacao = await ReconciliarAsync(raw, resumo, cancellationToken);
        cronometroReconciliacao.Stop();

        // Item de gate do PO: "Somente marcar CargaFullInicialValidada/IncrementalLiberado se NÃO houver
        // divergência material categoria D." A própria execução RAW registra o resultado da reconciliação
        // (nunca inferido depois, sempre um fato persistido) — só então HomologarBaseline pode aceitar essa
        // execução como candidata, e o próprio método da entidade re-valida tudo (Completa, Modo=Full,
        // ReconciliacaoStatus=Aprovada) antes de liberar o incremental.
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
                var estado = await context.LinxDatasetLoadStates.SingleOrDefaultAsync(s => s.UnidadeNegocioId == unidadeNegocioId && s.Dataset == dataset, cancellationToken);
                if (estado is null)
                {
                    estado = LinxDatasetLoadState.Novo(unidadeNegocioId, dataset);
                    context.LinxDatasetLoadStates.Add(estado);
                }

                // Decisão do PO (B3/Bloco 5A, regra definitiva de watermark): o watermark representa o instante
                // de INÍCIO da execução que estabelece a baseline — nunca o de conclusão. Usar ConcluidoEm
                // arriscaria não capturar uma alteração feita no Linx durante a própria janela de leitura da
                // carga Full. A janela de sobreposição de 5 minutos do contrato é aplicada pelo GATE ao
                // RESOLVER o watermark efetivo de uma futura execução Incremental (LinxDatasetLoadStateGate).
                watermarkInicial = execucaoTracked.IniciadoEm;
                estado.HomologarBaseline(execucaoTracked, agora, watermarkInicial);
                baselineHomologada = true;
                break;
            }
            case ProximaAcaoBaseline.AvancarWatermark:
            {
                // Ciclo incremental completo (B3/Bloco 5A): RAW já capturou WatermarkFinal=IniciadoEm no
                // momento da leitura (GovernedLiveReadCliHandler); aqui, só depois de REFINED aplicado E
                // reconciliação Aprovada, o watermark efetivamente avança — nunca em caso de falha, execução
                // parcial ou reconciliação reprovada (ver DecisaoPosReconciliacao, que já filtrou esses casos
                // para ProximaAcaoBaseline.Nenhuma antes de chegarmos aqui).
                var estado = await context.LinxDatasetLoadStates.SingleAsync(s => s.UnidadeNegocioId == unidadeNegocioId && s.Dataset == dataset, cancellationToken);
                estado.AvancarWatermark(execucaoTracked);
                watermarkInicial = estado.UltimoWatermarkValido;
                watermarkAvancado = true;
                break;
            }
            case ProximaAcaoBaseline.Nenhuma:
                logger.LogWarning("Nenhuma ação de baseline/watermark para dataset {Dataset}: Modo {Modo}, Reconciliação {Status}.", dataset, execucaoTracked.Modo, execucaoTracked.ReconciliacaoStatus);
                break;
        }

        await context.SaveChangesAsync(cancellationToken);

        var estadoFinal = await context.LinxDatasetLoadStates.AsNoTracking().SingleOrDefaultAsync(s => s.UnidadeNegocioId == unidadeNegocioId && s.Dataset == dataset, cancellationToken);

        return new(
            execucaoRaw.Id, dryRun, Aplicado: true, resumo, plano.ConflitosPrincipal, plano.Erros, limiarExcedido, reconciliacao,
            cronometroRefined.Elapsed, cronometroAplicacao.Elapsed, cronometroReconciliacao.Elapsed, batches, ocorrenciasPersistidas,
            baselineHomologada, estadoFinal?.CargaFullInicialValidada ?? false, estadoFinal?.IncrementalLiberado ?? false, watermarkInicial, watermarkAvancado);
    }

    private async Task AplicarLoteAsync(IReadOnlyList<FornecedorRefinedDecision> lote, Guid unidadeNegocioId, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        var idsExistentes = lote.Where(f => f.FornecedorExistenteId.HasValue).Select(f => f.FornecedorExistenteId!.Value).ToList();
        var fornecedoresTracked = idsExistentes.Count == 0
            ? []
            : await context.Fornecedores.Where(f => idsExistentes.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken);

        var vinculoIdsExistentes = lote.SelectMany(f => f.Vinculos).Where(v => v.VinculoExistenteId.HasValue).Select(v => v.VinculoExistenteId!.Value).ToList();
        var vinculosTracked = vinculoIdsExistentes.Count == 0
            ? []
            : await context.FornecedorLinxVinculos.Where(v => vinculoIdsExistentes.Contains(v.Id)).ToDictionaryAsync(v => v.Id, cancellationToken);

        foreach (var decisao in lote)
        {
            var hash = HashDaDecisao(decisao);
            Fornecedor fornecedor;
            if (decisao.FornecedorExistenteId is Guid existenteId && fornecedoresTracked.TryGetValue(existenteId, out var tracked))
            {
                fornecedor = tracked;
                fornecedor.AplicarIdentidadeLinxRefined(decisao.RazaoSocial, decisao.NomeFantasia, decisao.TipoPessoa, decisao.Ativo, hash, agora);
            }
            else
            {
                fornecedor = new Fornecedor(Guid.NewGuid(), decisao.RazaoSocial,
                    BlueprintOS.Domain.Procurement.Suppliers.DocumentoFiscal.Create(decisao.Cnpj), decisao.TipoPessoa,
                    categoria: null, email: null, telefone: null, website: null, cidade: null, estado: null, pais: null,
                    status: decisao.Ativo ? "Ativo" : "Inativo", scoreIA: null, createdAt: agora, unidadeNegocioId: unidadeNegocioId,
                    nomeFantasia: decisao.NomeFantasia);
                context.Fornecedores.Add(fornecedor);
            }

            foreach (var v in decisao.Vinculos)
            {
                if (v.VinculoExistenteId is Guid vId && vinculosTracked.TryGetValue(vId, out var vinculoTracked))
                {
                    if (v.Action != RefinedAction.NoChange)
                    {
                        vinculoTracked.AtualizarDadosErp(v.NomeClifor, v.InativoFornecedores, v.InativoCadastroCliFor, v.UltimaAlteracao, agora);
                    }

                    if (v.RemoverPrincipal) vinculoTracked.RemoverComoPrincipal(agora);
                    if (v.AtribuirPrincipal) vinculoTracked.DefinirComoPrincipal(agora);
                }
                else
                {
                    var novoVinculo = new FornecedorLinxVinculo(
                        fornecedor.Id, unidadeNegocioId, FornecedorRefinedProjector.ErpSistema, v.CodigoErp, v.NomeClifor,
                        v.InativoFornecedores, v.InativoCadastroCliFor, v.UltimaAlteracao, v.AtribuirPrincipal, agora);
                    context.FornecedorLinxVinculos.Add(novoVinculo);
                }
            }
        }
    }

    /// <summary>Item de gate do PO: "Não validar apenas COUNT(*)... separar diferença esperada por
    /// transformação do modelo de divergência real." Reconcilia por contagens-chave do contrato
    /// (CNPJ corporativo, vínculos, ativo/inativo, Principal) — nunca só a contagem bruta de linhas.</summary>
    private async Task<ReconciliacaoResultado> ReconciliarAsync(IReadOnlyList<RawLinxFornecedorSnapshotRegistro> raw, RefinedPlanResumo resumo, CancellationToken cancellationToken)
    {
        var divergencias = new List<string>();
        var cnpjsValidos = new HashSet<string>(StringComparer.Ordinal);
        // Item de gate do PO: os 1.963 documentos rejeitados NUNCA foram elegíveis para o domínio (REFINED
        // os excluiu desde o início) — a reconciliação precisa comparar contra o MESMO escopo que REFINED
        // realmente tentou aplicar, nunca contra o RAW bruto e não filtrado (essa era a causa da divergência
        // "categoria A" abaixo antes desta correção: comparar 78.374 linhas brutas contra 76.411 vínculos
        // realmente elegíveis nunca poderia bater, e não seria uma divergência real).
        var rawValido = new List<RawLinxFornecedorSnapshotRegistro>(raw.Count);
        foreach (var row in raw)
        {
            try
            {
                cnpjsValidos.Add(BlueprintOS.Domain.Procurement.Suppliers.DocumentoFiscal.Create(row.CnpjCpf ?? string.Empty).Value);
                rawValido.Add(row);
            }
            catch (ArgumentException) { /* já contabilizado como ocorrência ERROR no plano — fora do escopo elegível */ }
        }

        // Achado real (Onda 2, bateria final de certificação B3, 04/09/2026): sob Incremental, RAW é
        // append-only (nunca trunca — só Full trunca), então pode conter mais de uma linha para o mesmo
        // CodigoFornecedor (a versão antiga e a recém-anexada). FornecedorRefinedProjector.Projetar já
        // resolve isso corretamente por LWW (OrderByDescending UltimaAlteracao); a reconciliação precisa da
        // MESMA visão "vencedor por Fornecedor", nunca contar `rawValido` bruto — do contrário, uma linha
        // antiga (já superada) infla "ativos esperados" mesmo depois do REFINED já ter aplicado corretamente
        // a alteração real (reproduzido neste teste: alteração controlada e reversível em 101 vínculos,
        // divergência fantasma de 101 até esta correção).
        var rawVencedorPorFornecedor = rawValido
            .GroupBy(r => r.CodigoFornecedor)
            .Select(g => g.OrderByDescending(r => r.UltimaAlteracao ?? DateTime.MinValue).First())
            .ToList();

        var vinculosEsperadosAtivos = rawVencedorPorFornecedor.Count(r => !r.InativoFornecedores && !r.InativoCadastroCliFor);
        var vinculosEsperadosTotal = rawVencedorPorFornecedor.Count;

        var fornecedoresNoDominio = await context.Fornecedores.AsNoTracking()
            .Where(f => cnpjsValidos.Contains(f.Cnpj_Cpf))
            .Select(f => new { f.Id, f.Status })
            .ToListAsync(cancellationToken);
        var idsNoEscopo = fornecedoresNoDominio.Select(f => f.Id).ToHashSet();

        var vinculosNoDominio = await context.FornecedorLinxVinculos.AsNoTracking()
            .Where(v => v.ErpSistema == FornecedorRefinedProjector.ErpSistema)
            .Select(v => new { v.Id, v.FornecedorId, v.InativoFornecedores, v.InativoCadastroCliFor, v.Principal })
            .ToListAsync(cancellationToken);
        var vinculosDosFornecedoresNoEscopo = vinculosNoDominio.Where(v => idsNoEscopo.Contains(v.FornecedorId)).ToList();

        var fornecedoresAtivosNoDominio = fornecedoresNoDominio.Count(f => f.Status == "Ativo");
        var vinculosAtivosNoDominio = vinculosDosFornecedoresNoEscopo.Count(v => !v.InativoFornecedores && !v.InativoCadastroCliFor);

        // Reconciliação de Principal: item de gate do PO — "não validar apenas COUNT(*)". Verifica a
        // invariante real (nunca uma comparação de uma contagem contra si mesma): todo Fornecedor ATIVO no
        // escopo desta execução deve ter EXATAMENTE um vínculo Principal-e-Ativo, exceto os CNPJs
        // registrados como conflito de empate pelo REFINED (esses ficam, por decisão do PO, sem Principal
        // operacional até o comprador decidir).
        var fornecedoresAtivosSemPrincipalAtivo = vinculosDosFornecedoresNoEscopo
            .GroupBy(v => v.FornecedorId)
            .Count(g => fornecedoresNoDominio.Single(f => f.Id == g.Key).Status == "Ativo" && !g.Any(v => v.Principal && !v.InativoFornecedores && !v.InativoCadastroCliFor));
        var principaisAtivosNoDominio = vinculosDosFornecedoresNoEscopo.Count(v => v.Principal && !v.InativoFornecedores && !v.InativoCadastroCliFor);
        var fornecedoresAtivosComPrincipalEsperado = fornecedoresAtivosNoDominio - resumo.SemPrincipal;

        if (fornecedoresNoDominio.Count != cnpjsValidos.Count)
        {
            divergencias.Add($"Fornecedores no domínio ({fornecedoresNoDominio.Count}) difere de CNPJs válidos no RAW ({cnpjsValidos.Count}).");
        }

        if (vinculosDosFornecedoresNoEscopo.Count != vinculosEsperadosTotal)
        {
            divergencias.Add($"Vínculos no domínio ({vinculosDosFornecedoresNoEscopo.Count}) difere de CodigoFornecedor distintos no RAW ({vinculosEsperadosTotal}).");
        }

        if (vinculosAtivosNoDominio != vinculosEsperadosAtivos)
        {
            divergencias.Add($"Vínculos ativos no domínio ({vinculosAtivosNoDominio}) difere do esperado pelo RAW ({vinculosEsperadosAtivos}).");
        }

        if (fornecedoresAtivosSemPrincipalAtivo != resumo.SemPrincipal)
        {
            divergencias.Add($"Fornecedores ativos sem Principal ativo no domínio ({fornecedoresAtivosSemPrincipalAtivo}) difere dos conflitos de empate registrados pelo REFINED ({resumo.SemPrincipal}).");
        }

        return new ReconciliacaoResultado(
            cnpjsValidos.Count, fornecedoresNoDominio.Count, vinculosEsperadosTotal, vinculosDosFornecedoresNoEscopo.Count,
            vinculosEsperadosAtivos, vinculosAtivosNoDominio, fornecedoresAtivosComPrincipalEsperado, principaisAtivosNoDominio, divergencias);
    }

    private static string HashDaDecisao(FornecedorRefinedDecision decisao) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{decisao.RazaoSocial}|{decisao.NomeFantasia}|{decisao.TipoPessoa}|{decisao.Ativo}")));

    private static IEnumerable<IReadOnlyList<T>> Chunk<T>(IReadOnlyList<T> source, int size)
    {
        for (var i = 0; i < source.Count; i += size)
        {
            yield return source.Skip(i).Take(size).ToList();
        }
    }
}
