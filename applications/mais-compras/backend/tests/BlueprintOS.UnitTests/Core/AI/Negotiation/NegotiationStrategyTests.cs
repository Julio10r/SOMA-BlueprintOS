using BlueprintOS.Core.AI.Memory.Models;
using BlueprintOS.Core.AI.Negotiation;
using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;
using BlueprintOS.Core.AI.Negotiation.Rules;

namespace BlueprintOS.UnitTests.Core.AI.Negotiation;

public class NegotiationStrategyTests
{
    private static NegotiationStrategy CreateSut(NegotiationStrategyOptions? options = null)
    {
        var effectiveOptions = options ?? new NegotiationStrategyOptions();

        IEnumerable<INegotiationStrategyRule> rules =
        [
            new EmergencyUrgencyRule(),
            new PartnershipHighScoreRecurringRule(effectiveOptions),
            new CompetitiveExpensiveSupplierRule(effectiveOptions),
            new AggressivePriceAboveHistoryRule(effectiveOptions),
            new BalancedNewSupplierRule(),
            new ConservativeFallbackRule(),
        ];

        return new NegotiationStrategy(rules, effectiveOptions);
    }

    private static NegotiationContext CreateContext(
        decimal currentPrice = 100m,
        decimal? historicalBestPrice = 100m,
        double supplierScore = 50,
        int leadTime = 10,
        double sla = 80,
        decimal purchaseValue = 1000m,
        bool isCriticalItem = false,
        bool isRecurringPurchase = false,
        int numberOfSuppliers = 1,
        decimal? budgetLimit = null,
        NegotiationUrgencyLevel urgencyLevel = NegotiationUrgencyLevel.Normal,
        int negotiationCount = 5,
        PriceTrend priceTrend = PriceTrend.Stable)
        => new()
        {
            SupplierId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            CurrentPrice = currentPrice,
            HistoricalBestPrice = historicalBestPrice,
            SupplierScore = supplierScore,
            LeadTime = leadTime,
            Sla = sla,
            PurchaseValue = purchaseValue,
            IsCriticalItem = isCriticalItem,
            IsRecurringPurchase = isRecurringPurchase,
            NumberOfSuppliers = numberOfSuppliers,
            BudgetLimit = budgetLimit,
            UrgencyLevel = urgencyLevel,
            NegotiationCount = negotiationCount,
            PriceTrend = priceTrend,
        };

    [Fact]
    public void Evaluate_Should_Throw_When_Context_Is_Null()
    {
        var sut = CreateSut();

        Assert.Throws<ArgumentNullException>(() => sut.Evaluate(null!));
    }

    [Fact]
    public void Evaluate_Should_Recommend_Emergency_When_Urgency_Is_Critical()
    {
        var sut = CreateSut();
        var context = CreateContext(urgencyLevel: NegotiationUrgencyLevel.Critical);

        var recommendation = sut.Evaluate(context);

        Assert.Equal(NegotiationStrategyType.Emergency, recommendation.Strategy);
    }

    [Fact]
    public void Evaluate_Should_Recommend_Emergency_When_Urgency_Is_High()
    {
        var sut = CreateSut();
        var context = CreateContext(urgencyLevel: NegotiationUrgencyLevel.High);

        var recommendation = sut.Evaluate(context);

        Assert.Equal(NegotiationStrategyType.Emergency, recommendation.Strategy);
    }

    [Fact]
    public void Evaluate_Should_Recommend_Partnership_For_High_Score_Recurring_Supplier()
    {
        var sut = CreateSut();
        var context = CreateContext(supplierScore: 90, isRecurringPurchase: true);

        var recommendation = sut.Evaluate(context);

        Assert.Equal(NegotiationStrategyType.Partnership, recommendation.Strategy);
    }

    [Fact]
    public void Evaluate_Should_Recommend_Competitive_For_Expensive_Supplier_With_Many_Competitors()
    {
        var sut = CreateSut();
        var context = CreateContext(currentPrice: 115m, historicalBestPrice: 100m, numberOfSuppliers: 5);

        var recommendation = sut.Evaluate(context);

        Assert.Equal(NegotiationStrategyType.Competitive, recommendation.Strategy);
    }

    [Fact]
    public void Evaluate_Should_Recommend_Aggressive_When_Price_Is_Far_Above_History()
    {
        var sut = CreateSut();
        var context = CreateContext(currentPrice: 130m, historicalBestPrice: 100m, numberOfSuppliers: 1);

        var recommendation = sut.Evaluate(context);

        Assert.Equal(NegotiationStrategyType.Aggressive, recommendation.Strategy);
    }

