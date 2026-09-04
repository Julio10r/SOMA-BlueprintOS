#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>
/// An adapter that can perform a REAL, non-mutating bulk read of a pre-registered dataset — not only a dry
/// run. It extends <see cref="IGovernedToolAdapter"/> rather than replacing it: the Tool Gateway still
/// validates capability, owner, connection profile, identity, policy decision and approval exactly as for a
/// write, and only then calls <see cref="ExecuteAsync"/>.
///
/// Deliberately separate from <see cref="IWriteExecutionAdapter"/>: a read has no before/after-state to
/// snapshot, nothing to roll back, and no post-write validation rule to satisfy. An adapter that implements
/// only <see cref="IWriteExecutionAdapter"/> can never execute under <see cref="GovernedExecutionMode.LiveRead"/>,
/// and an adapter that implements only this interface can never execute under
/// <see cref="GovernedExecutionMode.LiveExecution"/> — the Tool Gateway enforces both directions.
/// </summary>
public interface IReadExecutionAdapter : IGovernedToolAdapter
{
    /// <summary>
    /// Performs the read and streams it to its destination (e.g. a RAW staging table). The Gateway/Policy
    /// Engine authorize this call exactly once for the entire dataset — never once per row — so the
    /// implementation is expected to stream (e.g. <c>SqlDataReader</c> into <c>SqlBulkCopy</c>), never to
    /// materialize the full result set in memory before returning.
    /// </summary>
    Task<ReadExecutionResult> ExecuteAsync(
        ToolGatewayRequest request,
        CancellationToken cancellationToken = default);
}
