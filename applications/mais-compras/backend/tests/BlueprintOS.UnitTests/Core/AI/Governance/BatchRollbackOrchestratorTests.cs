using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

/// <summary>
/// Batch rollback: full and selective, concurrency exclusion per item, mixed-operations refusal, confirmation
/// mismatch. Mirrors <see cref="RollbackOrchestratorTests"/>'s guarantee: every refusal asserts nothing was
/// written; the two items here use two independent fake adapters so a test can prove one item's write never
/// touched the other's key.
/// </summary>
public sealed class BatchRollbackOrchestratorTests : IDisposable
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);
    private const string Key1 = "CGC_CPF=00000000000191";
    private const string Key2 = "CGC_CPF=00000000000282";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blueprintos-batch-rollback-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Full_Batch_Rollback_Restores_Every_Item_And_Marks_The_Index_RolledBack()
    {
        var fixture = await CreateFixtureAsync();

        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.BatchExecutionId, [], fixture.Snapshots);
        Assert.Equal(BatchRollbackAnalysisStatus.ReadyForConfirmation, analysis.Status);
        Assert.Equal(2, analysis.ReadyItems.Count);
        Assert.Empty(analysis.ConcurrencyFindings);

        var result = await fixture.ExecuteAsync(analysis);

        Assert.Equal(BatchRollbackExecutionStatus.Completed, result.Status);
        Assert.Equal(2, result.ItemOutcomes.Count);
        Assert.All(result.ItemOutcomes, o => Assert.True(o.Success));
        Assert.Equal(1, fixture.AdapterFor(Key1).ExecuteCallCount);
        Assert.Equal(1, fixture.AdapterFor(Key2).ExecuteCallCount);
        Assert.Equal("0", fixture.CurrentState[Key1]);
        Assert.Equal("0", fixture.CurrentState[Key2]);

        var entry = Assert.Single(await fixture.Index.FindAsync(new RecoveryIndexQuery { ExecutionId = fixture.BatchExecutionId }));
        Assert.Equal(RecoveryPackageStatus.RolledBack, entry.Status);
    }

    [Fact]
    public async Task Selective_Rollback_Restores_Only_The_Requested_Item_And_Leaves_The_Other_Untouched()
    {
        var fixture = await CreateFixtureAsync();

        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.BatchExecutionId, [Key1], fixture.Snapshots);
        Assert.Equal(BatchRollbackAnalysisStatus.ReadyForConfirmation, analysis.Status);
        Assert.Single(analysis.ReadyItems);
        Assert.Equal(Key1, analysis.ReadyItems[0].BusinessKey);

        var result = await fixture.ExecuteAsync(analysis);

        Assert.Equal(BatchRollbackExecutionStatus.Completed, result.Status);
        Assert.Single(result.ItemOutcomes);
        Assert.Equal(1, fixture.AdapterFor(Key1).ExecuteCallCount);
        Assert.Equal(0, fixture.AdapterFor(Key2).ExecuteCallCount);
        Assert.Equal("0", fixture.CurrentState[Key1]);
        Assert.Equal("1", fixture.CurrentState[Key2]); // untouched — still what the original forward write left.

        // The batch as a whole is not fully rolled back yet (one item still pending), so the index stays Active.
        var entry = Assert.Single(await fixture.Index.FindAsync(new RecoveryIndexQuery { ExecutionId = fixture.BatchExecutionId }));
        Assert.Equal(RecoveryPackageStatus.Active, entry.Status);
    }

    [Fact]
    public async Task Concurrent_Change_On_One_Item_Excludes_Only_That_Item_From_The_Ready_Set()
    {
        var fixture = await CreateFixtureAsync();
        fixture.CurrentState[Key2] = "9"; // somebody else changed item 2 after the original execution

        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.BatchExecutionId, [], fixture.Snapshots);

        Assert.Equal(BatchRollbackAnalysisStatus.ReadyForConfirmation, analysis.Status);
        Assert.Single(analysis.ReadyItems);
        Assert.Equal(Key1, analysis.ReadyItems[0].BusinessKey);
        Assert.NotEmpty(analysis.ConcurrencyFindings);

        var result = await fixture.ExecuteAsync(analysis);
        Assert.Equal(BatchRollbackExecutionStatus.Completed, result.Status);
        Assert.Equal(0, fixture.AdapterFor(Key2).ExecuteCallCount);
        Assert.Equal("9", fixture.CurrentState[Key2]); // never touched.
    }

    [Fact]
    public async Task When_Every_Targeted_Item_Has_Concurrent_Change_Rollback_Is_Fully_Blocked()
    {
        var fixture = await CreateFixtureAsync();
        fixture.CurrentState[Key1] = "9";
        fixture.CurrentState[Key2] = "9";

        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.BatchExecutionId, [], fixture.Snapshots);

        Assert.Equal(BatchRollbackAnalysisStatus.BlockedConcurrentChange, analysis.Status);
        Assert.Empty(analysis.ReadyItems);
        Assert.Null(analysis.ConfirmationHandle);
        Assert.Equal(0, fixture.AdapterFor(Key1).ExecuteCallCount);
        Assert.Equal(0, fixture.AdapterFor(Key2).ExecuteCallCount);
    }

    [Fact]
    public async Task Confirmation_With_Wrong_Handle_Blocks_Execution_Entirely()
    {
        var fixture = await CreateFixtureAsync();
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.BatchExecutionId, [], fixture.Snapshots);

        var wrongConfirmation = new BatchRollbackConfirmation(fixture.BatchExecutionId, new string('0', 64), "subject-requester-001", "forcar", Now);
        var result = await fixture.Orchestrator.ExecuteAsync(analysis, wrongConfirmation, fixture.GatewayFactory, fixture.CaptureAfterRollbackAsync, fixture.Approve);

        Assert.Equal(BatchRollbackExecutionStatus.Blocked, result.Status);
        Assert.Equal(0, fixture.AdapterFor(Key1).ExecuteCallCount);
        Assert.Equal(0, fixture.AdapterFor(Key2).ExecuteCallCount);
    }

    [Fact]
    public async Task Rollback_Always_Uses_A_Brand_New_Approval()
    {
        var fixture = await CreateFixtureAsync();
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.BatchExecutionId, [], fixture.Snapshots);
        await fixture.ExecuteAsync(analysis);

        Assert.Equal(1, fixture.ApprovalCallbackCount);
        Assert.NotEqual(OriginalProposalHash, fixture.ApprovedProposalHash);
    }

    [Fact]
    public async Task Unknown_Business_Key_In_Selective_Rollback_Is_Refused()
    {
        var fixture = await CreateFixtureAsync();
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.BatchExecutionId, ["CGC_CPF=99999999999999"], fixture.Snapshots);

        Assert.Equal(BatchRollbackAnalysisStatus.NotAvailable, analysis.Status);
        Assert.Contains("BUSINESS_KEY_NOT_IN_BATCH", analysis.Reasons);
    }

    // --- helpers -------------------------------------------------------------------------------------

    private const string OriginalProposalHash = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    private async Task<Fixture> CreateFixtureAsync()
    {
        var governanceRoot = Path.Combine(_root, "governance");
        var clock = new FixedTimeProvider(Now);
        var batchWriter = new BatchRecoveryPackageWriter(_root);
        var index = new InMemoryRecoveryIndexStore();
        var governanceAudit = new FileGovernanceAuditStore(governanceRoot);
        var rollbackAudit = new InMemoryRollbackAuditStore();
        var approvals = new FileApprovalStore(governanceRoot);
        var profileStore = new InMemoryWriteVerificationProfileStore();

        var batchExecutionId = Guid.NewGuid();
        var expiresAt = ExecutedAt.AddDays(30);
        var manifest = new BatchRecoveryPackageManifest
        {
            BatchExecutionId = batchExecutionId,
            ExecutionName = "garantir-fornecedores-lote",
            AgentId = "linx-database-specialist-agent",
            Capability = "fake-fornecedor-governed-write",
            ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment,
            Server = "192.168.9.98",
            Database = "SOMA_DESENV",
            ExecutedAt = ExecutedAt,
            Requester = "subject-requester-001",
            Origin = "unit-test",
            OriginalRequestSummary = "Garantir fornecedores em lote.",
            OperationTypes = [ActionOperation.Update],
            TablesAffected = ["FORNECEDORES"],
            TotalItems = 0,
            ChunkCount = 0,
            MaxItemsPerChunk = 0,
            MaxChunkSizeBytes = 0,
            BackupRequired = true,
            RollbackSupported = true,
            RetentionDays = 30,
            ExpiresAt = expiresAt,
            ValidationRuleId = PostWriteValidationRuleCatalog.FornecedoresRule.RuleId,
            ProposalHash = OriginalProposalHash,
            Status = BatchStatus.Active,
            ChunkBeforeDataChecksumsSha256 = new Dictionary<int, string>(),
        };

        var items = new[]
        {
            new BatchRecoveryItem(Key1, "FORNECEDORES", Row(Key1, "0"), Row(Key1, "1")),
            new BatchRecoveryItem(Key2, "FORNECEDORES", Row(Key2, "0"), Row(Key2, "1")),
        };

        var receipt = await batchWriter.CreateBatchAsync(manifest, items);
        // Records the "current" state (what the original forward write left behind) for the concurrency check —
        // exactly what GovernedWriteExecutionOrchestrator.WriteAfterDataAsync would have done for a real batch
        // write orchestrator, done here directly since building one is outside this task's scope.
        await batchWriter.WriteChunkAfterDataAsync(receipt.PackagePath, 1, [Row(Key1, "1"), Row(Key2, "1")]);

        await index.AppendAsync(new RecoveryIndexEntry(
            batchExecutionId, manifest.ExecutionName, manifest.AgentId, manifest.ConnectionProfile, manifest.Server,
            manifest.Database, manifest.ExecutedAt, manifest.Requester, manifest.OperationTypes, manifest.TablesAffected,
            [Key1, Key2], 2, true, true, 30, expiresAt, receipt.PackagePath, receipt.ManifestChecksumSha256,
            RecoveryPackageStatus.Active, manifest.ProposalHash, manifest.ValidationRuleId));

        var orchestrator = new BatchRollbackOrchestrator(
            index, batchWriter, new PostWriteValidationRuleCatalog(), new AIGovernancePolicyEngine(), new ApprovalPolicy(),
            approvals, profileStore, rollbackAudit, governanceAudit, clock);

        var currentState = new Dictionary<string, string> { [Key1] = "1", [Key2] = "1" };
        var snapshots = new FakeSnapshotSource(currentState);
        var adapters = new Dictionary<string, FakeRollbackWriteAdapter>
        {
            [Key1] = new(currentState, Key1),
            [Key2] = new(currentState, Key2),
        };

        return new Fixture(orchestrator, index, snapshots, adapters, currentState, batchExecutionId, clock, governanceAudit);
    }

    private static RecoveryDataSet Row(string key, string inativo) => new("FORNECEDORES",
        [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123", ["CGC_CPF"] = key, ["FORNECEDOR"] = "ACME", ["INATIVO"] = inativo }]);

    private sealed class Fixture(
        BatchRollbackOrchestrator orchestrator,
        InMemoryRecoveryIndexStore index,
        FakeSnapshotSource snapshots,
        Dictionary<string, FakeRollbackWriteAdapter> adapters,
        Dictionary<string, string> currentState,
        Guid batchExecutionId,
        TimeProvider clock,
        IGovernanceAuditStore governanceAudit)
    {
        public BatchRollbackOrchestrator Orchestrator { get; } = orchestrator;
        public InMemoryRecoveryIndexStore Index { get; } = index;
        public FakeSnapshotSource Snapshots { get; } = snapshots;
        public Dictionary<string, string> CurrentState { get; } = currentState;
        public Guid BatchExecutionId { get; } = batchExecutionId;
        public int ApprovalCallbackCount { get; private set; }
        public string? ApprovedProposalHash { get; private set; }

        public FakeRollbackWriteAdapter AdapterFor(string key) => adapters[key];

        public (IToolGateway Gateway, string Capability) GatewayFactory(BatchItemRestorePlan item)
        {
            var adapter = adapters[item.BusinessKey];
            var gateway = new ToolGateway([adapter], new ApprovalPolicy(), governanceAudit, clock);
            return (gateway, adapter.Capability);
        }

        public Task<IReadOnlyList<RecoveryDataSet>> CaptureAfterRollbackAsync(string key, CancellationToken ct) =>
            Snapshots.CaptureSnapshotAsync([key], ct);

        public Task<ApprovalGrant?> Approve(ActionProposal proposal, PolicyDecision decision, ApprovalRequest request, CancellationToken ct)
        {
            ApprovalCallbackCount++;
            ApprovedProposalHash = proposal.ProposalHash;
            return Task.FromResult<ApprovalGrant?>(new ApprovalGrant(
                Guid.NewGuid(), request.Id, proposal.ProposalHash, "subject-product-owner-001",
                clock.GetUtcNow(), clock.GetUtcNow().AddMinutes(30), "rollback de lote", null, null));
        }

        public Task<BatchRollbackExecutionResult> ExecuteAsync(BatchRollbackSafetyAnalysis analysis)
        {
            var confirmation = new BatchRollbackConfirmation(
                BatchExecutionId, analysis.ConfirmationHandle ?? "none", "subject-requester-001",
                "Reverter alteracao em lote de fornecedores.", clock.GetUtcNow());
            return Orchestrator.ExecuteAsync(analysis, confirmation, GatewayFactory, CaptureAfterRollbackAsync, Approve);
        }
    }

    private sealed class FakeSnapshotSource(Dictionary<string, string> currentState) : ISnapshotCapableAdapter
    {
        public Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default)
        {
            var key = businessKeys[0];
            return Task.FromResult<IReadOnlyList<RecoveryDataSet>>([Row(key, currentState[key])]);
        }
    }

    private sealed class FakeRollbackWriteAdapter(Dictionary<string, string> currentState, string key) : IWriteExecutionAdapter
    {
        public string Capability => "fake-fornecedor-governed-write";
        public string OwnerAgent => "linx-database-specialist-agent";
        public IReadOnlyList<string> AllowedConnectionProfiles => [WriteVerificationProfileSeeds.LinxDevelopment];
        public int ExecuteCallCount { get; private set; }

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
            currentState[key] = "0"; // restores to the original before-value.
            return Task.FromResult(new WriteExecutionResult(true, 1, [], ["LIVE_EXECUTION_COMPLETED"]));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
