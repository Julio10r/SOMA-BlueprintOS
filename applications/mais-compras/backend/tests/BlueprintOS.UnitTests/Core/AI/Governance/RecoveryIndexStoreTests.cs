using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class RecoveryIndexStoreTests
{
    private static readonly DateTimeOffset Day1 = new(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day2 = new(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day3 = new(2026, 8, 27, 10, 0, 0, TimeSpan.Zero);

    public static TheoryData<string> StoreKinds => ["in-memory", "ef"];

    [Theory]
    [MemberData(nameof(StoreKinds))]
    public async Task Find_Always_Returns_A_List_Even_For_A_Single_Exact_Match(string kind)
    {
        var store = await CreateSeededStoreAsync(kind);
        var result = await store.FindAsync(new RecoveryIndexQuery { ExecutionId = ExecutionA });

        Assert.IsAssignableFrom<IReadOnlyList<RecoveryIndexEntry>>(result);
        Assert.Single(result);
        Assert.Equal(ExecutionA, result[0].ExecutionId);
    }

    [Theory]
    [MemberData(nameof(StoreKinds))]
    public async Task Find_Returns_Empty_List_When_Nothing_Matches(string kind)
    {
        var store = await CreateSeededStoreAsync(kind);
        Assert.Empty(await store.FindAsync(new RecoveryIndexQuery { ExecutionId = Guid.NewGuid() }));
    }

    [Theory]
    [MemberData(nameof(StoreKinds))]
    public async Task Ambiguous_Query_Returns_Every_Candidate_And_Never_Picks_One(string kind)
    {
        var store = await CreateSeededStoreAsync(kind);
        var result = await store.FindAsync(new RecoveryIndexQuery { Table = "FORNECEDORES" });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, entry => entry.ExecutionId == ExecutionA);
        Assert.Contains(result, entry => entry.ExecutionId == ExecutionB);
    }

    [Theory]
    [MemberData(nameof(StoreKinds))]
    public async Task Criteria_Combine_With_And_Semantics(string kind)
    {
        var store = await CreateSeededStoreAsync(kind);
        var narrowed = await store.FindAsync(new RecoveryIndexQuery
        {
            Table = "FORNECEDORES",
            Requester = "subject-requester-002",
        });

        Assert.Equal(ExecutionB, Assert.Single(narrowed).ExecutionId);
    }

    [Theory]
    [MemberData(nameof(StoreKinds))]
    public async Task Period_Agent_Profile_And_Status_Criteria_All_Filter(string kind)
    {
        var store = await CreateSeededStoreAsync(kind);

        Assert.Equal(2, (await store.FindAsync(new RecoveryIndexQuery { ExecutedFrom = Day2 })).Count);
        Assert.Single(await store.FindAsync(new RecoveryIndexQuery { ExecutedTo = Day1 }));
        Assert.Equal(3, (await store.FindAsync(new RecoveryIndexQuery { AgentId = "linx-database-specialist-agent" })).Count);
        Assert.Equal(3, (await store.FindAsync(new RecoveryIndexQuery { ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment })).Count);
        Assert.Single(await store.FindAsync(new RecoveryIndexQuery { Status = RecoveryPackageStatus.Expired }));
    }

    [Theory]
    [MemberData(nameof(StoreKinds))]
    public async Task Business_Key_Criterion_Matches_A_Single_Execution(string kind)
    {
        var store = await CreateSeededStoreAsync(kind);
        var result = await store.FindAsync(new RecoveryIndexQuery { BusinessKey = "00000000000191" });
        Assert.Equal(ExecutionA, Assert.Single(result).ExecutionId);
    }

    [Theory]
    [MemberData(nameof(StoreKinds))]
    public async Task Empty_Query_Lists_Everything_Newest_First(string kind)
    {
        var store = await CreateSeededStoreAsync(kind);
        var all = await store.FindAsync(new RecoveryIndexQuery());

        Assert.Equal(3, all.Count);
        Assert.True(all[0].ExecutedAt >= all[1].ExecutedAt && all[1].ExecutedAt >= all[2].ExecutedAt);
    }

    [Theory]
    [MemberData(nameof(StoreKinds))]
    public async Task Status_Update_Keeps_The_Row_And_Changes_Only_Its_Status(string kind)
    {
        var store = await CreateSeededStoreAsync(kind);
        await store.UpdateStatusAsync(ExecutionA, RecoveryPackageStatus.RolledBack);

        var entry = Assert.Single(await store.FindAsync(new RecoveryIndexQuery { ExecutionId = ExecutionA }));
        Assert.Equal(RecoveryPackageStatus.RolledBack, entry.Status);
        Assert.Equal(3, (await store.FindAsync(new RecoveryIndexQuery())).Count);
    }

    [Theory]
    [MemberData(nameof(StoreKinds))]
    public async Task Duplicate_Execution_Id_Is_Rejected(string kind)
    {
        var store = await CreateSeededStoreAsync(kind);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.AppendAsync(Entry(ExecutionA, Day1, "subject-requester-001", ["FORNECEDORES"], ["CGC_CPF=00000000000191"])));
    }

    [Theory]
    [MemberData(nameof(StoreKinds))]
    public async Task Unknown_Execution_Status_Update_Throws(string kind)
    {
        var store = await CreateSeededStoreAsync(kind);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => store.UpdateStatusAsync(Guid.NewGuid(), RecoveryPackageStatus.Expired));
    }

    private static readonly Guid ExecutionA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ExecutionB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ExecutionC = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static async Task<IRecoveryIndexStore> CreateSeededStoreAsync(string kind)
    {
        IRecoveryIndexStore store = kind == "ef"
            ? new EfRecoveryIndexStore(new BlueprintOSDbContext(new DbContextOptionsBuilder<BlueprintOSDbContext>()
                .UseInMemoryDatabase($"recovery-index-{Guid.NewGuid():N}").Options))
            : new InMemoryRecoveryIndexStore();

        await store.AppendAsync(Entry(ExecutionA, Day1, "subject-requester-001", ["FORNECEDORES"], ["CGC_CPF=00000000000191"]));
        await store.AppendAsync(Entry(ExecutionB, Day2, "subject-requester-002", ["FORNECEDORES", "CADASTRO_CLI_FOR"], ["CGC_CPF=00000000000272"]));
        await store.AppendAsync(Entry(ExecutionC, Day3, "subject-requester-003", ["CADASTRO_CLI_FOR"], ["CGC_CPF=00000000000353"]) with
        {
            Status = RecoveryPackageStatus.Expired,
        });
        return store;
    }

    private static RecoveryIndexEntry Entry(
        Guid executionId,
        DateTimeOffset executedAt,
        string requester,
        IReadOnlyList<string> tables,
        IReadOnlyList<string> businessKeys) => new(
        executionId, "garantir-fornecedor", "linx-database-specialist-agent",
        WriteVerificationProfileSeeds.LinxDevelopment, "192.168.9.98", "SOMA_DESENV", executedAt, requester,
        [ActionOperation.Update], tables, businessKeys, RecordsAffected: 1,
        BackupRequired: true, RollbackSupported: true, RetentionDays: 30, ExpiresAt: executedAt.AddDays(30),
        PackagePath: $"/tmp/recovery/{executionId:N}", ManifestChecksumSha256: new string('b', 64),
        Status: RecoveryPackageStatus.Active, ProposalHash: new string('a', 64),
        ValidationRuleId: "post-write-validation.fornecedores.v1");
}
