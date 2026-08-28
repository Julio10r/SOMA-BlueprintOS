#pragma warning disable CS1591

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Recovery;

/// <summary>
/// One set of records captured from a resource, as flat name/value pairs. Values are stringified on purpose:
/// a recovery package must be readable and comparable years later without the original CLR types, and a
/// rollback compares values, it does not re-hydrate objects.
/// </summary>
public sealed record RecoveryDataSet(
    string Resource,
    IReadOnlyList<IReadOnlyDictionary<string, string?>> Records);

/// <summary>
/// Structural snapshot of the affected objects. Modelled now so a recovery package written today can carry
/// DDL later without a format change; capture is NOT implemented yet and the property stays null until it is.
/// </summary>
public sealed record RecoveryPackageDdlSnapshot(
    string Resource,
    string? CreateStatement,
    IReadOnlyList<string> Columns,
    IReadOnlyList<string> Indexes,
    IReadOnlyList<string> Triggers,
    DateTimeOffset? CapturedAt);

/// <summary>
/// The identity card of one live execution: what was about to be changed, where, by whom, under which
/// guarantees, and how long the recovery material survives. The manifest is checksummed so a rollback can
/// prove the package it is about to trust has not been altered since it was written.
/// </summary>
public sealed record RecoveryPackageManifest
{
    public required Guid ExecutionId { get; init; }
    public required string ExecutionName { get; init; }
    public required string AgentId { get; init; }
    public required string ConnectionProfile { get; init; }
    public required string Server { get; init; }
    public required string Database { get; init; }
    public required DateTimeOffset ExecutedAt { get; init; }
    public required string Requester { get; init; }
    public required string OriginalRequestSummary { get; init; }
    public required IReadOnlyList<ActionOperation> OperationTypes { get; init; }
    public required IReadOnlyList<string> TablesAffected { get; init; }
    public required IReadOnlyList<string> BusinessKeys { get; init; }
    public required int RecordsExpectedToChange { get; init; }
    public required bool BackupRequired { get; init; }
    public required bool RollbackSupported { get; init; }
    public required int RetentionDays { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string ValidationRuleId { get; init; }
    public required string ProposalHash { get; init; }

    /// <summary>Reserved for structural capture; null until DDL capture is implemented.</summary>
    public RecoveryPackageDdlSnapshot? DdlSnapshot { get; init; }

    /// <summary>SHA-256 over every field above. Excluded from its own input, so it is stable and verifiable.</summary>
    [JsonIgnore]
    public string ManifestChecksumSha256 => ComputeChecksum(this);

    public static string ComputeChecksum(RecoveryPackageManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var payload = new
        {
            manifest.ExecutionId,
            manifest.ExecutionName,
            manifest.AgentId,
            manifest.ConnectionProfile,
            manifest.Server,
            manifest.Database,
            ExecutedAt = manifest.ExecutedAt.ToUniversalTime().ToString("O"),
            manifest.Requester,
            manifest.OriginalRequestSummary,
            OperationTypes = manifest.OperationTypes.Select(item => item.ToString()).ToArray(),
            TablesAffected = manifest.TablesAffected.ToArray(),
            BusinessKeys = manifest.BusinessKeys.ToArray(),
            manifest.RecordsExpectedToChange,
            manifest.BackupRequired,
            manifest.RollbackSupported,
            manifest.RetentionDays,
            ExpiresAt = manifest.ExpiresAt.ToUniversalTime().ToString("O"),
            manifest.ValidationRuleId,
            manifest.ProposalHash,
            manifest.DdlSnapshot,
        };

        var json = JsonSerializer.Serialize(payload, ChecksumJsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static readonly JsonSerializerOptions ChecksumJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };
}

/// <summary>
/// What happened when the recovery package tried to capture the resource's state before the write. This is
/// deliberately three-valued instead of a bool: an empty snapshot means two very different things depending on
/// why it is empty, and collapsing them into "captured or not" is exactly what let a CREATE (where nothing can
/// exist yet, by definition) look identical to a capture that silently failed.
/// </summary>
public enum BeforeStateStatus
{
    /// <summary>The resource existed and its prior state was read successfully.</summary>
    Captured,

    /// <summary>The resource legitimately did not exist yet — valid only for an operation whose semantics allow
    /// that (a CREATE/INSERT of a new key), never inferred from an empty snapshot alone.</summary>
    NotExistent,

    /// <summary>Capture was attempted for an operation that requires a prior state (e.g. UPDATE/DELETE/ALTER)
    /// and came back empty, or capture itself failed. Always blocks live execution.</summary>
    CaptureFailed,
}

/// <summary>
/// Proof that a recovery package was written to durable storage BEFORE the live write was attempted. The
/// Tool Gateway accepts a live execution that requires a backup only when it is handed one of these.
/// </summary>
public sealed record RecoveryPackageReceipt(
    Guid ExecutionId,
    string PackagePath,
    string ManifestChecksumSha256,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    BeforeStateStatus BeforeState);

/// <summary>
/// Decides <see cref="BeforeStateStatus"/> from what was actually observed, explicitly and once, so every
/// caller that builds a <see cref="RecoveryPackageReceipt"/> (a fresh write or a rollback rebuilding the
/// original's receipt) applies the same rule instead of each guessing from an empty collection.
/// </summary>
public static class BeforeStateEvaluator
{
    /// <summary>Operations whose semantics always allow "the resource did not exist yet" as a legitimate
    /// before-state. Every other operation requires a prior state to act on, so an empty snapshot for it is a
    /// capture failure, never evidence of non-existence — unless the caller explicitly opts in via
    /// <c>allowsMissingPriorState</c> on <see cref="Evaluate"/>.</summary>
    private static readonly HashSet<ActionOperation> OperationsWithOptionalPriorState =
        [ActionOperation.Insert, ActionOperation.Create];

    /// <param name="operation">The proposal's governance-classified operation. Drives policy/approval, so it is
    /// never repurposed here just to make a before-state look acceptable.</param>
    /// <param name="beforeData">What was actually captured.</param>
    /// <param name="allowsMissingPriorState">Set by the caller — never inferred — for a capability whose
    /// operation is classified as something else for policy purposes (e.g. Update) but is actually
    /// insert-or-update by business key, decided by the write itself at execution time ("garantir X"). Reusing
    /// <see cref="ActionOperation.Merge"/> for that case was tried and rejected: it collides with the fixed
    /// "MERGE without an approved runbook is always blocked" policy rule, which is about literal SQL MERGE, not
    /// this. This flag keeps the two concerns — policy classification and before-state compatibility —
    /// independent, exactly as explicit and typed as the three-way status itself.</param>
    public static BeforeStateStatus Evaluate(ActionOperation operation, IReadOnlyList<RecoveryDataSet> beforeData, bool allowsMissingPriorState = false)
    {
        ArgumentNullException.ThrowIfNull(beforeData);
        var hasRecords = beforeData.Sum(set => set.Records.Count) > 0;
        if (hasRecords) return BeforeStateStatus.Captured;
        return OperationsWithOptionalPriorState.Contains(operation) || allowsMissingPriorState
            ? BeforeStateStatus.NotExistent
            : BeforeStateStatus.CaptureFailed;
    }
}
