namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Status possíveis de um <see cref="AdrRecord"/>, alinhados ao formato utilizado em <c>.ai/DECISIONS.md</c>.
/// </summary>
public enum AdrStatus
{
    /// <summary>
    /// Decisão proposta, ainda não aprovada.
    /// </summary>
    Proposed,

    /// <summary>
    /// Decisão aceita e em vigor.
    /// </summary>
    Accepted,

    /// <summary>
    /// Decisão descontinuada, sem substituição direta.
    /// </summary>
    Deprecated,

    /// <summary>
    /// Decisão substituída por uma ADR mais recente.
    /// </summary>
    Superseded,
}
