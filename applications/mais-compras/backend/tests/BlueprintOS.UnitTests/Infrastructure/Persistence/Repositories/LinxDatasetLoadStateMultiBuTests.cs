using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Infrastructure.Persistence.Repositories;

/// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): prova, contra o modelo EF
/// real (não um fake), que <see cref="LinxDatasetLoadState"/> passou a ter identidade composta
/// (UnidadeNegocioId, Dataset) — duas Unidades de Negócio executando o mesmo dataset nunca compartilham
/// bootstrap/baseline/watermark.</summary>
public sealed class LinxDatasetLoadStateMultiBuTests
{
    private static readonly Guid GrupoSoma = Guid.NewGuid();
    private static readonly Guid Reserva = Guid.NewGuid();
    private const string Dataset = "linx.itens-fiscais.snapshot";

    [Fact]
    public async Task Mesmo_Dataset_Em_Duas_BUs_Deve_Coexistir_Sem_Colidir()
    {
        await using var context = new BlueprintOSDbContext(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        context.LinxDatasetLoadStates.Add(LinxDatasetLoadState.Novo(GrupoSoma, Dataset));
        context.LinxDatasetLoadStates.Add(LinxDatasetLoadState.Novo(Reserva, Dataset));
        await context.SaveChangesAsync();

        Assert.Equal(2, await context.LinxDatasetLoadStates.CountAsync(s => s.Dataset == Dataset));
    }

    [Fact]
    public async Task Watermark_Homologado_Em_Uma_BU_Nao_Aparece_Na_Outra()
    {
        await using var context = new BlueprintOSDbContext(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var agora = DateTimeOffset.UtcNow;

        var full = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), Dataset, RawLoadMode.Full, agora.AddDays(-1));
        full.Concluir(agora.AddDays(-1).AddMinutes(1), completa: true, linhasLidas: 1, linhasGravadas: 1, isolamentoUtilizado: "READ UNCOMMITTED", erro: null);
        full.RegistrarReconciliacao(RawReconciliacaoStatus.Aprovada, agora);

        var estadoGrupoSoma = LinxDatasetLoadState.Novo(GrupoSoma, Dataset);
        estadoGrupoSoma.HomologarBaseline(full, agora, full.IniciadoEm);
        var estadoReserva = LinxDatasetLoadState.Novo(Reserva, Dataset);

        context.LinxDatasetLoadStates.Add(estadoGrupoSoma);
        context.LinxDatasetLoadStates.Add(estadoReserva);
        await context.SaveChangesAsync();

        var grupoSomaLido = await context.LinxDatasetLoadStates.AsNoTracking().SingleAsync(s => s.UnidadeNegocioId == GrupoSoma && s.Dataset == Dataset);
        var reservaLida = await context.LinxDatasetLoadStates.AsNoTracking().SingleAsync(s => s.UnidadeNegocioId == Reserva && s.Dataset == Dataset);

        Assert.True(grupoSomaLido.PodeExecutarIncremental());
        Assert.False(reservaLida.PodeExecutarIncremental());
        Assert.False(reservaLida.CargaFullInicialValidada);
    }
}
