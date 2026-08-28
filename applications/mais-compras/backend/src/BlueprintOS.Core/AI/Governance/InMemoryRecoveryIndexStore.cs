#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>In-memory recovery index with the same semantics as the EF one, including "always a list".</summary>
public sealed class InMemoryRecoveryIndexStore : IRecoveryIndexStore
{
    private readonly List<RecoveryIndexEntry> _entries = [];

    public InMemoryRecoveryIndexStore(IEnumerable<RecoveryIndexEntry>? seed = null)
    {
        if (seed is not null) _entries.AddRange(seed);
    }

    public Task<IReadOnlyList<RecoveryIndexEntry>> FindAsync(RecoveryIndexQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        lock (_entries)
        {
            return Task.FromResult<IReadOnlyList<RecoveryIndexEntry>>(
                _entries.Where(query.Matches).OrderByDescending(entry => entry.ExecutedAt).ToArray());
        }
    }

    public Task AppendAsync(RecoveryIndexEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        lock (_entries)
        {
            if (_entries.Any(item => item.ExecutionId == entry.ExecutionId))
            {
                throw new InvalidOperationException($"Recovery index already contains execution {entry.ExecutionId}.");
            }

            _entries.Add(entry);
        }

        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(Guid executionId, RecoveryPackageStatus status, CancellationToken cancellationToken = default)
    {
        lock (_entries)
        {
            var index = _entries.FindIndex(item => item.ExecutionId == executionId);
            if (index < 0) throw new KeyNotFoundException($"Recovery index entry {executionId} was not found.");
            _entries[index] = _entries[index] with { Status = status };
        }

        return Task.CompletedTask;
    }
}
