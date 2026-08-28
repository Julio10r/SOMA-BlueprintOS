#pragma warning disable CS1591

using System.Text.RegularExpressions;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>
/// Provider-agnostic implementation of the User Artifact Learning Policy
/// (agents/USER_ARTIFACT_LEARNING_POLICY.md). Nothing here branches on which LLM/provider is executing —
/// behavior is identical for Codex, Claude, ChatGPT or any future executor.
/// </summary>
public sealed class UserArtifactLearningPolicy
{
    private static readonly Regex SecretPattern = new(
        @"(?i)(password|passwd|pwd|secret|token|api[_-]?key|client[_-]?secret|private[_-]?key|cookie)\s*[:=]\s*\S+" +
        @"|connection\s*string\s*[:=].*\b(pwd|password)\s*=",
        RegexOptions.Compiled);

    /// <summary>
    /// Classifies a user-supplied artifact. An artifact is always Evidence or HistoricalReference; it is
    /// never automatically executable and never, by itself, constitutes approval for execution — regardless
    /// of whether the user asked for it to be run immediately (Rule 1, Rule 2, Rule 3).
    /// </summary>
    public ArtifactClassificationResult Classify(UserArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var classification = LooksHistorical(artifact)
            ? ArtifactClassification.HistoricalReference
            : ArtifactClassification.Evidence;

        var rationale = artifact.UserRequestedImmediateExecution
            ? "Artefato fornecido com pedido de execucao imediata; permanece evidencia. Execucao real depende do Governed Write Stack (ActionProposal, Policy Engine, ApprovalPolicy, ToolGateway)."
            : "Artefato de usuario classificado como evidencia/fonte de conhecimento, conforme USER_ARTIFACT_LEARNING_POLICY.md.";

        return new ArtifactClassificationResult
        {
            Classification = classification,
            ConstitutesApproval = false,
            IsAutomaticallyExecutable = false,
            Rationale = rationale,
        };
    }

    private static bool LooksHistorical(UserArtifact artifact) =>
        artifact.Description.Contains("historic", StringComparison.OrdinalIgnoreCase) ||
        artifact.Description.Contains("historico", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether a piece of learned knowledge may be persisted to the responsible Agent's
    /// canonical knowledge store. Persistence requires provenance, reusability, and the absolute absence
    /// of secrets (Rule 8, Rule 10).
    /// </summary>
    public KnowledgePersistenceDecision EvaluatePersistence(LearnedKnowledgeItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.ContainsSecret || ContainsSecretPattern(item.Statement))
        {
            return new KnowledgePersistenceDecision
            {
                CanPersist = false,
                Reason = "Item de conhecimento contem segredo/credencial e nunca pode ser persistido no knowledge store.",
            };
        }

        if (!item.IsReusable)
        {
            return new KnowledgePersistenceDecision
            {
                CanPersist = false,
                Reason = "Observacao transitoria; nao e reutilizavel o suficiente para o knowledge store canonico.",
            };
        }

        if (string.IsNullOrWhiteSpace(item.AgentId) || string.IsNullOrWhiteSpace(item.Statement))
        {
            return new KnowledgePersistenceDecision
            {
                CanPersist = false,
                Reason = "Item de conhecimento incompleto: agente responsavel e enunciado sao obrigatorios.",
            };
        }

        return new KnowledgePersistenceDecision
        {
            CanPersist = true,
            Reason = $"Conhecimento com proveniencia {item.Provenance} e confianca {item.Confidence} pode ser incorporado ao knowledge store de {item.AgentId}.",
        };
    }

    /// <summary>
    /// Promotes an Inferred item to Confirmed only when a new, direct-provenance validation event supports it
    /// (Rule 9). Any other confidence level, or a provenance that is not direct, leaves the item unchanged.
    /// </summary>
    public LearnedKnowledgeItem PromoteConfidence(LearnedKnowledgeItem item, KnowledgeProvenance validationProvenance)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Confidence != KnowledgeConfidence.Inferred)
        {
            return item;
        }

        if (!DirectKnowledgeProvenance.Values.Contains(validationProvenance))
        {
            return item;
        }

        return item with { Confidence = KnowledgeConfidence.Confirmed, Provenance = validationProvenance };
    }

    private static bool ContainsSecretPattern(string statement) => SecretPattern.IsMatch(statement);
}
