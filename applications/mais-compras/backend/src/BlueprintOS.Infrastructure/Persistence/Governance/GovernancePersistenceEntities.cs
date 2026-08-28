using System.Text.Json;
using BlueprintOS.Core.AI.Governance.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

public sealed class GovernanceApprovalRequestEntity
{
    public Guid Id { get; set; }
    public Guid ActionProposalId { get; set; }
    public string ProposalHash { get; set; } = string.Empty;
    public RiskClassification RiskClassification { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RequiredApprover { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public ApprovalRequestStatus Status { get; set; }
}

public sealed class GovernanceApprovalGrantEntity
{
    public Guid Id { get; set; }
    public Guid ApprovalRequestId { get; set; }
    public string ProposalHash { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTimeOffset ApprovedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class GovernanceAuditEventEntity
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public Guid? ActionProposalId { get; set; }
    public string? ProposalHash { get; set; }
    public string? AgentId { get; set; }
    public string? SubjectId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string CategoriesJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }

    public IReadOnlyList<string> Categories => JsonSerializer.Deserialize<string[]>(CategoriesJson) ?? [];
}

public sealed class GovernanceApprovalRequestConfiguration : IEntityTypeConfiguration<GovernanceApprovalRequestEntity>
{
    public void Configure(EntityTypeBuilder<GovernanceApprovalRequestEntity> builder)
    {
        builder.ToTable("AIGovernanceApprovalRequests");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.ProposalHash).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.RequiredApprover).HasMaxLength(160).IsRequired();
        builder.HasIndex(entity => entity.ProposalHash);
        builder.HasIndex(entity => new { entity.Status, entity.ExpiresAt });
    }
}

public sealed class GovernanceApprovalGrantConfiguration : IEntityTypeConfiguration<GovernanceApprovalGrantEntity>
{
    public void Configure(EntityTypeBuilder<GovernanceApprovalGrantEntity> builder)
    {
        builder.ToTable("AIGovernanceApprovalGrants");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.ProposalHash).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.ApprovedBy).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Scope).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.Notes).HasMaxLength(1000);
        builder.HasIndex(entity => entity.ApprovalRequestId);
        builder.HasIndex(entity => entity.ProposalHash);
        builder.HasOne<GovernanceApprovalRequestEntity>()
            .WithMany()
            .HasForeignKey(entity => entity.ApprovalRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GovernanceAuditEventConfiguration : IEntityTypeConfiguration<GovernanceAuditEventEntity>
{
    public void Configure(EntityTypeBuilder<GovernanceAuditEventEntity> builder)
    {
        builder.ToTable("AIGovernanceAuditEvents");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.EventType).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.RequestId).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.ProposalHash).HasMaxLength(64);
        builder.Property(entity => entity.AgentId).HasMaxLength(160);
        builder.Property(entity => entity.SubjectId).HasMaxLength(160);
        builder.Property(entity => entity.Outcome).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.CategoriesJson).HasMaxLength(2000).IsRequired();
        builder.Ignore(entity => entity.Categories);
        builder.HasIndex(entity => new { entity.RequestId, entity.CreatedAt });
        builder.HasIndex(entity => entity.ActionProposalId);
    }
}
