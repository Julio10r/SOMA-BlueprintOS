#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>Request to move a connection profile to a NEW write verification policy version.</summary>
public sealed record WriteVerificationProfileChangeRequest(
    string RequestId,
    string RequestingAgent,
    string RequestedBy,
    GovernanceEnvironment Environment,
    WriteVerificationProfile ProposedVersion,
    string Purpose);

/// <summary>A profile change that has been turned into a governed <see cref="ActionProposal"/> and evaluated.
/// Nothing has been written to the profile store at this point.</summary>
public sealed record WriteVerificationProfileChangeProposal(
    ActionProposal Proposal,
    PolicyDecision Decision,
    WriteVerificationProfile ProposedVersion,
    WriteVerificationProfile? CurrentVersion,
    bool ReducesGuarantees);

/// <summary>
/// Changing a write verification policy is itself a governed action. There is deliberately no
/// <c>SetAsync</c> on this service: a change becomes an <see cref="ActionProposal"/> with
/// <see cref="ActionResourceType.GovernancePolicy"/> and <see cref="ActionOperation.Create"/>, is evaluated
/// by <see cref="IAIGovernancePolicyEngine"/>, and can only be appended to the store once the resulting
/// decision permits it (with a valid approval grant when the decision requires approval).
///
/// Reducing protection in Production is Red/Blocked by a fixed rule in the policy engine — no approval
/// can unblock it through this service.
/// </summary>
public sealed class WriteVerificationProfileGovernanceService(
    IWriteVerificationProfileStore profileStore,
    IAIGovernancePolicyEngine policyEngine,
    IApprovalPolicy approvalPolicy,
    IGovernanceAuditStore auditStore,
    TimeProvider timeProvider)
{
    public const string ResourcePrefix = "write-verification-profile:";

    public async Task<WriteVerificationProfileChangeProposal> ProposeAsync(
        WriteVerificationProfileChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = timeProvider.GetUtcNow();
        var proposed = request.ProposedVersion;
        var current = await profileStore.ResolveAsync(proposed.ConnectionProfile, now, cancellationToken);
        var reduces = current is not null && current.ReducesGuaranteesComparedTo(proposed);

        var proposal = new ActionProposal
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            RequestingAgent = request.RequestingAgent,
            Environment = request.Environment,
            System = "BlueprintOS/Governance",
            ResourceType = ActionResourceType.GovernancePolicy,
            Resource = ResourcePrefix + proposed.ConnectionProfile,
            Operation = ActionOperation.Create,
            Fields = ["backup_required", "rollback_supported", "backup_retention_days", "post_write_validation_required"],
            FilterSummary = $"connection_profile={proposed.ConnectionProfile}; new_policy_version={proposed.PolicyVersion}",
            ExpectedAffectedRows = 1,
            Purpose = request.Purpose,
            DataClassification = DataClassification.Internal,
            ContainsPersonalData = false,
            ContainsSensitivePersonalData = false,
            ContainsSecrets = false,
            Reversibility = ActionReversibility.Reversible,
            AdditionalContext =
                $"current_policy_version={current?.PolicyVersion ?? "<none>"}; " +
                $"backup_required {Show(current?.BackupRequired)}->{proposed.BackupRequired}; " +
                $"rollback_supported {Show(current?.RollbackSupported)}->{proposed.RollbackSupported}; " +
                $"post_write_validation_required {Show(current?.PostWriteValidationRequired)}->{proposed.PostWriteValidationRequired}",
            ReducesWriteSafetyGuarantees = reduces,
        };

        var decision = policyEngine.Evaluate(proposal, now);
        await auditStore.AppendAsync(new GovernanceAuditEvent(
            Guid.NewGuid(), "write-verification-profile.proposed", request.RequestId, proposal.Id, proposal.ProposalHash,
            request.RequestingAgent, request.RequestedBy, decision.Status.ToString(),
            [decision.RiskClassification.ToString(), reduces ? "REDUCES_WRITE_SAFETY_GUARANTEES" : "PRESERVES_WRITE_SAFETY_GUARANTEES"],
            now), cancellationToken);

        return new(proposal, decision, proposed, current, reduces);
    }

    /// <summary>Appends the proposed version once — and only once — governance permits it. Throws otherwise;
    /// there is no path that writes a policy version without a passing decision.</summary>
    public async Task<WriteVerificationProfile> ApplyAsync(
        WriteVerificationProfileChangeProposal proposal,
        ApprovalGrant? grant,
        string requestId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        var now = timeProvider.GetUtcNow();

        if (proposal.Decision.Status == PolicyDecisionStatus.Blocked || proposal.Decision.RiskClassification == RiskClassification.Red)
        {
            await AuditRejectionAsync(requestId, proposal, "POLICY_BLOCKED", now, cancellationToken);
            throw new InvalidOperationException(
                $"Write verification profile change for '{proposal.ProposedVersion.ConnectionProfile}' is blocked by governance: {string.Join(" | ", proposal.Decision.Reasons)}");
        }

        if (proposal.Decision.Status == PolicyDecisionStatus.RequiresApproval
            && (grant is null || !approvalPolicy.IsGrantValidFor(proposal.Proposal, grant, now)))
        {
            await AuditRejectionAsync(requestId, proposal, "VALID_APPROVAL_REQUIRED", now, cancellationToken);
            throw new InvalidOperationException(
                $"Write verification profile change for '{proposal.ProposedVersion.ConnectionProfile}' requires a valid approval grant bound to this exact proposal.");
        }

        await profileStore.AppendVersionAsync(proposal.ProposedVersion, cancellationToken);
        await auditStore.AppendAsync(new GovernanceAuditEvent(
            Guid.NewGuid(), "write-verification-profile.applied", requestId, proposal.Proposal.Id, proposal.Proposal.ProposalHash,
            proposal.Proposal.RequestingAgent, grant?.ApprovedBy, "applied",
            [proposal.ProposedVersion.PolicyVersion], now), cancellationToken);
        return proposal.ProposedVersion;
    }

    private Task AuditRejectionAsync(string requestId, WriteVerificationProfileChangeProposal proposal, string reason, DateTimeOffset now, CancellationToken ct) =>
        auditStore.AppendAsync(new GovernanceAuditEvent(
            Guid.NewGuid(), "write-verification-profile.rejected", requestId, proposal.Proposal.Id, proposal.Proposal.ProposalHash,
            proposal.Proposal.RequestingAgent, null, "blocked", [reason], now), ct);

    private static string Show(bool? value) => value?.ToString() ?? "<none>";
}
