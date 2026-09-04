using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.IntegrationTests.Persistence;

public sealed class FornecedorDiscoveryIntegrationTests
{
    /// <summary>B3 — Bloco 5A.9 (correção do resíduo arquitetural TemporaryUserId, decisão do Product
    /// Owner): descoberta de fornecedor é corporativa por CodigoItem — não pertence a quem a disparou.
    /// A persistência e a listagem não recebem mais nenhum "dono" como filtro.</summary>
    [Fact]
    public async Task Discovery_Should_Persist_And_Be_Visible_To_Any_Query()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorDescobertoRepository(context);
        var useCase = new DescobrirFornecedoresUseCase(new FakeErpRepository(), repository);

        var result = await useCase.ExecuteAsync(new DescobrirFornecedoresDto("SKU-99", "Calça", "Calças"));

        Assert.Single(result);
        Assert.Single(await repository.ListarAsync());
        Assert.NotNull(await repository.ObterPorIdAsync(result[0].Id));
    }

    private sealed class FakeErpRepository : IErpFornecedorDiscoveryRepository
    {
        public Task<IReadOnlyList<ErpFornecedorCandidate>> DescobrirAsync(FornecedorDiscoveryQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ErpFornecedorCandidate>>([new("Fornecedor ERP", "123", "ERP-1", true, false, false, false)]);
    }
}
