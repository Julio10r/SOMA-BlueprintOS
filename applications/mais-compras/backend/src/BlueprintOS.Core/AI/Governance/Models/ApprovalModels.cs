#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Models;

public sealed record ApprovalRequest(
    Guid Id,
    Guid ActionProposalId,
    string ProposalHash,
    RiskClassification RiskClassification,
    string Reason,
    string RequiredApprover,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    ApprovalRequestStatus Status);

public sealed record ApprovalGrant(
    Guid Id,
    Guid ApprovalRequestId,
    string ProposalHash,
    string ApprovedBy,
    DateTimeOffset ApprovedAt,
    DateTimeOffset ExpiresAt,
    string Scope,
    string? Notes,
    DateTimeOffset? RevokedAt)
{
    public bool IsValidFor(ActionProposal proposal, DateTimeOffset now) =>
        RevokedAt is null
        && now <= ExpiresAt
        && string.Equals(ProposalHash, proposal.ProposalHash, StringComparison.Ordinal);
}

