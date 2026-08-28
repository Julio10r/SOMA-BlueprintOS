#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Recovery;

public enum RollbackDiscoveryStatus
{
    /// <summary>No execution matched the criteria. Reason code: ROLLBACK_NOT_FOUND.</summary>
    NotFound = 1,

    /// <summary>Exactly one execution matched. It is RETURNED, not executed and not selected.</summary>
    SingleCandidate = 2,

    /// <summary>More than one execution matched. The whole list is returned; the runtime never picks.</summary>
    MultipleCandidates = 3,
}

public enum RollbackAnalysisStatus
{
    /// <summary>Analysis passed. A confirmation handle was issued; nothing has been written.</summary>
    ReadyForConfirmation = 1,

    /// <summary>The current state no longer matches the state this execution left behind: something else
    /// changed these records. Reason code: ROLLBACK_BLOCKED_CONCURRENT_CHANGE.</summary>
    BlockedConcurrentChange = 2,

    /// <summary>The recovery material cannot support a rollback (expired, deleted, no backup, rollback not
    /// supported, or a corrupted manifest). Reason code: ROLLBACK_NOT_AVAILABLE.</summary>
    NotAvailable = 3,

    /// <summary>The execution id does not exist in the index. Reason code: ROLLBACK_NOT_FOUND.</summary>
    NotFound = 4,
}

public enum RollbackExecutionStatus
{
    /// <summary>Confirmation did not match the analysis (wrong handle, or a different execution id).
    /// Reason code: ROLLBACK_CONFIRMATION_MISMATCH. Nothing was written.</summary>
    Blocked = 1,

    /// <summary>Governance refused the rollback proposal itself. Nothing was written.</summary>
    GovernanceBlocked = 2,

    /// <summary>A fresh, specific human approval for the rollback was not granted. Nothing was written.</summary>
    ApprovalRequired = 3,

    /// <summary>The restoring write failed.</summary>
    ExecutionFailed = 4,

    /// <summary>The restoring write ran but the state does not match the original before-data.
    /// Reason code: ROLLBACK_VALIDATION failed.</summary>
    ValidationFailed = 5,

    /// <summary>The rollback ran and post-rollback validation confirmed the original state is back.</summary>
    Completed = 6,
}

public sealed record RollbackDiscoveryResult(
    RollbackDiscoveryStatus Status,
    IReadOnlyList<RecoveryIndexEntry> Candidates,
    IReadOnlyList<string> Reasons);

/// <summary>
/// Result of the automatic safety pre-analysis for ONE explicitly selected execution. When the status is
/// <see cref="RollbackAnalysisStatus.ReadyForConfirmation"/> it carries a <see cref="ConfirmationHandle"/>
/// that the caller must hand back to execute — and nothing else will do.
/// </summary>
public sealed record RollbackSafetyAnalysis(
    RollbackAnalysisStatus Status,
    Guid ExecutionId,
    RecoveryIndexEntry? Entry,
    RecoveryPackageManifest? Manifest,
    IReadOnlyList<RecoveryDataSet> BeforeData,
    IReadOnlyList<RecoveryDataSet> ExpectedCurrentState,
    IReadOnlyList<RecoveryDataSet> ObservedCurrentState,
    IReadOnlyList<string> ConcurrencyFindings,
    string Summary,
    string? ConfirmationHandle,
    IReadOnlyList<string> Reasons);

/// <summary>The explicit final confirmation. It must name the execution AND carry the handle issued for that
/// exact analysis.</summary>
public sealed record RollbackConfirmation(
    Guid ExecutionId,
    string ConfirmationHandle,
    string RequestedBy,
    string Justification,
    DateTimeOffset ConfirmedAt);

/// <summary>
/// A rollback expressed as a governed action in its own right. It carries an EquivalentProposal built from the
/// captured before-data — the rollback is a new write, evaluated by the policy engine and approved on its own
/// merits, never a replay of the original execution's authorization.
/// </summary>
public sealed record RollbackActionProposal(
    Guid OriginalExecutionId,
    ActionProposal EquivalentProposal,
    string RequestedBy,
    string Justification);

/// <summary>Permanent audit of a rollback attempt, successful or not.</summary>
public sealed record RollbackAuditRecord
{
    public required Guid RollbackExecutionId { get; init; }
    public required Guid OriginalExecutionId { get; init; }
    public required string Requester { get; init; }
    public required DateTimeOffset RequestedAt { get; init; }
    public required bool ExplicitConfirmationReceived { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
    public required string Justification { get; init; }
    public required IReadOnlyList<string> TablesAffected { get; init; }
    public required IReadOnlyList<string> BusinessKeys { get; init; }
    public required int RecordsAffected { get; init; }
    public required IReadOnlyList<string> ConcurrencyFindings { get; init; }
    public required string ExpectedStateSummary { get; init; }
    public required string ObservedStateSummary { get; init; }
    public required RollbackExecutionStatus Status { get; init; }
    public bool PostRollbackValidationPassed { get; init; }
    public string? PostRollbackValidationRuleId { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public string? RollbackProposalHash { get; init; }
}

public sealed record RollbackExecutionResult(
    RollbackExecutionStatus Status,
    Guid RollbackExecutionId,
    Guid OriginalExecutionId,
    IReadOnlyList<string> Reasons,
    RollbackActionProposal? Proposal = null,
    PolicyDecision? Decision = null,
    PostWriteValidationReport? Validation = null,
    RollbackAuditRecord? Audit = null);
