using System.Text.Json;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// PERMANENT record of a governed write execution, in its own table. Recovery packages expire and their files
/// are deleted; this row is never deleted by anything — <see cref="RecoveryRetentionCleanupService"/> does not
/// even take a dependency on this store. Payloads are summarized, not copied, so the row can be kept forever.
/// </summary>
public sealed class WriteExecutionAuditEntity
{
    public Guid ExecutionId { get; set; }
    public string ExecutionName { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string ConnectionProfile { get; set; } = string.Empty;
    public string WriteVerificationPolicyVersion { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public string Requester { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public string OperationsJson { get; set; } = "[]";
    public string TablesAffectedJson { get; set; } = "[]";
    public string BusinessKeysJson { get; set; } = "[]";
    public int RecordsAffected { get; set; }
    public string ProceduresInvokedJson { get; set; } = "[]";
    public string BeforeAfterSummary { get; set; } = string.Empty;
    public string ChangedFieldsJson { get; set; } = "[]";
    public string ValidationRuleId { get; set; } = string.Empty;
    public int RecordsValidated { get; set; }
    public int RecordsWithErrors { get; set; }
    public bool PostWriteValidationPassed { get; set; }
    public bool BackupRequired { get; set; }
    public bool BackupCreated { get; set; }
    public int RetentionDays { get; set; }
    public DateTimeOffset? BackupExpiresAt { get; set; }
    public RecoveryPackageStatus RecoveryPackageStatus { get; set; }
    public bool RollbackAvailable { get; set; }
    public bool RollbackExecuted { get; set; }
    public string? RollbackResult { get; set; }
    public string ErrorsJson { get; set; } = "[]";
    public string KnowledgeGapsJson { get; set; } = "[]";
    public WriteExecutionOutcome Outcome { get; set; }
    public string? ProposalHash { get; set; }
}

public sealed class WriteExecutionAuditConfiguration : IEntityTypeConfiguration<WriteExecutionAuditEntity>
{
    public void Configure(EntityTypeBuilder<WriteExecutionAuditEntity> builder)
    {
        builder.ToTable("AIGovernanceWriteExecutionAudit");
        builder.HasKey(entity => entity.ExecutionId);
        builder.Property(entity => entity.ExecutionName).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.AgentId).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.ConnectionProfile).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.WriteVerificationPolicyVersion).HasMaxLength(60).IsRequired();
        builder.Property(entity => entity.Server).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Database).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Requester).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Intent).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.OperationsJson).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.TablesAffectedJson).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.BusinessKeysJson).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.ProceduresInvokedJson).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.BeforeAfterSummary).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.ChangedFieldsJson).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.ValidationRuleId).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.RollbackResult).HasMaxLength(200);
        builder.Property(entity => entity.ErrorsJson).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.KnowledgeGapsJson).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.ProposalHash).HasMaxLength(64);
        builder.HasIndex(entity => new { entity.ConnectionProfile, entity.StartedAt });
        builder.HasIndex(entity => new { entity.AgentId, entity.StartedAt });
        builder.HasIndex(entity => entity.Requester);
        builder.HasIndex(entity => entity.Outcome);
    }
}

