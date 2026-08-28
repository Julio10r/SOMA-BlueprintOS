using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// File-based <see cref="IWriteExecutionAuditStore"/> under <c>{root}/write-execution-audit/{yyyy-MM-dd}/{executionId:N}.json</c>,
/// one file per <see cref="WriteExecutionAuditRecord"/> keyed by ExecutionId, partitioned by StartedAt (UTC)
/// date. <see cref="GetAsync"/> is a by-id lookup, so this store uses approach (a): scan date subfolders for
/// the matching filename rather than maintain a secondary index — simplest, and cannot desync from the truth.
/// PERMANENT: this store's rows are never deleted for any reason, including recovery-package retention
/// expiry (only <see cref="RecoveryPackageStatus"/> is updated here via <see cref="UpdateRecoveryPackageStatusAsync"/>).
/// </summary>
public sealed class FileWriteExecutionAuditStore(string rootDirectory) : IWriteExecutionAuditStore
{
    private readonly string _root = Path.Combine(rootDirectory, "write-execution-audit");

    public Task AppendAsync(WriteExecutionAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        var path = BuildPath(record.ExecutionId, record.StartedAt);
        return AtomicFileWriter.WithFileLockAsync(path, () => AtomicFileWriter.WriteJsonAsync(path, record, cancellationToken).ContinueWith(_ => true, cancellationToken));
    }

    public async Task<WriteExecutionAuditRecord?> GetAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var path = await FindPathByIdAsync(executionId, cancellationToken);
        return path is null ? null : await AtomicFileWriter.ReadJsonAsync<WriteExecutionAuditRecord>(path, cancellationToken);
    }

    public async Task<IReadOnlyList<WriteExecutionAuditRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<WriteExecutionAuditRecord>();
        await foreach (var item in AtomicFileWriter.ScanAllAsync<WriteExecutionAuditRecord>(_root, cancellationToken))
        {
            results.Add(item);
        }

        return results.OrderBy(item => item.StartedAt).ToArray();
    }

    public async Task MarkRollbackAsync(Guid executionId, bool rollbackExecuted, string? rollbackResult, RecoveryPackageStatus packageStatus, CancellationToken cancellationToken = default)
    {
        var path = await FindPathByIdAsync(executionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Write execution audit {executionId} was not found.");
        await AtomicFileWriter.WithFileLockAsync(path, async () =>
        {
            var existing = await AtomicFileWriter.ReadJsonAsync<WriteExecutionAuditRecord>(path, cancellationToken)
                ?? throw new KeyNotFoundException($"Write execution audit {executionId} was not found.");
            var updated = existing with
            {
                RollbackExecuted = rollbackExecuted,
                RollbackResult = rollbackResult,
                RecoveryPackageStatus = packageStatus,
            };
            await AtomicFileWriter.WriteJsonAsync(path, updated, cancellationToken);
            return true;
        });
    }

    public async Task UpdateRecoveryPackageStatusAsync(Guid executionId, RecoveryPackageStatus packageStatus, CancellationToken cancellationToken = default)
    {
        var path = await FindPathByIdAsync(executionId, cancellationToken);
        if (path is null) return;
        await AtomicFileWriter.WithFileLockAsync(path, async () =>
        {
            var existing = await AtomicFileWriter.ReadJsonAsync<WriteExecutionAuditRecord>(path, cancellationToken);
            if (existing is null) return true;
            await AtomicFileWriter.WriteJsonAsync(path, existing with { RecoveryPackageStatus = packageStatus }, cancellationToken);
            return true;
        });
    }

    private string BuildPath(Guid executionId, DateTimeOffset startedAt) =>
        Path.Combine(_root, BrazilTimeZoneProvider.ToSaoPaulo(startedAt).ToString("yyyy-MM-dd"), $"{executionId:N}.json");

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
