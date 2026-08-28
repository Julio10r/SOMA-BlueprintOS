#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Models;

/// <summary>
/// How to prove, AFTER a live write, that the write actually produced the intended state. A rule names the
/// business keys used to re-read the affected records and the fields whose values must match what the
/// proposal expected. Without a rule there is no way to distinguish "the write worked" from "the driver
/// returned no error", which is why a missing rule blocks the write rather than degrading it.
/// </summary>
public sealed record PostWriteValidationRule(
    string RuleId,
    string Resource,
    IReadOnlyList<ActionOperation> Operations,
    IReadOnlyList<string> BusinessKeyFields,
    IReadOnlyList<string> FieldsToCompare,
    string Description,
    string PolicyVersion)
{
    public bool Covers(ActionOperation operation, string resource) =>
        string.Equals(Resource, resource, StringComparison.OrdinalIgnoreCase)
        && Operations.Contains(operation);
}

/// <summary>Outcome of applying a <see cref="PostWriteValidationRule"/> to the state observed after a write.</summary>
public sealed record PostWriteValidationReport(
    string RuleId,
    bool Passed,
    int RecordsValidated,
    int RecordsWithErrors,
    IReadOnlyList<string> Mismatches,
    DateTimeOffset ValidatedAt);

/// <summary>
/// A recorded gap: a governed write was attempted for an (operation, resource) pair that no post-write
/// validation rule covers. This is a KNOWLEDGE gap in the sense of
/// agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md — the capability exists, the knowledge of how to
/// verify it does not — so the flow is interrupted and the gap is recorded for a human to close. It is
/// never auto-resolved and never downgraded into a warning.
/// </summary>
public sealed record WriteValidationKnowledgeGap(
    Guid Id,
    string RequestId,
    string AgentId,
    string ConnectionProfile,
    string Resource,
    ActionOperation Operation,
    string Reason,
    Guid? ActionProposalId,
    DateTimeOffset DetectedAt)
{
    public const string ReasonCode = "WRITE_VALIDATION_RULE_UNKNOWN";
}

/// <summary>
/// A recorded gap: the resolved <c>WriteVerificationProfile</c> for this connection profile requires rollback
/// support (<c>RollbackSupported=true</c>), but the capability actually performing the write declares
/// <c>RollbackStrategy.NotSupported</c>. This is a CAPABILITY gap, not a knowledge gap — the framework
/// knows exactly how it WOULD roll back (see <c>RollbackOrchestrator</c>), but this specific capability's own
/// business rules do not allow it. The write is blocked before anything is touched; nothing is improvised.
/// </summary>
public sealed record RollbackCapabilityGap(
    Guid Id,
    string RequestId,
    string AgentId,
    string ConnectionProfile,
    string Capability,
    string Resource,
    string Reason,
    Guid? ActionProposalId,
    DateTimeOffset DetectedAt)
{
    public const string ReasonCode = "ROLLBACK_STRATEGY_NOT_SUPPORTED";
}
