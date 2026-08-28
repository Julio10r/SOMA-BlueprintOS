using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

/// <summary>
/// End-to-end coverage of the governed live-write orchestration with a FAKE adapter. No SQL, no ERP, no
/// network: the point is the ordering and the refusals, not the driver.
/// </summary>
public sealed class GovernedWriteExecutionOrchestratorTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 15, 20, 0, TimeSpan.Zero);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blueprintos-orchestrator-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Happy_Path_Writes_Recovery_Package_Then_Executes_Then_Validates()
    {
        var fixture = CreateFixture();
        var result = await ExecuteWithApprovalAsync(fixture);

        Assert.Equal(GovernedWriteExecutionStatus.Completed, result.Status);
        Assert.NotNull(result.RecoveryPackage);
        Assert.True(result.Validation!.Passed);
        Assert.Equal(1, result.Validation.RecordsValidated);
        Assert.Equal(0, result.Validation.RecordsWithErrors);
        Assert.Equal(1, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task Recovery_Package_Is_Written_Before_The_Write_Is_Attempted()
    {
        var fixture = CreateFixture();
        await ExecuteWithApprovalAsync(fixture);

        Assert.Equal(["capture-before", "create-package", "execute", "capture-after"], fixture.Adapter.Journal);
    }

    [Fact]
    public async Task Package_Contains_Manifest_Before_Expected_After_And_Validation_Report()
    {
        var fixture = CreateFixture();
        var result = await ExecuteWithApprovalAsync(fixture);
        var path = result.RecoveryPackage!.PackagePath;

        Assert.True(File.Exists(Path.Combine(path, RecoveryPackageWriter.ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(path, RecoveryPackageWriter.BeforeDataFileName)));
        Assert.True(File.Exists(Path.Combine(path, RecoveryPackageWriter.ExpectedAfterFileName)));
        Assert.True(File.Exists(Path.Combine(path, RecoveryPackageWriter.AfterDataFileName)));
        Assert.True(File.Exists(Path.Combine(path, RecoveryPackageWriter.ValidationReportFileName)));
    }

    [Fact]
    public async Task Execution_Is_Indexed_So_A_Later_Rollback_Can_Discover_It()
    {
        var fixture = CreateFixture();
        var result = await ExecuteWithApprovalAsync(fixture);

        var entry = Assert.Single(await fixture.RecoveryIndex.FindAsync(new RecoveryIndexQuery { ExecutionId = result.ExecutionId }));
        Assert.Equal(RecoveryPackageStatus.Active, entry.Status);
        Assert.Equal(result.RecoveryPackage!.PackagePath, entry.PackagePath);
        Assert.Contains("CGC_CPF=00000000000191", entry.BusinessKeys);
    }

    [Fact]
    public async Task Permanent_Audit_Row_Is_Appended_For_A_Completed_Execution()
    {
        var fixture = CreateFixture();
        var result = await ExecuteWithApprovalAsync(fixture);

        var audit = await fixture.WriteAudit.GetAsync(result.ExecutionId);
        Assert.NotNull(audit);
        Assert.Equal(WriteExecutionOutcome.Completed, audit!.Outcome);
        Assert.True(audit.PostWriteValidationPassed);
        Assert.True(audit.BackupCreated);
        Assert.True(audit.RollbackAvailable);
        Assert.Equal(30, audit.RetentionDays);
        Assert.Equal("1.0-phase-a", audit.WriteVerificationPolicyVersion);
    }

    [Fact]
    public async Task Missing_Approval_Stops_Before_Any_Backup_Or_Write()
    {
        var fixture = CreateFixture();
        var result = await fixture.Orchestrator.ExecuteAsync(Request(), null, fixture.Adapter);

        Assert.Equal(GovernedWriteExecutionStatus.AwaitingApproval, result.Status);
        Assert.Null(result.RecoveryPackage);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
        Assert.Empty(fixture.Adapter.Journal);
    }

    [Fact]
    public async Task Unknown_Validation_Rule_Blocks_And_Records_A_Knowledge_Gap()
    {
        var fixture = CreateFixture();
        var request = Request() with { ExpectedAfter = [] };
        var context = request.Context with { Resource = "PRODUTOS" };
        var result = await fixture.Orchestrator.ExecuteAsync(
            request with { Context = context }, GrantFor(fixture, context), fixture.Adapter);

        Assert.Equal(GovernedWriteExecutionStatus.Blocked, result.Status);
        Assert.Contains("WRITE_VALIDATION_RULE_UNKNOWN", result.Reasons);
        Assert.NotNull(result.KnowledgeGap);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);

        var gap = Assert.Single(await fixture.Gaps.ListAsync());
        Assert.Equal("PRODUTOS", gap.Resource);
    }

    [Fact]
    public async Task Ungoverned_Connection_Profile_Blocks_Before_Any_Write()
    {
        var fixture = CreateFixture();
        var request = Request() with { ConnectionProfile = "some-database-nobody-governed" };
        var result = await fixture.Orchestrator.ExecuteAsync(request, GrantFor(fixture, request.Context), fixture.Adapter);

        Assert.Equal(GovernedWriteExecutionStatus.Blocked, result.Status);
        Assert.Contains("WRITE_VERIFICATION_PROFILE_NOT_FOUND", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task Adapter_Failure_Keeps_The_Recovery_Package_And_Reports_ExecutionFailed()
    {
        var fixture = CreateFixture();
        fixture.Adapter.FailWith = "constraint violation";
        var result = await ExecuteWithApprovalAsync(fixture);

        Assert.Equal(GovernedWriteExecutionStatus.ExecutionFailed, result.Status);
        Assert.NotNull(result.RecoveryPackage);
        Assert.True(File.Exists(Path.Combine(result.RecoveryPackage!.PackagePath, RecoveryPackageWriter.BeforeDataFileName)));
        Assert.Equal(WriteExecutionOutcome.ExecutionFailed, (await fixture.WriteAudit.GetAsync(result.ExecutionId))!.Outcome);
    }

    [Fact]
    public async Task State_That_Does_Not_Match_The_Expectation_Fails_Validation_Not_Silently_Passes()
    {
        var fixture = CreateFixture();
        fixture.Adapter.AfterOverride = new RecoveryDataSet(PostWriteValidationRuleCatalog.FornecedoresResource,
            [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123", ["CGC_CPF"] = "00000000000191", ["FORNECEDOR"] = "ACME", ["INATIVO"] = "0" }]);
        var result = await ExecuteWithApprovalAsync(fixture);

        Assert.Equal(GovernedWriteExecutionStatus.ValidationFailed, result.Status);
        Assert.False(result.Validation!.Passed);
        Assert.Contains(result.Validation.Mismatches, item => item.Contains("INATIVO", StringComparison.Ordinal));
        Assert.Equal(WriteExecutionOutcome.ValidationFailed, (await fixture.WriteAudit.GetAsync(result.ExecutionId))!.Outcome);
    }

    [Fact]
    public async Task Record_That_Disappeared_After_The_Write_Fails_Validation()
    {
        var fixture = CreateFixture();
        fixture.Adapter.AfterOverride = new RecoveryDataSet(PostWriteValidationRuleCatalog.FornecedoresResource, []);
        var result = await ExecuteWithApprovalAsync(fixture);

        Assert.Equal(GovernedWriteExecutionStatus.ValidationFailed, result.Status);
        Assert.Equal(1, result.Validation!.RecordsWithErrors);
    }

    private async Task<GovernedWriteExecutionResult> ExecuteWithApprovalAsync(Fixture fixture)
    {
        var request = Request();
        return await fixture.Orchestrator.ExecuteAsync(request, GrantFor(fixture, request.Context), fixture.Adapter);
    }

    /// <summary>Builds an approval grant bound to the exact proposal the stack will build for this context.</summary>
    private static ApprovalGrant GrantFor(Fixture fixture, StructuredActionContext context)
    {
        var build = new StructuredActionProposalAdapter().Build(context, Routing(), Analysis(), Now);
        var hash = build.Proposal?.ProposalHash ?? new string('0', 64);
        return new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), hash, "subject-product-owner-001", Now,
            Now.AddMinutes(30), "specific proposal", null, null);
    }

    private static GovernedWriteExecutionRequest Request() => new(
        Context(), Routing(), Analysis(),
        new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true),
        "garantir-fornecedor",
        WriteVerificationProfileSeeds.LinxDevelopment,
        "192.168.9.98",
        "SOMA_DESENV",
        ["CGC_CPF=00000000000191"],
        [ExpectedAfterSet()],
        "Garantir fornecedor por CNPJ no ERP de desenvolvimento.",
        ["LX_SEQUENCIAL"]);

    private static StructuredActionContext Context() => new(
        "REQ-GOV-EXEC-001", "subject-requester-001", GovernanceEnvironment.Development, "SOMA/Linx",
        ActionResourceType.DatabaseTable, PostWriteValidationRuleCatalog.FornecedoresResource,
        OperationIntent.Update, [FakeFornecedorWriteAdapter.CapabilityId], ["INATIVO"],
        "COD_FORNECEDOR=000123", 1, "Garantir fornecedor no ERP.", DataClassification.Internal,
        false, false, false, ActionReversibility.Reversible,
        ConnectionProfile: WriteVerificationProfileSeeds.LinxDevelopment);

    private static RoutingEvidence Routing() => new(true, FakeFornecedorWriteAdapter.OwnerAgentId, [], [], [], []);

    private static AgentWriteAnalysis Analysis() => new(
        FakeFornecedorWriteAdapter.OwnerAgentId, FakeFornecedorWriteAdapter.CapabilityId,
        ["INATIVO"], "COD_FORNECEDOR=000123", 1, ActionReversibility.Reversible);

    private static RecoveryDataSet ExpectedAfterSet() => new(PostWriteValidationRuleCatalog.FornecedoresResource,
        [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123", ["CGC_CPF"] = "00000000000191", ["FORNECEDOR"] = "ACME", ["INATIVO"] = "1" }]);

    private Fixture CreateFixture()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseInMemoryDatabase($"orchestrator-{Guid.NewGuid():N}").Options;
        var db = new BlueprintOSDbContext(options);
        var clock = new FixedTimeProvider(Now);
        var audit = new EfGovernanceAuditStore(db);
        var approvals = new EfApprovalStore(db);
        var adapter = new FakeFornecedorWriteAdapter();
        var gateway = new ToolGateway([adapter], new ApprovalPolicy(), audit, clock);
        var stack = new GovernedWriteStack(new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, audit, gateway, clock);
        var recoveryWriter = new RecoveryPackageWriter(_root);
        var index = new InMemoryRecoveryIndexStore();
        var gaps = new InMemoryWriteValidationKnowledgeGapStore();
        var writeAudit = new InMemoryWriteExecutionAuditStore();
        var orchestrator = new GovernedWriteExecutionOrchestrator(
            stack, new InMemoryWriteVerificationProfileStore(), new PostWriteValidationRuleCatalog(),
            gaps, recoveryWriter, index, gateway, writeAudit, clock);
        return new(orchestrator, adapter, index, gaps, writeAudit);
    }

    private sealed record Fixture(
        GovernedWriteExecutionOrchestrator Orchestrator,
        FakeFornecedorWriteAdapter Adapter,
        InMemoryRecoveryIndexStore RecoveryIndex,
        InMemoryWriteValidationKnowledgeGapStore Gaps,
        InMemoryWriteExecutionAuditStore WriteAudit);

    /// <summary>Fake adapter that records the ORDER of the calls the orchestrator makes, which is the property
    /// under test: backup before write, re-read after write.</summary>
    private sealed class FakeFornecedorWriteAdapter : IWriteExecutionAdapter, ISnapshotCapableAdapter
    {
        public const string CapabilityId = "fake-fornecedor-governed-write";
        public const string OwnerAgentId = "linx-database-specialist-agent";

        public string Capability => CapabilityId;
        public string OwnerAgent => OwnerAgentId;
        public IReadOnlyList<string> AllowedConnectionProfiles =>
            [WriteVerificationProfileSeeds.LinxDevelopment, "some-database-nobody-governed"];

        // This fake represents a capability that DOES support rollback (unlike the real
        // GarantirFornecedorGovernedWriteAdapter it is named after) — these tests exercise the ordinary
        // successful-write path under a profile that requires rollback support, not the capability-gap path
        // (see RollbackCapabilityGapTests for that).
        public RollbackStrategy RollbackStrategy => RollbackStrategy.RestoreBeforeState;

        public List<string> Journal { get; } = [];
        public int ExecuteCallCount { get; private set; }
        public string? FailWith { get; set; }
        public RecoveryDataSet? AfterOverride { get; set; }

        private bool _written;

        public Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default)
        {
            Journal.Add(_written ? "capture-after" : "capture-before");
            if (!_written)
            {
                return Task.FromResult<IReadOnlyList<RecoveryDataSet>>(
                    [new RecoveryDataSet("FORNECEDORES", [new Dictionary<string, string?>
                    {
                        ["COD_FORNECEDOR"] = "000123", ["CGC_CPF"] = "00000000000191", ["FORNECEDOR"] = "ACME", ["INATIVO"] = "0",
                    }])]);
            }

            return Task.FromResult<IReadOnlyList<RecoveryDataSet>>(
                [AfterOverride ?? new RecoveryDataSet("FORNECEDORES", [new Dictionary<string, string?>
                {
                    ["COD_FORNECEDOR"] = "000123", ["CGC_CPF"] = "00000000000191", ["FORNECEDOR"] = "ACME", ["INATIVO"] = "1",
                }])]);
        }

        public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SomaLinxDryRunPreview(
                request.Proposal.System, request.Proposal.Environment, request.Proposal.Resource, request.Proposal.Operation,
                request.Proposal.Fields, request.Proposal.FilterSummary, request.Proposal.ExpectedAffectedRows,
                request.Proposal.Purpose, request.ConnectionProfile, request.PolicyDecision.RiskClassification,
                request.PolicyDecision.Status, "granted", request.Proposal.Reversibility, request.ExecutionMode,
                true, true, false, false));

        public Task<WriteExecutionResult> ExecuteAsync(ToolGatewayRequest request, RecoveryPackageReceipt? recoveryPackage, CancellationToken cancellationToken = default)
        {
            // Proves the package really existed on disk before the write ran.
            if (recoveryPackage is not null && Directory.Exists(recoveryPackage.PackagePath)) Journal.Add("create-package");
            Journal.Add("execute");
            ExecuteCallCount++;
            if (FailWith is not null) return Task.FromResult(new WriteExecutionResult(false, 0, [], ["WRITE_FAILED"], FailWith));
            _written = true;
            return Task.FromResult(new WriteExecutionResult(true, 1, [], ["LIVE_EXECUTION_COMPLETED"], null, "000123"));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
