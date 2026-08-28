#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Recovery;

public enum RecoveryPackageStatus
{
    /// <summary>Package exists on disk and is within its retention window.</summary>
    Active = 1,

    /// <summary>Retention elapsed; the package files were deleted. The index row and the permanent audit stay.</summary>
    Expired = 2,

    /// <summary>A governed rollback for this execution completed.</summary>
    RolledBack = 3,
}

/// <summary>
/// The searchable index of recovery packages. This is what makes a rollback possible from a cold start:
/// discovery reads the index, never a conversation, never a cached session, never a remembered path.
/// </summary>
public sealed record RecoveryIndexEntry(
    Guid ExecutionId,
    string ExecutionName,
    string AgentId,
    string ConnectionProfile,
    string Server,
    string Database,
    DateTimeOffset ExecutedAt,
    string Requester,
    IReadOnlyList<ActionOperation> OperationTypes,
    IReadOnlyList<string> TablesAffected,
    IReadOnlyList<string> BusinessKeys,
    int RecordsAffected,
    bool BackupRequired,
    bool RollbackSupported,
    int RetentionDays,
    DateTimeOffset ExpiresAt,
    string PackagePath,
    string ManifestChecksumSha256,
    RecoveryPackageStatus Status,
    string ProposalHash,
    string ValidationRuleId);

/// <summary>
/// Multi-criteria discovery query. Every criterion is optional and criteria AND together. An empty query
/// deliberately matches everything rather than nothing — discovery's job is to show the operator what exists,
/// and narrowing is their decision, not the runtime's.
/// </summary>
public sealed record RecoveryIndexQuery
{
    public Guid? ExecutionId { get; init; }
    public DateTimeOffset? ExecutedFrom { get; init; }
    public DateTimeOffset? ExecutedTo { get; init; }
    public string? AgentId { get; init; }
    public string? ConnectionProfile { get; init; }
    public string? Table { get; init; }
    public string? BusinessKey { get; init; }
    public string? Requester { get; init; }
    public RecoveryPackageStatus? Status { get; init; }

    public bool Matches(RecoveryIndexEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (ExecutionId is not null && entry.ExecutionId != ExecutionId) return false;
        if (ExecutedFrom is not null && entry.ExecutedAt < ExecutedFrom) return false;
        if (ExecutedTo is not null && entry.ExecutedAt > ExecutedTo) return false;
        if (!MatchesText(AgentId, entry.AgentId)) return false;
        if (!MatchesText(ConnectionProfile, entry.ConnectionProfile)) return false;
        if (!MatchesText(Requester, entry.Requester)) return false;
        if (Status is not null && entry.Status != Status) return false;
        if (!string.IsNullOrWhiteSpace(Table)
            && !entry.TablesAffected.Any(item => string.Equals(item, Table.Trim(), StringComparison.OrdinalIgnoreCase))) return false;
        if (!string.IsNullOrWhiteSpace(BusinessKey)
            && !entry.BusinessKeys.Any(item => item.Contains(BusinessKey.Trim(), StringComparison.OrdinalIgnoreCase))) return false;
        return true;
    }

    private static bool MatchesText(string? criterion, string value) =>
        string.IsNullOrWhiteSpace(criterion) || string.Equals(criterion.Trim(), value, StringComparison.OrdinalIgnoreCase);
}
