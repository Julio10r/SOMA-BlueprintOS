#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>In-memory permanent rollback audit. Append-only; no delete exists on purpose.</summary>
public sealed class InMemoryRollbackAuditStore : IRollbackAuditStore
{
    private readonly List<RollbackAuditRecord> _records = [];

    public Task AppendAsync(RollbackAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        lock (_records) _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RollbackAuditRecord>> ListByOriginalExecutionAsync(Guid originalExecutionId, CancellationToken cancellationToken = default)
    {
        lock (_records)
        {
            return Task.FromResult<IReadOnlyList<RollbackAuditRecord>>(
                _records.Where(item => item.OriginalExecutionId == originalExecutionId).ToArray());
        }
    }

    public Task<IReadOnlyList<RollbackAuditRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_records) return Task.FromResult<IReadOnlyList<RollbackAuditRecord>>(_records.ToArray());
    }
}
