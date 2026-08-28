namespace BlueprintOS.Core.AI.Negotiation.Models;

/// <summary>
/// Recomendação de negociação produzida pelo agente comprador sênior para um
/// determinado contexto de compra.
/// </summary>
/// <param name="Strategy">Estratégia de negociação escolhida.</param>
/// <param name="TargetPrice">Preço alvo a ser buscado na negociação.</param>
/// <param name="ExpectedDiscountPercentage">Percentual de desconto esperado em relação ao preço atual.</param>
/// <param name="Justification">Justificativa da escolha da estratégia, com base nas regras aplicadas.</param>
/// <param name="EstimatedRisk">Risco estimado de a negociação falhar ou comprometer o fornecimento.</param>
/// <param name="SuccessProbability">Probabilidade estimada (0 a 100) de sucesso da negociação.</param>
/// <param name="Notes">Observações adicionais relevantes para a condução da negociação.</param>
public sealed record NegotiationRecommendation(
    NegotiationStrategyType Strategy,
    decimal TargetPrice,
    double ExpectedDiscountPercentage,
    string Justification,
    NegotiationRiskLevel EstimatedRisk,
    double SuccessProbability,
    IReadOnlyCollection<string> Notes);
