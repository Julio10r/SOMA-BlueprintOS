using System.Text.Json;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Infrastructure.Persistence.Governance;

/// <summary>
/// Proves the timezone fix (America/Sao_Paulo folder dating + explicit -03:00 persisted offsets) never
/// breaks lookup of records created BEFORE the fix, when folders were still named by the UTC date. The fix
/// only changes where NEW records are written; every store's lookup-by-id scans all date partitions rather
/// than recomputing an expected path from "now" (see FileRecoveryIndexStore/FileWriteExecutionAuditStore
/// remarks), so this is a property of the existing scan-based design, verified here explicitly for the
/// specific scenario the migration cares about.
/// </summary>
public sealed class RuntimeTimezoneMigrationCompatibilityTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blueprintos-tz-compat-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task RecoveryIndex_Finds_An_Entry_Written_Under_The_Old_Utc_Dated_Folder()
    {
        var executionId = Guid.NewGuid();
        var utcExecutedAt = new DateTimeOffset(2026, 8, 28, 21, 8, 18, TimeSpan.Zero);

        // Simulate a pre-fix record: folder named by the UTC date (2026-08-28), exactly as the old
        // `.UtcDateTime.ToString("yyyy-MM-dd")` code produced, regardless of what the new Sao-Paulo-based
        // naming would compute for the same instant.
        var oldStyleFolder = Path.Combine(_root, "recovery-index", utcExecutedAt.UtcDateTime.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(oldStyleFolder);
        var entry = new RecoveryIndexEntry(
            executionId, "ped-grade-adjustment-piloto-1741628-v2", "linx-erp-specialist-agent",
            WriteVerificationProfileSeeds.LinxProduction, "192.168.9.200", "SOMA", utcExecutedAt,
            "julio.cesar@somagrupo.com.br", [ActionOperation.Update], ["COMPRAS_PRODUTO"],
            ["PEDIDO=1741628|PRODUTO=15.29433|COR_PRODUTO=59768"], 1, true, true, 30,
            utcExecutedAt.AddDays(30), "/irrelevant/for/this/test", new string('a', 64),
            RecoveryPackageStatus.Active, new string('b', 64), "post-write-validation.ped-grade-adjustment.v1");
        await File.WriteAllTextAsync(Path.Combine(oldStyleFolder, $"{executionId:N}.json"), JsonSerializer.Serialize(entry));

        var store = new FileRecoveryIndexStore(_root);
        var found = await store.FindAsync(new RecoveryIndexQuery { ExecutionId = executionId });

        Assert.Single(found);
        Assert.Equal(executionId, found[0].ExecutionId);
        Assert.Equal("SOMA", found[0].Database);
        // ConnectionProfile remains obrigatory metadata, unaffected by the timezone/path change.
        Assert.Equal(WriteVerificationProfileSeeds.LinxProduction, found[0].ConnectionProfile);
    }

    [Fact]
    public async Task RecoveryIndex_New_Entry_Lands_Under_The_Sao_Paulo_Dated_Folder_And_Is_Still_Found_Alongside_Old_Ones()
    {
        // An old-style (UTC-dated) entry already on disk...
        var oldExecutionId = Guid.NewGuid();
        var oldUtc = new DateTimeOffset(2026, 8, 27, 10, 0, 0, TimeSpan.Zero);
        var oldFolder = Path.Combine(_root, "recovery-index", oldUtc.UtcDateTime.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(oldFolder);
        var oldEntry = new RecoveryIndexEntry(
            oldExecutionId, "old-style", "linx-erp-specialist-agent", WriteVerificationProfileSeeds.LinxDevelopment,
            "192.168.9.98", "SOMA_DESENV", oldUtc, "someone", [ActionOperation.Update], ["COMPRAS_PRODUTO"],
            ["k=v"], 1, true, true, 30, oldUtc.AddDays(30), "/irrelevant", new string('a', 64),
            RecoveryPackageStatus.Active, new string('c', 64), "post-write-validation.ped-grade-adjustment.v1");
        await File.WriteAllTextAsync(Path.Combine(oldFolder, $"{oldExecutionId:N}.json"), JsonSerializer.Serialize(oldEntry));

        // ...and a NEW one written through the store after the timezone fix, using a Sao-Paulo-offset timestamp
        // exactly like SaoPauloTimeProvider now supplies to the real live-execution path.
        var newExecutionId = Guid.NewGuid();
        var newSaoPauloNow = SaoPauloTimeProvider.Instance.GetUtcNow();
        var store = new FileRecoveryIndexStore(_root);
        var newEntry = new RecoveryIndexEntry(
            newExecutionId, "new-style", "linx-erp-specialist-agent", WriteVerificationProfileSeeds.LinxProduction,
            "192.168.9.200", "SOMA", newSaoPauloNow, "julio.cesar@somagrupo.com.br", [ActionOperation.Update],
            ["COMPRAS_PRODUTO"], ["k2=v2"], 1, true, true, 30, newSaoPauloNow.AddDays(30), "/irrelevant",
            new string('d', 64), RecoveryPackageStatus.Active, new string('e', 64), "post-write-validation.ped-grade-adjustment.v1");
        await store.AppendAsync(newEntry);

        var expectedNewFolder = Path.Combine(_root, "recovery-index", BrazilTimeZoneProvider.ToSaoPaulo(newSaoPauloNow).ToString("yyyy-MM-dd"));
        Assert.True(File.Exists(Path.Combine(expectedNewFolder, $"{newExecutionId:N}.json")));

        var all = await store.FindAsync(new RecoveryIndexQuery());
        Assert.Contains(all, e => e.ExecutionId == oldExecutionId);
        Assert.Contains(all, e => e.ExecutionId == newExecutionId);
    }
}
