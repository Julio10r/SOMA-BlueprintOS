#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Contracts;

public interface IAIGovernancePolicyEngine
{
    PolicyDecision Evaluate(ActionProposal proposal, DateTimeOffset now);
}

