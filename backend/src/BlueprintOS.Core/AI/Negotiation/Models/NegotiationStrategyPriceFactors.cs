namespace BlueprintOS.Core.AI.Negotiation.Models;

/// <summary>
/// Fatores multiplicadores aplicados ao preço base calculado para cada estratégia de
/// negociação, refletindo o quanto cada postura pressiona o preço para baixo (ou,
/// no caso de urgência, aceita pagar um prêmio).
/// </summary>
public sealed class NegotiationStrategyPriceFactors
{
    /// <summary>
    /// Fator aplicado na estratégia <see cref="NegotiationStrategyType.Aggressive"/>.
    /// </summary>
    public double Aggressive { get; init; } = 0.85;

    /// <summary>
    /// Fator aplicado na estratégia <see cref="NegotiationStrategyType.Balanced"/>.
    /// </summary>
    public double Balanced { get; init; } = 0.95;

    /// <summary>
    /// Fator aplicado na estratégia <see cref="NegotiationStrategyType.Conservative"/>.
    /// </summary>
    public double Conservative { get; init; } = 0.98;

    /// <summary>
    /// Fator aplicado na estratégia <see cref="NegotiationStrategyType.Partnership"/>.
    /// </summary>
    public double Partnership { get; init; } = 1.00;

    /// <summary>
    /// Fator aplicado na estratégia <see cref="NegotiationStrategyType.Competitive"/>.
    /// </summary>
    public double Competitive { get; init; } = 0.90;

    /// <summary>
    /// Fator aplicado na estratégia <see cref="NegotiationStrategyType.Emergency"/>.
    /// </summary>
    public double Emergency { get; init; } = 1.05;

    /// <summary>
    /// Obtém o fator correspondente à estratégia informada.
    /// </summary>
    /// <param name="strategy">Estratégia de negociação escolhida.</param>
    public double GetFactor(NegotiationStrategyType strategy) => strategy switch
    {
        NegotiationStrategyType.Aggressive => Aggressive,
        NegotiationStrategyType.Balanced => Balanced,
        NegotiationStrategyType.Conservative => Conservative,
        NegotiationStrategyType.Partnership => Partnership,
        NegotiationStrategyType.Competitive => Competitive,
        NegotiationStrategyType.Emergency => Emergency,
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Estratégia de negociação desconhecida."),
    };
}
