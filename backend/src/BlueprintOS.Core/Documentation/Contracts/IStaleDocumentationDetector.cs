using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do detector de documentação desatualizada ("stale"), construído sobre
/// <see cref="IDocumentationSyncService"/>.
/// </summary>
public interface IStaleDocumentationDetector
{
    /// <summary>
    /// Retorna apenas os documentos considerados desatualizados dentre os verificados.
    /// </summary>
    Task<IReadOnlyList<StaleDocumentationInfo>> DetectStaleAsync(
        IReadOnlyList<DocumentationSyncCheck> checks,
        CancellationToken cancellationToken = default);
}
