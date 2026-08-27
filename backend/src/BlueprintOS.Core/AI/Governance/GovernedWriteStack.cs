#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

public sealed class GovernedWriteStack(
    IActionProposalAdapter proposalAdapter,
    IAIGovernancePolicyEngine policyEngine,
    IApprovalStore approvalStore,
    IGovernanceAuditStore auditStore,
    IToolGateway toolGateway,
    TimeProvider timeProvider)
{
    public async Task<GovernedWritePreparation> PrepareAsync(
        StructuredActionContext context,
        RoutingEvidence routing,
        AgentWriteAnalysis analysis,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        await auditStore.AppendAsync(Event("context.accepted", context, null, analysis.AgentId, "accepted", [context.OperationIntent.ToString()], now), cancellationToken);
        await auditStore.AppendAsync(Event("routing.resolved", context, null, analysis.AgentId, routing.RoutingResolved ? "resolved" : "blocked", [analysis.Capability], now), cancellationToken);
        var build = proposalAdapter.Build(context, routing, analysis, now);
        if (!build.Succeeded)
        {
            await auditStore.AppendAsync(Event("proposal.context-gap", context, null, analysis.AgentId, "blocked", build.ContextGaps.Select(gap => gap.Code).ToArray(), now), cancellationToken);
            return new(build, null, null);
        }

        var proposal = build.Proposal!;
        await auditStore.AppendAsync(Event("proposal.created", context, proposal, analysis.AgentId, "created", [proposal.Operation.ToString()], now), cancellationToken);
        var decision = policyEngine.Evaluate(proposal, now);
        await auditStore.AppendAsync(Event("policy.evaluated", context, proposal, analysis.AgentId, decision.Status.ToString(), [decision.RiskClassification.ToString()], now), cancellationToken);
        ApprovalRequest? request = null;
        if (decision.Status == PolicyDecisionStatus.RequiresApproval)
        {
            request = new ApprovalRequest(Guid.NewGuid(), proposal.Id, proposal.ProposalHash, decision.RiskClassification,
                string.Join(" | ", decision.Reasons), "authorized-product-owner", now, now.AddHours(1), ApprovalRequestStatus.Pending);
            await approvalStore.SaveRequestAsync(request, cancellationToken);
            await auditStore.AppendAsync(Event("approval.requested", context, proposal, analysis.AgentId, "pending", [decision.RiskClassification.ToString()], now), cancellationToken);
        }
        return new(build, decision, request);
    }

    public async Task<ApprovalGrant> GrantAsync(
        ApprovalRequest request,
        string approvedBy,
        DateTimeOffset expiresAt,
        string scope,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(approvedBy)) throw new ArgumentException("Approver identity is required.", nameof(approvedBy));
        var now = timeProvider.GetUtcNow();
        if (expiresAt <= now) throw new ArgumentOutOfRangeException(nameof(expiresAt), "Approval expiration must be in the future.");
        var grant = new ApprovalGrant(Guid.NewGuid(), request.Id, request.ProposalHash, approvedBy, now, expiresAt, scope, notes, null);
        await approvalStore.SaveGrantAsync(grant, cancellationToken);
        await approvalStore.UpdateRequestStatusAsync(request.Id, ApprovalRequestStatus.Approved, cancellationToken);
        await auditStore.AppendAsync(new GovernanceAuditEvent(Guid.NewGuid(), "approval.granted", request.Id.ToString("N"), request.ActionProposalId,
            request.ProposalHash, null, approvedBy, "granted", [request.RiskClassification.ToString()], now), cancellationToken);
        return grant;
    }

    public async Task DenyAsync(ApprovalRequest request, string deniedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deniedBy)) throw new ArgumentException("Decider identity is required.", nameof(deniedBy));
        var now = timeProvider.GetUtcNow();
        await approvalStore.UpdateRequestStatusAsync(request.Id, ApprovalRequestStatus.Rejected, cancellationToken);
        await auditStore.AppendAsync(new GovernanceAuditEvent(Guid.NewGuid(), "approval.denied", request.Id.ToString("N"), request.ActionProposalId,
            request.ProposalHash, null, deniedBy, "denied", [request.RiskClassification.ToString()], now), cancellationToken);
    }

    public async Task RevokeAsync(ApprovalGrant grant, string revokedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(revokedBy)) throw new ArgumentException("Revoker identity is required.", nameof(revokedBy));
        var now = timeProvider.GetUtcNow();
        await approvalStore.RevokeGrantAsync(grant.Id, now, cancellationToken);
        await approvalStore.UpdateRequestStatusAsync(grant.ApprovalRequestId, ApprovalRequestStatus.Revoked, cancellationToken);
        await auditStore.AppendAsync(new GovernanceAuditEvent(Guid.NewGuid(), "approval.revoked", grant.ApprovalRequestId.ToString("N"), null,
            grant.ProposalHash, null, revokedBy, "revoked", [], now), cancellationToken);
    }

    public Task<ToolGatewayResult> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default) =>
        toolGateway.InvokeAsync(request with { ExecutionMode = request.ExecutionMode }, cancellationToken);

    private static GovernanceAuditEvent Event(string type, StructuredActionContext context, ActionProposal? proposal, string? agentId, string outcome, IReadOnlyList<string> categories, DateTimeOffset now) =>
        new(Guid.NewGuid(), type, context.RequestId, proposal?.Id, proposal?.ProposalHash, agentId, null, outcome, categories, now);
}
