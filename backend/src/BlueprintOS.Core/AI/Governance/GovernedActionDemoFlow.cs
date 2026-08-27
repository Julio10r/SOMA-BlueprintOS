#pragma warning disable CS1591

#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

public sealed class GovernedActionDemoFlow(
    IAIGovernancePolicyEngine policyEngine,
    IApprovalPolicy approvalPolicy,
    IGovernanceAuditRecorder auditRecorder)
{
    public GovernedActionDemoResult Evaluate(ActionProposal proposal, ApprovalGrant? grant, DateTimeOffset now)
    {
        var decision = policyEngine.Evaluate(proposal, now);
        var allowedByApproval = decision.Status == PolicyDecisionStatus.RequiresApproval
            && grant is not null
            && approvalPolicy.IsGrantValidFor(proposal, grant, now);

        var canExecute = decision.Status == PolicyDecisionStatus.Allowed || allowedByApproval;
        var result = canExecute ? "Execucao demonstrativa autorizada; nenhuma operacao real foi executada." : "Execucao demonstrativa bloqueada.";

        auditRecorder.Record(new GovernanceAuditEntry(
            Guid.NewGuid(),
            proposal.Id,
            proposal.ProposalHash,
            decision.RiskClassification,
            decision.Status,
            decision.Reasons,
            proposal.RequestingAgent,
            now,
            ApprovalGrantId: grant?.Id,
            HumanDecision: grant is null ? null : (allowedByApproval ? "approved" : "invalid"),
            Result: result));

        return new GovernedActionDemoResult(decision, canExecute, result);
    }
}

public sealed record GovernedActionDemoResult(PolicyDecision Decision, bool CanExecute, string Result);

