using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Core.AI.Negotiation.Contracts;

/// <summary>
/// Define o contrato da engine de estratégia de negociação utilizada pelo agente
/// comprador sênior para decidir automaticamente como negociar com um fornecedor.
/// </summary>
public interface INegotiationStrategy
{
    /// <summary>
    /// Avalia o contexto de compra informado e retorna a recomendação de negociação
    /// mais adequada, incluindo estratégia, preço alvo e probabilidade de sucesso.
    /// </summary>
    /// <param name="context">Contexto da compra a ser negociada.</param>
    NegotiationRecommendation Evaluate(NegotiationContext context);
}
