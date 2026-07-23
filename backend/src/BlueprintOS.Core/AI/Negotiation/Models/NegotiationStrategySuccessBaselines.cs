namespace BlueprintOS.Core.AI.Negotiation.Models;

/// <summary>
/// Probabilidade base de sucesso (0 a 100) associada a cada estratégia de negociação,
/// utilizada como um dos componentes do cálculo da probabilidade final de sucesso.
/// </summary>
public sealed class NegotiationStrategySuccessBaselines
{
    /// <summary>
    /// Probabilidade base para a estratégia <see cref="NegotiationStrategyType.Aggressive"/>.
    /// </summary>
    public double Aggressive { get; init; } = 45;

    /// <summary>
    /// Probabilidade base para a estratégia <see cref="NegotiationStrategyType.Balanced"/>.
    /// </summary>
    public double Balanced { get; init; } = 60;

    /// <summary>
    /// Probabilidade base para a estratégia <see cref="NegotiationStrategyType.Conservative"/>.
    /// </summary>
    public double Conservative { get; init; } = 55;

    /// <summary>
    /// Probabilidade base para a estratégia <see cref="NegotiationStrategyType.Partnership"/>.
    /// </summary>
    public double Partnership { get; init; } = 85;

    /// <summary>
    /// Probabilidade base para a estratégia <see cref="NegotiationStrategyType.Competitive"/>.
    /// </summary>
    public double Competitive { get; init; } = 65;

    /// <summary>
    /// Probabilidade base para a estratégia <see cref="NegotiationStrategyType.Emergency"/>.
    /// </summary>
    public double Emergency { get; init; } = 90;

    /// <summary>
    /// Obtém a probabilidade base correspondente à estratégia informada.
    /// </summary>
    /// <param name="strategy">Estratégia de negociação escolhida.</param>
    public double GetBaseline(NegotiationStrategyType strategy) => strategy switch
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
