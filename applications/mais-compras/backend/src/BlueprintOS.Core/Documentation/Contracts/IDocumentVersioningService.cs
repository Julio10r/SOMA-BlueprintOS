using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do serviço de versionamento de documentos.
/// </summary>
public interface IDocumentVersioningService
{
    /// <summary>
    /// Registra uma nova versão para o documento informado.
    /// </summary>
    Task<DocumentVersion> RegisterVersionAsync(
        string documentId,
        string content,
        string changeSummary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o histórico completo de versões de um documento, em ordem crescente de versão.
    /// </summary>
    Task<IReadOnlyList<DocumentVersion>> GetHistoryAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma versão específica de um documento, ou <c>null</c> se não existir.
    /// </summary>
    Task<DocumentVersion?> GetVersionAsync(string documentId, int versionNumber, CancellationToken cancellationToken = default);
}
