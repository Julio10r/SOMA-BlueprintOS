namespace BlueprintOS.Core.AI.Negotiation.Models;

/// <summary>
/// Representa o grau de urgência de uma necessidade de compra.
/// </summary>
public enum NegotiationUrgencyLevel
{
    /// <summary>
    /// Sem urgência, há tempo hábil para uma negociação mais elaborada.
    /// </summary>
    Low,

    /// <summary>
    /// Urgência dentro do esperado para o processo normal de compras.
    /// </summary>
    Normal,

    /// <summary>
    /// Urgência elevada, com prazo apertado para conclusão da negociação.
    /// </summary>
    High,

    /// <summary>
    /// Urgência crítica, com risco iminente de desabastecimento.
    /// </summary>
    Critical,
}
