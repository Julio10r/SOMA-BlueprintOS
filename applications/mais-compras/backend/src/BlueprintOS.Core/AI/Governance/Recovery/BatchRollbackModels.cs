#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Recovery;

public enum BatchRollbackAnalysisStatus
{
    NotFound,
    NotAvailable,

    /// <summary>Every requested item that survived concurrency screening shares one operation
    /// (insert/update/delete, decided per item exactly like the single-item rollback does) — a precondition for
    /// executing them as one governed write. When the ready set is heterogeneous, rollback refuses rather than
    /// silently picking one operation for all of them.</summary>
    MixedOperationsNotSupported,

    /// <summary>Every targeted item had a concurrency finding, so nothing is left to roll back.</summary>
    BlockedConcurrentChange,

    ReadyForConfirmation,
}

/// <summary>One item's restore plan, decided objectively from before/current existence — the same rule
/// <c>RollbackOrchestrator.BuildEquivalentProposal</c> applies to a single execution, applied here per item.</summary>
public sealed record BatchItemRestorePlan(
    string BusinessKey,
    string Resource,
    ActionOperation Operation,
    IReadOnlyDictionary<string, string?>? TargetRecord,
    IReadOnlyDictionary<string, string?>? CurrentRecord);

/// <summary>
/// Result of DISCOVER→ANALYZE for a batch rollback, targeting either the whole batch (empty
/// <see cref="RequestedBusinessKeys"/>) or an explicit subset (selective rollback). Writes nothing — mirrors
/// <see cref="RollbackSafetyAnalysis"/>'s guarantee for the single-item path.
/// </summary>
public sealed record BatchRollbackSafetyAnalysis(
    BatchRollbackAnalysisStatus Status,
    Guid BatchExecutionId,
    RecoveryIndexEntry? Entry,
    BatchRecoveryPackageManifest? Manifest,
    IReadOnlyList<string> RequestedBusinessKeys,
    IReadOnlyList<BatchItemRestorePlan> ReadyItems,
    IReadOnlyList<string> ConcurrencyFindings,
    string Summary,
    string? ConfirmationHandle,
    IReadOnlyList<string> Reasons);

/// <summary>Explicit human confirmation for a batch rollback — bound to one analysis via
/// <see cref="ConfirmationHandle"/>, exactly like <see cref="RollbackConfirmation"/>. Never derived from the
/// original execution's approval.</summary>
public sealed record BatchRollbackConfirmation(
    Guid BatchExecutionId,
    string ConfirmationHandle,
    string RequestedBy,
    string Justification,
    DateTimeOffset ConfirmedAt);

public enum BatchRollbackExecutionStatus
{
    Blocked,
    GovernanceBlocked,
    ApprovalRequired,
    Completed,
    PartiallyCompleted,
    ExecutionFailed,
    ValidationFailed,
}

public sealed record BatchItemRollbackOutcome(
    string BusinessKey,
    bool Success,
    IReadOnlyList<string> Reasons);

public sealed record BatchRollbackExecutionResult(
    BatchRollbackExecutionStatus Status,
    Guid RollbackExecutionId,
    Guid BatchExecutionId,
    IReadOnlyList<BatchItemRollbackOutcome> ItemOutcomes,
    IReadOnlyList<string> Reasons,
    RollbackActionProposal? Proposal = null,
    PolicyDecision? Decision = null);
