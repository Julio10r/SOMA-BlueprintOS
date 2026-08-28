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
}

public enum ToolGatewayStatus
{
    Blocked = 1,
    DryRunCompleted = 2,
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
    GovernedExecutionMode ExecutionMode);

public sealed record ToolGatewayResult(
    ToolGatewayStatus Status,
    IReadOnlyList<string> Reasons,
    SomaLinxDryRunPreview? Preview,
    bool LiveExecutionEnabled = false,
    bool DirectBypassAllowed = false,
    bool PrivilegeEscalationAllowed = false);

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
