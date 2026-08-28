using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// File-based <see cref="IWriteValidationKnowledgeGapStore"/> under
/// <c>{root}/knowledge-gaps/{yyyy-MM-dd}/{id:N}.json</c>, one file per <see cref="WriteValidationKnowledgeGap"/>
/// keyed by Id, partitioned by DetectedAt (UTC) date. Only <see cref="ListAsync"/> is exposed by the
/// interface (no by-id lookup), so every read is a full scan across date partitions. Append-only, never
/// deleted: a gap is closed by adding a validation rule and re-running, not by erasing the evidence it
/// existed.
/// </summary>
public sealed class FileWriteValidationKnowledgeGapStore(string rootDirectory) : IWriteValidationKnowledgeGapStore
{
    private readonly string _root = Path.Combine(rootDirectory, "knowledge-gaps");

    public Task RecordAsync(WriteValidationKnowledgeGap gap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gap);
        var path = BuildPath(gap.Id, gap.DetectedAt);
        return AtomicFileWriter.WithFileLockAsync(path, () => AtomicFileWriter.WriteJsonAsync(path, gap, cancellationToken).ContinueWith(_ => true, cancellationToken));
    }

    public async Task<IReadOnlyList<WriteValidationKnowledgeGap>> ListAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<WriteValidationKnowledgeGap>();
        await foreach (var item in AtomicFileWriter.ScanAllAsync<WriteValidationKnowledgeGap>(_root, cancellationToken))
        {
            results.Add(item);
        }

        return results.OrderBy(item => item.DetectedAt).ToArray();
    }

    private string BuildPath(Guid id, DateTimeOffset detectedAt) =>
        Path.Combine(_root, detectedAt.UtcDateTime.ToString("yyyy-MM-dd"), $"{id:N}.json");
}
