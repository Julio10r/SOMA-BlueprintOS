namespace BlueprintOS.Core.AI.Negotiation.Models;

/// <summary>
/// Representa o risco estimado associado a uma recomendação de negociação.
/// </summary>
public enum NegotiationRiskLevel
{
    /// <summary>
    /// Baixo risco de a negociação falhar ou comprometer o fornecimento.
    /// </summary>
    Low,

    /// <summary>
    /// Risco moderado, exigindo atenção ao conduzir a negociação.
    /// </summary>
    Medium,

    /// <summary>
    /// Alto risco de a negociação falhar ou comprometer o fornecimento.
    /// </summary>
    High,
}
