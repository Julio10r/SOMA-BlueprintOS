using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Core.AI.Negotiation.Rules;

/// <summary>
/// Regra de fallback que recomenda a estratégia <see cref="NegotiationStrategyType.Conservative"/>
/// quando nenhuma outra regra se aplica ao contexto informado. Deve ser sempre a
/// última regra avaliada pela engine.
/// </summary>
public sealed class ConservativeFallbackRule : INegotiationStrategyRule
{
    /// <inheritdoc />
    public int Priority => int.MaxValue;

    /// <inheritdoc />
    public NegotiationStrategyType Strategy => NegotiationStrategyType.Conservative;

    /// <inheritdoc />
    public bool Matches(NegotiationContext context) => true;

    /// <inheritdoc />
    public string BuildJustification(NegotiationContext context)
        => "Nenhuma condição específica identificada: adotar postura cautelosa, priorizando a continuidade do fornecimento.";
}
