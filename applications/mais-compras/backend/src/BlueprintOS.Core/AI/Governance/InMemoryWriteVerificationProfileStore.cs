#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>
/// Provider-agnostic in-memory implementation of <see cref="IWriteVerificationProfileStore"/>, seeded from
/// <see cref="WriteVerificationProfileSeeds"/>. Used by tests and by any host that has no relational store;
/// it enforces exactly the same append-only, newest-effective-version semantics as the EF implementation.
/// </summary>
public sealed class InMemoryWriteVerificationProfileStore : IWriteVerificationProfileStore
{
    private readonly List<WriteVerificationProfile> _versions;

    public InMemoryWriteVerificationProfileStore(IEnumerable<WriteVerificationProfile>? seed = null) =>
        _versions = [.. seed ?? WriteVerificationProfileSeeds.All];

    public Task<WriteVerificationProfile?> ResolveAsync(string connectionProfile, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
        Task.FromResult(_versions
            .Where(item => string.Equals(item.ConnectionProfile, connectionProfile, StringComparison.Ordinal) && item.EffectiveFrom <= asOf)
            .OrderByDescending(item => item.EffectiveFrom)
            .ThenByDescending(item => item.PolicyVersion, StringComparer.Ordinal)
            .FirstOrDefault());

    public Task<IReadOnlyList<WriteVerificationProfile>> ListVersionsAsync(string connectionProfile, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WriteVerificationProfile>>(_versions
            .Where(item => string.Equals(item.ConnectionProfile, connectionProfile, StringComparison.Ordinal))
            .OrderBy(item => item.EffectiveFrom)
            .ToArray());

    public Task AppendVersionAsync(WriteVerificationProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (_versions.Any(item => string.Equals(item.ConnectionProfile, profile.ConnectionProfile, StringComparison.Ordinal)
                && string.Equals(item.PolicyVersion, profile.PolicyVersion, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Write verification profile '{profile.ConnectionProfile}' already has a version '{profile.PolicyVersion}'. Versions are append-only and immutable.");
        }

        _versions.Add(profile);
        return Task.CompletedTask;
    }
}
