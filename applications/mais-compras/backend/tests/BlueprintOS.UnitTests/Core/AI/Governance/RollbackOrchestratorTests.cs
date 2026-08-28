using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

/// <summary>
/// Scenarios A–J of the governed rollback. The through-line: DISCOVER != SELECT != AUTHORIZE != EXECUTE.
/// Every test that expects a refusal also asserts that NOTHING was written.
/// </summary>
public sealed class RollbackOrchestratorTests : IDisposable
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blueprintos-rollback-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    // --- A -------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_Discovery_With_Zero_Candidates_Reports_RollbackNotFound()
    {
        var fixture = await CreateFixtureAsync();
        var discovery = await fixture.Orchestrator.DiscoverAsync(new RecoveryIndexQuery { BusinessKey = "99999999999999" });

        Assert.Equal(RollbackDiscoveryStatus.NotFound, discovery.Status);
        Assert.Empty(discovery.Candidates);
        Assert.Contains(RollbackOrchestrator.NotFoundReason, discovery.Reasons);
    }

    // --- B -------------------------------------------------------------------------------------------

    [Fact]
    public async Task B_Single_Candidate_Is_Located_But_Never_Executed_Automatically()
    {
        var fixture = await CreateFixtureAsync();
        var discovery = await fixture.Orchestrator.DiscoverAsync(new RecoveryIndexQuery { ExecutionId = fixture.ExecutionId });

        Assert.Equal(RollbackDiscoveryStatus.SingleCandidate, discovery.Status);
        Assert.Single(discovery.Candidates);
        Assert.Contains("AWAITING_EXPLICIT_SELECTION", discovery.Reasons);
        Assert.Equal(0, fixture.WriteAdapter.ExecuteCallCount);
        Assert.Empty(await fixture.RollbackAudit.ListAsync());
    }

    // --- C -------------------------------------------------------------------------------------------

    [Fact]
    public async Task C_Multiple_Candidates_Are_All_Presented_And_None_Is_Chosen()
    {
        var fixture = await CreateFixtureAsync(extraExecutions: 2);
        var discovery = await fixture.Orchestrator.DiscoverAsync(new RecoveryIndexQuery { Table = "FORNECEDORES" });

        Assert.Equal(RollbackDiscoveryStatus.MultipleCandidates, discovery.Status);
        Assert.Equal(3, discovery.Candidates.Count);
        Assert.Contains("AWAITING_EXPLICIT_SELECTION", discovery.Reasons);
        Assert.Equal(0, fixture.WriteAdapter.ExecuteCallCount);
    }

    // --- D -------------------------------------------------------------------------------------------

    [Fact]
    public async Task D_Selecting_A_Candidate_Runs_The_Analysis_But_Still_Does_Not_Execute()
    {
        var fixture = await CreateFixtureAsync();
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);

        Assert.Equal(RollbackAnalysisStatus.ReadyForConfirmation, analysis.Status);
        Assert.NotNull(analysis.ConfirmationHandle);
        Assert.Contains("Confirmacao explicita obrigatoria", analysis.Summary, StringComparison.Ordinal);
        Assert.NotEmpty(analysis.BeforeData);
        Assert.Equal(0, fixture.WriteAdapter.ExecuteCallCount);
    }

    // --- E -------------------------------------------------------------------------------------------

    [Fact]
    public async Task E_Explicit_Confirmation_With_The_Right_Handle_Runs_The_Governed_Rollback_And_Validates()
    {
        var fixture = await CreateFixtureAsync();
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);
        fixture.Snapshots.RestoreOnNextWrite = true;

        var result = await ExecuteAsync(fixture, analysis, Confirmation(analysis));

        Assert.Equal(RollbackExecutionStatus.Completed, result.Status);
        Assert.Contains("ROLLBACK_VALIDATION=PASS", result.Reasons);
        Assert.True(result.Validation!.Passed);
        Assert.Equal(1, fixture.WriteAdapter.ExecuteCallCount);
        Assert.NotNull(result.Proposal);
        Assert.Equal(fixture.ExecutionId, result.Proposal!.OriginalExecutionId);
    }

    [Fact]
    public async Task E_Rollback_Uses_A_Brand_New_Approval_Never_The_Original_One()
    {
        var fixture = await CreateFixtureAsync();
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);
        fixture.Snapshots.RestoreOnNextWrite = true;

        await ExecuteAsync(fixture, analysis, Confirmation(analysis));

        Assert.Equal(1, fixture.ApprovalCallbackCount);
        Assert.NotEqual(OriginalProposalHash, fixture.ApprovedProposalHash);
    }

    [Fact]
    public async Task E_Rollback_Marks_The_Package_As_RolledBack_And_Writes_A_Permanent_Audit()
    {
        var fixture = await CreateFixtureAsync();
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);
        fixture.Snapshots.RestoreOnNextWrite = true;
        var result = await ExecuteAsync(fixture, analysis, Confirmation(analysis));

        var entry = Assert.Single(await fixture.Index.FindAsync(new RecoveryIndexQuery { ExecutionId = fixture.ExecutionId }));
        Assert.Equal(RecoveryPackageStatus.RolledBack, entry.Status);

        var audit = Assert.Single(await fixture.RollbackAudit.ListByOriginalExecutionAsync(fixture.ExecutionId));
        Assert.Equal(RollbackExecutionStatus.Completed, audit.Status);
        Assert.True(audit.ExplicitConfirmationReceived);
        Assert.NotNull(audit.ConfirmedAt);
        Assert.Equal(result.RollbackExecutionId, audit.RollbackExecutionId);
        Assert.True(audit.PostRollbackValidationPassed);
        Assert.Contains("FORNECEDORES", audit.TablesAffected);
    }

    [Fact]
    public async Task E_State_That_Does_Not_Come_Back_Fails_Post_Rollback_Validation()
    {
        var fixture = await CreateFixtureAsync();
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);
        fixture.Snapshots.RestoreOnNextWrite = false; // the write "succeeds" but the state does not return

        var result = await ExecuteAsync(fixture, analysis, Confirmation(analysis));

        Assert.Equal(RollbackExecutionStatus.ValidationFailed, result.Status);
        Assert.Contains("ROLLBACK_VALIDATION=FAIL", result.Reasons);
    }

    // --- F -------------------------------------------------------------------------------------------

    [Fact]
    public async Task F_Concurrent_Change_Blocks_The_Rollback_Without_Any_Write()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Snapshots.CurrentInativo = "9"; // somebody else changed the record after the original execution

        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);

        Assert.Equal(RollbackAnalysisStatus.BlockedConcurrentChange, analysis.Status);
        Assert.Contains(RollbackOrchestrator.ConcurrentChangeReason, analysis.Reasons);
        Assert.Null(analysis.ConfirmationHandle);
        Assert.NotEmpty(analysis.ConcurrencyFindings);
        Assert.Equal(0, fixture.WriteAdapter.ExecuteCallCount);
    }

    [Fact]
    public async Task F_A_Blocked_Analysis_Cannot_Be_Forced_Into_Execution()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Snapshots.CurrentInativo = "9";
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);

        var result = await ExecuteAsync(fixture, analysis, new RollbackConfirmation(
            fixture.ExecutionId, "handle-that-does-not-exist", "subject-requester-001", "forcar", Now));

        Assert.Equal(RollbackExecutionStatus.Blocked, result.Status);
        Assert.Equal(0, fixture.WriteAdapter.ExecuteCallCount);
    }

    // --- G -------------------------------------------------------------------------------------------

    [Fact]
    public async Task G_Expired_Recovery_Package_Reports_RollbackNotAvailable_Without_Any_Write()
    {
        var fixture = await CreateFixtureAsync(expiresAt: Now.AddDays(-1));
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);

        Assert.Equal(RollbackAnalysisStatus.NotAvailable, analysis.Status);
        Assert.Contains(RollbackOrchestrator.NotAvailableReason, analysis.Reasons);
        Assert.Null(analysis.ConfirmationHandle);
        Assert.Equal(0, fixture.WriteAdapter.ExecuteCallCount);
    }

    [Fact]
    public async Task G_Package_Removed_From_Disk_Reports_RollbackNotAvailable()
    {
        var fixture = await CreateFixtureAsync();
        Directory.Delete(fixture.PackagePath, recursive: true);

        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);
        Assert.Equal(RollbackAnalysisStatus.NotAvailable, analysis.Status);
    }

    [Fact]
    public async Task G_Corrupted_Manifest_Checksum_Reports_RollbackNotAvailable()
    {
        var fixture = await CreateFixtureAsync(indexChecksumOverride: new string('f', 64));
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);

        Assert.Equal(RollbackAnalysisStatus.NotAvailable, analysis.Status);
        Assert.Contains("RECOVERY_PACKAGE_INTEGRITY_FAILED", analysis.Reasons);
    }

    // --- H -------------------------------------------------------------------------------------------

    [Fact]
    public async Task H_No_Backup_Means_No_Reconstruction_Is_Even_Attempted()
    {
        var fixture = await CreateFixtureAsync(backupRequired: false);
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);

        Assert.Equal(RollbackAnalysisStatus.NotAvailable, analysis.Status);
        Assert.Contains("NO_BACKUP_WAS_TAKEN", analysis.Reasons);
        Assert.Empty(analysis.BeforeData);
        Assert.Equal(0, fixture.Snapshots.CaptureCount);
        Assert.Equal(0, fixture.WriteAdapter.ExecuteCallCount);
    }

    [Fact]
    public async Task H_Rollback_Not_Supported_By_Policy_Means_No_Reconstruction_Is_Attempted()
    {
        var fixture = await CreateFixtureAsync(rollbackSupported: false);
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);

        Assert.Equal(RollbackAnalysisStatus.NotAvailable, analysis.Status);
        Assert.Contains("ROLLBACK_NOT_SUPPORTED_BY_POLICY", analysis.Reasons);
        Assert.Equal(0, fixture.Snapshots.CaptureCount);
    }

    // --- I -------------------------------------------------------------------------------------------

    [Fact]
    public async Task I_Confirmation_With_A_Different_Handle_Is_Blocked_And_Demands_A_New_Confirmation()
    {
        var fixture = await CreateFixtureAsync();
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);

        var result = await ExecuteAsync(fixture, analysis, Confirmation(analysis) with { ConfirmationHandle = new string('0', 64) });

        Assert.Equal(RollbackExecutionStatus.Blocked, result.Status);
        Assert.Contains(RollbackOrchestrator.ConfirmationMismatchReason, result.Reasons);
        Assert.Contains("NEW_CONFIRMATION_REQUIRED", result.Reasons);
        Assert.Equal(0, fixture.WriteAdapter.ExecuteCallCount);
    }

    [Fact]
    public async Task I_Confirmation_Naming_A_Different_Execution_Is_Blocked()
    {
        var fixture = await CreateFixtureAsync(extraExecutions: 1);
        var analysis = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);

        var result = await ExecuteAsync(fixture, analysis, Confirmation(analysis) with { ExecutionId = Guid.NewGuid() });

        Assert.Equal(RollbackExecutionStatus.Blocked, result.Status);
        Assert.Contains(RollbackOrchestrator.ConfirmationMismatchReason, result.Reasons);
        Assert.Equal(0, fixture.WriteAdapter.ExecuteCallCount);
    }

    [Fact]
    public async Task I_A_Handle_Issued_For_Another_Execution_Cannot_Be_Replayed()
    {
        var fixture = await CreateFixtureAsync(extraExecutions: 1);
        var other = (await fixture.Index.FindAsync(new RecoveryIndexQuery()))
            .First(entry => entry.ExecutionId != fixture.ExecutionId);

        var analysisA = await fixture.Orchestrator.AnalyzeAsync(fixture.ExecutionId, fixture.Snapshots);
        var analysisB = await fixture.Orchestrator.AnalyzeAsync(other.ExecutionId, fixture.Snapshots);

        Assert.NotEqual(analysisA.ConfirmationHandle, analysisB.ConfirmationHandle);

        var result = await ExecuteAsync(fixture, analysisA, Confirmation(analysisA) with { ConfirmationHandle = analysisB.ConfirmationHandle! });
        Assert.Equal(RollbackExecutionStatus.Blocked, result.Status);
        Assert.Equal(0, fixture.WriteAdapter.ExecuteCallCount);
    }

    // --- J -------------------------------------------------------------------------------------------

    [Fact]
    public async Task J_Discovery_Works_From_A_Cold_Start_With_Only_The_Index()
    {
        // Build the index, then throw away every object that created it and construct a brand-new orchestrator
        // with no prior context whatsoever. Discovery must still find the execution.
        var seeded = await CreateFixtureAsync();
        var executionId = seeded.ExecutionId;

        var coldOrchestrator = new RollbackOrchestrator(
            seeded.Index,
            new RecoveryPackageWriter(_root),
            new PostWriteValidationRuleCatalog(),
            new AIGovernancePolicyEngine(),
            new ApprovalPolicy(),
            new EfApprovalStore(NewDb()),
            new ToolGateway([new FakeRollbackWriteAdapter()], new ApprovalPolicy(), new EfGovernanceAuditStore(NewDb()), new FixedTimeProvider(Now)),
            new InMemoryWriteVerificationProfileStore(),
            new InMemoryRollbackAuditStore(),
            new InMemoryWriteExecutionAuditStore(),
            new EfGovernanceAuditStore(NewDb()),
            new FixedTimeProvider(Now));

        var byId = await coldOrchestrator.DiscoverAsync(new RecoveryIndexQuery { ExecutionId = executionId });
        var byKey = await coldOrchestrator.DiscoverAsync(new RecoveryIndexQuery { BusinessKey = Cnpj });
        var byPeriod = await coldOrchestrator.DiscoverAsync(new RecoveryIndexQuery { ExecutedFrom = ExecutedAt.AddDays(-1), ExecutedTo = ExecutedAt.AddDays(1) });

        Assert.Equal(RollbackDiscoveryStatus.SingleCandidate, byId.Status);
        Assert.Equal(RollbackDiscoveryStatus.SingleCandidate, byKey.Status);
        Assert.Equal(RollbackDiscoveryStatus.SingleCandidate, byPeriod.Status);
        Assert.Equal(executionId, byKey.Candidates[0].ExecutionId);
    }

    // --- helpers -------------------------------------------------------------------------------------

    private const string Cnpj = "00000000000191";
    private const string OriginalProposalHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static RollbackConfirmation Confirmation(RollbackSafetyAnalysis analysis) => new(
        analysis.ExecutionId, analysis.ConfirmationHandle ?? "none", "subject-requester-001",
        "Reverter alteracao indevida de status do fornecedor.", Now);

    private static Task<RollbackExecutionResult> ExecuteAsync(Fixture fixture, RollbackSafetyAnalysis analysis, RollbackConfirmation confirmation) =>
        fixture.Orchestrator.ExecuteAsync(analysis, confirmation, fixture.Snapshots, fixture.WriteAdapter, fixture.Approve);

    private BlueprintOSDbContext NewDb() => new(new DbContextOptionsBuilder<BlueprintOSDbContext>()
        .UseInMemoryDatabase($"rollback-{Guid.NewGuid():N}").Options);

    private async Task<Fixture> CreateFixtureAsync(
        int extraExecutions = 0,
        DateTimeOffset? expiresAt = null,
        bool backupRequired = true,
        bool rollbackSupported = true,
        string? indexChecksumOverride = null)
    {
        var db = NewDb();
        var clock = new FixedTimeProvider(Now);
        var writer = new RecoveryPackageWriter(_root);
        var index = new InMemoryRecoveryIndexStore();
        var snapshots = new FakeSnapshotSource();
        var writeAdapter = new FakeRollbackWriteAdapter(snapshots);
        var governanceAudit = new EfGovernanceAuditStore(db);
        var rollbackAudit = new InMemoryRollbackAuditStore();
        var writeAudit = new InMemoryWriteExecutionAuditStore();
        var gateway = new ToolGateway([writeAdapter], new ApprovalPolicy(), governanceAudit, clock);

        var executionId = Guid.NewGuid();
        var packagePath = await SeedExecutionAsync(writer, index, writeAudit, executionId, ExecutedAt,
            expiresAt ?? ExecutedAt.AddDays(30), backupRequired, rollbackSupported, indexChecksumOverride);

        for (var i = 0; i < extraExecutions; i++)
        {
            await SeedExecutionAsync(writer, index, writeAudit, Guid.NewGuid(), ExecutedAt.AddHours(i + 1),
                (expiresAt ?? ExecutedAt.AddDays(30)).AddHours(i + 1), backupRequired, rollbackSupported, null);
        }

        var orchestrator = new RollbackOrchestrator(
            index, writer, new PostWriteValidationRuleCatalog(), new AIGovernancePolicyEngine(), new ApprovalPolicy(),
            new EfApprovalStore(db), gateway, new InMemoryWriteVerificationProfileStore(), rollbackAudit, writeAudit,
            governanceAudit, clock);

        return new(orchestrator, index, snapshots, writeAdapter, rollbackAudit, writeAudit, executionId, packagePath);
    }

    private static async Task<string> SeedExecutionAsync(
        RecoveryPackageWriter writer,
        InMemoryRecoveryIndexStore index,
        InMemoryWriteExecutionAuditStore writeAudit,
        Guid executionId,
        DateTimeOffset executedAt,
        DateTimeOffset expiresAt,
        bool backupRequired,
        bool rollbackSupported,
        string? indexChecksumOverride)
    {
        var manifest = new RecoveryPackageManifest
        {
            ExecutionId = executionId,
            ExecutionName = "garantir-fornecedor",
            AgentId = "linx-database-specialist-agent",
            ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment,
            Server = "192.168.9.98",
            Database = "SOMA_DESENV",
            ExecutedAt = executedAt,
            Requester = "subject-requester-001",
            OriginalRequestSummary = "Garantir fornecedor por CNPJ.",
            OperationTypes = [ActionOperation.Update],
            TablesAffected = ["FORNECEDORES"],
            BusinessKeys = [$"CGC_CPF={Cnpj}"],
            RecordsExpectedToChange = 1,
            BackupRequired = backupRequired,
            RollbackSupported = rollbackSupported,
            RetentionDays = 30,
            ExpiresAt = expiresAt,
            ValidationRuleId = PostWriteValidationRuleCatalog.FornecedoresRule.RuleId,
            ProposalHash = OriginalProposalHash,
        };

        var receipt = await writer.CreateAsync(manifest, [Row("0")], [Row("1")]);
        await writer.WriteAfterDataAsync(receipt, [Row("1")]);

        await index.AppendAsync(new RecoveryIndexEntry(
            executionId, manifest.ExecutionName, manifest.AgentId, manifest.ConnectionProfile, manifest.Server,
            manifest.Database, manifest.ExecutedAt, manifest.Requester, manifest.OperationTypes, manifest.TablesAffected,
            manifest.BusinessKeys, 1, backupRequired, rollbackSupported, 30, expiresAt, receipt.PackagePath,
            indexChecksumOverride ?? receipt.ManifestChecksumSha256, RecoveryPackageStatus.Active,
            manifest.ProposalHash, manifest.ValidationRuleId));

        await writeAudit.AppendAsync(new WriteExecutionAuditRecord
        {
            ExecutionId = executionId,
            ExecutionName = manifest.ExecutionName,
            AgentId = manifest.AgentId,
            ConnectionProfile = manifest.ConnectionProfile,
            WriteVerificationPolicyVersion = "1.0-phase-a",
            Server = manifest.Server,
            Database = manifest.Database,
            StartedAt = executedAt,
            CompletedAt = executedAt,
            Requester = manifest.Requester,
            Intent = "Garantir fornecedor no ERP.",
            Operations = manifest.OperationTypes,
            TablesAffected = manifest.TablesAffected,
            BusinessKeys = manifest.BusinessKeys,
            RecordsAffected = 1,
            BeforeAfterSummary = "before=1; after=1",
            ValidationRuleId = manifest.ValidationRuleId,
            RecordsValidated = 1,
            RecordsWithErrors = 0,
            PostWriteValidationPassed = true,
            BackupRequired = backupRequired,
            BackupCreated = backupRequired,
            RetentionDays = 30,
            BackupExpiresAt = expiresAt,
            RecoveryPackageStatus = RecoveryPackageStatus.Active,
            RollbackAvailable = backupRequired && rollbackSupported,
            Outcome = WriteExecutionOutcome.Completed,
        });

        return receipt.PackagePath;
    }

    private static RecoveryDataSet Row(string inativo) => new("FORNECEDORES",
        [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123", ["CGC_CPF"] = Cnpj, ["FORNECEDOR"] = "ACME", ["INATIVO"] = inativo }]);

    private sealed class Fixture(
        RollbackOrchestrator orchestrator,
        InMemoryRecoveryIndexStore index,
        FakeSnapshotSource snapshots,
        FakeRollbackWriteAdapter writeAdapter,
        InMemoryRollbackAuditStore rollbackAudit,
        InMemoryWriteExecutionAuditStore writeAudit,
        Guid executionId,
        string packagePath)
    {
        public RollbackOrchestrator Orchestrator { get; } = orchestrator;
        public InMemoryRecoveryIndexStore Index { get; } = index;
        public FakeSnapshotSource Snapshots { get; } = snapshots;
        public FakeRollbackWriteAdapter WriteAdapter { get; } = writeAdapter;
        public InMemoryRollbackAuditStore RollbackAudit { get; } = rollbackAudit;
        public InMemoryWriteExecutionAuditStore WriteAudit { get; } = writeAudit;
        public Guid ExecutionId { get; } = executionId;
        public string PackagePath { get; } = packagePath;

        public int ApprovalCallbackCount { get; private set; }
        public string? ApprovedProposalHash { get; private set; }

        /// <summary>Stands in for the human approver. It grants a NEW approval bound to the rollback proposal's
        /// own hash — it cannot express "reuse the original approval" even if it wanted to.</summary>
        public Task<ApprovalGrant?> Approve(ActionProposal proposal, PolicyDecision decision, ApprovalRequest request, CancellationToken ct)
        {
            ApprovalCallbackCount++;
            ApprovedProposalHash = proposal.ProposalHash;
            return Task.FromResult<ApprovalGrant?>(new ApprovalGrant(
                Guid.NewGuid(), request.Id, proposal.ProposalHash, "subject-product-owner-001",
                Now, Now.AddMinutes(30), "rollback especifico", null, null));
        }
    }

    private sealed class FakeSnapshotSource : ISnapshotCapableAdapter
    {
        public int CaptureCount { get; private set; }

        /// <summary>The INATIVO value the resource currently reports. "1" is the state the original execution
        /// left behind; anything else looks like a concurrent change.</summary>
        public string CurrentInativo { get; set; } = "1";

        /// <summary>When true, a write flips the state back to the original before-value.</summary>
        public bool RestoreOnNextWrite { get; set; }

        public void ApplyWrite()
        {
            if (RestoreOnNextWrite) CurrentInativo = "0";
        }

        public Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default)
        {
            CaptureCount++;
            return Task.FromResult<IReadOnlyList<RecoveryDataSet>>([Row(CurrentInativo)]);
        }
    }

    private sealed class FakeRollbackWriteAdapter(FakeSnapshotSource? snapshots = null) : IWriteExecutionAdapter
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
            snapshots?.ApplyWrite();
            return Task.FromResult(new WriteExecutionResult(true, 1, [], ["LIVE_EXECUTION_COMPLETED"]));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
