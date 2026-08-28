using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Core.AI.Negotiation.Rules;

/// <summary>
/// Recomenda a estratégia <see cref="NegotiationStrategyType.Emergency"/> quando a
/// compra possui urgência alta ou crítica, priorizando a garantia do fornecimento
/// sobre o ganho de preço. Avaliada antes de qualquer outra regra.
/// </summary>
public sealed class EmergencyUrgencyRule : INegotiationStrategyRule
{
    /// <inheritdoc />
    public int Priority => 10;

    /// <inheritdoc />
    public NegotiationStrategyType Strategy => NegotiationStrategyType.Emergency;

    /// <inheritdoc />
    public bool Matches(NegotiationContext context)
        => context.UrgencyLevel is NegotiationUrgencyLevel.High or NegotiationUrgencyLevel.Critical;

    /// <inheritdoc />
    public string BuildJustification(NegotiationContext context)
        => $"Compra com urgência {context.UrgencyLevel}: priorizar a garantia do fornecimento sobre o ganho de preço.";
}
