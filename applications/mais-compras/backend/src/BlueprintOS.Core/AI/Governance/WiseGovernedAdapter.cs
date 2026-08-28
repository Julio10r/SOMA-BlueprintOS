#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>
/// Governed DryRun adapter for the WISE write path. This is the counterpart to
/// the WAVE 0 containment applied to scripts/linx_wise_daily_integration.py:
/// the script's plan-only output (governed_write_plan.json) is the same shape
/// this adapter previews, so a WISE write can be routed through the Gateway
/// like any other governed capability instead of running pyodbc directly.
/// Never opens a WISE connection and never generates SQL — DryRun only, same
/// as every other adapter registered on this Gateway.
/// </summary>
public sealed class WiseGovernedAdapter : IGovernedToolAdapter
{
    public const string Capability = "wise-database-write-proposal";
    public const string OwnerAgent = "wise-agent";

    string IGovernedToolAdapter.Capability => Capability;
    string IGovernedToolAdapter.OwnerAgent => OwnerAgent;
    public IReadOnlyList<string> AllowedConnectionProfiles { get; } = ["wise-governed-write"];

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
