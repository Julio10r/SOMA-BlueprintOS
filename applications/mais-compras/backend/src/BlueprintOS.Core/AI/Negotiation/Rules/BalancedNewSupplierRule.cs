using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Core.AI.Negotiation.Rules;

/// <summary>
/// Recomenda a estratégia <see cref="NegotiationStrategyType.Balanced"/> quando o
/// fornecedor ainda não possui histórico de negociações consolidado, evitando posturas
/// extremas antes que haja dados suficientes sobre o relacionamento.
/// </summary>
public sealed class BalancedNewSupplierRule : INegotiationStrategyRule
{
    /// <inheritdoc />
    public int Priority => 50;

    /// <inheritdoc />
    public NegotiationStrategyType Strategy => NegotiationStrategyType.Balanced;

    /// <inheritdoc />
    public bool Matches(NegotiationContext context) => context.NegotiationCount <= 0;

    /// <inheritdoc />
    public string BuildJustification(NegotiationContext context)
        => "Fornecedor novo, sem histórico de negociações consolidado: adotar postura equilibrada.";
}
