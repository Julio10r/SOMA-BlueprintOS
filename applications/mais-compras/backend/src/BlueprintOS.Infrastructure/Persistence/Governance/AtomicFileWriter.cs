using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// Shared atomic-write helper for the file-based Governance stores under <c>runtime/governance/</c>.
///
/// Write path: serialize to a temp file in the SAME directory as the final file (so the subsequent move is
/// same-filesystem/atomic on POSIX and near-atomic via <see cref="File.Move(string,string,bool)"/> on
/// Windows), flush and dispose the stream, then move the temp file over the final path with overwrite. The
/// temp file is opened with default <see cref="FileShare.None"/> so a genuinely concurrent OS-level writer to
/// the exact same temp name fails fast instead of corrupting the write — full cross-process advisory locking
/// beyond that is NOT implemented; this is an accepted limitation because this stage of the project runs as a
/// single active writer process.
///
/// In-process concurrency (two callers in the same process racing a read-modify-write against the same final
/// path) is serialized by <see cref="WithFileLockAsync"/>, keyed by the absolute final file path.
/// </summary>
internal static class AtomicFileWriter
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static SemaphoreSlim LockFor(string path) =>
        Locks.GetOrAdd(Path.GetFullPath(path), _ => new SemaphoreSlim(1, 1));

    /// <summary>Serializes a read-modify-write (or create-if-absent) sequence for the given final file path
    /// within this process.</summary>
    public static async Task<T> WithFileLockAsync<T>(string finalPath, Func<Task<T>> action)
    {
        var gate = LockFor(finalPath);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    public static async Task WriteJsonAsync<T>(string finalPath, T payload, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(finalPath)!;
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, $"{Path.GetFileName(finalPath)}.tmp-{Guid.NewGuid():N}");
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, payload, JsonOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, finalPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            }

            throw;
        }
    }

    public static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return default;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sanitizes a string for use as a path segment: alphanumeric/-/_ only, same spirit as
    /// <see cref="RecoveryPackageWriter"/>'s own Sanitize.</summary>
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";
        var cleaned = new string(value.Trim().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-').ToArray());
        cleaned = cleaned.Trim('-');
        return string.IsNullOrEmpty(cleaned) ? "unknown" : cleaned;
    }

    /// <summary>Scans a store root's date subfolders for files, skipping and logging any file that fails to
    /// deserialize instead of letting one corrupt file abort the whole read.</summary>
    public static async IAsyncEnumerable<T> ScanAllAsync<T>(string root, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(root)) yield break;

        foreach (var file in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Path.GetFileName(file).Contains(".tmp-", StringComparison.Ordinal)) continue;

            T? value = default;
            var ok = true;
            try
            {
                value = await ReadJsonAsync<T>(file, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                ok = false;
                Console.Error.WriteLine($"[GOVERNANCE_FILE_STORE_CORRUPTION] Failed to read '{file}': {ex.Message}");
            }

            if (ok && value is not null) yield return value;
        }
    }
}
