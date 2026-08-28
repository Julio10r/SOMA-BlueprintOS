#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

public sealed class SomaLinxDryRunAdapter : IGovernedToolAdapter
{
    public string Capability => StructuredActionProposalAdapter.Capability;
    public string OwnerAgent => StructuredActionProposalAdapter.OwnerAgent;
    public IReadOnlyList<string> AllowedConnectionProfiles { get; } = ["linx-erp-governed-write"];

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
