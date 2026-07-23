using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Core.AI.Negotiation.Rules;

/// <summary>
/// Recomenda a estratégia <see cref="NegotiationStrategyType.Competitive"/> quando o
/// fornecedor está caro em relação ao histórico do produto e há concorrência elevada
/// no mercado, permitindo explorar alternativas para obter melhores condições.
/// </summary>
public sealed class CompetitiveExpensiveSupplierRule : INegotiationStrategyRule
{
    private readonly NegotiationStrategyOptions _options;

    /// <summary>
    /// Inicializa a regra com as opções de configuração da engine de estratégia.
    /// </summary>
    /// <param name="options">Opções contendo os limiares de preço caro e de concorrência mínima.</param>
    public CompetitiveExpensiveSupplierRule(NegotiationStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public int Priority => 30;

    /// <inheritdoc />
    public NegotiationStrategyType Strategy => NegotiationStrategyType.Competitive;

    /// <inheritdoc />
    public bool Matches(NegotiationContext context)
        => context.NumberOfSuppliers >= _options.MinCompetitorsForCompetitive
           && IsExpensiveVersusHistory(context, _options.ExpensivePriceDeviationThreshold);

    /// <inheritdoc />
    public string BuildJustification(NegotiationContext context)
        => $"Fornecedor caro em relação ao histórico e {context.NumberOfSuppliers} concorrentes disponíveis: explorar a concorrência do mercado.";

    private static bool IsExpensiveVersusHistory(NegotiationContext context, double threshold)
    {
        if (context.HistoricalBestPrice is not { } bestPrice || bestPrice <= 0)
        {
            return false;
        }

        var deviation = (double)((context.CurrentPrice - bestPrice) / bestPrice);
        return deviation >= threshold;
    }
}
