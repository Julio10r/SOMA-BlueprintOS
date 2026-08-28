using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class RecoveryPackageWriterTests : IDisposable
{
    // UTC 14:35 == America/Sao_Paulo (-03:00) 11:35, same calendar date — folder names below reflect the
    // Sao Paulo-local rendering (BrazilTimeZoneProvider.ToSaoPaulo), never UTC.
    private static readonly DateTimeOffset ExecutedAt = new(2026, 8, 28, 14, 35, 0, TimeSpan.Zero);

    // Every test writes into an isolated temp directory — never into the repository's runtime/backups.
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blueprintos-recovery-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Package_Is_Written_Under_Agent_Database_Date_Time_Action_Execution_Path()
    {
        var writer = new RecoveryPackageWriter(_root);
        var manifest = Manifest();

        var receipt = await writer.CreateAsync(manifest, [BeforeSet()], [ExpectedAfterSet()]);

        var relative = Path.GetRelativePath(_root, receipt.PackagePath).Replace('\\', '/');
        // The path component is the REAL, validated Database ("SOMA_DESENV", lowercased by Sanitize like every
        // other path segment) — never the logical ConnectionProfile name ("linx-development"), which stays
        // recorded as metadata inside the manifest.
        Assert.Equal(
            $"linx-database-specialist-agent/soma_desenv/2026-08-28/1135-garantir-fornecedor__{manifest.ExecutionId:N}",
            relative);
        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, RecoveryPackageWriter.ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, RecoveryPackageWriter.BeforeDataFileName)));
        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, RecoveryPackageWriter.ExpectedAfterFileName)));
    }

    [Fact]
    public async Task Two_Different_Databases_Land_In_Physically_Separate_Subtrees()
    {
        var writer = new RecoveryPackageWriter(_root);
        var somaManifest = Manifest() with { ExecutionId = Guid.NewGuid(), Database = "SOMA", Server = "192.168.9.200", ConnectionProfile = WriteVerificationProfileSeeds.LinxProduction };
        var somaDesenvManifest = Manifest() with { ExecutionId = Guid.NewGuid() };

        var somaReceipt = await writer.CreateAsync(somaManifest, [BeforeSet()], [ExpectedAfterSet()]);
        var somaDesenvReceipt = await writer.CreateAsync(somaDesenvManifest, [BeforeSet()], [ExpectedAfterSet()]);

        Assert.Contains($"{Path.DirectorySeparatorChar}soma{Path.DirectorySeparatorChar}", somaReceipt.PackagePath);
        Assert.Contains($"{Path.DirectorySeparatorChar}soma_desenv{Path.DirectorySeparatorChar}", somaDesenvReceipt.PackagePath);
        Assert.NotEqual(Path.GetDirectoryName(Path.GetDirectoryName(somaReceipt.PackagePath)), Path.GetDirectoryName(Path.GetDirectoryName(somaDesenvReceipt.PackagePath)));

        // ConnectionProfile is preserved as metadata even though it no longer shapes the physical path.
        var reloadedSoma = await writer.ReadManifestAsync(somaReceipt.PackagePath);
        Assert.Equal(WriteVerificationProfileSeeds.LinxProduction, reloadedSoma!.ConnectionProfile);
    }

    [Fact]
    public async Task Folder_Date_Uses_Sao_Paulo_Local_Date_Not_Utc_Even_Across_The_Day_Boundary()
    {
        // 2026-08-29T01:00:00Z is still 2026-08-28 22:00 in America/Sao_Paulo (-03:00) — a UTC-based folder
        // would land in "2026-08-29", but the canonical operational timezone must place it in "2026-08-28".
        var crossingUtc = new DateTimeOffset(2026, 8, 29, 1, 0, 0, TimeSpan.Zero);
        var writer = new RecoveryPackageWriter(_root);
        var manifest = Manifest() with { ExecutionId = Guid.NewGuid(), ExecutedAt = crossingUtc, ExpiresAt = crossingUtc.AddDays(30) };

        var receipt = await writer.CreateAsync(manifest, [BeforeSet()], [ExpectedAfterSet()]);

        var relative = Path.GetRelativePath(_root, receipt.PackagePath).Replace('\\', '/');
        Assert.Equal(
            $"linx-database-specialist-agent/soma_desenv/2026-08-28/2200-garantir-fornecedor__{manifest.ExecutionId:N}",
            relative);
    }

    [Fact]
    public async Task Receipt_Carries_The_Manifest_Checksum_And_Expiration()
    {
        var writer = new RecoveryPackageWriter(_root);
        var manifest = Manifest();
        var receipt = await writer.CreateAsync(manifest, [BeforeSet()], [ExpectedAfterSet()]);

        Assert.Equal(manifest.ExecutionId, receipt.ExecutionId);
        Assert.Equal(manifest.ManifestChecksumSha256, receipt.ManifestChecksumSha256);
        Assert.Equal(manifest.ExpiresAt, receipt.ExpiresAt);
        Assert.Equal(BeforeStateStatus.Captured, receipt.BeforeState);
    }

    [Fact]
    public async Task BeforeState_Is_NotExistent_When_Snapshot_Is_Empty_For_A_Create()
    {
        var writer = new RecoveryPackageWriter(_root);
        var manifest = Manifest() with { OperationTypes = [ActionOperation.Create] };

        var receipt = await writer.CreateAsync(manifest, [], [ExpectedAfterSet()]);

        Assert.Equal(BeforeStateStatus.NotExistent, receipt.BeforeState);
    }

    [Fact]
    public async Task BeforeState_Is_NotExistent_When_Snapshot_Is_Empty_For_An_Insert()
    {
        var writer = new RecoveryPackageWriter(_root);
        var manifest = Manifest() with { OperationTypes = [ActionOperation.Insert] };

        var receipt = await writer.CreateAsync(manifest, [], [ExpectedAfterSet()]);

        Assert.Equal(BeforeStateStatus.NotExistent, receipt.BeforeState);
    }

    [Fact]
    public async Task BeforeState_Is_CaptureFailed_When_Snapshot_Is_Empty_For_An_Update()
    {
        var writer = new RecoveryPackageWriter(_root);
        var manifest = Manifest() with { OperationTypes = [ActionOperation.Update] };

        var receipt = await writer.CreateAsync(manifest, [], [ExpectedAfterSet()]);

        Assert.Equal(BeforeStateStatus.CaptureFailed, receipt.BeforeState);
    }

    [Fact]
    public async Task Manifest_Round_Trips_And_Its_Checksum_Still_Verifies()
    {
        var writer = new RecoveryPackageWriter(_root);
        var manifest = Manifest();
        var receipt = await writer.CreateAsync(manifest, [BeforeSet()], [ExpectedAfterSet()]);

        var reloaded = await writer.ReadManifestAsync(receipt.PackagePath);
        Assert.NotNull(reloaded);
        Assert.Equal(manifest.ManifestChecksumSha256, reloaded!.ManifestChecksumSha256);
        Assert.Equal(manifest.ProposalHash, reloaded.ProposalHash);
        Assert.Equal(manifest.OperationTypes, reloaded.OperationTypes);
        Assert.Null(reloaded.DdlSnapshot);
    }

    [Fact]
    public async Task Checksum_Changes_When_Any_Manifest_Field_Changes()
    {
        var manifest = Manifest();
        Assert.NotEqual(manifest.ManifestChecksumSha256, (manifest with { RecordsExpectedToChange = 2 }).ManifestChecksumSha256);
        Assert.NotEqual(manifest.ManifestChecksumSha256, (manifest with { RollbackSupported = false }).ManifestChecksumSha256);
        Assert.Equal(manifest.ManifestChecksumSha256, (manifest with { }).ManifestChecksumSha256);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Before_And_After_Data_Round_Trip()
    {
        var writer = new RecoveryPackageWriter(_root);
        var receipt = await writer.CreateAsync(Manifest(), [BeforeSet()], [ExpectedAfterSet()]);
        await writer.WriteAfterDataAsync(receipt, [ExpectedAfterSet()]);

        var before = await writer.ReadBeforeDataAsync(receipt.PackagePath);
        var after = await writer.ReadAfterDataAsync(receipt.PackagePath);

        Assert.Equal("FORNECEDORES", Assert.Single(before).Resource);
        Assert.Equal("0", Assert.Single(before).Records[0]["INATIVO"]);
        Assert.Equal("1", Assert.Single(after).Records[0]["INATIVO"]);
    }

    [Fact]
    public async Task After_Data_Is_Empty_Before_It_Is_Written()
    {
        var writer = new RecoveryPackageWriter(_root);
        var receipt = await writer.CreateAsync(Manifest(), [BeforeSet()], [ExpectedAfterSet()]);
        Assert.Empty(await writer.ReadAfterDataAsync(receipt.PackagePath));
    }

    [Fact]
    public async Task Validation_Report_Is_Written_Into_The_Package()
    {
        var writer = new RecoveryPackageWriter(_root);
        var receipt = await writer.CreateAsync(Manifest(), [BeforeSet()], [ExpectedAfterSet()]);
        await writer.WriteValidationReportAsync(receipt, new PostWriteValidationReport(
            "post-write-validation.fornecedores.v1", Passed: true, RecordsValidated: 1, RecordsWithErrors: 0,
            Mismatches: [], ValidatedAt: ExecutedAt));

        var file = Path.Combine(receipt.PackagePath, RecoveryPackageWriter.ValidationReportFileName);
        Assert.True(File.Exists(file));
        Assert.Contains("post-write-validation.fornecedores.v1", await File.ReadAllTextAsync(file), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Package_Can_Be_Detected_And_Deleted()
    {
        var writer = new RecoveryPackageWriter(_root);
        var receipt = await writer.CreateAsync(Manifest(), [BeforeSet()], [ExpectedAfterSet()]);
        Assert.True(writer.PackageExists(receipt.PackagePath));

        await writer.DeletePackageAsync(receipt.PackagePath);
        Assert.False(writer.PackageExists(receipt.PackagePath));
        Assert.Null(await writer.ReadManifestAsync(receipt.PackagePath));
    }

    [Fact]
    public void Root_Directory_Is_Required()
    {
        Assert.Throws<ArgumentException>(() => new RecoveryPackageWriter("  "));
    }

    private static RecoveryPackageManifest Manifest() => new()
    {
        ExecutionId = Guid.NewGuid(),
        ExecutionName = "garantir-fornecedor",
        AgentId = "linx-database-specialist-agent",
        ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment,
        Server = "192.168.9.98",
        Database = "SOMA_DESENV",
        ExecutedAt = ExecutedAt,
        Requester = "subject-requester-001",
        OriginalRequestSummary = "Garantir fornecedor por CNPJ.",
        OperationTypes = [ActionOperation.Update],
        TablesAffected = ["FORNECEDORES"],
        BusinessKeys = ["CGC_CPF=00000000000191"],
        RecordsExpectedToChange = 1,
        BackupRequired = true,
        RollbackSupported = true,
        RetentionDays = 30,
        ExpiresAt = ExecutedAt.AddDays(30),
        ValidationRuleId = "post-write-validation.fornecedores.v1",
        ProposalHash = new string('a', 64),
    };

    private static RecoveryDataSet BeforeSet() => new("FORNECEDORES",
        [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123", ["INATIVO"] = "0" }]);

    private static RecoveryDataSet ExpectedAfterSet() => new("FORNECEDORES",
        [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123", ["INATIVO"] = "1" }]);
}
