namespace BlueprintOS.Domain.Procurement.Suppliers.Raw;

/// <summary>
/// B3 — Bloco 5A.9, revisão pré-Gate B (decisão do Product Owner): estado/configuração de bootstrap e
/// baseline de UM dataset governado — nunca linha de histórico de execução (isso é
/// <see cref="RawLinxFornecedorSnapshotExecucao"/>). Existe exatamente uma instância por dataset.
///
/// Regra central, fixada pelo PO: nenhum dataset Incremental pode começar operando incrementalmente.
/// <see cref="IncrementalLiberado"/> só se torna verdadeiro através de <see cref="HomologarBaseline"/>, que só
/// pode ser chamado depois de uma carga Full concluída, processada e reconciliada com sucesso — nunca por
/// inferência, nunca porque uma execução "terminou sem exception".
/// </summary>
public sealed class LinxDatasetLoadState
{
    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): compõe a identidade do
    /// estado junto com <see cref="Dataset"/> — Grupo Soma e Reserva executando o mesmo dataset nunca
    /// compartilham bootstrap/baseline/watermark. O Gate governado de LiveRead
    /// (<c>IDatasetLoadGate</c>/<c>LinxDatasetSnapshotReadAdapter</c>, <c>BlueprintOS.Core</c>) permanece,
    /// deliberadamente, sem esta dimensão nesta rodada — ver GAP registrado em
    /// <c>applications/mais-compras/docs/cadernos/Onda-2.md</c> — apenas os pipelines REFINED que acessam
    /// esta entidade diretamente (Item Fiscal, Cadastro de Apoio, Fornecedor) ficam BU-aware aqui.</summary>
    public Guid UnidadeNegocioId { get; private set; }
    public string Dataset { get; private set; } = string.Empty;
    public bool CargaFullInicialValidada { get; private set; }
    public bool IncrementalLiberado { get; private set; }
    public Guid? BaselineExecucaoId { get; private set; }
    public DateTimeOffset? BaselineHomologadaEm { get; private set; }
    public Guid? UltimaExecucaoValidaId { get; private set; }
    public DateTimeOffset? UltimoWatermarkValido { get; private set; }

    private LinxDatasetLoadState()
    {
    }

    public static LinxDatasetLoadState Novo(Guid unidadeNegocioId, string dataset)
    {
        if (unidadeNegocioId == Guid.Empty) throw new ArgumentException("UnidadeNegocioId é obrigatória (Onda 2, Multi-BU).", nameof(unidadeNegocioId));
        return new()
        {
            UnidadeNegocioId = unidadeNegocioId,
            Dataset = dataset,
            CargaFullInicialValidada = false,
            IncrementalLiberado = false,
        };
    }

    /// <summary>Incremental só é permitido depois do bootstrap Full reconciliado e homologado.</summary>
    public bool PodeExecutarIncremental() => CargaFullInicialValidada && IncrementalLiberado;

    /// <summary>
    /// Marca a baseline como homologada (item de gate do PO: ingestão concluída != processamento concluído
    /// != reconciliação concluída != baseline homologada/PASS — só o último libera o incremental). O único
    /// ponto do sistema que pode ligar <see cref="CargaFullInicialValidada"/> e <see cref="IncrementalLiberado"/>,
    /// e só aceita fazer isso a partir de uma execução Full que já esteja <see cref="RawLinxFornecedorSnapshotExecucao.Completa"/>
    /// E cuja <see cref="RawLinxFornecedorSnapshotExecucao.ReconciliacaoStatus"/> seja
    /// <see cref="RawReconciliacaoStatus.Aprovada"/> — nunca a partir do simples fato de a ingestão ter
    /// terminado sem exceção, contagens baterem, ou <c>SqlBulkCopy</c> ter retornado.
    /// </summary>
    public void HomologarBaseline(RawLinxFornecedorSnapshotExecucao execucaoFull, DateTimeOffset homologadaEm, DateTimeOffset? watermarkInicial)
    {
        if (execucaoFull.Modo != RawLoadMode.Full)
            throw new InvalidOperationException("A baseline so pode ser homologada a partir de uma execucao Full.");
        if (!execucaoFull.Completa)
            throw new InvalidOperationException("A baseline nunca e homologada a partir de uma ingestao incompleta.");
        if (execucaoFull.ReconciliacaoStatus != RawReconciliacaoStatus.Aprovada)
            throw new InvalidOperationException("A baseline so e homologada apos reconciliacao Aprovada — contagem igual nao prova igualdade.");

        BaselineExecucaoId = execucaoFull.Id;
        BaselineHomologadaEm = homologadaEm;
        CargaFullInicialValidada = true;
        IncrementalLiberado = true;
        UltimaExecucaoValidaId = execucaoFull.Id;
        UltimoWatermarkValido = watermarkInicial;
    }

    /// <summary>
    /// Avança o watermark após uma execução Incremental validada. Nunca deve ser chamado para uma execução
    /// iniciada, cancelada, com timeout, com erro ou parcialmente concluída — essa regra é reforçada aqui, não
    /// apenas documentada: uma <see cref="RawLinxFornecedorSnapshotExecucao"/> que não esteja
    /// <see cref="RawLinxFornecedorSnapshotExecucao.Completa"/>, que não seja do modo Incremental, ou cujo
    /// <see cref="RawLinxFornecedorSnapshotExecucao.WatermarkFinal"/> esteja ausente é rejeitada. O watermark
    /// nunca regride — se o novo valor for anterior ao já armazenado, a chamada é rejeitada silenciosamente
    /// sem avançar (o pipeline é idempotente; reprocessar uma janela já coberta nunca é um erro).
    /// </summary>
    public void AvancarWatermark(RawLinxFornecedorSnapshotExecucao execucao)
    {
        if (!PodeExecutarIncremental())
            throw new InvalidOperationException($"Dataset '{Dataset}' nao tem incremental liberado — bootstrap Full ainda nao homologado.");
        if (execucao.Modo != RawLoadMode.Incremental)
            throw new InvalidOperationException("Watermark so avanca a partir de uma execucao Incremental.");
        if (!execucao.Completa)
            throw new InvalidOperationException("Watermark nunca avanca a partir de uma execucao incompleta, cancelada, com timeout ou com erro.");
        if (execucao.WatermarkFinal is null)
            throw new InvalidOperationException("Execucao Incremental concluida sem WatermarkFinal registrado — nao ha o que avancar.");

        if (UltimoWatermarkValido is not null && execucao.WatermarkFinal.Value < UltimoWatermarkValido.Value)
            return;

        UltimoWatermarkValido = execucao.WatermarkFinal;
        UltimaExecucaoValidaId = execucao.Id;
    }
}
