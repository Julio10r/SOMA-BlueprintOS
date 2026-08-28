using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do serviço de registro de alterações (changelog) de documentos.
/// </summary>
public interface IChangeLogService
{
    /// <summary>
    /// Registra uma alteração para o documento informado.
    /// </summary>
    Task<ChangeLogEntry> RecordChangeAsync(
        string documentId,
        string summary,
        string? author = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém as alterações registradas para um documento específico, em ordem cronológica.
    /// </summary>
    Task<IReadOnlyList<ChangeLogEntry>> GetChangesAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as alterações registradas, de todos os documentos, em ordem cronológica.
    /// </summary>
    Task<IReadOnlyList<ChangeLogEntry>> GetAllAsync(CancellationToken cancellationToken = default);
}
