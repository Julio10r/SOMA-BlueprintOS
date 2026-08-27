#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Models;

public sealed record GovernanceAuditEntry(
    Guid Id,
    Guid ActionProposalId,
    string ProposalHash,
    RiskClassification RiskClassification,
    PolicyDecisionStatus DecisionStatus,
    IReadOnlyList<string> Reasons,
    string RequestingAgent,
    DateTimeOffset CreatedAt,
    Guid? ApprovalRequestId = null,
    Guid? ApprovalGrantId = null,
    string? HumanDecision = null,
    string? Result = null);

