#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>
/// Append-only store of <see cref="WriteVerificationProfile"/> versions. The ONLY sanctioned way to learn
/// whether a live write through a connection profile needs a backup, supports rollback, or requires
/// post-write validation. Never infer these from a database or server name.
/// </summary>
public interface IWriteVerificationProfileStore
{
    /// <summary>Newest version whose EffectiveFrom is at or before <paramref name="asOf"/>; null when the
    /// profile has no effective policy, which callers must treat as "cannot write", never as "no guarantees needed".</summary>
    Task<WriteVerificationProfile?> ResolveAsync(string connectionProfile, DateTimeOffset asOf, CancellationToken cancellationToken = default);

    /// <summary>All recorded versions for a profile, oldest first. Audit surface; never used to pick a policy.</summary>
    Task<IReadOnlyList<WriteVerificationProfile>> ListVersionsAsync(string connectionProfile, CancellationToken cancellationToken = default);

    /// <summary>Appends a NEW version. Never edits an existing one. Callers must have obtained a governed
    /// approval first — see <c>WriteVerificationProfileGovernanceService</c>.</summary>
    Task AppendVersionAsync(WriteVerificationProfile profile, CancellationToken cancellationToken = default);
}
