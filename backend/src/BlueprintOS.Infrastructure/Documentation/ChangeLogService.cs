using System.Collections.Concurrent;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação em memória de <see cref="IChangeLogService"/>.
/// </summary>
public sealed class ChangeLogService : IChangeLogService
{
    private readonly ConcurrentQueue<ChangeLogEntry> _entries = new();

    /// <inheritdoc />
    public Task<ChangeLogEntry> RecordChangeAsync(
        string documentId,
        string summary,
        string? author = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new ChangeLogEntry(Guid.NewGuid().ToString("N"), documentId, DateTimeOffset.UtcNow, summary, author);
        _entries.Enqueue(entry);
        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ChangeLogEntry>> GetChangesAsync(string documentId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChangeLogEntry> result = _entries
            .Where(e => e.DocumentId == documentId)
            .OrderBy(e => e.ChangedAt)
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ChangeLogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChangeLogEntry> result = _entries.OrderBy(e => e.ChangedAt).ToList();
        return Task.FromResult(result);
    }
}
