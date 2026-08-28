#pragma warning disable CS1591

#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

public sealed class ApprovalPolicy : IApprovalPolicy
{
    public bool IsGrantValidFor(ActionProposal proposal, ApprovalGrant grant, DateTimeOffset now) =>
        grant.IsValidFor(proposal, now);
}

