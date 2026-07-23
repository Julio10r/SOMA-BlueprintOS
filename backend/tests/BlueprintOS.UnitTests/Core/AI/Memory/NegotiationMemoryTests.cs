using BlueprintOS.Core.AI.Memory;
using BlueprintOS.Core.AI.Memory.Models;
using BlueprintOS.Infrastructure.Memory;

namespace BlueprintOS.UnitTests.Core.AI.Memory;

public class NegotiationMemoryTests
{
    private static NegotiationMemory CreateSut(NegotiationScoreOptions? options = null)
        => new(new InMemoryNegotiationMemoryStore(), options ?? new NegotiationScoreOptions());

    private static NegotiationRecord CreateNegotiation(
        Guid productId,
        Guid supplierId,
        string supplierName = "Fornecedor A",
        decimal price = 100m,
        decimal listPrice = 100m,
        int deliveryDays = 10,
        int promisedDeliveryDays = 10,
        decimal quantityOrdered = 100m,
        decimal quantityDelivered = 100m,
        double slaScore = 90,
        double qualityScore = 90,
        DateTime? date = null,
        string? observations = null)
        => new(
            productId,
            supplierId,
            supplierName,
            price,
            listPrice,
            Freight: 10m,
            Taxes: 5m,
            deliveryDays,
            promisedDeliveryDays,
            quantityOrdered,
            quantityDelivered,
            slaScore,
            qualityScore,
            date ?? new DateTime(2026, 1, 1),
            Observations: observations);

    [Fact]
    public void GetSupplierHistory_Should_Return_Null_When_No_History_Exists()
    {
        var sut = CreateSut();

        var history = sut.GetSupplierHistory(Guid.NewGuid());

        Assert.Null(history);
    }

    [Fact]
    public void GetPriceHistory_Should_Return_Empty_Collection_When_No_History_Exists()
    {
        var sut = CreateSut();

        var history = sut.GetPriceHistory(Guid.NewGuid());

        Assert.Empty(history);
    }

    [Fact]
    public void FindBestSupplier_Should_Return_Null_When_No_Negotiation_Exists_For_Product()
    {
        var sut = CreateSut();

        var best = sut.FindBestSupplier(Guid.NewGuid());

        Assert.Null(best);
    }

    [Fact]
    public void FindBestHistoricalPrice_Should_Return_Null_When_No_History_Exists()
    {
        var sut = CreateSut();

        var bestPrice = sut.FindBestHistoricalPrice(Guid.NewGuid());

        Assert.Null(bestPrice);
    }

