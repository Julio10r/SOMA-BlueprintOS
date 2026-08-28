#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

public sealed class StructuredActionProposalAdapter : IActionProposalAdapter
{
    // Default capability/owner constants, preserved for the original Linx/SOMA
    // governed-write path and for any caller that still references them directly.
    // Build() itself no longer hardcodes a single owner/capability — it now
    // requires internal consistency (routing.PrimaryAgent == analysis.AgentId,
    // and a non-empty analysis.Capability) so the same adapter can build a valid
    // ActionProposal for any capability/owner pair resolved by the Runtime
    // Registry (e.g. wise-agent + wise-database-write-proposal), not only for
    // linx-database-specialist-agent. Ownership enforcement for execution still
    // happens at the Tool Gateway via IGovernedToolAdapter.OwnerAgent.
    public const string Capability = "soma-database-write-proposal";
    public const string OwnerAgent = "linx-database-specialist-agent";

    public ActionProposalBuildResult Build(
        StructuredActionContext context,
        RoutingEvidence routing,
        AgentWriteAnalysis analysis,
        DateTimeOffset now)
    {
        var gaps = new List<ActionProposalContextGap>();
        Required(context.RequestId, "request_id", gaps);
        Required(context.RequestedBy, "requested_by", gaps);
        Required(context.System, "system", gaps);
        Required(context.Resource, "resource", gaps);
        Required(context.Purpose, "purpose", gaps);
        Required(analysis.AgentId, "analysis.agent_id", gaps);
        Required(analysis.Capability, "analysis.capability", gaps);
        if (context.Environment == GovernanceEnvironment.Unknown) gaps.Add(new("environment", "ACTION_PROPOSAL_CONTEXT_GAP"));
        if (context.ResourceType == ActionResourceType.Unknown) gaps.Add(new("resource_type", "ACTION_PROPOSAL_CONTEXT_GAP"));
        if (context.OperationIntent == OperationIntent.Unknown) gaps.Add(new("operation_intent", "ACTION_PROPOSAL_CONTEXT_GAP"));
        if (!routing.RoutingResolved || routing.CapabilityGaps.Count > 0 || routing.RoutingConflicts.Count > 0)
            gaps.Add(new("routing", "ACTION_PROPOSAL_ROUTING_GAP"));
        if (!string.Equals(routing.PrimaryAgent, analysis.AgentId, StringComparison.Ordinal)) gaps.Add(new("primary_agent", "ACTION_PROPOSAL_OWNER_GAP"));

        var operation = MapOperation(context.OperationIntent);
        if (operation is (ActionOperation.Insert or ActionOperation.Update or ActionOperation.Merge) && analysis.Fields.Count == 0)
            gaps.Add(new("fields", "ACTION_PROPOSAL_CONTEXT_GAP"));
        if (operation is (ActionOperation.Insert or ActionOperation.Update or ActionOperation.Merge or ActionOperation.Export)
            && analysis.ExpectedAffectedRows is null)
            gaps.Add(new("expected_affected_rows", "ACTION_PROPOSAL_CONTEXT_GAP"));

        if (gaps.Count > 0) return new(null, gaps);

        return new(new ActionProposal
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            RequestingAgent = analysis.AgentId,
            Environment = context.Environment,
            System = context.System,
            ResourceType = context.ResourceType,
            Resource = context.Resource,
            Operation = operation,
            Fields = analysis.Fields,
            FilterSummary = analysis.FilterSummary,
            ExpectedAffectedRows = analysis.ExpectedAffectedRows,
            Purpose = context.Purpose,
            DataClassification = context.DataClassification,
            ContainsPersonalData = context.ContainsPersonalData,
            ContainsSensitivePersonalData = context.ContainsSensitivePersonalData,
            ContainsSecrets = context.ContainsSecrets,
            Reversibility = analysis.Reversibility,
            RunbookReference = context.RunbookReference,
            IsRunbookApprovedOperation = analysis.IsRunbookApprovedOperation,
            RunbookExpectedAffectedRows = analysis.RunbookExpectedAffectedRows,
            AdditionalContext = context.AdditionalContext,
        }, Array.Empty<ActionProposalContextGap>());
    }

    private static ActionOperation MapOperation(OperationIntent intent) => intent switch
    {
        OperationIntent.Read => ActionOperation.Select,
        OperationIntent.Analyze => ActionOperation.Analyze,
        OperationIntent.Export => ActionOperation.Export,
        OperationIntent.Create => ActionOperation.Insert,
        OperationIntent.Update => ActionOperation.Update,
        OperationIntent.Delete => ActionOperation.Delete,
        OperationIntent.Truncate => ActionOperation.Truncate,
        OperationIntent.ExecuteWorkflow => ActionOperation.ExecuteProcedure,
        OperationIntent.Configure => ActionOperation.Alter,
        _ => ActionOperation.Unknown,
    };

    private static void Required(string value, string field, ICollection<ActionProposalContextGap> gaps)
    {
        if (string.IsNullOrWhiteSpace(value)) gaps.Add(new(field, "ACTION_PROPOSAL_CONTEXT_GAP"));
    }
}
