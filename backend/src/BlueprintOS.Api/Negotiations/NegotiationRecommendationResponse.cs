using BlueprintOS.Application.Procurement.Negotiations.Models;

namespace BlueprintOS.Api.Negotiations;

/// <summary>Resposta HTTP consultiva da recomendação de negociação.</summary>
public sealed record NegotiationRecommendationResponse(
    string RequestId,
    decimal TargetPrice,
    double ExpectedDiscountPercentage,
    string Strategy,
    IReadOnlyCollection<string> Justifications,
    IReadOnlyCollection<string> Alerts,
    string EstimatedRisk,
    double? SuccessProbability,
    bool UsesHistoricalData,
    bool HumanDecisionRequired)
{
    public static NegotiationRecommendationResponse From(NegotiationRecommendationResult result) => new(
        result.RequestId,
        result.Recommendation.TargetPrice,
        result.Recommendation.ExpectedDiscountPercentage,
        result.Recommendation.Strategy.ToString(),
        [result.Recommendation.Justification],
        result.Recommendation.Notes,
        result.Recommendation.EstimatedRisk.ToString(),
        result.Recommendation.SuccessProbability,
        result.UsesHistoricalData,
        HumanDecisionRequired: true);
}
