#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Contracts;

public interface IApprovalPolicy
{
    bool IsGrantValidFor(ActionProposal proposal, ApprovalGrant grant, DateTimeOffset now);
}

