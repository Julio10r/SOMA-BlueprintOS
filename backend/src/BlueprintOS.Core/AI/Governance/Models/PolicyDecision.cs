#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Models;

public sealed record PolicyDecision(
    Guid Id,
    Guid ActionProposalId,
    string ProposalHash,
    RiskClassification RiskClassification,
    PolicyDecisionStatus Status,
    IReadOnlyList<string> Reasons,
    DateTimeOffset CreatedAt,
    bool RequiresHumanApproval,
    bool IsMaterialDeviation);

