using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Application.Procurement.Negotiations.Models;

/// <summary>Resultado consultivo retornado pelo caso de uso de negociação.</summary>
public sealed record NegotiationRecommendationResult(
    string RequestId,
    NegotiationRecommendation Recommendation,
    bool UsesHistoricalData);