    [Fact]
    public void RegisterNegotiation_Should_Create_History_For_New_Supplier()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: 150m));

        var history = sut.GetSupplierHistory(supplierId);

        Assert.NotNull(history);
        Assert.Equal(1, history!.NegotiationCount);
        Assert.Equal(150m, history.LastPrice);
        Assert.Equal(150m, history.BestPrice);
        Assert.Equal(150m, history.WorstPrice);
    }

    [Fact]
    public void RegisterNegotiation_Should_Update_Existing_Supplier_History()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: 150m, deliveryDays: 10));
        sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: 100m, deliveryDays: 20));

        var history = sut.GetSupplierHistory(supplierId);

        Assert.NotNull(history);
        Assert.Equal(2, history!.NegotiationCount);
        Assert.Equal(100m, history.LastPrice);
        Assert.Equal(100m, history.BestPrice);
        Assert.Equal(150m, history.WorstPrice);
        Assert.Equal(15, history.AverageLeadTime);
        Assert.Equal(200m, history.TotalPurchased);
    }

    [Fact]
    public void RegisterNegotiation_Should_Append_Price_History_For_Product()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: 100m, date: new DateTime(2026, 1, 1)));
        sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: 110m, date: new DateTime(2026, 1, 2)));

        var priceHistory = sut.GetPriceHistory(productId);

        Assert.Equal(2, priceHistory.Count);
        Assert.Contains(priceHistory, p => p.Price == 100m);
        Assert.Contains(priceHistory, p => p.Price == 110m);
    }

    [Fact]
    public void CalculateSupplierScore_Should_Return_Zero_For_Unknown_Supplier()
    {
        var sut = CreateSut();

        var score = sut.CalculateSupplierScore(Guid.NewGuid());

        Assert.Equal(0, score);
    }

    [Fact]
    public void CalculateSupplierScore_Should_Reward_Better_Price_Lead_Time_And_Sla()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var goodSupplier = Guid.NewGuid();
        var badSupplier = Guid.NewGuid();

        sut.RegisterNegotiation(CreateNegotiation(
            productId, goodSupplier, price: 90m, deliveryDays: 5, promisedDeliveryDays: 5, slaScore: 95, qualityScore: 95));
        sut.RegisterNegotiation(CreateNegotiation(
            productId, badSupplier, price: 150m, deliveryDays: 25, promisedDeliveryDays: 10, slaScore: 40, qualityScore: 40));

        var goodScore = sut.CalculateSupplierScore(goodSupplier);
        var badScore = sut.CalculateSupplierScore(badSupplier);

        Assert.True(goodScore > badScore);
        Assert.InRange(goodScore, 0, 100);
        Assert.InRange(badScore, 0, 100);
    }

    [Fact]
    public void FindBestSupplier_Should_Return_Supplier_With_Highest_Score()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var goodSupplier = Guid.NewGuid();
        var badSupplier = Guid.NewGuid();

        sut.RegisterNegotiation(CreateNegotiation(
            productId, goodSupplier, supplierName: "Bom Fornecedor", price: 90m, deliveryDays: 5, promisedDeliveryDays: 5, slaScore: 95, qualityScore: 95));
        sut.RegisterNegotiation(CreateNegotiation(
            productId, badSupplier, supplierName: "Fornecedor Ruim", price: 150m, deliveryDays: 25, promisedDeliveryDays: 10, slaScore: 40, qualityScore: 40));

        var best = sut.FindBestSupplier(productId);

        Assert.NotNull(best);
        Assert.Equal(goodSupplier, best!.SupplierId);
    }

    [Fact]
    public void FindBestHistoricalPrice_Should_Return_Lowest_Recorded_Price()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: 120m, date: new DateTime(2026, 1, 1)));
        sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: 95m, date: new DateTime(2026, 1, 2)));
        sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: 110m, date: new DateTime(2026, 1, 3)));

        var bestPrice = sut.FindBestHistoricalPrice(productId);

        Assert.Equal(95m, bestPrice);
    }

    [Fact]
    public void GetPriceTrend_Should_Return_Stable_When_Not_Enough_History()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: 100m, date: new DateTime(2026, 1, 1)));

        var trend = sut.GetPriceTrend(productId);

        Assert.Equal(PriceTrend.Stable, trend);
    }

    [Fact]
    public void GetPriceTrend_Should_Return_Increasing_For_Consistently_Rising_Prices()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        decimal[] prices = [100m, 103m, 105m, 108m];

        for (var i = 0; i < prices.Length; i++)
        {
            sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: prices[i], date: new DateTime(2026, 1, 1).AddDays(i)));
        }

        var trend = sut.GetPriceTrend(productId);

        Assert.Equal(PriceTrend.Increasing, trend);
    }

    [Fact]
    public void GetPriceTrend_Should_Return_Decreasing_For_Consistently_Falling_Prices()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        decimal[] prices = [100m, 98m, 95m, 92m];

        for (var i = 0; i < prices.Length; i++)
        {
            sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: prices[i], date: new DateTime(2026, 1, 1).AddDays(i)));
        }

        var trend = sut.GetPriceTrend(productId);

        Assert.Equal(PriceTrend.Decreasing, trend);
    }

    [Fact]
    public void GetPriceTrend_Should_Return_Stable_For_Minor_Oscillations()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        decimal[] prices = [100m, 101m, 99m, 100m];

        for (var i = 0; i < prices.Length; i++)
        {
            sut.RegisterNegotiation(CreateNegotiation(productId, supplierId, price: prices[i], date: new DateTime(2026, 1, 1).AddDays(i)));
        }

        var trend = sut.GetPriceTrend(productId);

        Assert.Equal(PriceTrend.Stable, trend);
    }
}
