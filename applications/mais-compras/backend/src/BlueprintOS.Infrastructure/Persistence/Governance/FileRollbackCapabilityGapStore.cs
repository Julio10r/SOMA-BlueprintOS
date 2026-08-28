using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// File-based <see cref="IRollbackCapabilityGapStore"/> under
/// <c>{root}/rollback-capability-gaps/{yyyy-MM-dd}/{id:N}.json</c>, one file per <see cref="RollbackCapabilityGap"/>
/// keyed by Id, partitioned by DetectedAt (UTC) date. Only <see cref="ListAsync"/> is exposed by the
/// interface (no by-id lookup), so every read is a full scan across date partitions. Append-only, never
/// deleted: a gap is closed by adding rollback support to the capability and re-running, not by erasing the
/// evidence it existed.
/// </summary>
public sealed class FileRollbackCapabilityGapStore(string rootDirectory) : IRollbackCapabilityGapStore
{
    private readonly string _root = Path.Combine(rootDirectory, "rollback-capability-gaps");

    public Task RecordAsync(RollbackCapabilityGap gap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gap);
        var path = BuildPath(gap.Id, gap.DetectedAt);
        return AtomicFileWriter.WithFileLockAsync(path, () => AtomicFileWriter.WriteJsonAsync(path, gap, cancellationToken).ContinueWith(_ => true, cancellationToken));
    }

    public async Task<IReadOnlyList<RollbackCapabilityGap>> ListAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<RollbackCapabilityGap>();
        await foreach (var item in AtomicFileWriter.ScanAllAsync<RollbackCapabilityGap>(_root, cancellationToken))
        {
            results.Add(item);
        }

        return results.OrderBy(item => item.DetectedAt).ToArray();
    }

    private string BuildPath(Guid id, DateTimeOffset detectedAt) =>
        Path.Combine(_root, BrazilTimeZoneProvider.ToSaoPaulo(detectedAt).ToString("yyyy-MM-dd"), $"{id:N}.json");
}
