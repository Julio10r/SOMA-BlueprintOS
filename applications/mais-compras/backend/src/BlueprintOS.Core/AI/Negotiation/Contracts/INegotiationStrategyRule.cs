using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Core.AI.Negotiation.Contracts;

/// <summary>
/// Define uma regra individual da engine de estratégia de negociação. Cada regra
/// encapsula uma condição de negócio e a estratégia correspondente, permitindo que
/// novas regras sejam adicionadas sem alterar a engine ou as regras existentes.
/// </summary>
public interface INegotiationStrategyRule
{
    /// <summary>
    /// Prioridade de avaliação da regra: quanto menor o valor, mais cedo a regra é
    /// avaliada pela engine. Regras de maior prioridade devem tratar condições mais
    /// específicas ou críticas (por exemplo, urgência), enquanto regras de fallback
    /// devem receber os maiores valores de prioridade.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Estratégia de negociação recomendada por esta regra, quando aplicável.
    /// </summary>
    NegotiationStrategyType Strategy { get; }

    /// <summary>
    /// Indica se esta regra se aplica ao contexto de compra informado.
    /// </summary>
    /// <param name="context">Contexto da compra a ser negociada.</param>
    bool Matches(NegotiationContext context);

    /// <summary>
    /// Constrói a justificativa de negócio para a escolha desta estratégia, com base
    /// no contexto informado.
    /// </summary>
    /// <param name="context">Contexto da compra a ser negociada.</param>
    string BuildJustification(NegotiationContext context);
}
