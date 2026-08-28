using BlueprintOS.Application.Procurement.Negotiations.Models;

namespace BlueprintOS.Application.Procurement.Negotiations.Contracts;

/// <summary>Orquestra a recomendação consultiva de negociação do +COMPRAS.</summary>
public interface INegotiationRecommendationUseCase
{
    NegotiationRecommendationResult Execute(NegotiationRecommendationCommand command);
}
