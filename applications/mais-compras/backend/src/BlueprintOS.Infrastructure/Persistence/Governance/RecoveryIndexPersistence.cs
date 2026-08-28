using System.Text.Json;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>Index row for one recovery package. Retention deletes package FILES and flips
/// <see cref="Status"/> to Expired; this row itself is never deleted.</summary>
public sealed class RecoveryIndexEntryEntity
{
    public Guid ExecutionId { get; set; }
    public string ExecutionName { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string ConnectionProfile { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public DateTimeOffset ExecutedAt { get; set; }
    public string Requester { get; set; } = string.Empty;
    public string OperationTypesJson { get; set; } = "[]";
    public string TablesAffectedJson { get; set; } = "[]";
    public string BusinessKeysJson { get; set; } = "[]";
    public int RecordsAffected { get; set; }
    public bool BackupRequired { get; set; }
    public bool RollbackSupported { get; set; }
    public int RetentionDays { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string PackagePath { get; set; } = string.Empty;
    public string ManifestChecksumSha256 { get; set; } = string.Empty;
    public RecoveryPackageStatus Status { get; set; }
    public string ProposalHash { get; set; } = string.Empty;
    public string ValidationRuleId { get; set; } = string.Empty;
}

public sealed class RecoveryIndexEntryConfiguration : IEntityTypeConfiguration<RecoveryIndexEntryEntity>
{
    public void Configure(EntityTypeBuilder<RecoveryIndexEntryEntity> builder)
    {
        builder.ToTable("AIGovernanceRecoveryIndex");
        builder.HasKey(entity => entity.ExecutionId);
        builder.Property(entity => entity.ExecutionName).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.AgentId).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.ConnectionProfile).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Server).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Database).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Requester).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.OperationTypesJson).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.TablesAffectedJson).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.BusinessKeysJson).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.PackagePath).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.ManifestChecksumSha256).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.ProposalHash).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.ValidationRuleId).HasMaxLength(160).IsRequired();
        builder.HasIndex(entity => new { entity.ConnectionProfile, entity.ExecutedAt });
        builder.HasIndex(entity => new { entity.AgentId, entity.ExecutedAt });
        builder.HasIndex(entity => new { entity.Status, entity.ExpiresAt });
        builder.HasIndex(entity => entity.Requester);
    }
}

public sealed class EfRecoveryIndexStore(BlueprintOSDbContext context) : IRecoveryIndexStore
{
    public async Task<IReadOnlyList<RecoveryIndexEntry>> FindAsync(RecoveryIndexQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Scalar criteria are pushed to the database; the JSON-encoded collection criteria (table, business
        // key) are applied in memory against the already-narrowed set, via the same RecoveryIndexQuery.Matches
        // used by the in-memory store, so both implementations answer identically.
        var queryable = context.AIGovernanceRecoveryIndex.AsNoTracking().AsQueryable();
        if (query.ExecutionId is not null) queryable = queryable.Where(item => item.ExecutionId == query.ExecutionId);
        if (query.ExecutedFrom is not null) queryable = queryable.Where(item => item.ExecutedAt >= query.ExecutedFrom);
        if (query.ExecutedTo is not null) queryable = queryable.Where(item => item.ExecutedAt <= query.ExecutedTo);
        if (query.Status is not null) queryable = queryable.Where(item => item.Status == query.Status);

        var entities = await queryable.OrderByDescending(item => item.ExecutedAt).ToListAsync(cancellationToken);
        return entities.Select(Map).Where(query.Matches).ToArray();
    }

    public async Task AppendAsync(RecoveryIndexEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var exists = await context.AIGovernanceRecoveryIndex.AsNoTracking()
            .AnyAsync(item => item.ExecutionId == entry.ExecutionId, cancellationToken);
        if (exists) throw new InvalidOperationException($"Recovery index already contains execution {entry.ExecutionId}.");

        context.AIGovernanceRecoveryIndex.Add(new RecoveryIndexEntryEntity
        {
            ExecutionId = entry.ExecutionId,
            ExecutionName = entry.ExecutionName,
            AgentId = entry.AgentId,
            ConnectionProfile = entry.ConnectionProfile,
            Server = entry.Server,
            Database = entry.Database,
            ExecutedAt = entry.ExecutedAt,
            Requester = entry.Requester,
            OperationTypesJson = JsonSerializer.Serialize(entry.OperationTypes.Select(item => item.ToString())),
            TablesAffectedJson = JsonSerializer.Serialize(entry.TablesAffected),
            BusinessKeysJson = JsonSerializer.Serialize(entry.BusinessKeys),
            RecordsAffected = entry.RecordsAffected,
            BackupRequired = entry.BackupRequired,
            RollbackSupported = entry.RollbackSupported,
            RetentionDays = entry.RetentionDays,
            ExpiresAt = entry.ExpiresAt,
            PackagePath = entry.PackagePath,
            ManifestChecksumSha256 = entry.ManifestChecksumSha256,
            Status = entry.Status,
            ProposalHash = entry.ProposalHash,
            ValidationRuleId = entry.ValidationRuleId,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid executionId, RecoveryPackageStatus status, CancellationToken cancellationToken = default)
    {
        var entity = await context.AIGovernanceRecoveryIndex.SingleOrDefaultAsync(item => item.ExecutionId == executionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Recovery index entry {executionId} was not found.");
        entity.Status = status;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static RecoveryIndexEntry Map(RecoveryIndexEntryEntity entity) => new(
        entity.ExecutionId,
        entity.ExecutionName,
        entity.AgentId,
        entity.ConnectionProfile,
        entity.Server,
        entity.Database,
        entity.ExecutedAt,
        entity.Requester,
        (JsonSerializer.Deserialize<string[]>(entity.OperationTypesJson) ?? [])
            .Select(item => Enum.TryParse<ActionOperation>(item, out var parsed) ? parsed : ActionOperation.Unknown).ToArray(),
        JsonSerializer.Deserialize<string[]>(entity.TablesAffectedJson) ?? [],
        JsonSerializer.Deserialize<string[]>(entity.BusinessKeysJson) ?? [],
        entity.RecordsAffected,
        entity.BackupRequired,
        entity.RollbackSupported,
        entity.RetentionDays,
        entity.ExpiresAt,
        entity.PackagePath,
        entity.ManifestChecksumSha256,
        entity.Status,
        entity.ProposalHash,
        entity.ValidationRuleId);
}