public sealed class EfWriteExecutionAuditStore(BlueprintOSDbContext context) : IWriteExecutionAuditStore
{
    public async Task AppendAsync(WriteExecutionAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        context.AIGovernanceWriteExecutionAudit.Add(new WriteExecutionAuditEntity
        {
            ExecutionId = record.ExecutionId,
            ExecutionName = record.ExecutionName,
            AgentId = record.AgentId,
            ConnectionProfile = record.ConnectionProfile,
            WriteVerificationPolicyVersion = record.WriteVerificationPolicyVersion,
            Server = record.Server,
            Database = record.Database,
            StartedAt = record.StartedAt,
            CompletedAt = record.CompletedAt,
            Requester = record.Requester,
            Intent = record.Intent,
            OperationsJson = JsonSerializer.Serialize(record.Operations.Select(item => item.ToString())),
            TablesAffectedJson = JsonSerializer.Serialize(record.TablesAffected),
            BusinessKeysJson = JsonSerializer.Serialize(record.BusinessKeys),
            RecordsAffected = record.RecordsAffected,
            ProceduresInvokedJson = JsonSerializer.Serialize(record.ProceduresInvoked),
            BeforeAfterSummary = record.BeforeAfterSummary,
            ChangedFieldsJson = JsonSerializer.Serialize(record.ChangedFields),
            ValidationRuleId = record.ValidationRuleId,
            RecordsValidated = record.RecordsValidated,
            RecordsWithErrors = record.RecordsWithErrors,
            PostWriteValidationPassed = record.PostWriteValidationPassed,
            BackupRequired = record.BackupRequired,
            BackupCreated = record.BackupCreated,
            RetentionDays = record.RetentionDays,
            BackupExpiresAt = record.BackupExpiresAt,
            RecoveryPackageStatus = record.RecoveryPackageStatus,
            RollbackAvailable = record.RollbackAvailable,
            RollbackExecuted = record.RollbackExecuted,
            RollbackResult = record.RollbackResult,
            ErrorsJson = JsonSerializer.Serialize(record.Errors),
            KnowledgeGapsJson = JsonSerializer.Serialize(record.KnowledgeGaps),
            Outcome = record.Outcome,
            ProposalHash = record.ProposalHash,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<WriteExecutionAuditRecord?> GetAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var entity = await context.AIGovernanceWriteExecutionAudit.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ExecutionId == executionId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<WriteExecutionAuditRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.AIGovernanceWriteExecutionAudit.AsNoTracking()
            .OrderBy(item => item.StartedAt).ToListAsync(cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task MarkRollbackAsync(Guid executionId, bool rollbackExecuted, string? rollbackResult, RecoveryPackageStatus packageStatus, CancellationToken cancellationToken = default)
    {
        var entity = await context.AIGovernanceWriteExecutionAudit
            .SingleOrDefaultAsync(item => item.ExecutionId == executionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Write execution audit {executionId} was not found.");
        entity.RollbackExecuted = rollbackExecuted;
        entity.RollbackResult = rollbackResult;
        entity.RecoveryPackageStatus = packageStatus;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRecoveryPackageStatusAsync(Guid executionId, RecoveryPackageStatus packageStatus, CancellationToken cancellationToken = default)
    {
        var entity = await context.AIGovernanceWriteExecutionAudit
            .SingleOrDefaultAsync(item => item.ExecutionId == executionId, cancellationToken);
        if (entity is null) return;
        entity.RecoveryPackageStatus = packageStatus;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static WriteExecutionAuditRecord Map(WriteExecutionAuditEntity entity) => new()
    {
        ExecutionId = entity.ExecutionId,
        ExecutionName = entity.ExecutionName,
        AgentId = entity.AgentId,
        ConnectionProfile = entity.ConnectionProfile,
        WriteVerificationPolicyVersion = entity.WriteVerificationPolicyVersion,
        Server = entity.Server,
        Database = entity.Database,
        StartedAt = entity.StartedAt,
        CompletedAt = entity.CompletedAt,
        Requester = entity.Requester,
        Intent = entity.Intent,
        Operations = (JsonSerializer.Deserialize<string[]>(entity.OperationsJson) ?? [])
            .Select(item => Enum.TryParse<ActionOperation>(item, out var parsed) ? parsed : ActionOperation.Unknown).ToArray(),
        TablesAffected = JsonSerializer.Deserialize<string[]>(entity.TablesAffectedJson) ?? [],
        BusinessKeys = JsonSerializer.Deserialize<string[]>(entity.BusinessKeysJson) ?? [],
        RecordsAffected = entity.RecordsAffected,
        ProceduresInvoked = JsonSerializer.Deserialize<string[]>(entity.ProceduresInvokedJson) ?? [],
        BeforeAfterSummary = entity.BeforeAfterSummary,
        ChangedFields = JsonSerializer.Deserialize<string[]>(entity.ChangedFieldsJson) ?? [],
        ValidationRuleId = entity.ValidationRuleId,
        RecordsValidated = entity.RecordsValidated,
        RecordsWithErrors = entity.RecordsWithErrors,
        PostWriteValidationPassed = entity.PostWriteValidationPassed,
        BackupRequired = entity.BackupRequired,
        BackupCreated = entity.BackupCreated,
        RetentionDays = entity.RetentionDays,
        BackupExpiresAt = entity.BackupExpiresAt,
        RecoveryPackageStatus = entity.RecoveryPackageStatus,
        RollbackAvailable = entity.RollbackAvailable,
        RollbackExecuted = entity.RollbackExecuted,
        RollbackResult = entity.RollbackResult,
        Errors = JsonSerializer.Deserialize<string[]>(entity.ErrorsJson) ?? [],
        KnowledgeGaps = JsonSerializer.Deserialize<string[]>(entity.KnowledgeGapsJson) ?? [],
        Outcome = entity.Outcome,
        ProposalHash = entity.ProposalHash,
    };
}

/// <summary>Permanent audit of rollback attempts. Like the write execution audit, never deleted.</summary>
public sealed class RollbackAuditEntity
{
    public Guid RollbackExecutionId { get; set; }
    public Guid OriginalExecutionId { get; set; }
    public string Requester { get; set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; set; }
    public bool ExplicitConfirmationReceived { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string Justification { get; set; } = string.Empty;
    public string TablesAffectedJson { get; set; } = "[]";
    public string BusinessKeysJson { get; set; } = "[]";
    public int RecordsAffected { get; set; }
    public string ConcurrencyFindingsJson { get; set; } = "[]";
    public string ExpectedStateSummary { get; set; } = string.Empty;
    public string ObservedStateSummary { get; set; } = string.Empty;
    public RollbackExecutionStatus Status { get; set; }
    public bool PostRollbackValidationPassed { get; set; }
    public string? PostRollbackValidationRuleId { get; set; }
    public string ErrorsJson { get; set; } = "[]";
    public string? RollbackProposalHash { get; set; }
}

public sealed class RollbackAuditConfiguration : IEntityTypeConfiguration<RollbackAuditEntity>
{
    public void Configure(EntityTypeBuilder<RollbackAuditEntity> builder)
    {
        builder.ToTable("AIGovernanceRollbackAudit");
        builder.HasKey(entity => entity.RollbackExecutionId);
        builder.Property(entity => entity.Requester).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Justification).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.TablesAffectedJson).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.BusinessKeysJson).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.ConcurrencyFindingsJson).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.ExpectedStateSummary).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.ObservedStateSummary).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.PostRollbackValidationRuleId).HasMaxLength(160);
        builder.Property(entity => entity.ErrorsJson).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.RollbackProposalHash).HasMaxLength(64);
        builder.HasIndex(entity => entity.OriginalExecutionId);
        builder.HasIndex(entity => entity.RequestedAt);
    }
}

