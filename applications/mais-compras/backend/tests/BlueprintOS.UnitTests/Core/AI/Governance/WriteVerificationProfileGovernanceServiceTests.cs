using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class WriteVerificationProfileGovernanceServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reducing_Backup_In_Production_Is_Always_Red_And_Blocked()
    {
        var fixture = CreateFixture();
        var proposal = await fixture.Service.ProposeAsync(Request(
            GovernanceEnvironment.Production,
            WriteVerificationProfileSeeds.LinxProductionV1 with { BackupRequired = false, PolicyVersion = "2.0" }));

        Assert.True(proposal.ReducesGuarantees);
        Assert.Equal(RiskClassification.Red, proposal.Decision.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.Blocked, proposal.Decision.Status);
    }

    [Fact]
    public async Task Reducing_Rollback_In_Production_Is_Always_Red_And_Blocked()
    {
        var fixture = CreateFixture();
        var proposal = await fixture.Service.ProposeAsync(Request(
            GovernanceEnvironment.Production,
            WriteVerificationProfileSeeds.LinxProductionV1 with { RollbackSupported = false, PolicyVersion = "2.0" }));

        Assert.Equal(PolicyDecisionStatus.Blocked, proposal.Decision.Status);
    }

    [Fact]
    public async Task Blocked_Production_Reduction_Cannot_Be_Applied_Even_With_An_Approval_Grant()
    {
        var fixture = CreateFixture();
        var proposal = await fixture.Service.ProposeAsync(Request(
            GovernanceEnvironment.Production,
            WriteVerificationProfileSeeds.LinxProductionV1 with { BackupRequired = false, PolicyVersion = "2.0" }));
        var grant = Grant(proposal.Proposal);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ApplyAsync(proposal, grant, "REQ-1"));
        var versions = await fixture.Store.ListVersionsAsync(WriteVerificationProfileSeeds.LinxProduction);
        Assert.DoesNotContain(versions, item => item.PolicyVersion == "2.0");
    }

    [Fact]
    public async Task Reduction_Outside_Production_Requires_An_Explicit_Approval_Grant()
    {
        var fixture = CreateFixture();
        var proposal = await fixture.Service.ProposeAsync(Request(
            GovernanceEnvironment.Development,
            WriteVerificationProfileSeeds.LinxDevelopmentPhaseA with
            {
                BackupRequired = false,
                RollbackSupported = false,
                PolicyVersion = "2.0-phase-b-activated",
                EffectiveFrom = Now,
            }));

        Assert.Equal(PolicyDecisionStatus.RequiresApproval, proposal.Decision.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ApplyAsync(proposal, null, "REQ-2"));

        var applied = await fixture.Service.ApplyAsync(proposal, Grant(proposal.Proposal), "REQ-2");
        Assert.Equal("2.0-phase-b-activated", applied.PolicyVersion);
        Assert.Equal("2.0-phase-b-activated", (await fixture.Store.ResolveAsync(WriteVerificationProfileSeeds.LinxDevelopment, Now))!.PolicyVersion);
    }

    [Fact]
    public async Task Strengthening_Guarantees_Does_Not_Reduce_And_Is_Appended_As_A_New_Version()
    {
        var fixture = CreateFixture();
        var proposal = await fixture.Service.ProposeAsync(Request(
            GovernanceEnvironment.Production,
            WriteVerificationProfileSeeds.LinxProductionV1 with { BackupRetentionDays = 365, PolicyVersion = "2.0", EffectiveFrom = Now }));

        Assert.False(proposal.ReducesGuarantees);
        Assert.NotEqual(PolicyDecisionStatus.Blocked, proposal.Decision.Status);

        await fixture.Service.ApplyAsync(proposal, Grant(proposal.Proposal), "REQ-3");
        var versions = await fixture.Store.ListVersionsAsync(WriteVerificationProfileSeeds.LinxProduction);
        Assert.Equal(2, versions.Count);
        Assert.Contains(versions, item => item.PolicyVersion == "1.0");
    }

    [Fact]
    public async Task Change_Is_Modelled_As_A_GovernancePolicy_Create_ActionProposal()
    {
        var fixture = CreateFixture();
        var proposal = await fixture.Service.ProposeAsync(Request(
            GovernanceEnvironment.Production,
            WriteVerificationProfileSeeds.LinxProductionV1 with { BackupRetentionDays = 120, PolicyVersion = "2.0", EffectiveFrom = Now }));

        Assert.Equal(ActionResourceType.GovernancePolicy, proposal.Proposal.ResourceType);
        Assert.Equal(ActionOperation.Create, proposal.Proposal.Operation);
        Assert.StartsWith(WriteVerificationProfileGovernanceService.ResourcePrefix, proposal.Proposal.Resource, StringComparison.Ordinal);

        var audit = await fixture.Audit.ListByRequestAsync("REQ-PROFILE");
        Assert.Contains(audit, item => item.EventType == "write-verification-profile.proposed");
    }

    [Fact]
    public void Null_Safety_Flag_Keeps_The_Proposal_Hash_Byte_Identical()
    {
        var baseline = new ActionProposal
        {
            Id = Guid.NewGuid(),
            CreatedAt = Now,
            RequestingAgent = "agent",
            Environment = GovernanceEnvironment.Development,
            System = "SOMA/Linx",
            ResourceType = ActionResourceType.DatabaseTable,
            Resource = "FORNECEDORES",
            Operation = ActionOperation.Update,
            Fields = ["INATIVO"],
            FilterSummary = "one row",
            ExpectedAffectedRows = 1,
            Purpose = "test",
            DataClassification = DataClassification.Internal,
            ContainsPersonalData = false,
            ContainsSensitivePersonalData = false,
            ContainsSecrets = false,
            Reversibility = ActionReversibility.Reversible,
        };

        Assert.Null(baseline.ReducesWriteSafetyGuarantees);
        Assert.Equal(baseline.ProposalHash, (baseline with { ReducesWriteSafetyGuarantees = null }).ProposalHash);
        Assert.NotEqual(baseline.ProposalHash, (baseline with { ReducesWriteSafetyGuarantees = true }).ProposalHash);
    }

    private static WriteVerificationProfileChangeRequest Request(GovernanceEnvironment environment, WriteVerificationProfile proposed) =>
        new("REQ-PROFILE", "linx-database-specialist-agent", "subject-requester-001", environment, proposed,
            "Ajuste governado da politica de verificacao de escrita.");

    private static ApprovalGrant Grant(ActionProposal proposal) => new(
        Guid.NewGuid(), Guid.NewGuid(), proposal.ProposalHash, "subject-product-owner-001", Now,
        Now.AddMinutes(30), "specific proposal", null, null);

    private static Fixture CreateFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "blueprintos-governance-tests", Guid.NewGuid().ToString("N"));
        var audit = new FileGovernanceAuditStore(root);
        IWriteVerificationProfileStore store = new InMemoryWriteVerificationProfileStore();
        var service = new WriteVerificationProfileGovernanceService(
            store, new AIGovernancePolicyEngine(), new ApprovalPolicy(), audit, new FixedTimeProvider(Now));
        return new(store, audit, service);
    }

    private sealed record Fixture(
        IWriteVerificationProfileStore Store,
        FileGovernanceAuditStore Audit,
        WriteVerificationProfileGovernanceService Service);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
