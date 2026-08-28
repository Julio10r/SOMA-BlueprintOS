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
        var requestedEvent = request.ExecutionMode == GovernedExecutionMode.LiveExecution
            ? "gateway.live-execution.requested"
            : "gateway.dry-run.requested";
        await AuditAsync(request, requestedEvent, reasons.Count == 0 ? "accepted" : "blocked", reasons, now, cancellationToken);
        if (reasons.Count > 0)
        {
            await AuditAsync(request, "gateway.blocked", "blocked", reasons, now, cancellationToken);
            return new(ToolGatewayStatus.Blocked, reasons, null);
        }

        if (request.ExecutionMode == GovernedExecutionMode.LiveExecution)
        {
            return await ExecuteLiveAsync(request, (IWriteExecutionAdapter)adapter!, now, cancellationToken);
        }

        var preview = await adapter!.DryRunAsync(request, cancellationToken);
        await AuditAsync(request, "gateway.dry-run.completed", "completed", ["DRY_RUN_ONLY"], now, cancellationToken);
        return new(ToolGatewayStatus.DryRunCompleted, ["DRY_RUN_ONLY", "NO_EXTERNAL_EXECUTION"], preview);
    }

    /// <summary>
    /// Runs the real write. Reachable only after <see cref="Validate"/> has confirmed every ordinary
    /// governance check AND every live-execution guarantee, which is why this method itself performs no
    /// further permission logic — there is exactly one gate, above.
    /// </summary>
    private async Task<ToolGatewayResult> ExecuteLiveAsync(
        ToolGatewayRequest request,
        IWriteExecutionAdapter adapter,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await AuditAsync(request, "gateway.live-execution.started", "started",
            [request.WriteVerificationProfile!.PolicyVersion, request.PostWriteValidationRule?.RuleId ?? "NO_VALIDATION_RULE"],
            now, cancellationToken);

        WriteExecutionResult execution;
        try
        {
            execution = await adapter.ExecuteAsync(request, request.RecoveryPackageReceipt, cancellationToken);
        }
        catch (Exception ex)
        {
            // The adapter's own failure taxonomy is preserved by the adapter; here the gateway only records
            // that the live attempt ended in an error and returns a non-success status. It never swallows the
            // failure into a "completed" result.
            await AuditAsync(request, "gateway.live-execution.failed", "failed", ["ADAPTER_EXCEPTION"], now, cancellationToken);
            return new(ToolGatewayStatus.LiveExecutionFailed, ["LIVE_EXECUTION_ADAPTER_FAILED"], null,
                LiveExecutionEnabled: true, Execution: new WriteExecutionResult(false, 0, [], ["LIVE_EXECUTION_ADAPTER_FAILED"], ex.Message));
        }

        var status = execution.Succeeded ? ToolGatewayStatus.LiveExecutionCompleted : ToolGatewayStatus.LiveExecutionFailed;
        await AuditAsync(request, execution.Succeeded ? "gateway.live-execution.completed" : "gateway.live-execution.failed",
            execution.Succeeded ? "completed" : "failed",
            execution.Reasons.Count > 0 ? execution.Reasons : [status.ToString()], now, cancellationToken);

        return new(status, execution.Reasons, null, LiveExecutionEnabled: true, Execution: execution);
    }

    private List<string> Validate(ToolGatewayRequest request, IGovernedToolAdapter? adapter, int matchingAdapterCount, DateTimeOffset now)
    {
        var reasons = new List<string>();
        if (request.ExecutionMode == GovernedExecutionMode.LiveExecution) reasons.AddRange(ValidateLiveExecutionGuarantees(request, adapter, now));
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

    /// <summary>
    /// The live-execution gate. This replaced the former unconditional <c>LIVE_EXECUTION_DISABLED</c> branch,
    /// and it is deliberately written so that the DEFAULT is still "blocked": a request that carries none of
    /// the new guarantees (the shape every pre-existing caller produces) fails the very first check and gets
    /// <c>LIVE_EXECUTION_DISABLED</c> exactly as before. Live execution becomes possible only when every
    /// guarantee the resolved policy demands is present in the request.
    ///
    /// Note what is NOT checked here and is checked by the caller of Validate instead: policy decision status,
    /// approval validity, identity, owner and connection profile. Those already block the request through the
    /// ordinary path, and duplicating them here would create a second, divergent authorization surface.
    /// </summary>
    private static List<string> ValidateLiveExecutionGuarantees(ToolGatewayRequest request, IGovernedToolAdapter? adapter, DateTimeOffset now)
    {
        var reasons = new List<string>();
        var profile = request.WriteVerificationProfile;

        if (profile is null)
        {
            reasons.Add("WRITE_VERIFICATION_PROFILE_REQUIRED");
        }
        else
        {
            if (!string.Equals(profile.ConnectionProfile, request.ConnectionProfile, StringComparison.Ordinal))
            {
                reasons.Add("WRITE_VERIFICATION_PROFILE_MISMATCH");
            }

            if (profile.BackupRequired && !IsUsableReceipt(request.RecoveryPackageReceipt, now))
            {
                reasons.Add("RECOVERY_PACKAGE_REQUIRED_BEFORE_LIVE_EXECUTION");
            }

            if (profile.PostWriteValidationRequired && request.PostWriteValidationRule is null)
            {
                reasons.Add("WRITE_VALIDATION_RULE_UNKNOWN");
            }
            else if (request.PostWriteValidationRule is not null
                && !request.PostWriteValidationRule.Covers(request.Proposal.Operation, request.Proposal.Resource))
            {
                reasons.Add("WRITE_VALIDATION_RULE_DOES_NOT_COVER_OPERATION");
            }
        }

        if (adapter is not IWriteExecutionAdapter)
        {
            reasons.Add("LIVE_EXECUTION_ADAPTER_NOT_CAPABLE");
        }

        // Keep the historical reason code on every live block, so existing callers and audits that look for
        // LIVE_EXECUTION_DISABLED keep working, and prepend it so it reads first.
        if (reasons.Count > 0) reasons.Insert(0, "LIVE_EXECUTION_DISABLED");
        return reasons;
    }

    /// <summary>A receipt is usable only if it identifies a package, carries a full-length manifest checksum,
    /// has not passed its retention expiry, and its before-state is <c>Captured</c> or <c>NotExistent</c> —
    /// never <c>CaptureFailed</c>. <c>NotExistent</c> is accepted here because <see cref="Recovery.BeforeStateEvaluator"/>
    /// only ever produces it for an operation whose semantics allow a missing prior state (a CREATE): the
    /// compatibility check happens once, where the status is computed, not again here from a bare bool.</summary>
    private static bool IsUsableReceipt(Recovery.RecoveryPackageReceipt? receipt, DateTimeOffset now) =>
        receipt is not null
        && receipt.ExecutionId != Guid.Empty
        && !string.IsNullOrWhiteSpace(receipt.PackagePath)
        && receipt.ManifestChecksumSha256.Length == 64
        && receipt.BeforeState != Recovery.BeforeStateStatus.CaptureFailed
        && receipt.ExpiresAt > now;

    private Task AuditAsync(ToolGatewayRequest request, string eventType, string outcome, IReadOnlyList<string> categories, DateTimeOffset now, CancellationToken ct) =>
        auditStore.AppendAsync(new GovernanceAuditEvent(
            Guid.NewGuid(), eventType, request.Proposal.Id.ToString("N"), request.Proposal.Id,
            request.Proposal.ProposalHash, request.RoutedPrimaryAgent, request.Identity.SubjectId,
            outcome, categories, now), ct);
}
