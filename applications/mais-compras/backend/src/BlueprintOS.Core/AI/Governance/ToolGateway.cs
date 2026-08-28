#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

public sealed class ToolGateway(
    IEnumerable<IGovernedToolAdapter> adapters,
    IApprovalPolicy approvalPolicy,
    IGovernanceAuditStore auditStore,
    TimeProvider timeProvider) : IToolGateway
{
    public async Task<ToolGatewayResult> InvokeAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var matchingAdapters = adapters.Where(adapter => string.Equals(adapter.Capability, request.Capability, StringComparison.Ordinal)).ToList();
        var adapter = matchingAdapters.Count == 1 ? matchingAdapters[0] : null;
        var reasons = Validate(request, adapter, matchingAdapters.Count, now);
        await AuditAsync(request, "gateway.dry-run.requested", reasons.Count == 0 ? "accepted" : "blocked", reasons, now, cancellationToken);
        if (reasons.Count > 0)
        {
            await AuditAsync(request, "gateway.blocked", "blocked", reasons, now, cancellationToken);
            return new(ToolGatewayStatus.Blocked, reasons, null);
        }

        var preview = await adapter!.DryRunAsync(request, cancellationToken);
        await AuditAsync(request, "gateway.dry-run.completed", "completed", ["DRY_RUN_ONLY"], now, cancellationToken);
        return new(ToolGatewayStatus.DryRunCompleted, ["DRY_RUN_ONLY", "NO_EXTERNAL_EXECUTION"], preview);
    }

    private List<string> Validate(ToolGatewayRequest request, IGovernedToolAdapter? adapter, int matchingAdapterCount, DateTimeOffset now)
    {
        var reasons = new List<string>();
        if (request.ExecutionMode == GovernedExecutionMode.LiveExecution) reasons.Add("LIVE_EXECUTION_DISABLED");
        if (!request.RoutingResolved) reasons.Add("ROUTING_NOT_RESOLVED");
        if (matchingAdapterCount == 0) reasons.Add("CAPABILITY_NOT_REGISTERED");
        if (matchingAdapterCount > 1) reasons.Add("ADAPTER_NOT_UNIQUELY_REGISTERED");
        if (adapter is not null && !string.Equals(request.RoutedPrimaryAgent, adapter.OwnerAgent, StringComparison.Ordinal)) reasons.Add("OWNER_MISMATCH");
        if (!string.Equals(request.Proposal.RequestingAgent, request.RoutedPrimaryAgent, StringComparison.Ordinal)) reasons.Add("PROPOSAL_AGENT_MISMATCH");
        if (request.Proposal.Environment == GovernanceEnvironment.Unknown) reasons.Add("ENVIRONMENT_REQUIRED");
        if (request.Proposal.Environment == GovernanceEnvironment.Production
            && request.Proposal.Operation is (ActionOperation.Insert or ActionOperation.Update or ActionOperation.Delete or ActionOperation.Truncate or ActionOperation.Merge)
            && !request.CrossCuttingAgents.Contains("security-lgpd-agent", StringComparer.Ordinal))
            reasons.Add("SECURITY_LGPD_REVIEW_REQUIRED");
        if (string.IsNullOrWhiteSpace(request.ConnectionProfile)) reasons.Add("CONNECTION_PROFILE_REQUIRED");
        else if (adapter is not null && !adapter.AllowedConnectionProfiles.Contains(request.ConnectionProfile, StringComparer.Ordinal))
            reasons.Add("CONNECTION_PROFILE_NOT_GOVERNED");
        if (string.IsNullOrWhiteSpace(request.Identity.SubjectId)) reasons.Add("IDENTITY_REQUIRED");
        if (!request.Identity.HasEffectivePermission) reasons.Add("IDENTITY_PERMISSION_DENIED");
        if (request.Identity.PrivilegeEscalationAllowed) reasons.Add("PRIVILEGE_ESCALATION_FORBIDDEN");
        if (request.PolicyDecision.ActionProposalId != request.Proposal.Id
            || !string.Equals(request.PolicyDecision.ProposalHash, request.Proposal.ProposalHash, StringComparison.Ordinal))
            reasons.Add("POLICY_DECISION_PROPOSAL_MISMATCH");
        if (request.PolicyDecision.Status == PolicyDecisionStatus.Blocked || request.PolicyDecision.RiskClassification == RiskClassification.Red)
            reasons.Add("POLICY_BLOCKED");
        if (request.PolicyDecision.Status == PolicyDecisionStatus.RequiresApproval
            && (request.ApprovalGrant is null || !approvalPolicy.IsGrantValidFor(request.Proposal, request.ApprovalGrant, now)))
            reasons.Add("VALID_APPROVAL_REQUIRED");
        return reasons.Distinct(StringComparer.Ordinal).ToList();
    }

    private Task AuditAsync(ToolGatewayRequest request, string eventType, string outcome, IReadOnlyList<string> categories, DateTimeOffset now, CancellationToken ct) =>
        auditStore.AppendAsync(new GovernanceAuditEvent(
            Guid.NewGuid(), eventType, request.Proposal.Id.ToString("N"), request.Proposal.Id,
            request.Proposal.ProposalHash, request.RoutedPrimaryAgent, request.Identity.SubjectId,
            outcome, categories, now), ct);
}
