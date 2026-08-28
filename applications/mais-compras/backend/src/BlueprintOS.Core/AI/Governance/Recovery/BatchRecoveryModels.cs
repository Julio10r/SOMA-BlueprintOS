#pragma warning disable CS1591

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Recovery;

/// <summary>
/// Recovery Package v2 — ADDITIVE to the single-item format (<see cref="RecoveryPackageManifest"/>), which
/// stays exactly as-is and remains the only format the 78+ already-homologated executions were ever written
/// in. This format exists for a caller that governs N self-contained items as ONE logical execution/batch
/// (e.g. an operator confirming an in-bulk PED grade adjustment across many products) and wants one Recovery
/// Package, chunked, instead of either (a) one physical package per item, or (b) a single unbounded JSON file
/// that would not scale.
///
/// One batch package directory, rooted the same way as the single-item format
/// (<c>runtime/backups/&lt;agent-id&gt;/&lt;database&gt;/&lt;yyyy-MM-dd&gt;/&lt;HHmm&gt;-&lt;acao&gt;__&lt;batch-execution-id&gt;/</c>):
/// <list type="bullet">
/// <item><c>manifest.json</c> — this record, plus its checksum.</item>
/// <item><c>items-index.json</c> — <see cref="BatchItemsIndex"/>: locates any item by business key or position
/// without reading a chunk.</item>
/// <item><c>before-data-0001.json</c>, <c>expected-after-0001.json</c>, <c>after-data-0001.json</c>,
/// <c>validation-report-0001.json</c>, ... one set per chunk, 1-based and zero-padded to 4 digits.</item>
/// <item><c>validation-summary.json</c> — batch-level aggregate written once the whole batch has been
/// validated (see <see cref="BatchValidationSummary"/>).</item>
/// </list>
/// </summary>
public sealed record BatchRecoveryPackageManifest
{
    public required Guid BatchExecutionId { get; init; }
    public required string ExecutionName { get; init; }
    public required string AgentId { get; init; }
    public required string Capability { get; init; }
    public required string ConnectionProfile { get; init; }
    public required string Server { get; init; }
    public required string Database { get; init; }
    public required DateTimeOffset ExecutedAt { get; init; }
    public required string Requester { get; init; }
    public required string Origin { get; init; }
    public required string OriginalRequestSummary { get; init; }
    public required IReadOnlyList<ActionOperation> OperationTypes { get; init; }
    public required IReadOnlyList<string> TablesAffected { get; init; }
    public required int TotalItems { get; init; }
    public required int ChunkCount { get; init; }
    public required int MaxItemsPerChunk { get; init; }
    public required long MaxChunkSizeBytes { get; init; }
    public required bool BackupRequired { get; init; }
    public required bool RollbackSupported { get; init; }
    public required int RetentionDays { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string ValidationRuleId { get; init; }
    public required string ProposalHash { get; init; }
    public required BatchStatus Status { get; init; }

    /// <summary>SHA-256 of each chunk's before-data file at write time, keyed by chunk number — lets a later
    /// reader (rollback analysis, cleanup, an operator) prove a chunk on disk has not been altered since the
    /// batch was written, the same guarantee the single-item manifest's checksum gives for its one file.</summary>
    public required IReadOnlyDictionary<int, string> ChunkBeforeDataChecksumsSha256 { get; init; }

    public string ManifestChecksumSha256 => ComputeChecksum(this);

    public static string ComputeChecksum(BatchRecoveryPackageManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var payload = new
        {
            manifest.BatchExecutionId,
            manifest.ExecutionName,
            manifest.AgentId,
            manifest.Capability,
            manifest.ConnectionProfile,
            manifest.Server,
            manifest.Database,
            ExecutedAt = manifest.ExecutedAt.ToUniversalTime().ToString("O"),
            manifest.Requester,
            manifest.Origin,
            manifest.OriginalRequestSummary,
            OperationTypes = manifest.OperationTypes.Select(o => o.ToString()).ToArray(),
            TablesAffected = manifest.TablesAffected.ToArray(),
            manifest.TotalItems,
            manifest.ChunkCount,
            manifest.MaxItemsPerChunk,
            manifest.MaxChunkSizeBytes,
            manifest.BackupRequired,
            manifest.RollbackSupported,
            manifest.RetentionDays,
            ExpiresAt = manifest.ExpiresAt.ToUniversalTime().ToString("O"),
            manifest.ValidationRuleId,
            manifest.ProposalHash,
            ChunkChecksums = manifest.ChunkBeforeDataChecksumsSha256.OrderBy(kv => kv.Key).ToArray(),
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }
}

public enum BatchStatus
{
    Active = 1,
    Expired = 2,
    RolledBack = 3,
    PartiallyRolledBack = 4,
}

public enum BatchItemStatus
{
    Pending = 1,
    Written = 2,
    ValidationPassed = 3,
    ValidationFailed = 4,
    RolledBack = 5,
}

/// <summary>One self-contained item submitted to a batch write, before it has been chunked and located.</summary>
public sealed record BatchRecoveryItem(
    string BusinessKey,
    string Resource,
    RecoveryDataSet BeforeData,
    RecoveryDataSet ExpectedAfter);

/// <summary>Where one item physically lives once the batch has been chunked, plus its lifecycle status. This
/// is the value side of <see cref="BatchItemsIndex.ByBusinessKey"/> and the element type of
/// <see cref="BatchItemsIndex.ByPosition"/> — both point at the exact same location record so a lookup by
/// either key or position is O(1) and never needs to open a chunk file to find one.</summary>
public sealed record BatchItemLocation(
    string BusinessKey,
    string Resource,
    int Position,
    int ChunkNumber,
    int IndexWithinChunk,
    BatchItemStatus Status);

/// <summary>
/// Written once, at batch creation, alongside the chunk files. Never rewritten wholesale afterward — item
/// status transitions (Written → ValidationPassed/Failed → RolledBack) update this same file in place via
/// <c>BatchRecoveryPackageWriter.UpdateItemStatusAsync</c>, so a reader always sees the latest lifecycle state
/// without needing to reconcile it against the (immutable) before/after chunk payloads.
/// </summary>
public sealed record BatchItemsIndex(
    Guid BatchExecutionId,
    int TotalItems,
    int ChunkCount,
    IReadOnlyDictionary<string, BatchItemLocation> ByBusinessKey,
    IReadOnlyList<BatchItemLocation> ByPosition);

/// <summary>Aggregate result of validating every item in the batch, written once to
/// <c>validation-summary.json</c> at the batch package root (in addition to the per-chunk
/// <c>validation-report-000N.json</c> files, which carry the per-item detail for that chunk).</summary>
public sealed record BatchValidationSummary(
    Guid BatchExecutionId,
    int TotalItems,
    int Passed,
    int Failed,
    IReadOnlyList<string> FailedBusinessKeys,
    IReadOnlyList<string> FailureDetails,
    DateTimeOffset ValidatedAt);
