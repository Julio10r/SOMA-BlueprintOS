namespace BlueprintOS.Core.AI.Negotiation.Models;

/// <summary>
/// Pesos configuráveis utilizados no cálculo da probabilidade de sucesso de uma
/// negociação. Centraliza os fatores considerados para evitar números mágicos
/// espalhados pela lógica de cálculo.
/// </summary>
public sealed class NegotiationSuccessProbabilityWeights
{
    /// <summary>
    /// Peso do componente de score do fornecedor.
    /// </summary>
    public double Score { get; init; } = 0.30;

    /// <summary>
    /// Peso do componente de SLA do fornecedor.
    /// </summary>
    public double Sla { get; init; } = 0.20;

    /// <summary>
    /// Peso do componente de prazo de entrega (lead time).
    /// </summary>
    public double LeadTime { get; init; } = 0.15;

    /// <summary>
    /// Peso do componente de histórico de negociações (recorrência) com o fornecedor.
    /// </summary>
    public double History { get; init; } = 0.10;

    /// <summary>
    /// Peso do componente de probabilidade base da estratégia escolhida.
    /// </summary>
    public double StrategyBaseline { get; init; } = 0.25;

    /// <summary>
    /// Soma de todos os pesos configurados, utilizada para normalizar o resultado final.
    /// </summary>
    public double Total => Score + Sla + LeadTime + History + StrategyBaseline;
}
