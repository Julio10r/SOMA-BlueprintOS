using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="IStaleDocumentationDetector"/> construída sobre um
/// <see cref="IDocumentationSyncService"/>.
/// </summary>
public sealed class StaleDocumentationDetector : IStaleDocumentationDetector
{
    private readonly IDocumentationSyncService _syncService;

    public StaleDocumentationDetector(IDocumentationSyncService syncService)
    {
        _syncService = syncService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StaleDocumentationInfo>> DetectStaleAsync(
        IReadOnlyList<DocumentationSyncCheck> checks,
        CancellationToken cancellationToken = default)
    {
        var results = await _syncService.CheckAllAsync(checks, cancellationToken);
        return results.Where(r => r.IsStale).ToList();
    }
}
