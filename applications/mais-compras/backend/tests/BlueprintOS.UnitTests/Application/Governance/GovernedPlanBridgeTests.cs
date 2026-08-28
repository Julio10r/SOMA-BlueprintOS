using BlueprintOS.Application.Governance;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Application.Governance;

public sealed class GovernedPlanBridgeTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Valid_Wise_Plan_Payload_Should_Reach_Policy_Evaluation_As_Yellow()
    {
        var root = CreateGovernanceRoot();
        var bridge = CreateBridge(root);
        var payload = new GovernedPlanPayload(
            RequestId: "REQ-BRIDGE-001",
            RequestedBy: "subject-requester-001",
            AgentId: WiseGovernedAdapter.OwnerAgent,
            Capability: WiseGovernedAdapter.Capability,
            Environment: "Production",
            System: "WISE",
            ResourceType: "DatabaseTable",
            Resource: "ESTOQUE",
            OperationIntent: "UPDATE",
            Fields: ["QTD_ESTOQUE"],
            FilterSummary: "validated fictional set",
            ExpectedAffectedRows: 12,
            Purpose: "daily stock reconciliation",
            DataClassification: "Internal",
            ContainsPersonalData: false,
            ContainsSensitivePersonalData: false,
            ContainsSecrets: false,
            Reversibility: "Reversible",
            RunbookReference: null,
            ConnectionProfile: "wise-governed-write",
            AdditionalContext: null,
            CrossCuttingAgents: ["security-lgpd-agent"]);

        var preparation = await bridge.PrepareAsync(payload);

        Assert.True(preparation.ProposalBuild.Succeeded);
        Assert.Equal(RiskClassification.Yellow, preparation.PolicyDecision!.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.RequiresApproval, preparation.PolicyDecision.Status);
        Assert.NotNull(preparation.ApprovalRequest);
    }

    [Fact]
    public async Task Plan_Payload_With_Unknown_Environment_Should_Surface_As_Context_Gap()
    {
        var root = CreateGovernanceRoot();
        var bridge = CreateBridge(root);
        var payload = BasePayload() with { Environment = "Unknown" };

        var preparation = await bridge.PrepareAsync(payload);

        Assert.False(preparation.ProposalBuild.Succeeded);
        Assert.Contains(preparation.ProposalBuild.ContextGaps, gap => gap.Field == "environment");
    }

    [Fact]
    public async Task Invalid_Enum_Value_Should_Throw_Instead_Of_Silently_Defaulting()
    {
        var root = CreateGovernanceRoot();
        var bridge = CreateBridge(root);
        var payload = BasePayload() with { OperationIntent = "NOT_A_REAL_INTENT" };

        await Assert.ThrowsAsync<ArgumentException>(() => bridge.PrepareAsync(payload));
    }

    private static GovernedPlanPayload BasePayload() => new(
        RequestId: "REQ-BRIDGE-002",
        RequestedBy: "subject-requester-001",
        AgentId: WiseGovernedAdapter.OwnerAgent,
        Capability: WiseGovernedAdapter.Capability,
        Environment: "Production",
        System: "WISE",
        ResourceType: "DatabaseTable",
        Resource: "ESTOQUE",
        OperationIntent: "UPDATE",
        Fields: ["QTD_ESTOQUE"],
        FilterSummary: "validated fictional set",
        ExpectedAffectedRows: 12,
        Purpose: "daily stock reconciliation",
        DataClassification: "Internal",
        ContainsPersonalData: false,
        ContainsSensitivePersonalData: false,
        ContainsSecrets: false,
        Reversibility: "Reversible",
        RunbookReference: null,
        ConnectionProfile: "wise-governed-write",
        AdditionalContext: null,
        CrossCuttingAgents: ["security-lgpd-agent"]);

    private static string CreateGovernanceRoot() =>
        Path.Combine(Path.GetTempPath(), "blueprintos-governance-tests", Guid.NewGuid().ToString("N"));

    private static GovernedPlanBridge CreateBridge(string governanceRoot)
    {
        var approvals = new FileApprovalStore(governanceRoot);
        var audit = new FileGovernanceAuditStore(governanceRoot);
        var clock = new FixedTimeProvider(Now);
        var gateway = new ToolGateway([new WiseGovernedAdapter(), new SomaLinxDryRunAdapter(), new SomaLinxReadOnlyAdapter()], new ApprovalPolicy(), audit, clock);
        var stack = new GovernedWriteStack(new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, audit, gateway, clock);
        return new GovernedPlanBridge(stack);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
