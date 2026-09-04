namespace BlueprintOS.Domain.Procurement.Suppliers.Raw;

/// <summary>B3 — Bloco 5A, decisão do Product Owner: EstratégiaNormal de um dataset (definida no contrato do
/// dataset) é conceito distinto do modo de execução de UMA carga específica — um dataset normalmente
/// Incremental ainda pode executar uma carga Full sob demanda sem deixar de ser, normalmente, incremental.
/// Este enum representa o modo de execução (ModoExecucao), sempre um valor concreto por execução.</summary>
public enum RawLoadMode
{
    Full = 1,
    Incremental = 2,
}

/// <summary>Distingue os estágios de uma reconciliação (item de gate do PO: ingestão concluída != baseline
/// homologada). <see cref="NaoRealizada"/> é o estado inicial de toda execução — a maioria nunca sai dele,
/// já que reconciliação plena só é exigida para a execução candidata a baseline de um dataset Incremental.</summary>
public enum RawReconciliacaoStatus
{
    NaoRealizada = 0,
    Pendente = 1,
    Aprovada = 2,
    Reprovada = 3,
}

/// <summary>
/// B3 — Bloco 5A.9, Gate A/revisão pré-Gate B: identidade de uma execução do LiveRead governado do dataset
/// "linx.fornecedores.snapshot". Não é um agregado com regras de negócio de domínio — é o cabeçalho de
/// staging que dá a um carregamento RAW uma identidade, um modo (Full/Incremental) e um status de completude
/// e de reconciliação. Regra fixada pelo Product Owner: uma carga incompleta (<see cref="Completa"/> = false)
/// nunca é elegível para consumo por REFINED/DOMÍNIO, e nenhuma execução aqui registrada avança sozinha o
/// watermark de um dataset — isso é responsabilidade exclusiva de <see cref="LinxDatasetLoadState.AvancarWatermark"/>,
/// que valida esta própria execução antes de aceitar.
/// </summary>
public sealed class RawLinxFornecedorSnapshotExecucao
{
    public Guid Id { get; private set; }
    public string Dataset { get; private set; } = string.Empty;
    public RawLoadMode Modo { get; private set; }
    public DateTimeOffset IniciadoEm { get; private set; }
    public DateTimeOffset? ConcluidoEm { get; private set; }
    public bool Completa { get; private set; }
    public long LinhasLidas { get; private set; }
    public long LinhasGravadas { get; private set; }
    public string IsolamentoUtilizado { get; private set; } = string.Empty;
    public string? Erro { get; private set; }

    /// <summary>Watermark efetivamente usado para filtrar esta execução (já com a janela de sobreposição de
    /// segurança aplicada). Sempre null para <see cref="RawLoadMode.Full"/>.</summary>
    public DateTimeOffset? WatermarkInicial { get; private set; }

    /// <summary>Watermark observado ao final desta execução, candidato a se tornar o próximo
    /// <see cref="LinxDatasetLoadState.UltimoWatermarkValido"/> — só se esta execução for
    /// <see cref="Completa"/>. Sempre null para <see cref="RawLoadMode.Full"/>.</summary>
    public DateTimeOffset? WatermarkFinal { get; private set; }

    public RawReconciliacaoStatus ReconciliacaoStatus { get; private set; } = RawReconciliacaoStatus.NaoRealizada;
    public DateTimeOffset? ReconciliadoEm { get; private set; }

    private RawLinxFornecedorSnapshotExecucao()
    {
    }

    public static RawLinxFornecedorSnapshotExecucao Iniciar(Guid id, string dataset, RawLoadMode modo, DateTimeOffset iniciadoEm, DateTimeOffset? watermarkInicial = null) => new()
    {
        Id = id,
        Dataset = dataset,
        Modo = modo,
        IniciadoEm = iniciadoEm,
        Completa = false,
        WatermarkInicial = modo == RawLoadMode.Incremental ? watermarkInicial : null,
    };

    public void Concluir(DateTimeOffset concluidoEm, bool completa, long linhasLidas, long linhasGravadas, string isolamentoUtilizado, string? erro, DateTimeOffset? watermarkFinal = null)
    {
        ConcluidoEm = concluidoEm;
        Completa = completa;
        LinhasLidas = linhasLidas;
        LinhasGravadas = linhasGravadas;
        IsolamentoUtilizado = isolamentoUtilizado;
        Erro = erro;
        WatermarkFinal = Modo == RawLoadMode.Incremental && completa ? watermarkFinal : null;
    }

    /// <summary>Reconciliação plena (item de gate do PO: contagem igual não prova igualdade) — chamada só
    /// para a execução candidata a baseline. Exige que a ingestão já esteja <see cref="Completa"/>: nunca
    /// reconciliar uma carga incompleta.</summary>
    public void RegistrarReconciliacao(RawReconciliacaoStatus status, DateTimeOffset reconciliadoEm)
    {
        if (!Completa)
            throw new InvalidOperationException("Reconciliacao nunca e registrada para uma execucao que nao completou a ingestao.");
        if (status == RawReconciliacaoStatus.NaoRealizada)
            throw new ArgumentOutOfRangeException(nameof(status), status, "Registrar reconciliacao exige um resultado concreto (Pendente, Aprovada ou Reprovada).");

        ReconciliacaoStatus = status;
        ReconciliadoEm = reconciliadoEm;
    }
}
