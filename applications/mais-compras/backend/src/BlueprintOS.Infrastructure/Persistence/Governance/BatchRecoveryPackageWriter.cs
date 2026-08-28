using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// Filesystem implementation of <see cref="IBatchRecoveryPackageWriter"/> (Recovery Package v2 — batch,
/// chunked). Same physical root and folder-naming convention as <see cref="RecoveryPackageWriter"/>
/// (<c>runtime/backups/&lt;agent-id&gt;/&lt;database&gt;/&lt;yyyy-MM-dd&gt;/&lt;HHmm&gt;-&lt;acao&gt;__&lt;batch-execution-id&gt;/</c>),
/// so both formats coexist under the same tree without collision — a batch's <c>&lt;acao&gt;__&lt;id&gt;</c>
/// leaf directory simply contains a different set of files (items-index.json, numbered chunks) than a
/// single-item package does.
///
/// CHUNKING: an item is placed in the current chunk until either <see cref="MaxItemsPerChunk"/> is reached or
/// appending it would push the chunk's serialized before-data past <see cref="MaxChunkSizeBytes"/> — whichever
/// limit is hit first closes the chunk and starts a new one. Defaults (500 items / ~5 MB) are deliberately
/// conservative: large enough that a normal batch (tens to low thousands of rows) fits in one or two chunks,
/// small enough that no single JSON file becomes unwieldy to open, diff or re-checksum by hand.
/// </summary>
public sealed class BatchRecoveryPackageWriter(
    string rootDirectory,
    int maxItemsPerChunk = 500,
    long maxChunkSizeBytes = 5 * 1024 * 1024) : IBatchRecoveryPackageWriter
{
    public const string ManifestFileName = "manifest.json";
    public const string ItemsIndexFileName = "items-index.json";
    public const string ValidationSummaryFileName = "validation-summary.json";

    private static readonly JsonSerializerOptions FileJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string RootDirectory { get; } = string.IsNullOrWhiteSpace(rootDirectory)
        ? throw new ArgumentException("Batch recovery package root directory is required.", nameof(rootDirectory))
        : rootDirectory;

    public int MaxItemsPerChunk { get; } = maxItemsPerChunk > 0 ? maxItemsPerChunk : 500;

    public long MaxChunkSizeBytes { get; } = maxChunkSizeBytes > 0 ? maxChunkSizeBytes : 5 * 1024 * 1024;

    public async Task<BatchRecoveryPackageReceipt> CreateBatchAsync(
        BatchRecoveryPackageManifest manifestTemplate,
        IReadOnlyList<BatchRecoveryItem> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifestTemplate);
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0) throw new ArgumentException("A batch must contain at least one item.", nameof(items));

        var chunks = SplitIntoChunks(items);
        var packagePath = BuildPackagePath(manifestTemplate);
        Directory.CreateDirectory(packagePath);

        var chunkChecksums = new Dictionary<int, string>();
        var byPosition = new List<BatchItemLocation>(items.Count);
        var byBusinessKey = new Dictionary<string, BatchItemLocation>(StringComparer.OrdinalIgnoreCase);

        var position = 0;
        for (var chunkNumber = 1; chunkNumber <= chunks.Count; chunkNumber++)
        {
            var chunk = chunks[chunkNumber - 1];
            var beforeSets = chunk.Select(item => item.BeforeData).ToArray();
            var afterSets = chunk.Select(item => item.ExpectedAfter).ToArray();

            var beforeFile = Path.Combine(packagePath, BeforeDataFileName(chunkNumber));
            await WriteJsonAsync(beforeFile, beforeSets, cancellationToken);
            await WriteJsonAsync(Path.Combine(packagePath, ExpectedAfterFileName(chunkNumber)), afterSets, cancellationToken);
            chunkChecksums[chunkNumber] = await ComputeFileChecksumAsync(beforeFile, cancellationToken);

            for (var indexWithinChunk = 0; indexWithinChunk < chunk.Count; indexWithinChunk++)
            {
                var item = chunk[indexWithinChunk];
                var location = new BatchItemLocation(item.BusinessKey, item.Resource, position, chunkNumber, indexWithinChunk, BatchItemStatus.Written);
                byPosition.Add(location);
                byBusinessKey[item.BusinessKey] = location;
                position++;
            }
        }

        var manifest = manifestTemplate with
        {
            TotalItems = items.Count,
            ChunkCount = chunks.Count,
            MaxItemsPerChunk = MaxItemsPerChunk,
            MaxChunkSizeBytes = MaxChunkSizeBytes,
            ChunkBeforeDataChecksumsSha256 = chunkChecksums,
        };

        await WriteJsonAsync(Path.Combine(packagePath, ManifestFileName),
            new PersistedManifest(manifest, manifest.ManifestChecksumSha256), cancellationToken);

        var itemsIndex = new BatchItemsIndex(manifest.BatchExecutionId, items.Count, chunks.Count, byBusinessKey, byPosition);
        await WriteJsonAsync(Path.Combine(packagePath, ItemsIndexFileName), itemsIndex, cancellationToken);

        return new BatchRecoveryPackageReceipt(
            manifest.BatchExecutionId, packagePath, manifest.ManifestChecksumSha256,
            manifest.ExecutedAt, manifest.ExpiresAt, chunks.Count, items.Count);
    }

    public Task WriteChunkAfterDataAsync(string packagePath, int chunkNumber, IReadOnlyList<RecoveryDataSet> afterData, CancellationToken cancellationToken = default) =>
        WriteJsonAsync(Path.Combine(packagePath, AfterDataFileName(chunkNumber)), afterData, cancellationToken);

    public Task WriteChunkValidationReportAsync(string packagePath, int chunkNumber, IReadOnlyList<ItemValidationResult> results, CancellationToken cancellationToken = default) =>
        WriteJsonAsync(Path.Combine(packagePath, ValidationReportFileName(chunkNumber)), results, cancellationToken);

    public Task WriteValidationSummaryAsync(string packagePath, BatchValidationSummary summary, CancellationToken cancellationToken = default) =>
        WriteJsonAsync(Path.Combine(packagePath, ValidationSummaryFileName), summary, cancellationToken);

    public async Task UpdateItemStatusAsync(string packagePath, string businessKey, BatchItemStatus status, CancellationToken cancellationToken = default)
    {
        var index = await ReadItemsIndexAsync(packagePath, cancellationToken)
            ?? throw new InvalidOperationException($"items-index.json not found under '{packagePath}'.");
        if (!index.ByBusinessKey.TryGetValue(businessKey, out var location))
        {
            throw new KeyNotFoundException($"Business key '{businessKey}' not found in batch items index.");
        }

        var updatedLocation = location with { Status = status };
        var byBusinessKey = new Dictionary<string, BatchItemLocation>(index.ByBusinessKey, StringComparer.OrdinalIgnoreCase) { [businessKey] = updatedLocation };
        var byPosition = index.ByPosition.Select(l => string.Equals(l.BusinessKey, businessKey, StringComparison.OrdinalIgnoreCase) ? updatedLocation : l).ToArray();
        var updatedIndex = index with { ByBusinessKey = byBusinessKey, ByPosition = byPosition };
        await WriteJsonAsync(Path.Combine(packagePath, ItemsIndexFileName), updatedIndex, cancellationToken);
    }

    public async Task<BatchRecoveryPackageManifest?> ReadManifestAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var file = Path.Combine(packagePath, ManifestFileName);
        if (!File.Exists(file)) return null;
        await using var stream = File.OpenRead(file);
        var persisted = await JsonSerializer.DeserializeAsync<PersistedManifest>(stream, FileJsonOptions, cancellationToken);
        return persisted?.Manifest;
    }

    public async Task<BatchItemsIndex?> ReadItemsIndexAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var file = Path.Combine(packagePath, ItemsIndexFileName);
        if (!File.Exists(file)) return null;
        await using var stream = File.OpenRead(file);
        return await JsonSerializer.DeserializeAsync<BatchItemsIndex>(stream, FileJsonOptions, cancellationToken);
    }

    public Task<IReadOnlyList<RecoveryDataSet>> ReadChunkBeforeDataAsync(string packagePath, int chunkNumber, CancellationToken cancellationToken = default) =>
        ReadDataSetsAsync(Path.Combine(packagePath, BeforeDataFileName(chunkNumber)), cancellationToken);

    public Task<IReadOnlyList<RecoveryDataSet>> ReadChunkExpectedAfterAsync(string packagePath, int chunkNumber, CancellationToken cancellationToken = default) =>
        ReadDataSetsAsync(Path.Combine(packagePath, ExpectedAfterFileName(chunkNumber)), cancellationToken);

    public Task<IReadOnlyList<RecoveryDataSet>> ReadChunkAfterDataAsync(string packagePath, int chunkNumber, CancellationToken cancellationToken = default) =>
        ReadDataSetsAsync(Path.Combine(packagePath, AfterDataFileName(chunkNumber)), cancellationToken);

    public async Task<bool> VerifyChunkIntegrityAsync(string packagePath, BatchRecoveryPackageManifest manifest, int chunkNumber, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var file = Path.Combine(packagePath, BeforeDataFileName(chunkNumber));
        if (!File.Exists(file)) return false;
        if (!manifest.ChunkBeforeDataChecksumsSha256.TryGetValue(chunkNumber, out var expected)) return false;
        var actual = await ComputeFileChecksumAsync(file, cancellationToken);
        return string.Equals(expected, actual, StringComparison.Ordinal);
    }

    public bool PackageExists(string packagePath) =>
        !string.IsNullOrWhiteSpace(packagePath) && Directory.Exists(packagePath);

    public Task DeletePackageAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(packagePath) && Directory.Exists(packagePath))
        {
            Directory.Delete(packagePath, recursive: true);
        }

        return Task.CompletedTask;
    }

    private List<List<BatchRecoveryItem>> SplitIntoChunks(IReadOnlyList<BatchRecoveryItem> items)
    {
        var chunks = new List<List<BatchRecoveryItem>>();
        var current = new List<BatchRecoveryItem>();
        long currentApproxBytes = 2; // "[]"

        foreach (var item in items)
        {
            var itemBytes = JsonSerializer.SerializeToUtf8Bytes(item.BeforeData, FileJsonOptions).LongLength;
            var wouldExceedSize = current.Count > 0 && currentApproxBytes + itemBytes > MaxChunkSizeBytes;
            var wouldExceedCount = current.Count >= MaxItemsPerChunk;

            if (wouldExceedSize || wouldExceedCount)
            {
                chunks.Add(current);
                current = [];
                currentApproxBytes = 2;
            }

            current.Add(item);
            currentApproxBytes += itemBytes;
        }

        if (current.Count > 0) chunks.Add(current);
        return chunks;
    }

    private string BuildPackagePath(BatchRecoveryPackageManifest manifest)
    {
        var executedAt = BrazilTimeZoneProvider.ToSaoPaulo(manifest.ExecutedAt);
        return Path.Combine(
            RootDirectory,
            Sanitize(manifest.AgentId),
            Sanitize(manifest.Database),
            executedAt.ToString("yyyy-MM-dd"),
            $"{executedAt:HHmm}-{Sanitize(manifest.ExecutionName)}__{manifest.BatchExecutionId:N}");
    }

    private static string BeforeDataFileName(int chunkNumber) => $"before-data-{chunkNumber:0000}.json";
    private static string ExpectedAfterFileName(int chunkNumber) => $"expected-after-{chunkNumber:0000}.json";
    private static string AfterDataFileName(int chunkNumber) => $"after-data-{chunkNumber:0000}.json";
    private static string ValidationReportFileName(int chunkNumber) => $"validation-report-{chunkNumber:0000}.json";

    private static async Task<string> ComputeFileChecksumAsync(string file, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(file);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<IReadOnlyList<RecoveryDataSet>> ReadDataSetsAsync(string file, CancellationToken cancellationToken)
    {
        if (!File.Exists(file)) return [];
        await using var stream = File.OpenRead(file);
        return await JsonSerializer.DeserializeAsync<RecoveryDataSet[]>(stream, FileJsonOptions, cancellationToken) ?? [];
    }

    private static async Task WriteJsonAsync<T>(string file, T payload, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(file);
        await JsonSerializer.SerializeAsync(stream, payload, FileJsonOptions, cancellationToken);
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";
        var cleaned = new string(value.Trim().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-').ToArray());
        cleaned = cleaned.Trim('-');
        return string.IsNullOrEmpty(cleaned) ? "unknown" : cleaned.ToLowerInvariant();
    }

    private sealed record PersistedManifest(BatchRecoveryPackageManifest Manifest, string ManifestChecksumSha256);
}
