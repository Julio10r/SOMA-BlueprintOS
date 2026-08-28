using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

/// <summary>
/// Proves the rollback CAPABILITY gate (point 1 of the Production Write Verification &amp; Recovery Policy
/// addendum): a connection profile requiring rollback support (<c>RollbackSupported=true</c>) is a policy fact
/// about the ENVIRONMENT; whether a given capability can actually honor it is a fact about that capability's
/// own business rules, declared via <see cref="RollbackStrategy"/>. The two must be checked independently, and
/// a mismatch must block BEFORE any write or backup is attempted — never discovered later when a rollback is
/// requested, never worked around by improvising a delete the capability never declared it could do.
///
/// This is exactly the shape of the real <c>GarantirFornecedorGovernedWriteAdapter</c> (declares
/// <see cref="RollbackStrategy.NotSupported"/> because "garantir" never destroys an existing supplier role) —
/// modelled here with a fake so the test is about the framework's gate, not about ERP specifics.
/// </summary>
public sealed class RollbackCapabilityGapTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 17, 0, 0, TimeSpan.Zero);
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blueprintos-rollback-gap-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Write_Is_Blocked_Before_Any_Backup_Or_Execution_When_Profile_Requires_Rollback_But_Capability_Does_Not_Support_It()
    {
        var fixture = CreateFixture(RollbackStrategy.NotSupported, rollbackSupported: true);
        var request = Request();
        var grant = GrantFor(request.Context);

        var result = await fixture.Orchestrator.ExecuteAsync(request, grant, fixture.Adapter);

        Assert.Equal(GovernedWriteExecutionStatus.Blocked, result.Status);
        Assert.Contains(RollbackCapabilityGap.ReasonCode, result.Reasons);
        Assert.NotNull(result.RollbackGap);
        Assert.Equal(FakeCapabilityAdapter.CapabilityId, result.RollbackGap!.Capability);
        Assert.Null(result.RecoveryPackage);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);

        var gaps = await fixture.GapStore.ListAsync();
        var gap = Assert.Single(gaps);
        Assert.Equal(RollbackCapabilityGap.ReasonCode, gap.Reason);
        Assert.Equal(FakeCapabilityAdapter.CapabilityId, gap.Capability);
    }

    [Fact]
    public async Task Write_Proceeds_When_Profile_Requires_Rollback_And_Capability_Declares_RestoreBeforeState()
    {
        var fixture = CreateFixture(RollbackStrategy.RestoreBeforeState, rollbackSupported: true);
        var request = Request();
        var grant = GrantFor(request.Context);

        var result = await fixture.Orchestrator.ExecuteAsync(request, grant, fixture.Adapter);

        Assert.Equal(GovernedWriteExecutionStatus.Completed, result.Status);
        Assert.Null(result.RollbackGap);
        Assert.Equal(1, fixture.Adapter.ExecuteCallCount);
        Assert.Empty(await fixture.GapStore.ListAsync());
    }

    [Fact]
    public async Task Write_Proceeds_When_Profile_Does_Not_Require_Rollback_Even_If_Capability_Does_Not_Support_It()
    {
        // The realistic GarantirFornecedor combination: backup required, rollback not required. NotSupported
        // never blocks anything unless the profile actually asks for rollback.
        var fixture = CreateFixture(RollbackStrategy.NotSupported, rollbackSupported: false);
        var request = Request();
        var grant = GrantFor(request.Context);

        var result = await fixture.Orchestrator.ExecuteAsync(request, grant, fixture.Adapter);

        Assert.Equal(GovernedWriteExecutionStatus.Completed, result.Status);
        Assert.Null(result.RollbackGap);
        Assert.Equal(1, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task File_Store_Persists_And_Lists_Gaps_Without_Any_Relational_Store()
    {
        var governanceRoot = Path.Combine(_root, "file-gap-store");
        var store = new FileRollbackCapabilityGapStore(governanceRoot);
        var gap = new RollbackCapabilityGap(
            Guid.NewGuid(), "REQ-FILE-GAP-001", FakeCapabilityAdapter.OwnerAgentId,
            WriteVerificationProfileSeeds.LinxDevelopment, FakeCapabilityAdapter.CapabilityId,
            "BLUEPRINTOS_RECOVERY_HOMOLOGATION", RollbackCapabilityGap.ReasonCode, null, Now);

        await store.RecordAsync(gap);
        var persisted = Assert.Single(await store.ListAsync());

        Assert.Equal(gap.Id, persisted.Id);
        Assert.Equal(RollbackCapabilityGap.ReasonCode, persisted.Reason);
        Assert.True(File.Exists(Path.Combine(governanceRoot, "rollback-capability-gaps", "2026-08-28", $"{gap.Id:N}.json")));
    }

    // ------------------------------------------------------------------------------------------------------

    private static GovernedWriteExecutionRequest Request() => new(
        Context(), Routing(), Analysis(),
        new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true),
        "rollback-capability-gap-probe",
        WriteVerificationProfileSeeds.LinxDevelopment,
        "192.168.9.98",
        "SOMA_DESENV",
        ["ID=1"],
        [ExpectedAfterSet()],
        "Prova do gate de capability de rollback.",
        []);

    private static StructuredActionContext Context() => new(
        "REQ-ROLLBACK-GAP-001", "subject-requester-001", GovernanceEnvironment.Development, "SOMA/Linx",
        ActionResourceType.DatabaseTable, "BLUEPRINTOS_RECOVERY_HOMOLOGATION",
        OperationIntent.Update, [FakeCapabilityAdapter.CapabilityId], ["VALOR"],
        "ID=1", 1, "Prova do gate de capability de rollback.", DataClassification.Internal,
        false, false, false, ActionReversibility.Reversible,
        ConnectionProfile: WriteVerificationProfileSeeds.LinxDevelopment);

    private static RoutingEvidence Routing() => new(true, FakeCapabilityAdapter.OwnerAgentId, [], [], [], []);

    private static AgentWriteAnalysis Analysis() => new(
        FakeCapabilityAdapter.OwnerAgentId, FakeCapabilityAdapter.CapabilityId, ["VALOR"], "ID=1", 1, ActionReversibility.Reversible);

    private static RecoveryDataSet ExpectedAfterSet() => new("BLUEPRINTOS_RECOVERY_HOMOLOGATION",
        [new Dictionary<string, string?> { ["ID"] = "1", ["VALOR"] = "DEPOIS" }]);

    private static ApprovalGrant GrantFor(StructuredActionContext context)
    {
        var build = new StructuredActionProposalAdapter().Build(context, Routing(), Analysis(), Now);
        var hash = build.Proposal?.ProposalHash ?? new string('0', 64);
        return new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), hash, "subject-product-owner-001", Now,
            Now.AddMinutes(30), "specific proposal", null, null);
    }

    private Fixture CreateFixture(RollbackStrategy strategy, bool rollbackSupported)
    {
                var governanceRoot = Path.Combine(_root, "governance");
        var clock = new FixedTimeProvider(Now);
        var audit = new FileGovernanceAuditStore(governanceRoot);
        var approvals = new FileApprovalStore(governanceRoot);
        var adapter = new FakeCapabilityAdapter(strategy);
        var gateway = new ToolGateway([adapter], new ApprovalPolicy(), audit, clock);
        var stack = new GovernedWriteStack(new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, audit, gateway, clock);
        var recoveryWriter = new RecoveryPackageWriter(_root);
        var index = new InMemoryRecoveryIndexStore();
        var validationGaps = new InMemoryWriteValidationKnowledgeGapStore();
        var rollbackGaps = new InMemoryRollbackCapabilityGapStore();
        var writeAudit = new InMemoryWriteExecutionAuditStore();
        var profile = WriteVerificationProfileSeeds.LinxDevelopmentPhaseA with { RollbackSupported = rollbackSupported, PolicyVersion = "test-rollback-gap" };
        var profileStore = new InMemoryWriteVerificationProfileStore([profile]);
        var validationCatalog = new PostWriteValidationRuleCatalog([new PostWriteValidationRule(
            RuleId: "test.recovery-homologation.v1",
            Resource: "BLUEPRINTOS_RECOVERY_HOMOLOGATION",
            Operations: [ActionOperation.Update],
            BusinessKeyFields: ["ID"],
            FieldsToCompare: ["VALOR"],
            Description: "Reconsulta a linha de homologacao pela chave ID e confirma VALOR.",
            PolicyVersion: "1.0")]);
        var orchestrator = new GovernedWriteExecutionOrchestrator(
            stack, profileStore, validationCatalog, validationGaps, recoveryWriter, index,
            gateway, writeAudit, clock, rollbackGaps);
        return new(orchestrator, adapter, rollbackGaps);
    }

    private sealed record Fixture(GovernedWriteExecutionOrchestrator Orchestrator, FakeCapabilityAdapter Adapter, IRollbackCapabilityGapStore GapStore);

    /// <summary>A fake write adapter for the generic homologation table, whose rollback capability is
    /// configurable per test — the point is the framework's gate, not any real business rule.</summary>
    private sealed class FakeCapabilityAdapter(RollbackStrategy strategy) : IWriteExecutionAdapter, ISnapshotCapableAdapter
    {
        public const string CapabilityId = "fake-recovery-homologation-write";
        public const string OwnerAgentId = "linx-database-specialist-agent";

        public string Capability => CapabilityId;
        public string OwnerAgent => OwnerAgentId;
        public IReadOnlyList<string> AllowedConnectionProfiles => [WriteVerificationProfileSeeds.LinxDevelopment];
        public RollbackStrategy RollbackStrategy => strategy;
        public int ExecuteCallCount { get; private set; }
        private bool _written;

        public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SomaLinxDryRunPreview(
                request.Proposal.System, request.Proposal.Environment, request.Proposal.Resource, request.Proposal.Operation,
                request.Proposal.Fields, request.Proposal.FilterSummary, request.Proposal.ExpectedAffectedRows,
                request.Proposal.Purpose, request.ConnectionProfile, request.PolicyDecision.RiskClassification,
                request.PolicyDecision.Status, "granted", request.Proposal.Reversibility, request.ExecutionMode,
                true, true, false, false));

        public Task<WriteExecutionResult> ExecuteAsync(ToolGatewayRequest request, RecoveryPackageReceipt? recoveryPackage, CancellationToken cancellationToken = default)
        {
            ExecuteCallCount++;
            _written = true;
            return Task.FromResult(new WriteExecutionResult(true, 1,
                [new RecoveryDataSet("BLUEPRINTOS_RECOVERY_HOMOLOGATION", [new Dictionary<string, string?> { ["ID"] = "1", ["VALOR"] = "DEPOIS" }])],
                ["LIVE_EXECUTION_COMPLETED"]));
        }

        public Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecoveryDataSet>>(
                [new RecoveryDataSet("BLUEPRINTOS_RECOVERY_HOMOLOGATION", [new Dictionary<string, string?> { ["ID"] = "1", ["VALOR"] = _written ? "DEPOIS" : "ANTES" }])]);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
