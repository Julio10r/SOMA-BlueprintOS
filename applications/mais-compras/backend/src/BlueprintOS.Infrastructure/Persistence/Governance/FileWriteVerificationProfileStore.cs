using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// File-based <see cref="IWriteVerificationProfileStore"/> under
/// <c>{root}/profiles/{connectionProfile}__{policyVersion}.json</c>, one file per version keyed by the
/// composite (ConnectionProfile, PolicyVersion) — encoded directly into the sanitized filename, so lookup by
/// that composite key needs no scan or secondary index at all.
///
/// Self-seeding: on first use, if the profiles directory is empty, this store writes one file per
/// <see cref="WriteVerificationProfileSeeds.All"/> entry, mirroring what <c>InMemoryWriteVerificationProfileStore</c>
/// already does in its constructor — so the system works out of the box with zero manual seeding step. Seeding
/// is idempotent (guarded by a per-process flag plus an existence check per file) so repeated app starts never
/// attempt to recreate an already-seeded file.
/// </summary>
public sealed class FileWriteVerificationProfileStore(string rootDirectory) : IWriteVerificationProfileStore
{
    private readonly string _root = Path.Combine(rootDirectory, "profiles");
    private int _seeded;

    public async Task<WriteVerificationProfile?> ResolveAsync(string connectionProfile, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var versions = await ListVersionsAsync(connectionProfile, cancellationToken);
        return versions
            .Where(item => item.EffectiveFrom <= asOf)
            .OrderByDescending(item => item.EffectiveFrom)
            .ThenByDescending(item => item.PolicyVersion, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    public async Task<IReadOnlyList<WriteVerificationProfile>> ListVersionsAsync(string connectionProfile, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var results = new List<WriteVerificationProfile>();
        await foreach (var item in AtomicFileWriter.ScanAllAsync<WriteVerificationProfile>(_root, cancellationToken))
        {
            if (string.Equals(item.ConnectionProfile, connectionProfile, StringComparison.Ordinal)) results.Add(item);
        }

        return results.OrderBy(item => item.EffectiveFrom).ToArray();
    }

    public async Task AppendVersionAsync(WriteVerificationProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        await EnsureSeededAsync(cancellationToken);
        var path = BuildPath(profile.ConnectionProfile, profile.PolicyVersion);
        await AtomicFileWriter.WithFileLockAsync(path, async () =>
        {
            if (File.Exists(path))
            {
                throw new InvalidOperationException(
                    $"Write verification profile '{profile.ConnectionProfile}' already has a version '{profile.PolicyVersion}'. Versions are append-only and immutable.");
            }

            await AtomicFileWriter.WriteJsonAsync(path, profile, cancellationToken);
            return true;
        });
    }

    private async Task EnsureSeededAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _seeded, 1, 0) != 0) return;

        Directory.CreateDirectory(_root);
        if (Directory.EnumerateFiles(_root, "*.json", SearchOption.TopDirectoryOnly).Any()) return;

        foreach (var seed in WriteVerificationProfileSeeds.All)
        {
            var path = BuildPath(seed.ConnectionProfile, seed.PolicyVersion);
            if (File.Exists(path)) continue;
            await AtomicFileWriter.WithFileLockAsync(path, () => AtomicFileWriter.WriteJsonAsync(path, seed, cancellationToken).ContinueWith(_ => true, cancellationToken));
        }
    }

    private string BuildPath(string connectionProfile, string policyVersion) =>
        Path.Combine(_root, $"{AtomicFileWriter.Sanitize(connectionProfile)}__{AtomicFileWriter.Sanitize(policyVersion)}.json");
}
