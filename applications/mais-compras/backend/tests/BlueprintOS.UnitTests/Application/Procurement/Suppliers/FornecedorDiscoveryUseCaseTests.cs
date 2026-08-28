using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class FornecedorDiscoveryUseCaseTests
{
    [Theory]
    [InlineData(true, false, false, false, 100, "ItemExato")]
    [InlineData(false, true, false, false, 80, "Familia")]
    [InlineData(false, false, true, false, 60, "Categoria")]
    [InlineData(false, false, false, true, 40, "Historico")]
    public void Score_Should_Use_First_Matching_Criterion(bool item, bool family, bool category, bool history, decimal expected, string criterion)
    {
        Assert.Equal(expected, ScoreFornecedor.Calcular(item, family, category, history));
        Assert.Equal(criterion, ScoreFornecedor.DeterminarCriterio(item, family, category, history));
    }

    [Fact]
    public async Task Discover_Should_Read_ERP_Score_And_Persist_Each_Result()
    {
        var identity = new FakeIdentity(); var erp = new FakeErpRepository(); var persistence = new FakeDiscoveryRepository();
        var result = await new DescobrirFornecedoresUseCase(erp, persistence, identity)
            .ExecuteAsync(new DescobrirFornecedoresDto("SKU-1", "Camiseta básica", "Camisetas"));

        Assert.Equal(2, result.Count);
        Assert.Equal(100, result[0].Score);
        Assert.Equal(80, result[1].Score);
        Assert.All(result, x => Assert.Equal(identity.UserId, x.TemporaryUserId));
        Assert.Equal(2, persistence.Items.Count);
    }

    [Fact]
    public async Task Discover_Should_Reject_Missing_Item_And_Context()
    {
        var useCase = new DescobrirFornecedoresUseCase(new FakeErpRepository(), new FakeDiscoveryRepository(), new FakeIdentity());
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new("", null, null)));
    }

    private sealed class FakeIdentity : ICurrentIdentity { public Guid UserId { get; } = Guid.NewGuid(); public RequestIdentity GetRequired() => new(UserId, "Buyer"); }
    private sealed class FakeErpRepository : IErpFornecedorDiscoveryRepository
    {
        public Task<IReadOnlyList<ErpFornecedorCandidate>> DescobrirAsync(FornecedorDiscoveryQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ErpFornecedorCandidate>>([
                new("Fornecedor exato", "123", "F1", true, false, false, false),
                new("Fornecedor família", "456", "F2", false, true, false, false)]);
    }
    private sealed class FakeDiscoveryRepository : IFornecedorDescobertoRepository
    {
        public List<FornecedorDescoberto> Items { get; } = [];
        public Task AdicionarAsync(FornecedorDescoberto descoberta, CancellationToken cancellationToken = default) { Items.Add(descoberta); return Task.CompletedTask; }
        public Task<IReadOnlyList<FornecedorDescoberto>> ListarAsync(Guid temporaryUserId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FornecedorDescoberto>>(Items.Where(x => x.TemporaryUserId == temporaryUserId).ToArray());
        public Task<FornecedorDescoberto?> ObterPorIdAsync(Guid id, Guid temporaryUserId, CancellationToken cancellationToken = default) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id && x.TemporaryUserId == temporaryUserId));
    }
}
