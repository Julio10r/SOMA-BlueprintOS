using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Core.AI.Negotiation.Rules;

/// <summary>
/// Recomenda a estratégia <see cref="NegotiationStrategyType.Partnership"/> quando o
/// fornecedor possui score alto e a compra faz parte de um relacionamento recorrente,
/// favorecendo a construção de uma parceria de longo prazo.
/// </summary>
public sealed class PartnershipHighScoreRecurringRule : INegotiationStrategyRule
{
    private readonly NegotiationStrategyOptions _options;

    /// <summary>
    /// Inicializa a regra com as opções de configuração da engine de estratégia.
    /// </summary>
    /// <param name="options">Opções contendo o limiar de score considerado alto.</param>
    public PartnershipHighScoreRecurringRule(NegotiationStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public int Priority => 20;

    /// <inheritdoc />
    public NegotiationStrategyType Strategy => NegotiationStrategyType.Partnership;

    /// <inheritdoc />
    public bool Matches(NegotiationContext context)
        => context.IsRecurringPurchase && context.SupplierScore >= _options.HighSupplierScoreThreshold;

    /// <inheritdoc />
    public string BuildJustification(NegotiationContext context)
        => $"Fornecedor com score alto ({context.SupplierScore:F1}) e compra recorrente: construir parceria de longo prazo.";
}
