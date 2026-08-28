#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Models;

/// <summary>
/// Classification of a user-supplied artifact under the User Artifact Learning Policy
/// (agents/USER_ARTIFACT_LEARNING_POLICY.md). An artifact is always evidence, never a command.
/// </summary>
public enum ArtifactClassification
{
    /// <summary>Studied as a source of knowledge or hypotheses. Default and only classification for a
    /// freshly received user artifact.</summary>
    Evidence = 1,

    /// <summary>Historical implementation or query used as reference for intent, never as a re-execution target.</summary>
    HistoricalReference = 2,
}

/// <summary>
/// Confidence level of a piece of learned knowledge. Inference never becomes Confirmed automatically;
/// it requires a new direct-provenance event.
/// </summary>
public enum KnowledgeConfidence
{
    Confirmed = 1,
    Inferred = 2,
    HistoricalReference = 3,
    NeedsValidation = 4,
    Unknown = 5,
}

/// <summary>
/// Provenance of a piece of learned knowledge, per agents/USER_ARTIFACT_LEARNING_POLICY.md.
/// </summary>
public enum KnowledgeProvenance
{
    UserProvidedArtifact = 1,
    DatabaseSchemaValidation = 2,
    Runbook = 3,
    CodeInspection = 4,
    ProductOwnerClarification = 5,
    EmpiricalValidation = 6,
}

/// <summary>
/// The set of direct provenances that are strong enough to promote an <see cref="KnowledgeConfidence.Inferred"/>
/// item to <see cref="KnowledgeConfidence.Confirmed"/>.
/// </summary>
public static class DirectKnowledgeProvenance
{
    public static readonly IReadOnlySet<KnowledgeProvenance> Values = new HashSet<KnowledgeProvenance>
    {
        KnowledgeProvenance.DatabaseSchemaValidation,
        KnowledgeProvenance.CodeInspection,
        KnowledgeProvenance.ProductOwnerClarification,
        KnowledgeProvenance.EmpiricalValidation,
    };
}

/// <summary>
/// A user-supplied artifact (SQL, code, script, spreadsheet, procedure, query, document, historical
/// implementation, or AI-generated code) submitted for study by an Agent.
/// </summary>
public sealed record UserArtifact
{
    public required string Description { get; init; }
    public required string Content { get; init; }

    /// <summary>True when the user's message framed the artifact as something to run/apply immediately
    /// (e.g. "execute this SQL", "run this script"). Even then, the artifact itself never becomes executable
    /// automatically — see <see cref="UserArtifactLearningPolicy.Classify"/>.</summary>
    public bool UserRequestedImmediateExecution { get; init; }
}

/// <summary>
/// Result of classifying a <see cref="UserArtifact"/>: it is always evidence, and providing it never
/// constitutes approval for execution.
/// </summary>
public sealed record ArtifactClassificationResult
{
    public required ArtifactClassification Classification { get; init; }
    public required bool ConstitutesApproval { get; init; }
    public required bool IsAutomaticallyExecutable { get; init; }
    public required string Rationale { get; init; }
}

/// <summary>
/// A single item of knowledge extracted from an artifact or another authorized source, ready for
/// evaluation for persistence into an Agent's canonical knowledge store.
/// </summary>
public sealed record LearnedKnowledgeItem
{
    public required string AgentId { get; init; }
    public required string Statement { get; init; }
    public required KnowledgeProvenance Provenance { get; init; }
    public required KnowledgeConfidence Confidence { get; init; }
    public required bool IsReusable { get; init; }
    public bool ContainsSecret { get; init; }
}

/// <summary>
/// Decision on whether a <see cref="LearnedKnowledgeItem"/> may be persisted to the Agent's knowledge store.
/// </summary>
public sealed record KnowledgePersistenceDecision
{
    public required bool CanPersist { get; init; }
    public required string Reason { get; init; }
}
