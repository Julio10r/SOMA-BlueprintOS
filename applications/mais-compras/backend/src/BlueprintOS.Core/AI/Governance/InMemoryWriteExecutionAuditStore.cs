#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>In-memory permanent write execution audit. Append and update only — there is deliberately no
/// delete, mirroring the relational store.</summary>
public sealed class InMemoryWriteExecutionAuditStore : IWriteExecutionAuditStore
{
    private readonly List<WriteExecutionAuditRecord> _records = [];

    public Task AppendAsync(WriteExecutionAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        lock (_records) _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<WriteExecutionAuditRecord?> GetAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        lock (_records) return Task.FromResult(_records.LastOrDefault(item => item.ExecutionId == executionId));
    }

    public Task<IReadOnlyList<WriteExecutionAuditRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_records) return Task.FromResult<IReadOnlyList<WriteExecutionAuditRecord>>(_records.ToArray());
    }

    public Task MarkRollbackAsync(Guid executionId, bool rollbackExecuted, string? rollbackResult, RecoveryPackageStatus packageStatus, CancellationToken cancellationToken = default)
    {
        lock (_records)
        {
            var index = _records.FindLastIndex(item => item.ExecutionId == executionId);
            if (index < 0) throw new KeyNotFoundException($"Write execution audit {executionId} was not found.");
            _records[index] = _records[index] with
            {
                RollbackExecuted = rollbackExecuted,
                RollbackResult = rollbackResult,
                RecoveryPackageStatus = packageStatus,
            };
        }

        return Task.CompletedTask;
    }

    public Task UpdateRecoveryPackageStatusAsync(Guid executionId, RecoveryPackageStatus packageStatus, CancellationToken cancellationToken = default)
    {
        lock (_records)
        {
            var index = _records.FindLastIndex(item => item.ExecutionId == executionId);
            if (index < 0) return Task.CompletedTask;
            _records[index] = _records[index] with { RecoveryPackageStatus = packageStatus };
        }

        return Task.CompletedTask;
    }
}
