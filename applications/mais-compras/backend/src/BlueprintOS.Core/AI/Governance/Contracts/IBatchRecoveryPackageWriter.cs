#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>
/// Writes and reads Recovery Package v2 (batch, chunked). ADDITIVE sibling to <see cref="IRecoveryPackageWriter"/>:
/// a caller that governs one item at a time keeps using that interface exactly as before; a caller that governs
/// N self-contained items as one execution uses this one instead. Neither implementation depends on the other.
/// </summary>
public interface IBatchRecoveryPackageWriter
{
    /// <summary>Chunks <paramref name="items"/> per the writer's configured limits, writes manifest.json,
    /// items-index.json and every before-data/expected-after chunk file. Mirrors
    /// <see cref="IRecoveryPackageWriter.CreateAsync"/>: called BEFORE the live writes run.</summary>
    Task<BatchRecoveryPackageReceipt> CreateBatchAsync(
        BatchRecoveryPackageManifest manifestTemplate,
        IReadOnlyList<BatchRecoveryItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>Appends the after-state for one chunk, aligned positionally with that chunk's before-data.</summary>
    Task WriteChunkAfterDataAsync(string packagePath, int chunkNumber, IReadOnlyList<RecoveryDataSet> afterData, CancellationToken cancellationToken = default);

    /// <summary>Appends the per-item validation detail for one chunk.</summary>
    Task WriteChunkValidationReportAsync(string packagePath, int chunkNumber, IReadOnlyList<ItemValidationResult> results, CancellationToken cancellationToken = default);

    /// <summary>Writes the batch-level aggregate once every chunk has been validated.</summary>
    Task WriteValidationSummaryAsync(string packagePath, BatchValidationSummary summary, CancellationToken cancellationToken = default);

    /// <summary>Updates one item's lifecycle status in items-index.json in place (e.g. after a selective
    /// rollback). Never rewrites chunk payload files.</summary>
    Task UpdateItemStatusAsync(string packagePath, string businessKey, BatchItemStatus status, CancellationToken cancellationToken = default);

    Task<BatchRecoveryPackageManifest?> ReadManifestAsync(string packagePath, CancellationToken cancellationToken = default);

    Task<BatchItemsIndex?> ReadItemsIndexAsync(string packagePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecoveryDataSet>> ReadChunkBeforeDataAsync(string packagePath, int chunkNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecoveryDataSet>> ReadChunkExpectedAfterAsync(string packagePath, int chunkNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecoveryDataSet>> ReadChunkAfterDataAsync(string packagePath, int chunkNumber, CancellationToken cancellationToken = default);

    /// <summary>Verifies the chunk's before-data file on disk still hashes to the checksum recorded in the
    /// manifest at write time. False on any mismatch or missing file — the caller must never proceed on trust.</summary>
    Task<bool> VerifyChunkIntegrityAsync(string packagePath, BatchRecoveryPackageManifest manifest, int chunkNumber, CancellationToken cancellationToken = default);

    bool PackageExists(string packagePath);

    /// <summary>Permanently removes the package directory. Retention cleanup only; never touches audit.</summary>
    Task DeletePackageAsync(string packagePath, CancellationToken cancellationToken = default);
}

/// <summary>One item's per-item validation outcome, recorded inside a chunk's validation-report-000N.json.</summary>
public sealed record ItemValidationResult(
    string BusinessKey,
    bool Passed,
    IReadOnlyList<string> Mismatches);

/// <summary>Proof the batch package was durably written before the live writes ran — the batch analogue of
/// <see cref="RecoveryPackageReceipt"/>.</summary>
public sealed record BatchRecoveryPackageReceipt(
    Guid BatchExecutionId,
    string PackagePath,
    string ManifestChecksumSha256,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int ChunkCount,
    int TotalItems);
