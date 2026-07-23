using BlueprintOS.Core.AI.Memory.Models;
using BlueprintOS.Infrastructure.Memory;

namespace BlueprintOS.UnitTests.Infrastructure.Memory;

public class InMemoryNegotiationMemoryStoreTests
{
    [Fact]
    public void GetSupplierHistory_Should_Return_Null_When_Not_Saved()
    {
        var store = new InMemoryNegotiationMemoryStore();

        Assert.Null(store.GetSupplierHistory(Guid.NewGuid()));
    }

    [Fact]
    public void SaveSupplierHistory_Should_Return_A_Defensive_Copy_On_Read()
    {
        var store = new InMemoryNegotiationMemoryStore();
        var supplierId = Guid.NewGuid();
        var history = new SupplierHistory { SupplierId = supplierId, SupplierName = "Fornecedor A", LastPrice = 100m };

        store.SaveSupplierHistory(history);
        history.LastPrice = 999m;

        var persisted = store.GetSupplierHistory(supplierId);

        Assert.NotNull(persisted);
        Assert.Equal(100m, persisted!.LastPrice);
    }

    [Fact]
    public void GetOrCreateScoringMetrics_Should_Return_New_Instance_When_Not_Saved()
    {
        var store = new InMemoryNegotiationMemoryStore();
        var supplierId = Guid.NewGuid();

        var metrics = store.GetOrCreateScoringMetrics(supplierId);

        Assert.Equal(supplierId, metrics.SupplierId);
        Assert.Equal(0, metrics.DeliveryCount);
    }

    [Fact]
    public void AddPriceHistory_And_GetPriceHistory_Should_Accumulate_Records_Per_Product()
    {
        var store = new InMemoryNegotiationMemoryStore();
        var productId = Guid.NewGuid();

        store.AddPriceHistory(new PriceHistory { ProductId = productId, Price = 10m, Date = DateTime.UtcNow });
        store.AddPriceHistory(new PriceHistory { ProductId = productId, Price = 20m, Date = DateTime.UtcNow });

        var history = store.GetPriceHistory(productId);

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public void LinkSupplierToProduct_And_GetSuppliersForProduct_Should_Track_Distinct_Suppliers()
    {
        var store = new InMemoryNegotiationMemoryStore();
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        store.LinkSupplierToProduct(productId, supplierId);
        store.LinkSupplierToProduct(productId, supplierId);

        var suppliers = store.GetSuppliersForProduct(productId);

        Assert.Single(suppliers);
        Assert.Contains(supplierId, suppliers);
    }
}
