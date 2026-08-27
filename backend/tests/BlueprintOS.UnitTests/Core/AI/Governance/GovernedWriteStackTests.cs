using System.Text.Json;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class GovernedWriteStackTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Scenario_A_Controlled_Update_Should_Persist_Approval_And_Complete_DryRun_Only()
    {
        await using var fixture = CreateFixture();
        var preparation = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update), Routing(), Analysis());

        Assert.NotNull(preparation.ProposalBuild.Proposal);
        Assert.Equal(RiskClassification.Yellow, preparation.PolicyDecision!.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.RequiresApproval, preparation.PolicyDecision.Status);
        Assert.NotNull(preparation.ApprovalRequest);
        Assert.NotNull(await fixture.Approvals.GetRequestAsync(preparation.ApprovalRequest!.Id));

        var grant = await fixture.Stack.GrantAsync(preparation.ApprovalRequest, "subject-product-owner-001", Now.AddMinutes(30), "specific proposal");
        Assert.NotNull(await fixture.Approvals.GetGrantAsync(grant.Id));
        var result = await fixture.Stack.DryRunAsync(Request(preparation, grant));

        Assert.Equal(ToolGatewayStatus.DryRunCompleted, result.Status);
        Assert.NotNull(result.Preview);
        Assert.False(result.Preview!.SqlGenerated);
        Assert.False(result.Preview.ExternalExecutionPerformed);
        Assert.True(result.Preview.CredentialResolutionRequired);
        Assert.True(result.Preview.IdentityPermissionCheckRequired);
        Assert.Equal(GovernedExecutionMode.DryRun, result.Preview.ExecutionMode);
        Assert.False(result.LiveExecutionEnabled);
        Assert.False(result.DirectBypassAllowed);
    }

    [Fact]
    public async Task Scenario_B_Update_Without_Where_Should_Be_Red_And_Blocked()
    {
        await using var fixture = CreateFixture();
        var preparation = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update), Routing(), Analysis(filter: null));
        Assert.NotNull(preparation.ProposalBuild.Proposal);
        Assert.Equal(RiskClassification.Red, preparation.PolicyDecision!.RiskClassification);

        var result = await fixture.Stack.DryRunAsync(Request(preparation, FakeGrant(preparation.ProposalBuild.Proposal!)));
        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("POLICY_BLOCKED", result.Reasons);
    }

    [Fact]
    public async Task Scenario_C_Truncate_Should_Be_Red_And_Blocked()
    {
        await using var fixture = CreateFixture();
        var preparation = await fixture.Stack.PrepareAsync(Context(OperationIntent.Truncate), Routing(), Analysis(fields: [], expectedRows: null));
        Assert.Equal(RiskClassification.Red, preparation.PolicyDecision!.RiskClassification);
        Assert.Equal(ToolGatewayStatus.Blocked, (await fixture.Stack.DryRunAsync(Request(preparation, null))).Status);
    }

    [Fact]
    public async Task Scenario_D_Changed_Filter_Should_Invalidate_ProposalHash_Approval()
    {
        await using var fixture = CreateFixture();
        var original = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update), Routing(), Analysis());
        var grant = await fixture.Stack.GrantAsync(original.ApprovalRequest!, "subject-product-owner-001", Now.AddMinutes(30), "specific proposal");
        var changed = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update), Routing(), Analysis(filter: "different validated set"));

        Assert.NotEqual(original.ProposalBuild.Proposal!.ProposalHash, changed.ProposalBuild.Proposal!.ProposalHash);
        var result = await fixture.Stack.DryRunAsync(Request(changed, grant));
        Assert.Contains("VALID_APPROVAL_REQUIRED", result.Reasons);
    }

    [Fact]
    public async Task Scenario_E_Expired_Approval_Should_Block()
    {
        await using var fixture = CreateFixture();
        var preparation = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update), Routing(), Analysis());
        var expired = FakeGrant(preparation.ProposalBuild.Proposal!) with { ExpiresAt = Now.AddSeconds(-1) };
        Assert.Contains("VALID_APPROVAL_REQUIRED", (await fixture.Stack.DryRunAsync(Request(preparation, expired))).Reasons);
    }

    [Fact]
    public async Task Scenario_F_Revoked_Persisted_Approval_Should_Block()
    {
        await using var fixture = CreateFixture();
        var preparation = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update), Routing(), Analysis());
        var grant = await fixture.Stack.GrantAsync(preparation.ApprovalRequest!, "subject-product-owner-001", Now.AddMinutes(30), "specific proposal");
        await fixture.Stack.RevokeAsync(grant, "subject-product-owner-001");
        var revoked = await fixture.Approvals.GetGrantAsync(grant.Id);

        Assert.NotNull(revoked!.RevokedAt);
        Assert.Contains("VALID_APPROVAL_REQUIRED", (await fixture.Stack.DryRunAsync(Request(preparation, revoked))).Reasons);
    }

    [Fact]
    public async Task Scenario_G_Denied_Identity_Should_Block_Without_Privilege_Escalation()
    {
        await using var fixture = CreateFixture();
        var preparation = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update, GovernanceEnvironment.Development), Routing(), Analysis());
        var grant = await fixture.Stack.GrantAsync(preparation.ApprovalRequest!, "subject-product-owner-001", Now.AddMinutes(30), "specific proposal");
        var request = Request(preparation, grant) with { Identity = new("subject-developer-001", HasEffectivePermission: false) };
        var result = await fixture.Stack.DryRunAsync(request);

        Assert.Contains("IDENTITY_PERMISSION_DENIED", result.Reasons);
        Assert.False(result.PrivilegeEscalationAllowed);
    }

    [Fact]
    public async Task Scenario_H_Secret_Should_Be_Red_And_Audit_Should_Be_Redacted()
    {
        await using var fixture = CreateFixture();
        var context = Context(OperationIntent.Update) with
        {
            DataClassification = DataClassification.SecretCredential,
            ContainsSecrets = true,
            AdditionalContext = "sensitive-value-must-not-be-audited",
        };
        var preparation = await fixture.Stack.PrepareAsync(context, Routing(), Analysis());
        Assert.Equal(RiskClassification.Red, preparation.PolicyDecision!.RiskClassification);
        Assert.Equal(ToolGatewayStatus.Blocked, (await fixture.Stack.DryRunAsync(Request(preparation, null))).Status);

        var audit = await fixture.Audit.ListByRequestAsync(context.RequestId);
        var serialized = JsonSerializer.Serialize(audit);
        Assert.DoesNotContain("sensitive-value-must-not-be-audited", serialized);
        Assert.DoesNotContain("ENVIA_ATACADO_INTERNET", serialized);
        Assert.DoesNotContain("validated fictional set", serialized);
    }

    [Fact]
    public async Task Scenario_I_Massive_Pii_Export_Should_Be_Red_And_Live_Always_Blocked()
    {
        await using var fixture = CreateFixture();
        var context = Context(OperationIntent.Export) with
        {
            ResourceType = ActionResourceType.FileExport,
            Resource = "fictional-export",
            DataClassification = DataClassification.PersonalData,
            ContainsPersonalData = true,
        };
        var analysis = Analysis(fields: ["fictional-field"], expectedRows: 50000);
        var preparation = await fixture.Stack.PrepareAsync(context, Routing(), analysis);
        Assert.Equal(RiskClassification.Red, preparation.PolicyDecision!.RiskClassification);
        Assert.Equal(ToolGatewayStatus.Blocked, (await fixture.Stack.DryRunAsync(Request(preparation, null))).Status);

        var live = await fixture.Stack.DryRunAsync(Request(preparation, null) with { ExecutionMode = GovernedExecutionMode.LiveExecution });
        Assert.Contains("LIVE_EXECUTION_DISABLED", live.Reasons);
        Assert.False(live.LiveExecutionEnabled);
    }

    [Fact]
    public async Task Proposal_Context_Gap_Should_Not_Create_Proposal_Or_Policy_Decision()
    {
        await using var fixture = CreateFixture();
        var preparation = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update) with { Purpose = "" }, Routing(), Analysis());
        Assert.False(preparation.ProposalBuild.Succeeded);
        Assert.Null(preparation.PolicyDecision);
        Assert.Contains(preparation.ProposalBuild.ContextGaps, gap => gap.Field == "purpose");
    }

    [Fact]
    public async Task Live_Execution_Should_Be_Blocked_Even_With_Valid_Yellow_Approval()
    {
        await using var fixture = CreateFixture();
        var preparation = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update), Routing(), Analysis());
        var grant = await fixture.Stack.GrantAsync(preparation.ApprovalRequest!, "subject-product-owner-001", Now.AddMinutes(30), "specific proposal");
        var result = await fixture.Stack.DryRunAsync(Request(preparation, grant) with { ExecutionMode = GovernedExecutionMode.LiveExecution });
        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("LIVE_EXECUTION_DISABLED", result.Reasons);
    }

    [Fact]
    public async Task Multi_Adapter_Gateway_Should_Route_Unregistered_Capability_To_Capability_Gap()
    {
        await using var fixture = CreateFixture(includeReadOnlyAndWiseAdapters: true);
        var preparation = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update), Routing(), Analysis());
        var request = Request(preparation, FakeGrant(preparation.ProposalBuild.Proposal!)) with { Capability = "capability-nobody-registered" };
        var result = await fixture.Stack.DryRunAsync(request);
        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("CAPABILITY_NOT_REGISTERED", result.Reasons);
    }

    [Fact]
    public async Task Wise_Adapter_Should_DryRun_Without_Any_Real_Pyodbc_Style_Side_Effect()
    {
        await using var fixture = CreateFixture(includeReadOnlyAndWiseAdapters: true);
        var context = Context(OperationIntent.Update) with
        {
            RequestedCapabilities = [WiseGovernedAdapter.Capability],
            ConnectionProfile = "wise-governed-write",
        };
        var routing = new RoutingEvidence(true, WiseGovernedAdapter.OwnerAgent, [], ["security-lgpd-agent"], [], []);
        var analysis = new AgentWriteAnalysis(WiseGovernedAdapter.OwnerAgent, WiseGovernedAdapter.Capability, ["QTD_ESTOQUE"], "validated fictional set", 12, ActionReversibility.Reversible);
        var preparation = await fixture.Stack.PrepareAsync(context, routing, analysis);
        var grant = await fixture.Stack.GrantAsync(preparation.ApprovalRequest!, "subject-product-owner-001", Now.AddMinutes(30), "specific proposal");
        var request = new ToolGatewayRequest(
            WiseGovernedAdapter.Capability, WiseGovernedAdapter.OwnerAgent, true,
            preparation.ProposalBuild.Proposal!, preparation.PolicyDecision!, grant,
            ["security-lgpd-agent"], "wise-governed-write",
            new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true),
            GovernedExecutionMode.DryRun);
        var result = await fixture.Stack.DryRunAsync(request);

        Assert.Equal(ToolGatewayStatus.DryRunCompleted, result.Status);
        Assert.False(result.Preview!.SqlGenerated);
        Assert.False(result.Preview.ExternalExecutionPerformed);
    }

    [Fact]
    public async Task Wise_Adapter_Rejects_Wrong_Connection_Profile()
    {
        await using var fixture = CreateFixture(includeReadOnlyAndWiseAdapters: true);
        var context = Context(OperationIntent.Update) with
        {
            RequestedCapabilities = [WiseGovernedAdapter.Capability],
            ConnectionProfile = "linx-erp-governed-write",
        };
        var routing = new RoutingEvidence(true, WiseGovernedAdapter.OwnerAgent, [], ["security-lgpd-agent"], [], []);
        var analysis = new AgentWriteAnalysis(WiseGovernedAdapter.OwnerAgent, WiseGovernedAdapter.Capability, ["QTD_ESTOQUE"], "validated fictional set", 12, ActionReversibility.Reversible);
        var preparation = await fixture.Stack.PrepareAsync(context, routing, analysis);
        var grant = await fixture.Stack.GrantAsync(preparation.ApprovalRequest!, "subject-product-owner-001", Now.AddMinutes(30), "specific proposal");
        var request = new ToolGatewayRequest(
            WiseGovernedAdapter.Capability, WiseGovernedAdapter.OwnerAgent, true,
            preparation.ProposalBuild.Proposal!, preparation.PolicyDecision!, grant,
            ["security-lgpd-agent"], "linx-erp-governed-write",
            new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true),
            GovernedExecutionMode.DryRun);
        var result = await fixture.Stack.DryRunAsync(request);

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("CONNECTION_PROFILE_NOT_GOVERNED", result.Reasons);
    }

    [Fact]
    public async Task ReadOnly_Adapter_Allows_Select_Without_Approval_When_Policy_Green()
    {
        await using var fixture = CreateFixture(includeReadOnlyAndWiseAdapters: true);
        var context = Context(OperationIntent.Read) with
        {
            RequestedCapabilities = [SomaLinxReadOnlyAdapter.Capability],
            ConnectionProfile = "linx-erp-read-only",
        };
        var routing = new RoutingEvidence(true, SomaLinxReadOnlyAdapter.OwnerAgent, [], [], [], []);
        var analysis = new AgentWriteAnalysis(SomaLinxReadOnlyAdapter.OwnerAgent, SomaLinxReadOnlyAdapter.Capability, [], null, null, ActionReversibility.Reversible);
        var preparation = await fixture.Stack.PrepareAsync(context, routing, analysis);

        Assert.Equal(PolicyDecisionStatus.Allowed, preparation.PolicyDecision!.Status);
        Assert.Null(preparation.ApprovalRequest);

        var request = new ToolGatewayRequest(
            SomaLinxReadOnlyAdapter.Capability, SomaLinxReadOnlyAdapter.OwnerAgent, true,
            preparation.ProposalBuild.Proposal!, preparation.PolicyDecision!, null,
            [], "linx-erp-read-only",
            new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true),
            GovernedExecutionMode.DryRun);
        var result = await fixture.Stack.DryRunAsync(request);
        Assert.Equal(ToolGatewayStatus.DryRunCompleted, result.Status);
    }

    [Fact]
    public async Task Owner_Mismatch_Should_Block_Even_With_Registered_Adapter()
    {
        await using var fixture = CreateFixture(includeReadOnlyAndWiseAdapters: true);
        var preparation = await fixture.Stack.PrepareAsync(Context(OperationIntent.Update), Routing(), Analysis());
        var grant = await fixture.Stack.GrantAsync(preparation.ApprovalRequest!, "subject-product-owner-001", Now.AddMinutes(30), "specific proposal");
        var request = Request(preparation, grant) with { RoutedPrimaryAgent = "wise-agent" };
        var result = await fixture.Stack.DryRunAsync(request);
        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("OWNER_MISMATCH", result.Reasons);
    }

    private static Fixture CreateFixture(bool includeReadOnlyAndWiseAdapters = false)
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseInMemoryDatabase($"governed-write-{Guid.NewGuid():N}")
            .Options;
        var db = new BlueprintOSDbContext(options);
        var approvals = new EfApprovalStore(db);
        var audit = new EfGovernanceAuditStore(db);
        var clock = new FixedTimeProvider(Now);
        IGovernedToolAdapter[] adapters = includeReadOnlyAndWiseAdapters
            ? [new SomaLinxDryRunAdapter(), new SomaLinxReadOnlyAdapter(), new WiseGovernedAdapter()]
            : [new SomaLinxDryRunAdapter()];
        var gateway = new ToolGateway(adapters, new ApprovalPolicy(), audit, clock);
        var stack = new GovernedWriteStack(new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, audit, gateway, clock);
        return new(db, approvals, audit, stack);
    }

    private static StructuredActionContext Context(OperationIntent intent, GovernanceEnvironment environment = GovernanceEnvironment.Production) => new(
        "REQ-GOV-WRITE-001", "subject-requester-001", environment, "SOMA/Linx", ActionResourceType.DatabaseTable,
        "PRODUTOS", intent, [StructuredActionProposalAdapter.Capability], ["ENVIA_ATACADO_INTERNET"],
        "validated fictional set", 417, "validated integration", DataClassification.Internal,
        false, false, false, ActionReversibility.Reversible, ConnectionProfile: "linx-erp-governed-write");

    private static RoutingEvidence Routing() => new(
        true, StructuredActionProposalAdapter.OwnerAgent, [], ["security-lgpd-agent"], [], []);

    private static AgentWriteAnalysis Analysis(
        string? filter = "validated fictional set",
        IReadOnlyList<string>? fields = null,
        int? expectedRows = 417) => new(
        StructuredActionProposalAdapter.OwnerAgent,
        StructuredActionProposalAdapter.Capability,
        fields ?? ["ENVIA_ATACADO_INTERNET"],
        filter,
        expectedRows,
        ActionReversibility.Reversible);

    private static ToolGatewayRequest Request(GovernedWritePreparation preparation, ApprovalGrant? grant) => new(
        StructuredActionProposalAdapter.Capability,
        StructuredActionProposalAdapter.OwnerAgent,
        true,
        preparation.ProposalBuild.Proposal!,
        preparation.PolicyDecision!,
        grant,
        ["security-lgpd-agent"],
        "linx-erp-governed-write",
        new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true),
        GovernedExecutionMode.DryRun);

    private static ApprovalGrant FakeGrant(ActionProposal proposal) => new(
        Guid.NewGuid(), Guid.NewGuid(), proposal.ProposalHash, "subject-product-owner-001", Now,
        Now.AddMinutes(30), "specific proposal", null, null);

    private sealed record Fixture(
        BlueprintOSDbContext Db,
        EfApprovalStore Approvals,
        EfGovernanceAuditStore Audit,
        GovernedWriteStack Stack) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
