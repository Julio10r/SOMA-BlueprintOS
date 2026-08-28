#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>
/// Index of recovery packages.
///
/// <see cref="FindAsync"/> ALWAYS returns a list, even when exactly one entry matches. There is deliberately
/// no "get the one" convenience: a silent <c>.First()</c> is how a rollback ends up applied to the wrong
/// execution. Choosing among candidates is the caller's explicit, separate step.
/// </summary>
public interface IRecoveryIndexStore
{
    Task<IReadOnlyList<RecoveryIndexEntry>> FindAsync(RecoveryIndexQuery query, CancellationToken cancellationToken = default);

    Task AppendAsync(RecoveryIndexEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Updates only the lifecycle status of an entry. The row itself is never deleted.</summary>
    Task UpdateStatusAsync(Guid executionId, RecoveryPackageStatus status, CancellationToken cancellationToken = default);
}