    [Fact]
    public void Evaluate_Should_Recommend_Balanced_For_New_Supplier()
    {
        var sut = CreateSut();
        var context = CreateContext(negotiationCount: 0, historicalBestPrice: null);

        var recommendation = sut.Evaluate(context);

        Assert.Equal(NegotiationStrategyType.Balanced, recommendation.Strategy);
    }

    [Fact]
    public void Evaluate_Should_Recommend_Conservative_As_Fallback()
    {
        var sut = CreateSut();
        var context = CreateContext(
            currentPrice: 100m,
            historicalBestPrice: 100m,
            supplierScore: 50,
            isRecurringPurchase: false,
            numberOfSuppliers: 1,
            urgencyLevel: NegotiationUrgencyLevel.Normal,
            negotiationCount: 5);

        var recommendation = sut.Evaluate(context);

        Assert.Equal(NegotiationStrategyType.Conservative, recommendation.Strategy);
    }

    [Fact]
    public void Evaluate_Should_Prioritize_Emergency_Over_Any_Other_Matching_Rule()
    {
        var sut = CreateSut();
        var context = CreateContext(
            supplierScore: 90,
            isRecurringPurchase: true,
            urgencyLevel: NegotiationUrgencyLevel.Critical);

        var recommendation = sut.Evaluate(context);

        Assert.Equal(NegotiationStrategyType.Emergency, recommendation.Strategy);
    }

    [Fact]
    public void Evaluate_Should_Calculate_Target_Price_Below_Historical_Best_For_Aggressive_Strategy()
    {
        var sut = CreateSut();
        var context = CreateContext(currentPrice: 150m, historicalBestPrice: 100m, supplierScore: 0);

        var recommendation = sut.Evaluate(context);

        Assert.Equal(NegotiationStrategyType.Aggressive, recommendation.Strategy);
        Assert.True(recommendation.TargetPrice < context.HistoricalBestPrice);
        Assert.True(recommendation.TargetPrice > 0);
    }

    [Fact]
    public void Evaluate_Should_Calculate_Higher_Target_Price_When_Trend_Is_Increasing()
    {
        var sut = CreateSut();
        var stableContext = CreateContext(priceTrend: PriceTrend.Stable);
        var increasingContext = CreateContext(priceTrend: PriceTrend.Increasing);

        var stableRecommendation = sut.Evaluate(stableContext);
        var increasingRecommendation = sut.Evaluate(increasingContext);

        Assert.True(increasingRecommendation.TargetPrice > stableRecommendation.TargetPrice);
    }

    [Fact]
    public void Evaluate_Should_Calculate_Lower_Target_Price_When_Trend_Is_Decreasing()
    {
        var sut = CreateSut();
        var stableContext = CreateContext(priceTrend: PriceTrend.Stable);
        var decreasingContext = CreateContext(priceTrend: PriceTrend.Decreasing);

        var stableRecommendation = sut.Evaluate(stableContext);
        var decreasingRecommendation = sut.Evaluate(decreasingContext);

        Assert.True(decreasingRecommendation.TargetPrice < stableRecommendation.TargetPrice);
    }

    [Fact]
    public void Evaluate_Should_Calculate_Success_Probability_Within_Valid_Range()
    {
        var sut = CreateSut();
        var context = CreateContext();

        var recommendation = sut.Evaluate(context);

        Assert.InRange(recommendation.SuccessProbability, 0, 100);
    }

    [Fact]
    public void Evaluate_Should_Calculate_Higher_Success_Probability_For_Better_Supplier_Conditions()
    {
        var sut = CreateSut();
        var goodContext = CreateContext(supplierScore: 95, sla: 95, leadTime: 2, negotiationCount: 10);
        var badContext = CreateContext(supplierScore: 20, sla: 20, leadTime: 40, negotiationCount: 0);

        var goodRecommendation = sut.Evaluate(goodContext);
        var badRecommendation = sut.Evaluate(badContext);

        Assert.True(goodRecommendation.SuccessProbability > badRecommendation.SuccessProbability);
    }

    [Fact]
    public void Evaluate_Should_Include_Critical_Item_Note_When_Item_Is_Critical()
    {
        var sut = CreateSut();
        var context = CreateContext(isCriticalItem: true);

        var recommendation = sut.Evaluate(context);

        Assert.Contains(recommendation.Notes, note => note.Contains("crítico", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_Should_Return_Non_Empty_Justification()
    {
        var sut = CreateSut();
        var context = CreateContext();

        var recommendation = sut.Evaluate(context);

        Assert.False(string.IsNullOrWhiteSpace(recommendation.Justification));
    }

    [Fact]
    public void Constructor_Should_Throw_When_No_Rules_Are_Provided()
    {
        Assert.Throws<ArgumentException>(() => new NegotiationStrategy([], new NegotiationStrategyOptions()));
    }
}
