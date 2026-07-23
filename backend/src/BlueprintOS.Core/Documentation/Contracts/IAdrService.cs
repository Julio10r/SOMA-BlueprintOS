using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do serviço de gerenciamento de Architecture Decision Records (ADRs).
/// </summary>
public interface IAdrService
{
    /// <summary>
    /// Cria e persiste uma nova ADR.
    /// </summary>
    Task<AdrRecord> CreateAsync(
        string title,
        string context,
        string decision,
        string consequences,
        AdrStatus status = AdrStatus.Proposed,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma ADR pelo seu identificador, ou <c>null</c> se não existir.
    /// </summary>
    Task<AdrRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todas as ADRs persistidas.
    /// </summary>
    Task<IReadOnlyList<AdrRecord>> ListAllAsync(CancellationToken cancellationToken = default);
}