public sealed class EfRollbackAuditStore(BlueprintOSDbContext context) : IRollbackAuditStore
{
    public async Task AppendAsync(RollbackAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        context.AIGovernanceRollbackAudit.Add(new RollbackAuditEntity
        {
            RollbackExecutionId = record.RollbackExecutionId,
            OriginalExecutionId = record.OriginalExecutionId,
            Requester = record.Requester,
            RequestedAt = record.RequestedAt,
            ExplicitConfirmationReceived = record.ExplicitConfirmationReceived,
            ConfirmedAt = record.ConfirmedAt,
            Justification = record.Justification,
            TablesAffectedJson = JsonSerializer.Serialize(record.TablesAffected),
            BusinessKeysJson = JsonSerializer.Serialize(record.BusinessKeys),
            RecordsAffected = record.RecordsAffected,
            ConcurrencyFindingsJson = JsonSerializer.Serialize(record.ConcurrencyFindings),
            ExpectedStateSummary = record.ExpectedStateSummary,
            ObservedStateSummary = record.ObservedStateSummary,
            Status = record.Status,
            PostRollbackValidationPassed = record.PostRollbackValidationPassed,
            PostRollbackValidationRuleId = record.PostRollbackValidationRuleId,
            ErrorsJson = JsonSerializer.Serialize(record.Errors),
            RollbackProposalHash = record.RollbackProposalHash,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RollbackAuditRecord>> ListByOriginalExecutionAsync(Guid originalExecutionId, CancellationToken cancellationToken = default)
    {
        var entities = await context.AIGovernanceRollbackAudit.AsNoTracking()
            .Where(item => item.OriginalExecutionId == originalExecutionId)
            .OrderBy(item => item.RequestedAt).ToListAsync(cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<RollbackAuditRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await context.AIGovernanceRollbackAudit.AsNoTracking()
            .OrderBy(item => item.RequestedAt).ToListAsync(cancellationToken);
        return entities.Select(Map).ToArray();
    }

    private static RollbackAuditRecord Map(RollbackAuditEntity entity) => new()
    {
        RollbackExecutionId = entity.RollbackExecutionId,
        OriginalExecutionId = entity.OriginalExecutionId,
        Requester = entity.Requester,
        RequestedAt = entity.RequestedAt,
        ExplicitConfirmationReceived = entity.ExplicitConfirmationReceived,
        ConfirmedAt = entity.ConfirmedAt,
        Justification = entity.Justification,
        TablesAffected = JsonSerializer.Deserialize<string[]>(entity.TablesAffectedJson) ?? [],
        BusinessKeys = JsonSerializer.Deserialize<string[]>(entity.BusinessKeysJson) ?? [],
        RecordsAffected = entity.RecordsAffected,
        ConcurrencyFindings = JsonSerializer.Deserialize<string[]>(entity.ConcurrencyFindingsJson) ?? [],
        ExpectedStateSummary = entity.ExpectedStateSummary,
        ObservedStateSummary = entity.ObservedStateSummary,
        Status = entity.Status,
        PostRollbackValidationPassed = entity.PostRollbackValidationPassed,
        PostRollbackValidationRuleId = entity.PostRollbackValidationRuleId,
        Errors = JsonSerializer.Deserialize<string[]>(entity.ErrorsJson) ?? [],
        RollbackProposalHash = entity.RollbackProposalHash,
    };
}
