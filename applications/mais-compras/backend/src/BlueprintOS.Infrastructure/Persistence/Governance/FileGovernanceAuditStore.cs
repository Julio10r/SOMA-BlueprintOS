using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// File-based <see cref="IGovernanceAuditStore"/> under <c>{root}/audit/{yyyy-MM-dd}/{id:N}.json</c>, one file
/// per <see cref="GovernanceAuditEvent"/> keyed by Id, partitioned by the event's CreatedAt (UTC) date so the
/// directory does not grow unbounded. <see cref="ListByRequestAsync"/> is the only required lookup and it is
/// NOT by id, so no id->path index is needed here: it scans all date partitions (approach (a) from the spec —
/// simple, avoids an index-can-desync-from-truth failure mode, and fine at this project's volume).
/// This store is permanent: nothing here ever deletes a file.
/// </summary>
public sealed class FileGovernanceAuditStore(string rootDirectory) : IGovernanceAuditStore
{
    private readonly string _root = Path.Combine(rootDirectory, "audit");

    public Task AppendAsync(GovernanceAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        var path = BuildPath(auditEvent.Id, auditEvent.CreatedAt);
        return AtomicFileWriter.WithFileLockAsync(path, () => AtomicFileWriter.WriteJsonAsync(path, auditEvent, cancellationToken).ContinueWith(_ => true, cancellationToken));
    }

    public async Task<IReadOnlyList<GovernanceAuditEvent>> ListByRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        var results = new List<GovernanceAuditEvent>();
        await foreach (var item in AtomicFileWriter.ScanAllAsync<GovernanceAuditEvent>(_root, cancellationToken))
        {
            if (string.Equals(item.RequestId, requestId, StringComparison.Ordinal)) results.Add(item);
        }

        return results.OrderBy(item => item.CreatedAt).ToArray();
    }

    private string BuildPath(Guid id, DateTimeOffset createdAt) =>
        Path.Combine(_root, createdAt.UtcDateTime.ToString("yyyy-MM-dd"), $"{id:N}.json");
}
