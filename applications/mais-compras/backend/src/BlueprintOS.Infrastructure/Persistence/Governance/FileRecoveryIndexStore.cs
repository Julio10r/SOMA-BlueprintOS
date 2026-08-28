using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// File-based <see cref="IRecoveryIndexStore"/> under <c>{root}/recovery-index/{yyyy-MM-dd}/{executionId:N}.json</c>,
/// one file per entry keyed by ExecutionId, partitioned by ExecutedAt (UTC) date. Lookup-by-id (needed for
/// <see cref="UpdateStatusAsync"/> and for the duplicate-check in <see cref="AppendAsync"/>) uses approach (a):
/// scan all date partitions for a matching filename, rather than maintaining a secondary id->path index. At
/// this project's volume (dozens to low thousands of executions) a scan is fast and, more importantly, cannot
/// desync from the truth the way a secondary index file could.
///
/// This store is PERMANENT: rows are never deleted, only their Status is updated (e.g. to Expired by
/// <see cref="RecoveryRetentionCleanupService"/> after the physical Recovery Package files are removed).
/// <see cref="FindAsync"/> reuses <see cref="RecoveryIndexQuery.Matches"/> for all filter semantics.
/// </summary>
public sealed class FileRecoveryIndexStore(string rootDirectory) : IRecoveryIndexStore
{
    private readonly string _root = Path.Combine(rootDirectory, "recovery-index");

    public async Task<IReadOnlyList<RecoveryIndexEntry>> FindAsync(RecoveryIndexQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var results = new List<RecoveryIndexEntry>();
        await foreach (var entry in AtomicFileWriter.ScanAllAsync<RecoveryIndexEntry>(_root, cancellationToken))
        {
            if (query.Matches(entry)) results.Add(entry);
        }

        return results.OrderByDescending(entry => entry.ExecutedAt).ToArray();
    }

    public async Task AppendAsync(RecoveryIndexEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var path = BuildPath(entry.ExecutionId, entry.ExecutedAt);
        await AtomicFileWriter.WithFileLockAsync(path, async () =>
        {
            var existingPath = await FindPathByIdAsync(entry.ExecutionId, cancellationToken);
            if (existingPath is not null)
            {
                throw new InvalidOperationException($"Recovery index already contains execution {entry.ExecutionId}.");
            }

            await AtomicFileWriter.WriteJsonAsync(path, entry, cancellationToken);
            return true;
        });
    }

    public async Task UpdateStatusAsync(Guid executionId, RecoveryPackageStatus status, CancellationToken cancellationToken = default)
    {
        var path = await FindPathByIdAsync(executionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Recovery index entry {executionId} was not found.");
        await AtomicFileWriter.WithFileLockAsync(path, async () =>
        {
            var existing = await AtomicFileWriter.ReadJsonAsync<RecoveryIndexEntry>(path, cancellationToken)
                ?? throw new KeyNotFoundException($"Recovery index entry {executionId} was not found.");
            await AtomicFileWriter.WriteJsonAsync(path, existing with { Status = status }, cancellationToken);
            return true;
        });
    }

    private string BuildPath(Guid executionId, DateTimeOffset executedAt) =>
        Path.Combine(_root, BrazilTimeZoneProvider.ToSaoPaulo(executedAt).ToString("yyyy-MM-dd"), $"{executionId:N}.json");

    private async Task<string?> FindPathByIdAsync(Guid executionId, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_root)) return null;
        var fileName = $"{executionId:N}.json";
        foreach (var file in Directory.EnumerateFiles(_root, fileName, SearchOption.AllDirectories))
        {
            return file;
        }

        await Task.CompletedTask;
        return null;
    }
}
