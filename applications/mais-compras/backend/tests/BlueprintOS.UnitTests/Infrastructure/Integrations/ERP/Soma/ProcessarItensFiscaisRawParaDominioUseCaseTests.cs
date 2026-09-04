using BlueprintOS.Domain.Identity;
using BlueprintOS.Domain.Identity.Raw;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BlueprintOS.UnitTests.Infrastructure.Integrations.ERP.Soma;

/// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026) — comprova a resolução do GAP arquitetural de
/// <see cref="ItemFiscal.CriarDeErp"/>: o pipeline governado agora cria Item Fiscal novo usando a Business
/// Unit explícita da execução (nunca inferida), e falha fechado sem uma Business Unit válida.</summary>
public sealed class ProcessarItensFiscaisRawParaDominioUseCaseTests
{
    private static readonly Guid GrupoSomaId = Guid.NewGuid();
    private const string Dataset = LinxReadDatasetCatalog.ItensFiscaisSnapshot;

    [Fact]
    public async Task ExecutarAsync_Should_Fail_Closed_When_BusinessUnit_Is_Empty()
    {
        await using var context = NewContext();
        var useCase = new ProcessarItensFiscaisRawParaDominioUseCase(context, new IntegrationOccurrenceRepository(context), NullLogger<ProcessarItensFiscaisRawParaDominioUseCase>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecutarAsync(dryRun: false, Guid.Empty, TimeProvider.System, CancellationToken.None));
    }

    [Fact]
    public async Task ExecutarAsync_Should_Create_New_ItemFiscal_With_The_Execution_BusinessUnit()
    {
        await using var context = NewContext();
        var agora = DateTimeOffset.UtcNow;
        SeedExecucaoCompleta(context, agora);
        context.RawLinxItensFiscaisSnapshot.Add(RawLinxItemFiscalRegistro.ParaTeste("9999", "Item Fiscal Novo Do Linx", "UN", "1.1.01", false, agora.UtcDateTime));
        await context.SaveChangesAsync();

        var useCase = new ProcessarItensFiscaisRawParaDominioUseCase(context, new IntegrationOccurrenceRepository(context), NullLogger<ProcessarItensFiscaisRawParaDominioUseCase>.Instance);
        var resultado = await useCase.ExecutarAsync(dryRun: false, GrupoSomaId, TimeProvider.System, CancellationToken.None);

        Assert.Equal(1, resultado.NovosCriados);
        var criado = await context.ItensFiscais.SingleAsync(f => f.Codigo == "9999");
        Assert.Equal(GrupoSomaId, criado.UnidadeNegocioId);
        Assert.Equal(OrigemInformacaoItemFiscal.Linx, criado.OrigemInformacao);
    }

    [Fact]
    public async Task ExecutarAsync_Should_Never_Persist_A_Warning_Occurrence_For_A_New_Code_Anymore()
    {
        await using var context = NewContext();
        var agora = DateTimeOffset.UtcNow;
        var execucao = SeedExecucaoCompleta(context, agora);
        context.RawLinxItensFiscaisSnapshot.Add(RawLinxItemFiscalRegistro.ParaTeste("8888", "Outro Item Novo", null, null, false, agora.UtcDateTime));
        await context.SaveChangesAsync();

        var occurrenceRepository = new IntegrationOccurrenceRepository(context);
        var useCase = new ProcessarItensFiscaisRawParaDominioUseCase(context, occurrenceRepository, NullLogger<ProcessarItensFiscaisRawParaDominioUseCase>.Instance);
        await useCase.ExecutarAsync(dryRun: false, GrupoSomaId, TimeProvider.System, CancellationToken.None);

        var ocorrencias = await occurrenceRepository.ListarPorExecucaoAsync(execucao, CancellationToken.None);
        Assert.Empty(ocorrencias);
    }

    [Fact]
    public async Task ExecutarAsync_Should_Never_Change_UnidadeNegocioId_Of_An_Existing_ItemFiscal()
    {
        await using var context = NewContext();
        var agora = DateTimeOffset.UtcNow;
        var outraBu = Guid.NewGuid();
        var existente = new ItemFiscal("7777", "Item Ja Existente", "UN", "1.1.01", outraBu, agora.AddDays(-30));
        context.ItensFiscais.Add(existente);
        SeedExecucaoCompleta(context, agora);
        context.RawLinxItensFiscaisSnapshot.Add(RawLinxItemFiscalRegistro.ParaTeste("7777", "Item Ja Existente - Atualizado No Linx", "UN", "1.1.01", false, agora.UtcDateTime));
        await context.SaveChangesAsync();

        var useCase = new ProcessarItensFiscaisRawParaDominioUseCase(context, new IntegrationOccurrenceRepository(context), NullLogger<ProcessarItensFiscaisRawParaDominioUseCase>.Instance);
        await useCase.ExecutarAsync(dryRun: false, GrupoSomaId, TimeProvider.System, CancellationToken.None);

        var atualizado = await context.ItensFiscais.SingleAsync(f => f.Codigo == "7777");
        Assert.Equal(outraBu, atualizado.UnidadeNegocioId);
        Assert.Equal("Item Ja Existente - Atualizado No Linx", atualizado.Descricao);
    }

    private static Guid SeedExecucaoCompleta(BlueprintOSDbContext context, DateTimeOffset agora)
    {
        var execucao = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), Dataset, RawLoadMode.Full, agora.AddMinutes(-5));
        execucao.Concluir(agora, completa: true, linhasLidas: 1, linhasGravadas: 1, isolamentoUtilizado: "READ UNCOMMITTED", erro: null);
        context.RawLinxFornecedoresSnapshotExecucoes.Add(execucao);
        return execucao.Id;
    }

    private static BlueprintOSDbContext NewContext() =>
        new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
