using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

public sealed record RecoveryRetentionCleanupReport(
    int Inspected,
    int Expired,
    IReadOnlyList<Guid> ExpiredExecutionIds,
    IReadOnlyList<string> Errors);

/// <summary>
/// Deletes recovery package FILES whose retention window has closed, and marks their index rows Expired.
///
/// Three invariants, in order of importance:
///  1. It NEVER touches the permanent write execution audit. Recovery material expires; the record that a
///     write happened does not. That is the whole distinction between the two stores.
///  2. It never deletes an index row either — the row stays, with Status = Expired, so a later rollback
///     attempt can answer "this existed and is no longer recoverable" instead of "never happened".
///  3. <see cref="RunOnceAsync"/> takes <c>now</c> as an explicit argument and never reads the wall clock, so
///     a thirty-day retention can be tested in milliseconds.
/// </summary>
public sealed class RecoveryRetentionCleanupService(
    IRecoveryIndexStore recoveryIndexStore,
    IRecoveryPackageWriter recoveryPackageWriter)
{
    public async Task<RecoveryRetentionCleanupReport> RunOnceAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var active = await recoveryIndexStore.FindAsync(new RecoveryIndexQuery { Status = RecoveryPackageStatus.Active }, cancellationToken);
        var expiredIds = new List<Guid>();
        var errors = new List<string>();

        foreach (var entry in active.Where(entry => entry.ExpiresAt <= now))
        {
            try
            {
                await recoveryPackageWriter.DeletePackageAsync(entry.PackagePath, cancellationToken);
                await recoveryIndexStore.UpdateStatusAsync(entry.ExecutionId, RecoveryPackageStatus.Expired, cancellationToken);
                expiredIds.Add(entry.ExecutionId);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or KeyNotFoundException)
            {
                // One unreadable package must not stop the sweep, and it must not be silently marked expired
                // either — the index keeps saying Active so the next run tries again.
                errors.Add($"{entry.ExecutionId}: {ex.Message}");
            }
        }

        return new(active.Count, expiredIds.Count, expiredIds, errors);
    }
}
