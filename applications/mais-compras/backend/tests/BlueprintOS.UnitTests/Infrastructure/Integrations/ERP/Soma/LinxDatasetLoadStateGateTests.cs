using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;

namespace BlueprintOS.UnitTests.Infrastructure.Integrations.ERP.Soma;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): prova, contra a implementação REAL de
/// <see cref="IDatasetLoadGate"/> — não um fake — que a janela de overlap é de fato subtraída do último
/// watermark válido ao resolver o watermark efetivo de uma futura execução Incremental. A cobertura anterior
/// (ToolGatewayLiveReadTests) só exercitava um fake do gate para testar o adapter, nunca a matemática real.
/// </summary>
public sealed class LinxDatasetLoadStateGateTests
{
    private sealed class FakeRepository(LinxDatasetLoadState? estado) : ILinxDatasetLoadStateRepository
    {
        public Task<LinxDatasetLoadState?> ObterAsync(string dataset, CancellationToken cancellationToken = default) => Task.FromResult(estado);
        public Task SalvarAsync(LinxDatasetLoadState estado, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static LinxDatasetLoadState EstadoHomologado(DateTimeOffset watermark)
    {
        var full = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), "dataset-teste", RawLoadMode.Full, watermark);
        full.Concluir(watermark.AddMinutes(1), completa: true, linhasLidas: 1, linhasGravadas: 1, isolamentoUtilizado: "READ UNCOMMITTED", erro: null);
        full.RegistrarReconciliacao(RawReconciliacaoStatus.Aprovada, watermark.AddMinutes(2));
        var estado = LinxDatasetLoadState.Novo(Guid.NewGuid(), "dataset-teste");
        estado.HomologarBaseline(full, watermark.AddMinutes(2), watermark);
        return estado;
    }

    [Fact]
    public async Task Watermark_Efetivo_Subtrai_A_Janela_De_Overlap_Do_Ultimo_Watermark_Valido()
    {
        var watermark = new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
        var gate = new LinxDatasetLoadStateGate(new FakeRepository(EstadoHomologado(watermark)));
        var overlap = TimeSpan.FromMinutes(5);

        var autorizacao = await gate.AuthorizeIncrementalAsync("dataset-teste", overlap);

        Assert.True(autorizacao.Permitido);
        Assert.Equal(watermark - overlap, autorizacao.WatermarkEfetivo);
    }

    [Fact]
    public async Task Overlap_Zero_Nao_Move_O_Watermark_Efetivo()
    {
        var watermark = new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
        var gate = new LinxDatasetLoadStateGate(new FakeRepository(EstadoHomologado(watermark)));

        var autorizacao = await gate.AuthorizeIncrementalAsync("dataset-teste", TimeSpan.Zero);

        Assert.Equal(watermark, autorizacao.WatermarkEfetivo);
    }

    [Fact]
    public async Task Dataset_Sem_Baseline_Nunca_Autoriza_Incremental()
    {
        var gate = new LinxDatasetLoadStateGate(new FakeRepository(null));

        var autorizacao = await gate.AuthorizeIncrementalAsync("dataset-inexistente", TimeSpan.FromMinutes(5));

        Assert.False(autorizacao.Permitido);
        Assert.Null(autorizacao.WatermarkEfetivo);
    }

    [Fact]
    public async Task Dataset_Com_Baseline_Mas_Sem_Watermark_Valido_Nunca_Autoriza()
    {
        var estado = LinxDatasetLoadState.Novo(Guid.NewGuid(), "dataset-parcial");
        var gate = new LinxDatasetLoadStateGate(new FakeRepository(estado));

        var autorizacao = await gate.AuthorizeIncrementalAsync("dataset-parcial", TimeSpan.FromMinutes(5));

        Assert.False(autorizacao.Permitido);
    }
}
