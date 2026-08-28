using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>Persisted write-validation knowledge gap. Append-only: a gap is closed by seeding a rule and
/// re-running the flow, never by deleting the evidence that it existed.</summary>
public sealed class WriteValidationKnowledgeGapEntity
{
    public Guid Id { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string ConnectionProfile { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public ActionOperation Operation { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ActionProposalId { get; set; }
    public DateTimeOffset DetectedAt { get; set; }
}

public sealed class WriteValidationKnowledgeGapConfiguration : IEntityTypeConfiguration<WriteValidationKnowledgeGapEntity>
{
    public void Configure(EntityTypeBuilder<WriteValidationKnowledgeGapEntity> builder)
    {
        builder.ToTable("AIGovernanceWriteValidationKnowledgeGaps");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.RequestId).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.AgentId).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.ConnectionProfile).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Resource).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(500).IsRequired();
        builder.HasIndex(entity => new { entity.Resource, entity.Operation });
        builder.HasIndex(entity => entity.DetectedAt);
    }
}

public sealed class EfWriteValidationKnowledgeGapStore(BlueprintOSDbContext context) : IWriteValidationKnowledgeGapStore
{
    public async Task RecordAsync(WriteValidationKnowledgeGap gap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gap);
        context.AIGovernanceWriteValidationKnowledgeGaps.Add(new WriteValidationKnowledgeGapEntity
        {
            Id = gap.Id,
            RequestId = gap.RequestId,
            AgentId = gap.AgentId,
            ConnectionProfile = gap.ConnectionProfile,
            Resource = gap.Resource,
            Operation = gap.Operation,
            Reason = gap.Reason,
            ActionProposalId = gap.ActionProposalId,
            DetectedAt = gap.DetectedAt,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WriteValidationKnowledgeGap>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.AIGovernanceWriteValidationKnowledgeGaps.AsNoTracking()
            .OrderBy(item => item.DetectedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(item => new WriteValidationKnowledgeGap(
            item.Id, item.RequestId, item.AgentId, item.ConnectionProfile, item.Resource,
            item.Operation, item.Reason, item.ActionProposalId, item.DetectedAt)).ToArray();
    }
}
