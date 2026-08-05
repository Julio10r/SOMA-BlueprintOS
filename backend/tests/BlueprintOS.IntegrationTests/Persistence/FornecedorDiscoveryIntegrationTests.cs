using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.IntegrationTests.Persistence;

public sealed class FornecedorDiscoveryIntegrationTests
{
    [Fact]
    public async Task Discovery_Should_Persist_And_Isolate_By_Temporary_User()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorDescobertoRepository(context); var identity = new FakeIdentity();
        var useCase = new DescobrirFornecedoresUseCase(new FakeErpRepository(), repository, identity);

        var result = await useCase.ExecuteAsync(new DescobrirFornecedoresDto("SKU-99", "Calça", "Calças"));

        Assert.Single(result);
        Assert.Single(await repository.ListarAsync(identity.UserId));
        Assert.Empty(await repository.ListarAsync(Guid.NewGuid()));
        Assert.NotNull(await repository.ObterPorIdAsync(result[0].Id, identity.UserId));
    }

    private sealed class FakeIdentity : ICurrentIdentity { public Guid UserId { get; } = Guid.NewGuid(); public RequestIdentity GetRequired() => new(UserId, "Buyer"); }
    private sealed class FakeErpRepository : IErpFornecedorDiscoveryRepository
    {
        public Task<IReadOnlyList<ErpFornecedorCandidate>> DescobrirAsync(FornecedorDiscoveryQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ErpFornecedorCandidate>>([new("Fornecedor ERP", "123", "ERP-1", true, false, false, false)]);
    }
}
