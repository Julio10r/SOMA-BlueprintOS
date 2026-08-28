using BlueprintOS.Core.AI.Memory.Models;
using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Core.AI.Negotiation;

/// <summary>
/// Implementação de <see cref="INegotiationStrategy"/> que decide automaticamente a
/// postura de negociação a adotar com um fornecedor, a partir de uma engine de regras
/// extensível, e calcula o preço alvo e a probabilidade de sucesso da negociação.
/// </summary>
public sealed class NegotiationStrategy : INegotiationStrategy
{
    private readonly IReadOnlyList<INegotiationStrategyRule> _rules;
    private readonly NegotiationStrategyOptions _options;

    /// <summary>
    /// Inicializa a engine de estratégia de negociação com o conjunto de regras e as
    /// opções de cálculo a serem utilizados.
    /// </summary>
    /// <param name="rules">
    /// Regras que compõem a engine, avaliadas em ordem crescente de <see cref="INegotiationStrategyRule.Priority"/>.
    /// Deve conter ao menos uma regra que sempre corresponda ao contexto (regra de fallback).
    /// </param>
    /// <param name="options">Limiares e fatores utilizados pelas regras e pelos cálculos de preço e probabilidade.</param>
    public NegotiationStrategy(IEnumerable<INegotiationStrategyRule> rules, NegotiationStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(options);

        _rules = rules.OrderBy(rule => rule.Priority).ToList();
        _options = options;

        if (_rules.Count == 0)
        {
            throw new ArgumentException("A engine de estratégia de negociação exige ao menos uma regra.", nameof(rules));
        }
    }

    /// <inheritdoc />
    public NegotiationRecommendation Evaluate(NegotiationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rule = _rules.FirstOrDefault(r => r.Matches(context))
            ?? throw new InvalidOperationException("Nenhuma regra de negociação correspondeu ao contexto informado.");

        var strategy = rule.Strategy;
        var targetPrice = CalculateTargetPrice(context, strategy);
        var expectedDiscount = CalculateExpectedDiscountPercentage(context.CurrentPrice, targetPrice);
        var successProbability = CalculateSuccessProbability(context, strategy);
        var risk = EstimateRisk(strategy, context);
        var justification = rule.BuildJustification(context);
        var notes = BuildNotes(context, strategy);

        return new NegotiationRecommendation(
            strategy,
            targetPrice,
            expectedDiscount,
            justification,
            risk,
            successProbability,
            notes);
    }

    private decimal CalculateTargetPrice(NegotiationContext context, NegotiationStrategyType strategy)
    {
        var basePrice = context.HistoricalBestPrice ?? context.CurrentPrice;
        if (basePrice <= 0)
        {
            return 0m;
        }

        var inflationAdjusted = basePrice * (1 + (decimal)_options.InflationRatePercentage);
        var trendAdjusted = ApplyTrendAdjustment(inflationAdjusted, context.PriceTrend);

        var scoreFactor = 1 + (decimal)(context.SupplierScore / 100 * _options.ScoreInfluencePercentage);
        var strategyFactor = (decimal)_options.PriceFactors.GetFactor(strategy);

        var targetPrice = trendAdjusted * scoreFactor * strategyFactor;
        return Math.Max(targetPrice, 0m);
    }

    private decimal ApplyTrendAdjustment(decimal price, PriceTrend trend) => trend switch
    {
        PriceTrend.Increasing => price * (1 + (decimal)_options.TrendAdjustmentPercentage),
        PriceTrend.Decreasing => price * (1 - (decimal)_options.TrendAdjustmentPercentage),
        _ => price,
    };

    private static double CalculateExpectedDiscountPercentage(decimal currentPrice, decimal targetPrice)
    {
        if (currentPrice <= 0)
        {
            return 0d;
        }

        return (double)((currentPrice - targetPrice) / currentPrice) * 100;
    }

    private double CalculateSuccessProbability(NegotiationContext context, NegotiationStrategyType strategy)
    {
        var weights = _options.ProbabilityWeights;
        var totalWeight = weights.Total;
        if (totalWeight <= 0)
        {
            return 0d;
        }

        var scoreComponent = Math.Clamp(context.SupplierScore, 0, 100);
        var slaComponent = Math.Clamp(context.Sla, 0, 100);
        var leadTimeComponent = CalculateLeadTimeComponent(context.LeadTime);
        var historyComponent = CalculateHistoryComponent(context.NegotiationCount);
        var strategyBaselineComponent = Math.Clamp(_options.SuccessBaselines.GetBaseline(strategy), 0, 100);

        var weightedSum =
            weights.Score * scoreComponent +
            weights.Sla * slaComponent +
            weights.LeadTime * leadTimeComponent +
            weights.History * historyComponent +
            weights.StrategyBaseline * strategyBaselineComponent;

        return Math.Clamp(weightedSum / totalWeight, 0, 100);
    }

    private double CalculateLeadTimeComponent(int leadTime)
    {
        if (_options.MaxAcceptableLeadTimeDays <= 0)
        {
            return 0;
        }

        var score = 100 - leadTime / _options.MaxAcceptableLeadTimeDays * 100;
        return Math.Clamp(score, 0, 100);
    }

    private double CalculateHistoryComponent(int negotiationCount)
    {
        if (_options.MaxRelevantNegotiationCountForHistory <= 0)
        {
            return 0;
        }

        var score = (double)negotiationCount / _options.MaxRelevantNegotiationCountForHistory * 100;
        return Math.Clamp(score, 0, 100);
    }

    private static NegotiationRiskLevel EstimateRisk(NegotiationStrategyType strategy, NegotiationContext context)
    {
        if (context.IsCriticalItem && strategy is NegotiationStrategyType.Aggressive or NegotiationStrategyType.Competitive)
        {
            return NegotiationRiskLevel.High;
        }

        return strategy switch
        {
            NegotiationStrategyType.Aggressive => NegotiationRiskLevel.High,
            NegotiationStrategyType.Competitive => NegotiationRiskLevel.Medium,
            NegotiationStrategyType.Emergency => context.IsCriticalItem ? NegotiationRiskLevel.Medium : NegotiationRiskLevel.Low,
            NegotiationStrategyType.Balanced => NegotiationRiskLevel.Medium,
            NegotiationStrategyType.Conservative => NegotiationRiskLevel.Low,
            NegotiationStrategyType.Partnership => NegotiationRiskLevel.Low,
            _ => NegotiationRiskLevel.Medium,
        };
    }

    private static IReadOnlyCollection<string> BuildNotes(NegotiationContext context, NegotiationStrategyType strategy)
    {
        var notes = new List<string>();

        if (context.IsCriticalItem)
        {
            notes.Add("Item crítico para a operação: avaliar risco de desabastecimento antes de pressionar o preço.");
        }

        if (context.NegotiationCount <= 0)
        {
            notes.Add("Sem histórico de negociações anteriores com este fornecedor.");
        }

        if (context.BudgetLimit is { } budgetLimit && context.PurchaseValue > budgetLimit)
        {
            notes.Add($"Valor da compra ({context.PurchaseValue:F2}) excede o limite de orçamento informado ({budgetLimit:F2}).");
        }

        if (strategy == NegotiationStrategyType.Emergency)
        {
            notes.Add("Estratégia de emergência: aceitar condições de preço menos favoráveis em troca de garantia de fornecimento.");
        }

        return notes;
    }
}
