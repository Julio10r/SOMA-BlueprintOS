#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>
/// Governed Gateway front door for linx-erp-specialist-agent's real external
/// access path: ConnectionStrings:MaisComprasConnection (manifest profile
/// "linx-knowledge-store", agents/linx-erp-specialist-agent/agent.yaml), used
/// today by BlueprintOS.Infrastructure.Persistence.B1ConnectivityValidator for
/// read-only connectivity checks (the `validate-maiscompras` CLI command).
/// This adapter does not replace that reader — it gives the Gateway a
/// routable/auditable entry for the same read-only capability, resolving the
/// AFV2-GATEWAY-001 finding for this Agent as a real (not fabricated) adapter,
/// since the underlying external access genuinely exists and is read-only by
/// construction (SELECT 1 / SUSER_SNAME() connectivity probes only).
/// </summary>
public sealed class LinxKnowledgeStoreReadOnlyAdapter : IGovernedToolAdapter
{
    public const string Capability = "linx-erp-knowledge-read-proposal";
    public const string OwnerAgent = "linx-erp-specialist-agent";

    string IGovernedToolAdapter.Capability => Capability;
    string IGovernedToolAdapter.OwnerAgent => OwnerAgent;
    public IReadOnlyList<string> AllowedConnectionProfiles { get; } = ["linx-knowledge-store"];

    public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var proposal = request.Proposal;
        var preview = new SomaLinxDryRunPreview(
            proposal.System,
            proposal.Environment,
            proposal.Resource,
            proposal.Operation,
            proposal.Fields,
            proposal.FilterSummary,
            proposal.ExpectedAffectedRows,
            proposal.Purpose,
            request.ConnectionProfile,
            request.PolicyDecision.RiskClassification,
            request.PolicyDecision.Status,
            request.ApprovalGrant is null ? "not-required-or-not-present" : "valid-grant-present",
            proposal.Reversibility,
            GovernedExecutionMode.DryRun,
            CredentialResolutionRequired: true,
            IdentityPermissionCheckRequired: true,
            SqlGenerated: false,
            ExternalExecutionPerformed: false);
        return Task.FromResult(preview);
    }
}
