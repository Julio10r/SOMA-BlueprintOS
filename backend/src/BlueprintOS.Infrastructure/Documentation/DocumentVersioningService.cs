using System.Collections.Concurrent;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação em memória de <see cref="IDocumentVersioningService"/>.
/// </summary>
public sealed class DocumentVersioningService : IDocumentVersioningService
{
    private readonly ConcurrentDictionary<string, List<DocumentVersion>> _history = new();

    /// <inheritdoc />
    public Task<DocumentVersion> RegisterVersionAsync(
        string documentId,
        string content,
        string changeSummary,
        CancellationToken cancellationToken = default)
    {
        var versions = _history.GetOrAdd(documentId, _ => new List<DocumentVersion>());

        DocumentVersion version;
        lock (versions)
        {
            var nextNumber = versions.Count + 1;
            version = new DocumentVersion(documentId, nextNumber, content, DateTimeOffset.UtcNow, changeSummary);
            versions.Add(version);
        }

        return Task.FromResult(version);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentVersion>> GetHistoryAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (!_history.TryGetValue(documentId, out var versions))
        {
            return Task.FromResult<IReadOnlyList<DocumentVersion>>(Array.Empty<DocumentVersion>());
        }

        lock (versions)
        {
            IReadOnlyList<DocumentVersion> snapshot = versions.OrderBy(v => v.VersionNumber).ToList();
            return Task.FromResult(snapshot);
        }
    }

    /// <inheritdoc />
    public Task<DocumentVersion?> GetVersionAsync(string documentId, int versionNumber, CancellationToken cancellationToken = default)
    {
        if (!_history.TryGetValue(documentId, out var versions))
        {
            return Task.FromResult<DocumentVersion?>(null);
        }

        lock (versions)
        {
            var version = versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
            return Task.FromResult(version);
        }
    }
}
