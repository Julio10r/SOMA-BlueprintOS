using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Negotiations;
using BlueprintOS.Application.Procurement.Negotiations.Models;
using BlueprintOS.Core.AI.Memory.Contracts;
using BlueprintOS.Core.AI.Memory.Models;
using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.UnitTests.Application.Procurement.Negotiations;

public sealed class NegotiationRecommendationUseCaseTests
{
    [Fact]
    public void Execute_Should_Build_Context_From_Existing_Contracts_And_Return_Consultative_Recommendation()
    {
        var supplierId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var memory = new FakeNegotiationMemory(supplierId, productId);
        var strategy = new CapturingStrategy();
        var useCase = new NegotiationRecommendationUseCase(new FakeIdentity(), memory, strategy);
        var command = new NegotiationRecommendationCommand(
            "request-123", supplierId, productId, 120m, 7, 95, 1200m, false, true, 3, 1250m, NegotiationUrgencyLevel.Normal);

        var result = useCase.Execute(command);

        Assert.Equal("request-123", result.RequestId);
        Assert.True(result.UsesHistoricalData);
        Assert.True(result.Recommendation.Strategy is NegotiationStrategyType.Competitive);
        Assert.NotNull(strategy.Context);
        Assert.Equal(95m, strategy.Context!.HistoricalBestPrice);
        Assert.Equal(2, strategy.Context.NegotiationCount);
        Assert.Equal(PriceTrend.Decreasing, strategy.Context.PriceTrend);
    }

    private sealed class FakeIdentity : ICurrentIdentity
    {
        public RequestIdentity GetRequired() => new(Guid.NewGuid(), "Buyer");
    }

    private sealed class CapturingStrategy : INegotiationStrategy
    {
        public NegotiationContext? Context { get; private set; }

        public NegotiationRecommendation Evaluate(NegotiationContext context)
        {
            Context = context;
            return new NegotiationRecommendation(NegotiationStrategyType.Competitive, 95m, 20, "Histórico disponível.", NegotiationRiskLevel.Medium, 75, ["Avaliar concorrência."]);
        }
    }

    private sealed class FakeNegotiationMemory(Guid supplierId, Guid productId) : INegotiationMemory
    {
        private readonly SupplierHistory _history = new() { SupplierId = supplierId, NegotiationCount = 2, CurrentScore = 88 };

        public void RegisterNegotiation(NegotiationRecord negotiation) { }
        public SupplierHistory? GetSupplierHistory(Guid id) => id == supplierId ? _history : null;
        public IReadOnlyCollection<PriceHistory> GetPriceHistory(Guid id) => Array.Empty<PriceHistory>();
        public double CalculateSupplierScore(Guid id) => id == supplierId ? 88 : 0;
        public SupplierHistory? FindBestSupplier(Guid id) => null;
        public decimal? FindBestHistoricalPrice(Guid id) => id == productId ? 95m : null;
        public PriceTrend GetPriceTrend(Guid id) => id == productId ? PriceTrend.Decreasing : PriceTrend.Stable;
    }
}
