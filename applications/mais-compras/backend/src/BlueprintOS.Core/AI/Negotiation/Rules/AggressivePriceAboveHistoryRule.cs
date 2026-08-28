using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Core.AI.Negotiation.Rules;

/// <summary>
/// Recomenda a estratégia <see cref="NegotiationStrategyType.Aggressive"/> quando o
/// preço atual está muito acima do melhor preço já registrado no histórico do produto.
/// </summary>
public sealed class AggressivePriceAboveHistoryRule : INegotiationStrategyRule
{
    private readonly NegotiationStrategyOptions _options;

    /// <summary>
    /// Inicializa a regra com as opções de configuração da engine de estratégia.
    /// </summary>
    /// <param name="options">Opções contendo o limiar de desvio de preço considerado agressivo.</param>
    public AggressivePriceAboveHistoryRule(NegotiationStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public int Priority => 40;

    /// <inheritdoc />
    public NegotiationStrategyType Strategy => NegotiationStrategyType.Aggressive;

    /// <inheritdoc />
    public bool Matches(NegotiationContext context)
    {
        if (context.HistoricalBestPrice is not { } bestPrice || bestPrice <= 0)
        {
            return false;
        }

        var deviation = (double)((context.CurrentPrice - bestPrice) / bestPrice);
        return deviation >= _options.AggressivePriceDeviationThreshold;
    }

    /// <inheritdoc />
    public string BuildJustification(NegotiationContext context)
        => $"Preço atual ({context.CurrentPrice:F2}) muito acima do melhor preço histórico ({context.HistoricalBestPrice:F2}): adotar postura agressiva.";
}
