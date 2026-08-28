using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>Append-only version row of a write verification policy. Keyed by (profile, version): a change
/// is always a new row, never an UPDATE of an existing one.</summary>
public sealed class WriteVerificationProfileEntity
{
    public string ConnectionProfile { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public bool BackupRequired { get; set; }
    public bool RollbackSupported { get; set; }
    public int BackupRetentionDays { get; set; }
    public bool PostWriteValidationRequired { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTimeOffset EffectiveFrom { get; set; }
}

public sealed class WriteVerificationProfileConfiguration : IEntityTypeConfiguration<WriteVerificationProfileEntity>
{
    public void Configure(EntityTypeBuilder<WriteVerificationProfileEntity> builder)
    {
        builder.ToTable("AIGovernanceWriteVerificationProfiles");
        builder.HasKey(entity => new { entity.ConnectionProfile, entity.PolicyVersion });
        builder.Property(entity => entity.ConnectionProfile).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.PolicyVersion).HasMaxLength(60).IsRequired();
        builder.Property(entity => entity.ApprovedBy).HasMaxLength(160).IsRequired();
        builder.HasIndex(entity => new { entity.ConnectionProfile, entity.EffectiveFrom });

        builder.HasData(WriteVerificationProfileSeeds.All.Select(profile => new WriteVerificationProfileEntity
        {
            ConnectionProfile = profile.ConnectionProfile,
            PolicyVersion = profile.PolicyVersion,
            BackupRequired = profile.BackupRequired,
            RollbackSupported = profile.RollbackSupported,
            BackupRetentionDays = profile.BackupRetentionDays,
            PostWriteValidationRequired = profile.PostWriteValidationRequired,
            ApprovedBy = profile.ApprovedBy,
            EffectiveFrom = profile.EffectiveFrom,
        }));
    }
}

public sealed class EfWriteVerificationProfileStore(BlueprintOSDbContext context) : IWriteVerificationProfileStore
{
    public async Task<WriteVerificationProfile?> ResolveAsync(string connectionProfile, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var entity = await context.AIGovernanceWriteVerificationProfiles.AsNoTracking()
            .Where(item => item.ConnectionProfile == connectionProfile && item.EffectiveFrom <= asOf)
            .OrderByDescending(item => item.EffectiveFrom)
            .ThenByDescending(item => item.PolicyVersion)
            .FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<WriteVerificationProfile>> ListVersionsAsync(string connectionProfile, CancellationToken cancellationToken = default)
    {
        var entities = await context.AIGovernanceWriteVerificationProfiles.AsNoTracking()
            .Where(item => item.ConnectionProfile == connectionProfile)
            .OrderBy(item => item.EffectiveFrom)
            .ToListAsync(cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task AppendVersionAsync(WriteVerificationProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var exists = await context.AIGovernanceWriteVerificationProfiles.AsNoTracking()
            .AnyAsync(item => item.ConnectionProfile == profile.ConnectionProfile && item.PolicyVersion == profile.PolicyVersion, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException(
                $"Write verification profile '{profile.ConnectionProfile}' already has a version '{profile.PolicyVersion}'. Versions are append-only and immutable.");
        }

        context.AIGovernanceWriteVerificationProfiles.Add(new WriteVerificationProfileEntity
        {
            ConnectionProfile = profile.ConnectionProfile,
            PolicyVersion = profile.PolicyVersion,
            BackupRequired = profile.BackupRequired,
            RollbackSupported = profile.RollbackSupported,
            BackupRetentionDays = profile.BackupRetentionDays,
            PostWriteValidationRequired = profile.PostWriteValidationRequired,
            ApprovedBy = profile.ApprovedBy,
            EffectiveFrom = profile.EffectiveFrom,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static WriteVerificationProfile Map(WriteVerificationProfileEntity entity) => new(
        entity.ConnectionProfile, entity.BackupRequired, entity.RollbackSupported, entity.BackupRetentionDays,
        entity.PostWriteValidationRequired, entity.PolicyVersion, entity.ApprovedBy, entity.EffectiveFrom);
}
