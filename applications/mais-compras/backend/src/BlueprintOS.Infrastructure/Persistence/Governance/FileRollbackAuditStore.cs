using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// File-based <see cref="IRollbackAuditStore"/> under <c>{root}/rollback-audit/{yyyy-MM-dd}/{rollbackExecutionId:N}.json</c>,
/// one file per <see cref="RollbackAuditRecord"/> keyed by RollbackExecutionId, partitioned by RequestedAt
/// (UTC) date. Both interface lookups (<see cref="ListByOriginalExecutionAsync"/>, <see cref="ListAsync"/>)
/// are list-style, not by-id, so this store needs no id->path lookup at all — every read is a full scan of
/// all date partitions. Permanent: never deleted, never touched by retention cleanup.
/// </summary>
public sealed class FileRollbackAuditStore(string rootDirectory) : IRollbackAuditStore
{
    private readonly string _root = Path.Combine(rootDirectory, "rollback-audit");

    public Task AppendAsync(RollbackAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        var path = BuildPath(record.RollbackExecutionId, record.RequestedAt);
        return AtomicFileWriter.WithFileLockAsync(path, () => AtomicFileWriter.WriteJsonAsync(path, record, cancellationToken).ContinueWith(_ => true, cancellationToken));
    }

    public async Task<IReadOnlyList<RollbackAuditRecord>> ListByOriginalExecutionAsync(Guid originalExecutionId, CancellationToken cancellationToken = default)
    {
        var results = new List<RollbackAuditRecord>();
        await foreach (var item in AtomicFileWriter.ScanAllAsync<RollbackAuditRecord>(_root, cancellationToken))
        {
            if (item.OriginalExecutionId == originalExecutionId) results.Add(item);
        }

        return results.OrderBy(item => item.RequestedAt).ToArray();
    }

    public async Task<IReadOnlyList<RollbackAuditRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<RollbackAuditRecord>();
        await foreach (var item in AtomicFileWriter.ScanAllAsync<RollbackAuditRecord>(_root, cancellationToken))
        {
            results.Add(item);
        }

        return results.OrderBy(item => item.RequestedAt).ToArray();
    }

    private string BuildPath(Guid rollbackExecutionId, DateTimeOffset requestedAt) =>
        Path.Combine(_root, requestedAt.UtcDateTime.ToString("yyyy-MM-dd"), $"{rollbackExecutionId:N}.json");
}
