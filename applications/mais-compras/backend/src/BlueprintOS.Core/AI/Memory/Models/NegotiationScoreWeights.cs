namespace BlueprintOS.Core.AI.Memory.Models;

/// <summary>
/// Pesos configuráveis utilizados no cálculo do score de um fornecedor. Centraliza os
/// fatores considerados para evitar números mágicos espalhados pela lógica de cálculo.
/// </summary>
public sealed class NegotiationScoreWeights
{
    /// <summary>
    /// Peso do componente de competitividade de preço.
    /// </summary>
    public double Price { get; init; } = 0.30;

    /// <summary>
    /// Peso do componente de prazo de entrega (lead time).
    /// </summary>
    public double LeadTime { get; init; } = 0.15;

    /// <summary>
    /// Peso do componente de SLA.
    /// </summary>
    public double Sla { get; init; } = 0.15;

    /// <summary>
    /// Peso do componente de volume entregue.
    /// </summary>
    public double DeliveredQuantity { get; init; } = 0.10;

    /// <summary>
    /// Peso do componente de histórico de negociações (recorrência).
    /// </summary>
    public double NegotiationHistory { get; init; } = 0.10;

    /// <summary>
    /// Peso do componente de pontualidade (ausência de atrasos).
    /// </summary>
    public double Delays { get; init; } = 0.10;

    /// <summary>
    /// Peso do componente de qualidade.
    /// </summary>
    public double Quality { get; init; } = 0.10;

    /// <summary>
    /// Soma de todos os pesos configurados, utilizada para normalizar o score final.
    /// </summary>
    public double Total => Price + LeadTime + Sla + DeliveredQuantity + NegotiationHistory + Delays + Quality;
}
