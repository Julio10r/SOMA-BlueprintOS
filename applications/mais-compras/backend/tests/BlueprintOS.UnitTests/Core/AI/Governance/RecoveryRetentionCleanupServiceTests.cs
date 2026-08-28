using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class RecoveryRetentionCleanupServiceTests : IDisposable
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blueprintos-retention-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Package_Past_Its_Thirty_Day_Retention_Is_Deleted_And_Marked_Expired()
    {
        var fixture = await CreateFixtureAsync();

        var report = await fixture.Service.RunOnceAsync(CreatedAt.AddDays(31));

        Assert.Equal(1, report.Expired);
        Assert.Empty(report.Errors);
        Assert.False(Directory.Exists(fixture.PackagePath));
        var entry = Assert.Single(await fixture.Index.FindAsync(new RecoveryIndexQuery { ExecutionId = fixture.ExecutionId }));
        Assert.Equal(RecoveryPackageStatus.Expired, entry.Status);
    }

    [Fact]
    public async Task Permanent_Audit_Survives_The_Cleanup_And_Stays_Queryable()
    {
        var fixture = await CreateFixtureAsync();
        await fixture.Service.RunOnceAsync(CreatedAt.AddDays(31));

        var audit = await fixture.WriteAudit.GetAsync(fixture.ExecutionId);
        Assert.NotNull(audit);
        Assert.Equal(WriteExecutionOutcome.Completed, audit!.Outcome);
        Assert.Equal("garantir-fornecedor", audit.ExecutionName);
        Assert.Equal(1, audit.RecordsAffected);
        Assert.True(audit.PostWriteValidationPassed);
        Assert.Single(await fixture.WriteAudit.ListAsync());
    }

    [Fact]
    public async Task Index_Row_Survives_As_Evidence_The_Execution_Existed()
    {
        var fixture = await CreateFixtureAsync();
        await fixture.Service.RunOnceAsync(CreatedAt.AddDays(31));

        Assert.Single(await fixture.Index.FindAsync(new RecoveryIndexQuery()));
    }

    [Fact]
    public async Task Package_Still_Inside_Its_Retention_Window_Is_Untouched()
    {
        var fixture = await CreateFixtureAsync();

        var report = await fixture.Service.RunOnceAsync(CreatedAt.AddDays(29));

        Assert.Equal(0, report.Expired);
        Assert.True(Directory.Exists(fixture.PackagePath));
        var entry = Assert.Single(await fixture.Index.FindAsync(new RecoveryIndexQuery { ExecutionId = fixture.ExecutionId }));
        Assert.Equal(RecoveryPackageStatus.Active, entry.Status);
    }

    [Fact]
    public async Task Cleanup_Is_Idempotent_And_Skips_Already_Expired_Entries()
    {
        var fixture = await CreateFixtureAsync();
        await fixture.Service.RunOnceAsync(CreatedAt.AddDays(31));

        var second = await fixture.Service.RunOnceAsync(CreatedAt.AddDays(60));
        Assert.Equal(0, second.Inspected);
        Assert.Equal(0, second.Expired);
    }

    [Fact]
    public async Task Now_Is_An_Explicit_Argument_So_Retention_Is_Deterministic()
    {
        var fixture = await CreateFixtureAsync();

        // The real wall clock is far past this package's expiry in no test run; only the argument decides.
        Assert.Equal(0, (await fixture.Service.RunOnceAsync(CreatedAt)).Expired);
        Assert.Equal(1, (await fixture.Service.RunOnceAsync(CreatedAt.AddDays(30))).Expired);
    }

    [Fact]
    public async Task Batch_Format_Package_Is_Expired_The_Same_Way_As_Single_Item_Format()
    {
        // The cleanup service is format-agnostic: RecoveryPackageWriter.DeletePackageAsync/PackageExists are
        // plain directory operations that do not know or care whether the directory holds the old single-item
        // layout or the new batch/chunked one. Proven here directly against a real batch package, indexed the
        // way item 5 documents (one RecoveryIndexEntry per batch, keyed by BatchExecutionId, BusinessKeys
        // carrying every item's key so a business-key search still finds it).
        var batchWriter = new BatchRecoveryPackageWriter(_root);
        var recoveryWriter = new RecoveryPackageWriter(_root);
        var index = new InMemoryRecoveryIndexStore();
        var batchExecutionId = Guid.NewGuid();
        var expiresAt = CreatedAt.AddDays(30);

        var manifest = new BatchRecoveryPackageManifest
        {
            BatchExecutionId = batchExecutionId,
            ExecutionName = "ajuste-grade-lote",
            AgentId = "linx-database-specialist-agent",
            Capability = "ped-grade-adjustment-write",
            ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment,
            Server = "192.168.9.98",
            Database = "SOMA_DESENV",
            ExecutedAt = CreatedAt,
            Requester = "subject-requester-001",
            Origin = "governed-execute batch-run",
            OriginalRequestSummary = "Ajuste de grade em lote (teste de retencao).",
            OperationTypes = [ActionOperation.Update],
            TablesAffected = ["COMPRAS_PRODUTO"],
            TotalItems = 0,
            ChunkCount = 0,
            MaxItemsPerChunk = 0,
            MaxChunkSizeBytes = 0,
            BackupRequired = true,
            RollbackSupported = true,
            RetentionDays = 30,
            ExpiresAt = expiresAt,
            ValidationRuleId = "ped-grade-adjustment.v1",
            ProposalHash = new string('b', 64),
            Status = BatchStatus.Active,
            ChunkBeforeDataChecksumsSha256 = new Dictionary<int, string>(),
        };

        var items = new[] { "produto-0001", "produto-0002" }.Select(key => new BatchRecoveryItem(
            key, "COMPRAS_PRODUTO",
            new RecoveryDataSet("COMPRAS_PRODUTO", [new Dictionary<string, string?> { ["ID_PRODUTO"] = key, ["GRADE"] = "A" }]),
            new RecoveryDataSet("COMPRAS_PRODUTO", [new Dictionary<string, string?> { ["ID_PRODUTO"] = key, ["GRADE"] = "B" }]))).ToArray();

        var receipt = await batchWriter.CreateBatchAsync(manifest, items);

        await index.AppendAsync(new RecoveryIndexEntry(
            batchExecutionId, manifest.ExecutionName, manifest.AgentId, manifest.ConnectionProfile, manifest.Server,
            manifest.Database, manifest.ExecutedAt, manifest.Requester, manifest.OperationTypes, manifest.TablesAffected,
            items.Select(i => i.BusinessKey).ToArray(), items.Length, true, true, 30, expiresAt,
            receipt.PackagePath, receipt.ManifestChecksumSha256, RecoveryPackageStatus.Active,
            manifest.ProposalHash, manifest.ValidationRuleId));

        var service = new RecoveryRetentionCleanupService(index, recoveryWriter);
        var report = await service.RunOnceAsync(expiresAt.AddSeconds(1));

        Assert.Equal(1, report.Expired);
        Assert.False(Directory.Exists(receipt.PackagePath));
        var entry = Assert.Single(await index.FindAsync(new RecoveryIndexQuery { ExecutionId = batchExecutionId }));
        Assert.Equal(RecoveryPackageStatus.Expired, entry.Status);

        // Business-key search still locates the batch after expiry — only the physical files are gone.
        var byBusinessKey = await index.FindAsync(new RecoveryIndexQuery { BusinessKey = "produto-0002" });
        Assert.Single(byBusinessKey);
    }

    private async Task<Fixture> CreateFixtureAsync()
    {
        var writer = new RecoveryPackageWriter(_root);
        var index = new InMemoryRecoveryIndexStore();
        var writeAudit = new InMemoryWriteExecutionAuditStore();
        var executionId = Guid.NewGuid();
        var expiresAt = CreatedAt.AddDays(30);

        var manifest = new RecoveryPackageManifest
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
            ExpiresAt = expiresAt,
            ValidationRuleId = PostWriteValidationRuleCatalog.FornecedoresRule.RuleId,
            ProposalHash = new string('a', 64),
        };

        var receipt = await writer.CreateAsync(manifest,
            [new RecoveryDataSet("FORNECEDORES", [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123", ["INATIVO"] = "0" }])],
            [new RecoveryDataSet("FORNECEDORES", [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123", ["INATIVO"] = "1" }])]);

        await index.AppendAsync(new RecoveryIndexEntry(
            executionId, manifest.ExecutionName, manifest.AgentId, manifest.ConnectionProfile, manifest.Server,
            manifest.Database, manifest.ExecutedAt, manifest.Requester, manifest.OperationTypes, manifest.TablesAffected,
            manifest.BusinessKeys, 1, true, true, 30, expiresAt, receipt.PackagePath, receipt.ManifestChecksumSha256,
            RecoveryPackageStatus.Active, manifest.ProposalHash, manifest.ValidationRuleId));

        await writeAudit.AppendAsync(new WriteExecutionAuditRecord
        {
            ExecutionId = executionId,
            ExecutionName = manifest.ExecutionName,
            AgentId = manifest.AgentId,
            ConnectionProfile = manifest.ConnectionProfile,
            WriteVerificationPolicyVersion = "1.0-phase-a",
            Server = manifest.Server,
            Database = manifest.Database,
            StartedAt = CreatedAt,
            CompletedAt = CreatedAt,
            Requester = manifest.Requester,
            Intent = "Garantir fornecedor no ERP.",
            Operations = manifest.OperationTypes,
            TablesAffected = manifest.TablesAffected,
            BusinessKeys = manifest.BusinessKeys,
            RecordsAffected = 1,
            BeforeAfterSummary = "before=1 registro(s); after=1 registro(s)",
            ValidationRuleId = manifest.ValidationRuleId,
            RecordsValidated = 1,
            RecordsWithErrors = 0,
            PostWriteValidationPassed = true,
            BackupRequired = true,
            BackupCreated = true,
            RetentionDays = 30,
            BackupExpiresAt = expiresAt,
            RecoveryPackageStatus = RecoveryPackageStatus.Active,
            RollbackAvailable = true,
            Outcome = WriteExecutionOutcome.Completed,
        });

        return new(new RecoveryRetentionCleanupService(index, writer), index, writeAudit, executionId, receipt.PackagePath);
    }

    private sealed record Fixture(
        RecoveryRetentionCleanupService Service,
        InMemoryRecoveryIndexStore Index,
        InMemoryWriteExecutionAuditStore WriteAudit,
        Guid ExecutionId,
        string PackagePath);
}
