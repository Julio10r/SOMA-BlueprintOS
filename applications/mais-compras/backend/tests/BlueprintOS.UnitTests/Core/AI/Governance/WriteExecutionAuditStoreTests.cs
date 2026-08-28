using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class WriteExecutionAuditStoreTests : IDisposable
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blueprintos-audit-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Every_Documented_Field_Round_Trips_Through_The_Permanent_Table()
    {
        await using var db = NewDb();
        var store = new EfWriteExecutionAuditStore(db);
        var record = Record(Guid.NewGuid());

        await store.AppendAsync(record);
        var loaded = await store.GetAsync(record.ExecutionId);

        Assert.NotNull(loaded);
        Assert.Equal(record.ExecutionName, loaded!.ExecutionName);
        Assert.Equal(record.AgentId, loaded.AgentId);
        Assert.Equal(record.ConnectionProfile, loaded.ConnectionProfile);
        Assert.Equal(record.WriteVerificationPolicyVersion, loaded.WriteVerificationPolicyVersion);
        Assert.Equal(record.Server, loaded.Server);
        Assert.Equal(record.Database, loaded.Database);
        Assert.Equal(record.Requester, loaded.Requester);
        Assert.Equal(record.Intent, loaded.Intent);
        Assert.Equal(record.Operations, loaded.Operations);
        Assert.Equal(record.TablesAffected, loaded.TablesAffected);
        Assert.Equal(record.BusinessKeys, loaded.BusinessKeys);
        Assert.Equal(record.RecordsAffected, loaded.RecordsAffected);
        Assert.Equal(record.ProceduresInvoked, loaded.ProceduresInvoked);
        Assert.Equal(record.BeforeAfterSummary, loaded.BeforeAfterSummary);
        Assert.Equal(record.ChangedFields, loaded.ChangedFields);
        Assert.Equal(record.ValidationRuleId, loaded.ValidationRuleId);
        Assert.Equal(record.RecordsValidated, loaded.RecordsValidated);
        Assert.Equal(record.RecordsWithErrors, loaded.RecordsWithErrors);
        Assert.True(loaded.PostWriteValidationPassed);
        Assert.True(loaded.BackupRequired);
        Assert.True(loaded.BackupCreated);
        Assert.Equal(30, loaded.RetentionDays);
        Assert.Equal(record.BackupExpiresAt, loaded.BackupExpiresAt);
        Assert.Equal(RecoveryPackageStatus.Active, loaded.RecoveryPackageStatus);
        Assert.True(loaded.RollbackAvailable);
        Assert.False(loaded.RollbackExecuted);
        Assert.Equal(record.Errors, loaded.Errors);
        Assert.Equal(record.KnowledgeGaps, loaded.KnowledgeGaps);
        Assert.Equal(WriteExecutionOutcome.Completed, loaded.Outcome);
        Assert.Equal(record.ProposalHash, loaded.ProposalHash);
    }

    [Fact]
    public async Task Audit_Survives_Retention_Cleanup_And_Remains_Queryable()
    {
        await using var db = NewDb();
        var store = new EfWriteExecutionAuditStore(db);
        var writer = new RecoveryPackageWriter(_root);
        var index = new InMemoryRecoveryIndexStore();
        var executionId = Guid.NewGuid();

        var manifest = Manifest(executionId);
        var receipt = await writer.CreateAsync(manifest,
            [new RecoveryDataSet("FORNECEDORES", [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123" }])], []);
        await index.AppendAsync(new RecoveryIndexEntry(
            executionId, manifest.ExecutionName, manifest.AgentId, manifest.ConnectionProfile, manifest.Server,
            manifest.Database, manifest.ExecutedAt, manifest.Requester, manifest.OperationTypes, manifest.TablesAffected,
            manifest.BusinessKeys, 1, true, true, 30, manifest.ExpiresAt, receipt.PackagePath,
            receipt.ManifestChecksumSha256, RecoveryPackageStatus.Active, manifest.ProposalHash, manifest.ValidationRuleId));
        await store.AppendAsync(Record(executionId));

        var cleanup = new RecoveryRetentionCleanupService(index, writer);
        var report = await cleanup.RunOnceAsync(CreatedAt.AddDays(31));

        Assert.Equal(1, report.Expired);
        Assert.False(Directory.Exists(receipt.PackagePath));

        // The recovery material is gone; the permanent record of the write is not.
        var surviving = await store.GetAsync(executionId);
        Assert.NotNull(surviving);
        Assert.Equal(WriteExecutionOutcome.Completed, surviving!.Outcome);
        Assert.Single(await store.ListAsync());
    }

    [Fact]
    public async Task Rollback_Outcome_Is_Recorded_Against_The_Original_Execution()
    {
        await using var db = NewDb();
        var store = new EfWriteExecutionAuditStore(db);
        var record = Record(Guid.NewGuid());
        await store.AppendAsync(record);

        await store.MarkRollbackAsync(record.ExecutionId, true, "Completed", RecoveryPackageStatus.RolledBack);

        var loaded = await store.GetAsync(record.ExecutionId);
        Assert.True(loaded!.RollbackExecuted);
        Assert.Equal("Completed", loaded.RollbackResult);
        Assert.Equal(RecoveryPackageStatus.RolledBack, loaded.RecoveryPackageStatus);
    }

    [Fact]
    public async Task Rollback_Audit_Round_Trips_And_Is_Queryable_By_Original_Execution()
    {
        await using var db = NewDb();
        var store = new EfRollbackAuditStore(db);
        var originalExecutionId = Guid.NewGuid();

        await store.AppendAsync(new RollbackAuditRecord
        {
            RollbackExecutionId = Guid.NewGuid(),
            OriginalExecutionId = originalExecutionId,
            Requester = "subject-requester-001",
            RequestedAt = CreatedAt,
            ExplicitConfirmationReceived = true,
            ConfirmedAt = CreatedAt,
            Justification = "Reverter alteracao indevida.",
            TablesAffected = ["FORNECEDORES"],
            BusinessKeys = ["CGC_CPF=00000000000191"],
            RecordsAffected = 1,
            ConcurrencyFindings = [],
            ExpectedStateSummary = "1 registro(s) em [FORNECEDORES]",
            ObservedStateSummary = "1 registro(s) em [FORNECEDORES]",
            Status = RollbackExecutionStatus.Completed,
            PostRollbackValidationPassed = true,
            PostRollbackValidationRuleId = PostWriteValidationRuleCatalog.FornecedoresRule.RuleId,
        });

        var loaded = Assert.Single(await store.ListByOriginalExecutionAsync(originalExecutionId));
        Assert.Equal(RollbackExecutionStatus.Completed, loaded.Status);
        Assert.True(loaded.ExplicitConfirmationReceived);
        Assert.True(loaded.PostRollbackValidationPassed);
        Assert.Contains("FORNECEDORES", loaded.TablesAffected);
    }

    private static BlueprintOSDbContext NewDb() => new(new DbContextOptionsBuilder<BlueprintOSDbContext>()
        .UseInMemoryDatabase($"write-audit-{Guid.NewGuid():N}").Options);

    private static RecoveryPackageManifest Manifest(Guid executionId) => new()
    {
        ExecutionId = executionId,
        ExecutionName = "garantir-fornecedor",
        AgentId = "linx-database-specialist-agent",
        ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment,
        Server = "192.168.9.98",
        Database = "SOMA_DESENV",
        ExecutedAt = CreatedAt,
        Requester = "subject-requester-001",
        OriginalRequestSummary = "Garantir fornecedor por CNPJ.",
        OperationTypes = [ActionOperation.Update],
        TablesAffected = ["FORNECEDORES"],
        BusinessKeys = ["CGC_CPF=00000000000191"],
        RecordsExpectedToChange = 1,
        BackupRequired = true,
        RollbackSupported = true,
        RetentionDays = 30,
        ExpiresAt = CreatedAt.AddDays(30),
        ValidationRuleId = PostWriteValidationRuleCatalog.FornecedoresRule.RuleId,
        ProposalHash = new string('a', 64),
    };

    private static WriteExecutionAuditRecord Record(Guid executionId) => new()
    {
        ExecutionId = executionId,
        ExecutionName = "garantir-fornecedor",
        AgentId = "linx-database-specialist-agent",
        ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment,
        WriteVerificationPolicyVersion = "1.0-phase-a",
        Server = "192.168.9.98",
        Database = "SOMA_DESENV",
        StartedAt = CreatedAt,
        CompletedAt = CreatedAt.AddSeconds(2),
        Requester = "subject-requester-001",
        Intent = "Garantir fornecedor no ERP.",
        Operations = [ActionOperation.Update],
        TablesAffected = ["FORNECEDORES", "CADASTRO_CLI_FOR"],
        BusinessKeys = ["CGC_CPF=00000000000191"],
        RecordsAffected = 1,
        ProceduresInvoked = ["LX_SEQUENCIAL"],
        BeforeAfterSummary = "before=1 registro(s); after=1 registro(s); recursos=[FORNECEDORES]",
        ChangedFields = ["INATIVO"],
        ValidationRuleId = PostWriteValidationRuleCatalog.FornecedoresRule.RuleId,
        RecordsValidated = 1,
        RecordsWithErrors = 0,
        PostWriteValidationPassed = true,
        BackupRequired = true,
        BackupCreated = true,
        RetentionDays = 30,
        BackupExpiresAt = CreatedAt.AddDays(30),
        RecoveryPackageStatus = RecoveryPackageStatus.Active,
        RollbackAvailable = true,
        Errors = [],
        KnowledgeGaps = [],
        Outcome = WriteExecutionOutcome.Completed,
        ProposalHash = new string('a', 64),
    };
}
