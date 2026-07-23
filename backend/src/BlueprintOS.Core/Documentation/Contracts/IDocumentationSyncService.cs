using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do serviço de sincronização automática de documentação, responsável por
/// comparar o timestamp de arquivos de documentação com o dos arquivos de código-fonte relacionados.
/// </summary>
public interface IDocumentationSyncService
{
    /// <summary>
    /// Verifica se um único documento está desatualizado em relação às suas fontes.
    /// </summary>
    Task<StaleDocumentationInfo> CheckAsync(DocumentationSyncCheck check, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica múltiplos documentos em relação às suas fontes.
    /// </summary>
    Task<IReadOnlyList<StaleDocumentationInfo>> CheckAllAsync(
        IReadOnlyList<DocumentationSyncCheck> checks,
        CancellationToken cancellationToken = default);
}
