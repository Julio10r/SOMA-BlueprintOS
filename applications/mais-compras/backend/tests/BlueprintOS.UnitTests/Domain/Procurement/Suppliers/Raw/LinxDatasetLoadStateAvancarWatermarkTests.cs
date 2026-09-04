using BlueprintOS.Domain.Procurement.Suppliers.Raw;

namespace BlueprintOS.UnitTests.Domain.Procurement.Suppliers.Raw;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final), item de gate do PO: "AvancarWatermark nunca invocado"
/// não pode permanecer até a bateria final. Estes testes cobrem exatamente os 6 cenários exigidos: sucesso
/// avança, falha não avança, execução parcial não avança, overlap preservado (via <see cref="LinxDatasetLoadStateGate"/>,
/// testado em ToolGatewayLiveReadTests), reexecução idempotente.
/// </summary>
public sealed class LinxDatasetLoadStateAvancarWatermarkTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    private static LinxDatasetLoadState EstadoComIncrementalLiberado()
    {
        var full = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), "dataset-teste", RawLoadMode.Full, Now.AddDays(-1));
        full.Concluir(Now.AddDays(-1).AddMinutes(1), completa: true, linhasLidas: 10, linhasGravadas: 10, isolamentoUtilizado: "READ UNCOMMITTED", erro: null);
        full.RegistrarReconciliacao(RawReconciliacaoStatus.Aprovada, Now.AddDays(-1).AddMinutes(2));

        var estado = LinxDatasetLoadState.Novo(Guid.NewGuid(), "dataset-teste");
        estado.HomologarBaseline(full, Now.AddDays(-1).AddMinutes(2), full.IniciadoEm);
        return estado;
    }

    private static RawLinxFornecedorSnapshotExecucao ExecucaoIncremental(DateTimeOffset startedAt, bool completa, DateTimeOffset? watermarkFinal)
    {
        var execucao = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), "dataset-teste", RawLoadMode.Incremental, startedAt, watermarkInicial: startedAt.AddMinutes(-5));
        execucao.Concluir(startedAt.AddMinutes(1), completa, linhasLidas: 5, linhasGravadas: 5, isolamentoUtilizado: "READ UNCOMMITTED", erro: completa ? null : "falha simulada", watermarkFinal: completa ? watermarkFinal : null);
        return execucao;
    }

    [Fact]
    public void Sucesso_Avanca_O_Watermark_Para_O_StartedAt_Da_Execucao()
    {
        var estado = EstadoComIncrementalLiberado();
        var novoStartedAt = Now;
        var execucao = ExecucaoIncremental(novoStartedAt, completa: true, watermarkFinal: novoStartedAt);

        estado.AvancarWatermark(execucao);

        Assert.Equal(novoStartedAt, estado.UltimoWatermarkValido);
    }

    [Fact]
    public void Execucao_Incompleta_Falha_Nunca_Avanca()
    {
        var estado = EstadoComIncrementalLiberado();
        var watermarkAntes = estado.UltimoWatermarkValido;
        // Concluir(completa:false) força WatermarkFinal=null internamente — simula falha/execução parcial.
        var execucao = ExecucaoIncremental(Now, completa: false, watermarkFinal: null);

        Assert.Throws<InvalidOperationException>(() => estado.AvancarWatermark(execucao));
        Assert.Equal(watermarkAntes, estado.UltimoWatermarkValido);
    }

    [Fact]
    public void Execucao_Full_Nunca_Avanca_Watermark_Mesmo_Se_Completa()
    {
        var estado = EstadoComIncrementalLiberado();
        var watermarkAntes = estado.UltimoWatermarkValido;
        var full = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), "dataset-teste", RawLoadMode.Full, Now);
        full.Concluir(Now.AddMinutes(1), completa: true, linhasLidas: 1, linhasGravadas: 1, isolamentoUtilizado: "READ UNCOMMITTED", erro: null);

        Assert.Throws<InvalidOperationException>(() => estado.AvancarWatermark(full));
        Assert.Equal(watermarkAntes, estado.UltimoWatermarkValido);
    }

    [Fact]
    public void Incremental_Sem_Baseline_Homologada_Nunca_Avanca()
    {
        var estado = LinxDatasetLoadState.Novo(Guid.NewGuid(), "dataset-sem-baseline");
        var execucao = ExecucaoIncremental(Now, completa: true, watermarkFinal: Now);

        Assert.Throws<InvalidOperationException>(() => estado.AvancarWatermark(execucao));
        Assert.Null(estado.UltimoWatermarkValido);
    }

    [Fact]
    public void Watermark_Nunca_Regride()
    {
        var estado = EstadoComIncrementalLiberado();
        var watermarkAtual = estado.UltimoWatermarkValido!.Value;
        var execucaoAntiga = ExecucaoIncremental(watermarkAtual.AddHours(-2), completa: true, watermarkFinal: watermarkAtual.AddHours(-2));

        estado.AvancarWatermark(execucaoAntiga);

        Assert.Equal(watermarkAtual, estado.UltimoWatermarkValido);
    }

    [Fact]
    public void Reexecucao_Idempotente_Do_Mesmo_Watermark_Nao_Falha_E_Preserva_Estado()
    {
        var estado = EstadoComIncrementalLiberado();
        var execucao = ExecucaoIncremental(Now, completa: true, watermarkFinal: Now);

        estado.AvancarWatermark(execucao);
        var primeiraExecucaoValidaId = estado.UltimaExecucaoValidaId;
        estado.AvancarWatermark(execucao); // reprocessar a mesma execução não deve lançar nem regredir

        Assert.Equal(Now, estado.UltimoWatermarkValido);
        Assert.Equal(primeiraExecucaoValidaId, estado.UltimaExecucaoValidaId);
    }
}
