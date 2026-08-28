using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class BatchRecoveryPackageWriterTests : IDisposable
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 8, 28, 14, 35, 0, TimeSpan.Zero);
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blueprintos-batch-recovery-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Batch_Is_Written_Under_Same_Path_Convention_As_Single_Item_Format()
    {
        var writer = new BatchRecoveryPackageWriter(_root);
        var manifest = Manifest();
        var items = Items(3);

        var receipt = await writer.CreateBatchAsync(manifest, items);

        var relative = Path.GetRelativePath(_root, receipt.PackagePath).Replace('\\', '/');
        Assert.Equal(
            $"linx-database-specialist-agent/soma_desenv/2026-08-28/1135-ajuste-grade-lote__{manifest.BatchExecutionId:N}",
            relative);
        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, BatchRecoveryPackageWriter.ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, BatchRecoveryPackageWriter.ItemsIndexFileName)));
        Assert.Equal(3, receipt.TotalItems);
        Assert.Equal(1, receipt.ChunkCount);
    }

    [Fact]
    public async Task Items_Are_Split_Into_Multiple_Chunks_When_Max_Items_Per_Chunk_Is_Exceeded()
    {
        var writer = new BatchRecoveryPackageWriter(_root, maxItemsPerChunk: 2);
        var manifest = Manifest();
        var items = Items(5);

        var receipt = await writer.CreateBatchAsync(manifest, items);

        Assert.Equal(3, receipt.ChunkCount); // 2 + 2 + 1
        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, "before-data-0001.json")));
        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, "before-data-0002.json")));
        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, "before-data-0003.json")));
        Assert.False(File.Exists(Path.Combine(receipt.PackagePath, "before-data-0004.json")));

        var chunk1 = await writer.ReadChunkBeforeDataAsync(receipt.PackagePath, 1);
        var chunk2 = await writer.ReadChunkBeforeDataAsync(receipt.PackagePath, 2);
        var chunk3 = await writer.ReadChunkBeforeDataAsync(receipt.PackagePath, 3);
        Assert.Equal(2, chunk1.Count);
        Assert.Equal(2, chunk2.Count);
        Assert.Single(chunk3);
    }

    [Fact]
    public async Task Items_Index_Locates_Item_By_Business_Key_And_By_Position_At_Correct_Chunk()
    {
        var writer = new BatchRecoveryPackageWriter(_root, maxItemsPerChunk: 2);
        var manifest = Manifest();
        var items = Items(5);

        var receipt = await writer.CreateBatchAsync(manifest, items);
        var index = await writer.ReadItemsIndexAsync(receipt.PackagePath);

        Assert.NotNull(index);
        Assert.Equal(5, index!.TotalItems);
        Assert.Equal(3, index.ChunkCount);
        Assert.Equal(5, index.ByPosition.Count);

        var located = index.ByBusinessKey["produto-0004"];
        Assert.Equal(3, located.Position);
        Assert.Equal(2, located.ChunkNumber); // items 0,1 -> chunk1; 2,3 -> chunk2; 4 -> chunk3
        Assert.Equal(1, located.IndexWithinChunk);

        var lastItem = index.ByBusinessKey["produto-0005"];
        Assert.Equal(4, lastItem.Position);
        Assert.Equal(3, lastItem.ChunkNumber);
        Assert.Equal(0, lastItem.IndexWithinChunk);

        var byPosition = index.ByPosition[1];
        Assert.Equal("produto-0002", byPosition.BusinessKey);
        Assert.Equal(1, byPosition.ChunkNumber);
        Assert.Equal(1, byPosition.IndexWithinChunk);
    }

    [Fact]
    public async Task Manifest_Records_Per_Chunk_Checksum_And_Integrity_Verification_Detects_Tampering()
    {
        var writer = new BatchRecoveryPackageWriter(_root, maxItemsPerChunk: 2);
        var manifest = Manifest();
        var receipt = await writer.CreateBatchAsync(manifest, Items(4));

        var reloaded = await writer.ReadManifestAsync(receipt.PackagePath);
        Assert.NotNull(reloaded);
        Assert.Equal(2, reloaded!.ChunkBeforeDataChecksumsSha256.Count);

        Assert.True(await writer.VerifyChunkIntegrityAsync(receipt.PackagePath, reloaded, 1));
        Assert.True(await writer.VerifyChunkIntegrityAsync(receipt.PackagePath, reloaded, 2));

        await File.WriteAllTextAsync(Path.Combine(receipt.PackagePath, "before-data-0001.json"), "[]");
        Assert.False(await writer.VerifyChunkIntegrityAsync(receipt.PackagePath, reloaded, 1));
    }

    [Fact]
    public Task Manifest_Checksum_Changes_If_Chunk_Checksums_Differ()
    {
        var manifestA = Manifest() with { ChunkBeforeDataChecksumsSha256 = new Dictionary<int, string> { [1] = "aaa" } };
        var manifestB = manifestA with { ChunkBeforeDataChecksumsSha256 = new Dictionary<int, string> { [1] = "bbb" } };
        Assert.NotEqual(manifestA.ManifestChecksumSha256, manifestB.ManifestChecksumSha256);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Item_Status_Update_Persists_In_Items_Index_Without_Touching_Chunk_Files()
    {
        var writer = new BatchRecoveryPackageWriter(_root, maxItemsPerChunk: 2);
        var manifest = Manifest();
        var receipt = await writer.CreateBatchAsync(manifest, Items(3));
        var beforeChunk1Checksum = await File.ReadAllTextAsync(Path.Combine(receipt.PackagePath, "before-data-0001.json"));

        await writer.UpdateItemStatusAsync(receipt.PackagePath, "produto-0002", BatchItemStatus.RolledBack);
        var index = await writer.ReadItemsIndexAsync(receipt.PackagePath);

        Assert.Equal(BatchItemStatus.RolledBack, index!.ByBusinessKey["produto-0002"].Status);
        Assert.Equal(BatchItemStatus.Written, index.ByBusinessKey["produto-0001"].Status);
        var afterChunk1Content = await File.ReadAllTextAsync(Path.Combine(receipt.PackagePath, "before-data-0001.json"));
        Assert.Equal(beforeChunk1Checksum, afterChunk1Content);
    }

    [Fact]
    public async Task Validation_Summary_And_Chunk_Validation_Reports_Are_Written_And_Readable()
    {
        var writer = new BatchRecoveryPackageWriter(_root, maxItemsPerChunk: 2);
        var manifest = Manifest();
        var receipt = await writer.CreateBatchAsync(manifest, Items(3));

        await writer.WriteChunkValidationReportAsync(receipt.PackagePath, 1,
            [new ItemValidationResult("produto-0001", true, []), new ItemValidationResult("produto-0002", false, ["grade divergente"])]);
        await writer.WriteChunkValidationReportAsync(receipt.PackagePath, 2,
            [new ItemValidationResult("produto-0003", true, [])]);

        var summary = new BatchValidationSummary(manifest.BatchExecutionId, 3, 2, 1, ["produto-0002"], ["grade divergente"], ExecutedAt);
        await writer.WriteValidationSummaryAsync(receipt.PackagePath, summary);

        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, "validation-report-0001.json")));
        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, "validation-report-0002.json")));
        Assert.True(File.Exists(Path.Combine(receipt.PackagePath, BatchRecoveryPackageWriter.ValidationSummaryFileName)));
    }

    [Fact]
    public async Task Delete_Package_Removes_Entire_Batch_Directory()
    {
        var writer = new BatchRecoveryPackageWriter(_root, maxItemsPerChunk: 2);
        var receipt = await writer.CreateBatchAsync(Manifest(), Items(4));

        Assert.True(writer.PackageExists(receipt.PackagePath));
        await writer.DeletePackageAsync(receipt.PackagePath);
        Assert.False(writer.PackageExists(receipt.PackagePath));
    }

    private static BatchRecoveryPackageManifest Manifest() => new()
    {
        BatchExecutionId = Guid.NewGuid(),
        ExecutionName = "ajuste-grade-lote",
        AgentId = "linx-database-specialist-agent",
        Capability = "ped-grade-adjustment-write",
        ConnectionProfile = "linx-development",
        Server = "192.168.9.98",
        Database = "SOMA_DESENV",
        ExecutedAt = ExecutedAt,
        Requester = "julio.cesar@somagrupo.com.br",
        Origin = "governed-execute batch-run",
        OriginalRequestSummary = "Ajuste de grade em lote (teste unitario)",
        OperationTypes = [ActionOperation.Update],
        TablesAffected = ["COMPRAS_PRODUTO"],
        TotalItems = 0,
        ChunkCount = 0,
        MaxItemsPerChunk = 0,
        MaxChunkSizeBytes = 0,
        BackupRequired = true,
        RollbackSupported = true,
        RetentionDays = 30,
        ExpiresAt = ExecutedAt.AddDays(30),
        ValidationRuleId = "ped-grade-adjustment.v1",
        ProposalHash = "hash-teste",
        Status = BatchStatus.Active,
        ChunkBeforeDataChecksumsSha256 = new Dictionary<int, string>(),
    };

    private static List<BatchRecoveryItem> Items(int count) =>
        Enumerable.Range(1, count).Select(i =>
        {
            var key = $"produto-{i:0000}";
            return new BatchRecoveryItem(
                key,
                "COMPRAS_PRODUTO",
                new RecoveryDataSet("COMPRAS_PRODUTO", [new Dictionary<string, string?> { ["ID_PRODUTO"] = key, ["GRADE"] = "A" }]),
                new RecoveryDataSet("COMPRAS_PRODUTO", [new Dictionary<string, string?> { ["ID_PRODUTO"] = key, ["GRADE"] = "B" }]));
        }).ToList();
}
