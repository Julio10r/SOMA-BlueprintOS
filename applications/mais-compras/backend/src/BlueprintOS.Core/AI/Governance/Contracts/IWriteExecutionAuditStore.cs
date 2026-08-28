#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>
/// Permanent audit of governed write executions. Separate from <see cref="IGovernanceAuditStore"/> (which
/// records the fine-grained governance event stream) and from <see cref="IRecoveryIndexStore"/> (which tracks
/// recoverable packages). Rows here are never deleted — in particular, retention cleanup must never touch
/// this store.
/// </summary>
public interface IWriteExecutionAuditStore
{
    Task AppendAsync(WriteExecutionAuditRecord record, CancellationToken cancellationToken = default);

    Task<WriteExecutionAuditRecord?> GetAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WriteExecutionAuditRecord>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Records the outcome of a governed rollback against the original execution's audit row.</summary>
    Task MarkRollbackAsync(Guid executionId, bool rollbackExecuted, string? rollbackResult, RecoveryPackageStatus packageStatus, CancellationToken cancellationToken = default);

    /// <summary>Records that the recovery package for this execution reached a new lifecycle state (e.g. Expired).</summary>
    Task UpdateRecoveryPackageStatusAsync(Guid executionId, RecoveryPackageStatus packageStatus, CancellationToken cancellationToken = default);
}
