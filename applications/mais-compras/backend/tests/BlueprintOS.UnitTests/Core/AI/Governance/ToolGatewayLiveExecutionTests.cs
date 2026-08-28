using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

/// <summary>
/// Covers the NEW guarded live-execution path. GovernedWriteStackTests stays untouched and still proves that
/// a request without the new guarantees remains blocked; these tests prove the opposite half — that a request
/// WITH every guarantee reaches a write-capable adapter, and that removing any single guarantee blocks again.
/// </summary>
public sealed class ToolGatewayLiveExecutionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Fully_Guaranteed_Live_Request_Executes_Through_The_Write_Adapter()
    {
        var fixture = await CreateFixtureAsync();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest());

        Assert.Equal(ToolGatewayStatus.LiveExecutionCompleted, result.Status);
        Assert.True(result.LiveExecutionEnabled);
        Assert.Equal(1, fixture.Adapter.ExecuteCallCount);
        Assert.NotNull(result.Execution);
        Assert.True(result.Execution!.Succeeded);
        Assert.Equal(1, result.Execution.RecordsAffected);
        Assert.Single(result.Execution.AfterData);
    }

    [Fact]
    public async Task Adapter_Receives_The_Recovery_Receipt_It_Must_Be_Able_To_Trace()
    {
        var fixture = await CreateFixtureAsync();
        var request = fixture.LiveRequest();
        await fixture.Gateway.InvokeAsync(request);

        Assert.Equal(request.RecoveryPackageReceipt!.ExecutionId, fixture.Adapter.LastReceipt!.ExecutionId);
    }

    [Fact]
    public async Task Live_Request_Without_A_Write_Verification_Profile_Is_Blocked()
    {
        var fixture = await CreateFixtureAsync();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest() with { WriteVerificationProfile = null });

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("LIVE_EXECUTION_DISABLED", result.Reasons);
        Assert.Contains("WRITE_VERIFICATION_PROFILE_REQUIRED", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task Live_Request_Without_A_Recovery_Receipt_Is_Blocked_When_Backup_Is_Required()
    {
        var fixture = await CreateFixtureAsync();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest() with { RecoveryPackageReceipt = null });

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("RECOVERY_PACKAGE_REQUIRED_BEFORE_LIVE_EXECUTION", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task Expired_Or_CaptureFailed_Recovery_Receipt_Does_Not_Count_As_A_Backup()
    {
        var fixture = await CreateFixtureAsync();
        var request = fixture.LiveRequest();

        var expired = await fixture.Gateway.InvokeAsync(request with
        {
            RecoveryPackageReceipt = request.RecoveryPackageReceipt! with { ExpiresAt = Now.AddSeconds(-1) },
        });
        var captureFailed = await fixture.Gateway.InvokeAsync(request with
        {
            RecoveryPackageReceipt = request.RecoveryPackageReceipt! with { BeforeState = BeforeStateStatus.CaptureFailed },
        });

        Assert.Contains("RECOVERY_PACKAGE_REQUIRED_BEFORE_LIVE_EXECUTION", expired.Reasons);
        Assert.Contains("RECOVERY_PACKAGE_REQUIRED_BEFORE_LIVE_EXECUTION", captureFailed.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task NotExistent_Recovery_Receipt_Is_Accepted_As_A_Valid_Backup_For_A_Create()
    {
        var fixture = await CreateFixtureAsync();
        var request = fixture.LiveRequest();

        var result = await fixture.Gateway.InvokeAsync(request with
        {
            RecoveryPackageReceipt = request.RecoveryPackageReceipt! with { BeforeState = BeforeStateStatus.NotExistent },
        });

        Assert.DoesNotContain("RECOVERY_PACKAGE_REQUIRED_BEFORE_LIVE_EXECUTION", result.Reasons);
    }

    [Fact]
    public async Task Live_Request_Without_A_Validation_Rule_Is_Blocked_With_The_Unknown_Rule_Reason()
    {
        var fixture = await CreateFixtureAsync();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest() with { PostWriteValidationRule = null });

        Assert.Contains("WRITE_VALIDATION_RULE_UNKNOWN", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task Validation_Rule_That_Does_Not_Cover_The_Operation_Is_Rejected()
    {
        var fixture = await CreateFixtureAsync();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest() with
        {
            PostWriteValidationRule = PostWriteValidationRuleCatalog.CadastroCliForRule,
        });

        Assert.Contains("WRITE_VALIDATION_RULE_DOES_NOT_COVER_OPERATION", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task Profile_For_A_Different_Connection_Is_Rejected()
    {
        var fixture = await CreateFixtureAsync();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest() with
        {
            WriteVerificationProfile = WriteVerificationProfileSeeds.LinxProductionV1,
        });

        Assert.Contains("WRITE_VERIFICATION_PROFILE_MISMATCH", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task A_DryRun_Only_Adapter_Can_Never_Execute_Live_However_Complete_The_Request_Is()
    {
        var fixture = await CreateFixtureAsync(useWriteCapableAdapter: false);
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest());

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("LIVE_EXECUTION_ADAPTER_NOT_CAPABLE", result.Reasons);
    }

    [Fact]
    public async Task Blocked_Policy_Decision_Still_Blocks_Even_With_Every_Recovery_Guarantee()
    {
        var fixture = await CreateFixtureAsync();
        var blocked = fixture.Decision with { Status = PolicyDecisionStatus.Blocked, RiskClassification = RiskClassification.Red };
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest() with { PolicyDecision = blocked });

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("POLICY_BLOCKED", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task Missing_Approval_Still_Blocks_Even_With_Every_Recovery_Guarantee()
    {
        var fixture = await CreateFixtureAsync();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest() with { ApprovalGrant = null });

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("VALID_APPROVAL_REQUIRED", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task Adapter_Failure_Is_Reported_As_LiveExecutionFailed_Never_As_Success()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Adapter.FailWith = "constraint violation";
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest());

        Assert.Equal(ToolGatewayStatus.LiveExecutionFailed, result.Status);
        Assert.False(result.Execution!.Succeeded);
    }

    [Fact]
    public async Task Adapter_Exception_Is_Contained_And_Reported_As_LiveExecutionFailed()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Adapter.ThrowWith = "socket closed";
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest());

        Assert.Equal(ToolGatewayStatus.LiveExecutionFailed, result.Status);
        Assert.Contains("LIVE_EXECUTION_ADAPTER_FAILED", result.Reasons);
    }

    [Fact]
    public async Task Live_Execution_Is_Audited_As_Requested_And_Completed()
    {
        var fixture = await CreateFixtureAsync();
        await fixture.Gateway.InvokeAsync(fixture.LiveRequest());

        var audit = await fixture.Audit.ListByRequestAsync(fixture.Proposal.Id.ToString("N"));
        Assert.Contains(audit, item => item.EventType == "gateway.live-execution.requested");
        Assert.Contains(audit, item => item.EventType == "gateway.live-execution.started");
        Assert.Contains(audit, item => item.EventType == "gateway.live-execution.completed");
    }

    [Fact]
    public async Task DryRun_Through_The_Same_Write_Capable_Adapter_Still_Performs_No_Write()
    {
        var fixture = await CreateFixtureAsync();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveRequest() with { ExecutionMode = GovernedExecutionMode.DryRun });

        Assert.Equal(ToolGatewayStatus.DryRunCompleted, result.Status);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
        Assert.False(result.Preview!.ExternalExecutionPerformed);
    }

    private static async Task<Fixture> CreateFixtureAsync(bool useWriteCapableAdapter = true)
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseInMemoryDatabase($"live-execution-{Guid.NewGuid():N}").Options;
        var db = new BlueprintOSDbContext(options);
        var audit = new EfGovernanceAuditStore(db);
        var adapter = new FakeWriteExecutionAdapter();
        IGovernedToolAdapter[] adapters = useWriteCapableAdapter ? [adapter] : [new DryRunOnlyAdapter()];
        var gateway = new ToolGateway(adapters, new ApprovalPolicy(), audit, new FixedTimeProvider(Now));

        var profile = await new InMemoryWriteVerificationProfileStore()
            .ResolveAsync(WriteVerificationProfileSeeds.LinxDevelopment, Now);
        var proposal = Proposal();
        var decision = new PolicyDecision(Guid.NewGuid(), proposal.Id, proposal.ProposalHash,
            RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval, ["write requires approval"], Now, true, false);
        var grant = new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), proposal.ProposalHash,
            "subject-product-owner-001", Now, Now.AddMinutes(30), "specific proposal", null, null);

        return new(gateway, adapter, audit, proposal, decision, grant, profile!);
    }

    private static ActionProposal Proposal() => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = Now,
        RequestingAgent = FakeWriteExecutionAdapter.OwnerAgentId,
        Environment = GovernanceEnvironment.Development,
        System = "SOMA/Linx",
        ResourceType = ActionResourceType.DatabaseTable,
        Resource = PostWriteValidationRuleCatalog.FornecedoresResource,
        Operation = ActionOperation.Update,
        Fields = ["INATIVO"],
        FilterSummary = "COD_FORNECEDOR=000123",
        ExpectedAffectedRows = 1,
        Purpose = "Garantir fornecedor no ERP.",
        DataClassification = DataClassification.Internal,
        ContainsPersonalData = false,
        ContainsSensitivePersonalData = false,
        ContainsSecrets = false,
        Reversibility = ActionReversibility.Reversible,
    };

    private sealed record Fixture(
        ToolGateway Gateway,
        FakeWriteExecutionAdapter Adapter,
        EfGovernanceAuditStore Audit,
        ActionProposal Proposal,
        PolicyDecision Decision,
        ApprovalGrant Grant,
        WriteVerificationProfile Profile)
    {
        public ToolGatewayRequest LiveRequest() => new(
            FakeWriteExecutionAdapter.CapabilityId,
            FakeWriteExecutionAdapter.OwnerAgentId,
            true,
            Proposal,
            Decision,
            Grant,
            [],
            WriteVerificationProfileSeeds.LinxDevelopment,
            new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true),
            GovernedExecutionMode.LiveExecution,
            new RecoveryPackageReceipt(Guid.NewGuid(), "/tmp/recovery/package", new string('c', 64), Now, Now.AddDays(30), BeforeState: BeforeStateStatus.Captured),
            PostWriteValidationRuleCatalog.FornecedoresRule,
            Profile);
    }

    private sealed class FakeWriteExecutionAdapter : IWriteExecutionAdapter
    {
        public const string CapabilityId = "fake-governed-write";
        public const string OwnerAgentId = "linx-database-specialist-agent";

        public string Capability => CapabilityId;
        public string OwnerAgent => OwnerAgentId;
        public IReadOnlyList<string> AllowedConnectionProfiles => [WriteVerificationProfileSeeds.LinxDevelopment];

        public int ExecuteCallCount { get; private set; }
        public RecoveryPackageReceipt? LastReceipt { get; private set; }
        public string? FailWith { get; set; }
        public string? ThrowWith { get; set; }

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
            LastReceipt = recoveryPackage;
            if (ThrowWith is not null) throw new InvalidOperationException(ThrowWith);
            if (FailWith is not null) return Task.FromResult(new WriteExecutionResult(false, 0, [], ["WRITE_FAILED"], FailWith));

            return Task.FromResult(new WriteExecutionResult(true, 1,
                [new RecoveryDataSet(PostWriteValidationRuleCatalog.FornecedoresResource,
                    [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123", ["INATIVO"] = "1" }])],
                ["LIVE_EXECUTION_COMPLETED"], null, "000123"));
        }
    }

    private sealed class DryRunOnlyAdapter : IGovernedToolAdapter
    {
        public string Capability => FakeWriteExecutionAdapter.CapabilityId;
        public string OwnerAgent => FakeWriteExecutionAdapter.OwnerAgentId;
        public IReadOnlyList<string> AllowedConnectionProfiles => [WriteVerificationProfileSeeds.LinxDevelopment];

        public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Should never be reached in these tests.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
