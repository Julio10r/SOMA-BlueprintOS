#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Models;

public enum GapResolutionOutcome
{
    /// <summary>Flow interrupted: knowledge is missing even though the capability exists.</summary>
    KnowledgeGap = 1,

    /// <summary>Flow interrupted: no existing Agent declares the required capability.</summary>
    CapabilityGap = 2,

    /// <summary>Flow interrupted: no existing Agent is a natural owner; a new Agent must be proposed
    /// (never created automatically).</summary>
    NoNaturalOwnerProposeNewAgent = 3,

    /// <summary>Request is covered by an existing Agent's capability and knowledge; execution may proceed
    /// through the normal Governed Write Stack flow.</summary>
    Covered = 4,
}

/// <summary>
/// A capability request evaluated against the Agent registry, per
/// agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md.
/// </summary>
public sealed record CapabilityRequest
{
    public required string CapabilityId { get; init; }
    public required bool CapabilityDeclaredByAnyAgent { get; init; }
    public string? OwningAgentId { get; init; }
    public required bool KnowledgeSufficient { get; init; }
    public required bool ExistingAgentIsNaturalOwnerForEvolution { get; init; }
}

public sealed record GapResolution
{
    public required GapResolutionOutcome Outcome { get; init; }
    public required bool FlowInterrupted { get; init; }
    public required bool AutomaticExecutionAllowed { get; init; }
    public required string Explanation { get; init; }
}

/// <summary>
/// A proposal to evolve an existing Agent's capabilities. Material changes require explicit human approval
/// and must never be auto-applied.
/// </summary>
public sealed record AgentEvolutionProposal
{
    public required string AgentId { get; init; }
    public required string NewCapabilityId { get; init; }
    public required bool IsMaterialChange { get; init; }
    public bool HumanApprovalGranted { get; init; }
    public string? ApprovedBy { get; init; }
}

public sealed record AgentEvolutionDecision
{
    public required bool CanApply { get; init; }
    public required bool RequiresHumanApproval { get; init; }
    public required string Reason { get; init; }
}

/// <summary>
/// A proposal to create a brand-new Agent when no existing Agent is a natural owner. Creation is never
/// automatic; it always requires explicit human approval via the Agent Factory CREATE flow.
/// </summary>
public sealed record NewAgentProposal
{
    public required string ProposedAgentId { get; init; }
    public required IReadOnlyList<string> ExistingAgentsEvaluatedAndRejected { get; init; }
    public required string CapabilityGapEvidence { get; init; }
    public bool HumanApprovalGranted { get; init; }
}

public sealed record NewAgentDecision
{
    public required bool CanCreate { get; init; }
    public required string Reason { get; init; }
}
