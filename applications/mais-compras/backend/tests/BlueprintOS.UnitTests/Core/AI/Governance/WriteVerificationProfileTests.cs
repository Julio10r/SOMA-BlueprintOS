using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class WriteVerificationProfileTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LinxDevelopment_Resolves_To_PhaseA_Full_Protection_Today()
    {
        var store = new InMemoryWriteVerificationProfileStore();
        var profile = await store.ResolveAsync(WriteVerificationProfileSeeds.LinxDevelopment, Now);

        Assert.NotNull(profile);
        Assert.Equal("1.0-phase-a", profile!.PolicyVersion);
        Assert.True(profile.BackupRequired);
        Assert.True(profile.RollbackSupported);
        Assert.Equal(30, profile.BackupRetentionDays);
        Assert.True(profile.PostWriteValidationRequired);
    }

    [Fact]
    public async Task LinxDevelopment_PhaseB_Exists_As_Separate_Version_Not_An_Edit_Of_PhaseA()
    {
        var store = new InMemoryWriteVerificationProfileStore();
        var versions = await store.ListVersionsAsync(WriteVerificationProfileSeeds.LinxDevelopment);

        Assert.Equal(2, versions.Count);
        Assert.Contains(versions, item => item.PolicyVersion == "1.0-phase-a" && item.BackupRequired);
        var phaseB = Assert.Single(versions, item => item.PolicyVersion == "2.0-phase-b");
        Assert.False(phaseB.BackupRequired);
        Assert.False(phaseB.RollbackSupported);
        Assert.True(phaseB.PostWriteValidationRequired);
    }

    [Fact]
    public async Task LinxProduction_Requires_Backup_Rollback_And_Thirty_Day_Retention()
    {
        var store = new InMemoryWriteVerificationProfileStore();
        var profile = await store.ResolveAsync(WriteVerificationProfileSeeds.LinxProduction, Now);

        Assert.True(profile!.BackupRequired);
        Assert.True(profile.RollbackSupported);
        Assert.Equal(30, profile.BackupRetentionDays);
        Assert.True(profile.PostWriteValidationRequired);
    }

    [Fact]
    public async Task Wise_Is_Config_Only_But_Still_Requires_Post_Write_Validation()
    {
        var store = new InMemoryWriteVerificationProfileStore();
        var profile = await store.ResolveAsync(WriteVerificationProfileSeeds.Wise, Now);

        Assert.False(profile!.BackupRequired);
        Assert.False(profile.RollbackSupported);
        Assert.True(profile.PostWriteValidationRequired);
    }

    [Fact]
    public async Task Unknown_Profile_Resolves_To_Null_Never_To_A_Permissive_Default()
    {
        var store = new InMemoryWriteVerificationProfileStore();
        Assert.Null(await store.ResolveAsync("some-database-nobody-governed", Now));
    }

    [Fact]
    public async Task Appending_A_Duplicate_Version_Is_Rejected_Because_Versions_Are_Immutable()
    {
        var store = new InMemoryWriteVerificationProfileStore();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.AppendVersionAsync(WriteVerificationProfileSeeds.LinxDevelopmentPhaseA));
    }

    [Fact]
    public async Task Appended_Version_Becomes_Effective_Only_From_Its_EffectiveFrom()
    {
        var store = new InMemoryWriteVerificationProfileStore();
        var future = new WriteVerificationProfile(
            WriteVerificationProfileSeeds.LinxProduction, BackupRequired: true, RollbackSupported: true,
            BackupRetentionDays: 180, PostWriteValidationRequired: true, PolicyVersion: "2.0",
            ApprovedBy: "product-owner", EffectiveFrom: Now.AddDays(10));
        await store.AppendVersionAsync(future);

        Assert.Equal("1.0", (await store.ResolveAsync(WriteVerificationProfileSeeds.LinxProduction, Now))!.PolicyVersion);
        Assert.Equal("2.0", (await store.ResolveAsync(WriteVerificationProfileSeeds.LinxProduction, Now.AddDays(11)))!.PolicyVersion);
    }

    [Fact]
    public async Task File_Store_Round_Trips_Versions_With_The_Same_Semantics()
    {
        var root = Path.Combine(Path.GetTempPath(), "blueprintos-governance-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileWriteVerificationProfileStore(root);

            // The file store self-seeds from WriteVerificationProfileSeeds.All on first use, so both phase A
            // and B for linx-development already exist; appending them again must be rejected exactly like
            // the in-memory/EF stores did (append-only, immutable versions).
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.AppendVersionAsync(WriteVerificationProfileSeeds.LinxDevelopmentPhaseA));

            var resolved = await store.ResolveAsync(WriteVerificationProfileSeeds.LinxDevelopment, Now);
            Assert.Equal("1.0-phase-a", resolved!.PolicyVersion);
            Assert.Equal(2, (await store.ListVersionsAsync(WriteVerificationProfileSeeds.LinxDevelopment)).Count);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Profile_Detects_Reduction_Of_Guarantees()
    {
        var strict = WriteVerificationProfileSeeds.LinxProductionV1;
        var relaxed = strict with { BackupRequired = false, PolicyVersion = "2.0" };

        Assert.True(strict.ReducesGuaranteesComparedTo(relaxed));
        Assert.False(strict.ReducesGuaranteesComparedTo(strict with { BackupRetentionDays = 365, PolicyVersion = "3.0" }));
    }
}
