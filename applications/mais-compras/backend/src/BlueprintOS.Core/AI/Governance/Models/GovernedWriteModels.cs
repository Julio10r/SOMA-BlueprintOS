#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Models;

public enum OperationIntent
{
    Unknown = 0,
    Read = 1,
    Analyze = 2,
    Export = 3,
    Create = 4,
    Update = 5,
    Delete = 6,
    Truncate = 7,
    ExecuteWorkflow = 8,
    Configure = 9,
}

public enum GovernedExecutionMode
{
    DryRun = 1,
    LiveExecution = 2,

    /// <summary>A real, non-mutating bulk read of a pre-registered dataset (e.g. <c>linx.fornecedores.snapshot</c>).
    /// Deliberately NOT a variant of <see cref="LiveExecution"/>: it never carries or requires a
    /// <see cref="WriteVerificationProfile"/>, a <see cref="Recovery.RecoveryPackageReceipt"/>, or a
    /// <see cref="PostWriteValidationRule"/> — those guarantees exist to protect against an unrecoverable
    /// mutation, which a read cannot cause. Routing, ownership, policy decision, approval and auditing are
    /// still fully enforced by the same <see cref="ToolGateway"/>.</summary>
    LiveRead = 3,
}

public enum ToolGatewayStatus
{
    Blocked = 1,
    DryRunCompleted = 2,

    /// <summary>A live write ran and its adapter reported success. Only reachable through the guarded live
    /// path: an allowed/approved decision, a recovery package receipt when the profile requires a backup, and
    /// a resolved post-write validation rule when the profile requires validation.</summary>
    LiveExecutionCompleted = 3,

    /// <summary>The live path was permitted and attempted, but the adapter reported failure. Never a silent
    /// success: callers must treat this as "state unknown until validated/rolled back".</summary>
    LiveExecutionFailed = 4,

    /// <summary>A real bulk read ran and its adapter reported success.</summary>
    LiveReadCompleted = 5,

    /// <summary>The read path was permitted and attempted, but the adapter reported failure.</summary>
    LiveReadFailed = 6,
}

public sealed record StructuredActionContext(
    string RequestId,
    string RequestedBy,
    GovernanceEnvironment Environment,
    string System,
    ActionResourceType ResourceType,
    string Resource,
    OperationIntent OperationIntent,
    IReadOnlyList<string> RequestedCapabilities,
    IReadOnlyList<string> Fields,
    string? FilterSummary,
    int? ExpectedAffectedRows,
    string Purpose,
    DataClassification DataClassification,
    bool ContainsPersonalData,
    bool ContainsSensitivePersonalData,
    bool ContainsSecrets,
    ActionReversibility Reversibility,
    string? RunbookReference = null,
    string? WorkflowReference = null,
    string? ConnectionProfile = null,
    string? AdditionalContext = null);

public sealed record AgentWriteAnalysis(
    string AgentId,
    string Capability,
    IReadOnlyList<string> Fields,
    string? FilterSummary,
    int? ExpectedAffectedRows,
    ActionReversibility Reversibility,
    bool IsRunbookApprovedOperation = false,
    int? RunbookExpectedAffectedRows = null);

public sealed record RoutingEvidence(
    bool RoutingResolved,
    string? PrimaryAgent,
    IReadOnlyList<string> ComplementaryAgents,
    IReadOnlyList<string> CrossCuttingAgents,
    IReadOnlyList<string> CapabilityGaps,
    IReadOnlyList<string> RoutingConflicts);

public sealed record ActionProposalContextGap(string Field, string Code);

public sealed record ActionProposalBuildResult(
    ActionProposal? Proposal,
    IReadOnlyList<ActionProposalContextGap> ContextGaps)
{
    public bool Succeeded => Proposal is not null && ContextGaps.Count == 0;
}

public sealed record IdentityPermissionContext(
    string SubjectId,
    bool HasEffectivePermission,
    bool PrivilegeEscalationAllowed = false);

public sealed record SomaLinxDryRunPreview(
    string System,
    GovernanceEnvironment Environment,
    string Resource,
    ActionOperation Operation,
    IReadOnlyList<string> Fields,
    string? FilterSummary,
    int? ExpectedAffectedRows,
    string Purpose,
    string ConnectionProfile,
    RiskClassification RiskClassification,
    PolicyDecisionStatus PolicyStatus,
    string ApprovalStatus,
    ActionReversibility Reversibility,
    GovernedExecutionMode ExecutionMode,
    bool CredentialResolutionRequired,
    bool IdentityPermissionCheckRequired,
    bool SqlGenerated,
    bool ExternalExecutionPerformed);

public sealed record ToolGatewayRequest(
    string Capability,
    string RoutedPrimaryAgent,
    bool RoutingResolved,
    ActionProposal Proposal,
    PolicyDecision PolicyDecision,
    ApprovalGrant? ApprovalGrant,
    IReadOnlyList<string> CrossCuttingAgents,
    string ConnectionProfile,
    IdentityPermissionContext Identity,
    GovernedExecutionMode ExecutionMode,

    // --- Additive live-execution guarantees (default null) --------------------------------------------
    // All three default to null so every existing DryRun caller keeps compiling and behaving identically.
    // A LiveExecution request with any required guarantee missing stays blocked.

    // Proof that a recovery package was written BEFORE this write was attempted. Required when the
    // resolved write verification profile sets BackupRequired.
    Recovery.RecoveryPackageReceipt? RecoveryPackageReceipt = null,

    // The rule that will prove the write actually happened. Required when the resolved profile sets
    // PostWriteValidationRequired.
    PostWriteValidationRule? PostWriteValidationRule = null,

    // The write safety policy resolved from the profile store for this connection profile. Always
    // required for a live execution; never inferred from a database name.
    WriteVerificationProfile? WriteVerificationProfile = null);

public sealed record ToolGatewayResult(
    ToolGatewayStatus Status,
    IReadOnlyList<string> Reasons,
    SomaLinxDryRunPreview? Preview,
    bool LiveExecutionEnabled = false,
    bool DirectBypassAllowed = false,
    bool PrivilegeEscalationAllowed = false,
    WriteExecutionResult? Execution = null,
    bool LiveReadEnabled = false,
    ReadExecutionResult? ReadExecution = null);

/// <summary>What a write execution adapter reports back after a real write.</summary>
public sealed record WriteExecutionResult(
    bool Succeeded,
    int RecordsAffected,
    IReadOnlyList<Recovery.RecoveryDataSet> AfterData,
    IReadOnlyList<string> Reasons,
    string? ErrorMessage = null,
    string? ExternalIdentifier = null);

/// <summary>What a read execution adapter reports back after a real bulk read. Deliberately has no
/// after-state/rollback-shaped fields — a read has nothing to roll back — but does report enough for the
/// audit trail required by B3/Bloco 5A.9 (rows read vs. written, isolation level actually used, duration).</summary>
public sealed record ReadExecutionResult(
    bool Succeeded,
    long RowsRead,
    long RowsWritten,
    string IsolationLevelUsed,
    TimeSpan Duration,
    IReadOnlyList<string> Reasons,
    string? ErrorMessage = null);

public sealed record GovernanceAuditEvent(
    Guid Id,
    string EventType,
    string RequestId,
    Guid? ActionProposalId,
    string? ProposalHash,
    string? AgentId,
    string? SubjectId,
    string Outcome,
    IReadOnlyList<string> Categories,
    DateTimeOffset CreatedAt);

public sealed record GovernedWritePreparation(
    ActionProposalBuildResult ProposalBuild,
    PolicyDecision? PolicyDecision,
    ApprovalRequest? ApprovalRequest);
