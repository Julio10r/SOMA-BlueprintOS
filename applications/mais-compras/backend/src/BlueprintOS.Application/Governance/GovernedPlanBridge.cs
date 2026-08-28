#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Application.Governance;

/// <summary>
/// Payload contract produced by <c>tools/agents/governed-orchestrator.js</c>
/// (<c>GovernedOrchestrator.buildActionProposalPayload</c>) once a plan reaches
/// <c>READY_FOR_GOVERNANCE</c>, and by the plan-only mode of
/// <c>scripts/linx_wise_daily_integration.py</c>. Field names are PascalCase
/// here; the JS/Python producers emit camelCase JSON, which deserializes into
/// this record via <see cref="System.Text.Json.JsonSerializerOptions.PropertyNameCaseInsensitive"/>.
/// This is the process-boundary contract for WAVE A: the Orchestrator (JS) never
/// calls into this type directly — it only emits this shape; some out-of-band
/// step (CLI, test, or future service) hands the JSON to this bridge.
/// </summary>
public sealed record GovernedPlanPayload(
    string RequestId,
    string RequestedBy,
    string AgentId,
    string Capability,
    string Environment,
    string System,
    string ResourceType,
    string Resource,
    string OperationIntent,
    IReadOnlyList<string> Fields,
    string? FilterSummary,
    int? ExpectedAffectedRows,
    string Purpose,
    string DataClassification,
    bool ContainsPersonalData,
    bool ContainsSensitivePersonalData,
    bool ContainsSecrets,
    string Reversibility,
    string? RunbookReference,
    string? ConnectionProfile,
    string? AdditionalContext,
    IReadOnlyList<string>? CrossCuttingAgents = null);

/// <summary>
/// Converts a <see cref="GovernedPlanPayload"/> produced upstream by the
/// Governed Orchestrator (or the WISE plan-only script) into a real
/// <see cref="StructuredActionContext"/> / <see cref="RoutingEvidence"/> /
/// <see cref="AgentWriteAnalysis"/> triple and hands it to
/// <see cref="GovernedWriteStack.PrepareAsync"/>. This is the only place that
/// crosses the JS-planning / .NET-governance boundary; it grants no
/// authorization itself — <see cref="GovernedWriteStack"/> still runs the real
/// AIGovernancePolicyEngine and ApprovalPolicy checks.
/// </summary>
public sealed class GovernedPlanBridge(GovernedWriteStack writeStack)
{
    public Task<GovernedWritePreparation> PrepareAsync(GovernedPlanPayload payload, CancellationToken cancellationToken = default)
    {
        var (context, routing, analysis) = BuildTriple(payload);
        return writeStack.PrepareAsync(context, routing, analysis, cancellationToken);
    }

    /// <summary>
    /// The same <see cref="StructuredActionContext"/>/<see cref="RoutingEvidence"/>/<see cref="AgentWriteAnalysis"/>
    /// construction <see cref="PrepareAsync"/> uses internally, exposed so a second caller that needs the SAME
    /// triple outside of <see cref="GovernedWriteStack.PrepareAsync"/> (for example, a live-execution CLI that
    /// hands the triple to <c>GovernedWriteExecutionOrchestrator.ExecuteAsync</c> instead) can build it once,
    /// identically — never by re-deriving the mapping from a payload a second, divergent way, which would risk
    /// producing a proposal whose hash silently differs from the one an earlier `propose` step persisted.
    /// </summary>
    public static (StructuredActionContext Context, RoutingEvidence Routing, AgentWriteAnalysis Analysis) BuildTriple(GovernedPlanPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var context = new StructuredActionContext(
            RequestId: payload.RequestId,
            RequestedBy: payload.RequestedBy,
            Environment: Enum.Parse<GovernanceEnvironment>(payload.Environment, ignoreCase: true),
            System: payload.System,
            ResourceType: Enum.Parse<ActionResourceType>(payload.ResourceType, ignoreCase: true),
            Resource: payload.Resource,
            OperationIntent: Enum.Parse<OperationIntent>(payload.OperationIntent, ignoreCase: true),
            RequestedCapabilities: [payload.Capability],
            Fields: payload.Fields,
            FilterSummary: payload.FilterSummary,
            ExpectedAffectedRows: payload.ExpectedAffectedRows,
            Purpose: payload.Purpose,
            DataClassification: Enum.Parse<DataClassification>(payload.DataClassification, ignoreCase: true),
            ContainsPersonalData: payload.ContainsPersonalData,
            ContainsSensitivePersonalData: payload.ContainsSensitivePersonalData,
            ContainsSecrets: payload.ContainsSecrets,
            Reversibility: Enum.Parse<ActionReversibility>(payload.Reversibility, ignoreCase: true),
            RunbookReference: payload.RunbookReference,
            ConnectionProfile: payload.ConnectionProfile,
            AdditionalContext: payload.AdditionalContext);

        var routing = new RoutingEvidence(
            RoutingResolved: true,
            PrimaryAgent: payload.AgentId,
            ComplementaryAgents: [],
            CrossCuttingAgents: payload.CrossCuttingAgents ?? [],
            CapabilityGaps: [],
            RoutingConflicts: []);

        var analysis = new AgentWriteAnalysis(
            AgentId: payload.AgentId,
            Capability: payload.Capability,
            Fields: payload.Fields,
            FilterSummary: payload.FilterSummary,
            ExpectedAffectedRows: payload.ExpectedAffectedRows,
            Reversibility: Enum.Parse<ActionReversibility>(payload.Reversibility, ignoreCase: true));

        return (context, routing, analysis);
    }
}
