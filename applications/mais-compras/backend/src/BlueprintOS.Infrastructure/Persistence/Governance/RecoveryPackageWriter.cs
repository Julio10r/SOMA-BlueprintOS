using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// Filesystem implementation of <see cref="IRecoveryPackageWriter"/>.
///
/// Layout, rooted at <c>runtime/backups/</c>:
/// <c>&lt;agent-id&gt;/&lt;database&gt;/&lt;yyyy-MM-dd&gt;/&lt;HHmm&gt;-&lt;acao&gt;__&lt;execution-id&gt;/</c>
/// containing manifest.json, before-data.json, expected-after.json, after-data.json and
/// validation-report.json. The date/time segments come from the manifest's ExecutedAt (UTC), never from the
/// wall clock, so a package's path always matches the execution it documents.
///
/// The path component is the manifest's <c>Database</c> — the REAL, validated database identity of the
/// connection the write ran against (already checked by <c>LinxConnectionStringResolver.Resolve</c> before
/// the manifest was ever built), never the logical <c>ConnectionProfile</c> name and never guessed from it.
/// <c>ConnectionProfile</c> stays recorded as metadata inside the manifest — it is simply no longer a
/// component of the physical path.
///
/// The root is injected. Tests point it at a temp directory; nothing here ever writes into the repository's
/// real runtime folder unless a host explicitly configures that root.
/// </summary>
public sealed class RecoveryPackageWriter(string rootDirectory) : IRecoveryPackageWriter
{
    public const string ManifestFileName = "manifest.json";
    public const string BeforeDataFileName = "before-data.json";
    public const string ExpectedAfterFileName = "expected-after.json";
    public const string AfterDataFileName = "after-data.json";
    public const string ValidationReportFileName = "validation-report.json";

    private static readonly JsonSerializerOptions FileJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string RootDirectory { get; } = string.IsNullOrWhiteSpace(rootDirectory)
        ? throw new ArgumentException("Recovery package root directory is required.", nameof(rootDirectory))
        : rootDirectory;

    public async Task<RecoveryPackageReceipt> CreateAsync(
        RecoveryPackageManifest manifest,
        IReadOnlyList<RecoveryDataSet> beforeData,
        IReadOnlyList<RecoveryDataSet> expectedAfter,
        bool allowsMissingBeforeState = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(beforeData);
        ArgumentNullException.ThrowIfNull(expectedAfter);

        var packagePath = BuildPackagePath(manifest);
        Directory.CreateDirectory(packagePath);

        // The checksum is written alongside the manifest body so a later read can verify the file it loaded
        // matches the values it contains.
        await WriteJsonAsync(Path.Combine(packagePath, ManifestFileName),
            new PersistedManifest(manifest, manifest.ManifestChecksumSha256), cancellationToken);
        await WriteJsonAsync(Path.Combine(packagePath, BeforeDataFileName), beforeData, cancellationToken);
        await WriteJsonAsync(Path.Combine(packagePath, ExpectedAfterFileName), expectedAfter, cancellationToken);

        // The status is explicit and typed (BeforeStateEvaluator), never inferred from "collection is empty" in
        // isolation: for a CREATE an empty before-state is expected (NotExistent), while the same emptiness for
        // an UPDATE/DELETE/ALTER means capture failed — those must never be treated as equivalent.
        var beforeState = BeforeStateEvaluator.Evaluate(
            manifest.OperationTypes.Count > 0 ? manifest.OperationTypes[0] : ActionOperation.Update,
            beforeData,
            allowsMissingBeforeState);

        return new RecoveryPackageReceipt(
            manifest.ExecutionId,
            packagePath,
            manifest.ManifestChecksumSha256,
            manifest.ExecutedAt,
            manifest.ExpiresAt,
            BeforeState: beforeState);
    }

    public Task WriteAfterDataAsync(RecoveryPackageReceipt receipt, IReadOnlyList<RecoveryDataSet> afterData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return WriteJsonAsync(Path.Combine(receipt.PackagePath, AfterDataFileName), afterData, cancellationToken);
    }

    public Task WriteValidationReportAsync(RecoveryPackageReceipt receipt, PostWriteValidationReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return WriteJsonAsync(Path.Combine(receipt.PackagePath, ValidationReportFileName), report, cancellationToken);
    }

    public async Task<RecoveryPackageManifest?> ReadManifestAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var file = Path.Combine(packagePath, ManifestFileName);
        if (!File.Exists(file)) return null;
        await using var stream = File.OpenRead(file);
        var persisted = await JsonSerializer.DeserializeAsync<PersistedManifest>(stream, FileJsonOptions, cancellationToken);
        return persisted?.Manifest;
    }

    public Task<IReadOnlyList<RecoveryDataSet>> ReadBeforeDataAsync(string packagePath, CancellationToken cancellationToken = default) =>
        ReadDataSetsAsync(Path.Combine(packagePath, BeforeDataFileName), cancellationToken);

    public Task<IReadOnlyList<RecoveryDataSet>> ReadAfterDataAsync(string packagePath, CancellationToken cancellationToken = default) =>
        ReadDataSetsAsync(Path.Combine(packagePath, AfterDataFileName), cancellationToken);

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

    private string BuildPackagePath(RecoveryPackageManifest manifest)
    {
        var executedAt = manifest.ExecutedAt.ToUniversalTime();
        return Path.Combine(
            RootDirectory,
            Sanitize(manifest.AgentId),
            Sanitize(manifest.Database),
            executedAt.ToString("yyyy-MM-dd"),
            $"{executedAt:HHmm}-{Sanitize(manifest.ExecutionName)}__{manifest.ExecutionId:N}");
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

    /// <summary>Keeps a path segment to characters that are safe on every filesystem; an agent id or an
    /// execution name never gets to shape the directory tree.</summary>
    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";
        var cleaned = new string(value.Trim().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-').ToArray());
        cleaned = cleaned.Trim('-');
        return string.IsNullOrEmpty(cleaned) ? "unknown" : cleaned.ToLowerInvariant();
    }

    private sealed record PersistedManifest(RecoveryPackageManifest Manifest, string ManifestChecksumSha256);
}
