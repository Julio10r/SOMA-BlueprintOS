#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>
/// Formal Gateway-registered adapter for read-only Linx/SOMA access
/// (schema discovery, vendor/filial/centro-de-custo lookups). The underlying
/// SQL readers in BlueprintOS.Infrastructure.Integrations.ERP.Soma remain
/// SELECT-only by construction; this adapter is the governed front door that
/// makes that access routable/auditable through the same Tool Gateway used
/// for governed writes, without requiring approval for a plain read when the
/// Policy Engine does not demand it.
/// </summary>
public sealed class SomaLinxReadOnlyAdapter : IGovernedToolAdapter
{
    public const string Capability = "soma-database-read-proposal";
    public const string OwnerAgent = "linx-database-specialist-agent";

    string IGovernedToolAdapter.Capability => Capability;
    string IGovernedToolAdapter.OwnerAgent => OwnerAgent;
    public IReadOnlyList<string> AllowedConnectionProfiles { get; } = ["linx-erp-read-only", "linx-erp-governed-write"];

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
