using BlueprintOS.Application.Procurement.Negotiations.Models;
using BlueprintOS.Core.AI.Negotiation.Models;

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
    public static NegotiationRecommendationResponse From(
        NegotiationRecommendation recommendation,
        bool usesHistoricalData) =>
        From(
            string.Empty,
            recommendation,
            usesHistoricalData);

    public static NegotiationRecommendationResponse From(
        NegotiationRecommendationResult result) =>
        From(
            result.RequestId,
            result.Recommendation,
            result.UsesHistoricalData);

    public static NegotiationRecommendationResponse From(
        string requestId,
        NegotiationRecommendation recommendation,
        bool usesHistoricalData) => new(
            RequestId: requestId,
            TargetPrice: recommendation.TargetPrice,
            ExpectedDiscountPercentage: recommendation.ExpectedDiscountPercentage,
            Strategy: recommendation.Strategy.ToString(),
            Justifications: new[] { recommendation.Justification },
            Alerts: recommendation.Notes,
            EstimatedRisk: recommendation.EstimatedRisk.ToString(),
            SuccessProbability: recommendation.SuccessProbability,
            UsesHistoricalData: usesHistoricalData,
            HumanDecisionRequired: true);
}