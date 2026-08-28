#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>Permanent audit of rollback attempts. Append-only; never touched by retention cleanup.</summary>
public interface IRollbackAuditStore
{
    Task AppendAsync(RollbackAuditRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RollbackAuditRecord>> ListByOriginalExecutionAsync(Guid originalExecutionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RollbackAuditRecord>> ListAsync(CancellationToken cancellationToken = default);
}
