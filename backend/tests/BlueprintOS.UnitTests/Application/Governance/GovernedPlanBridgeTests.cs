using BlueprintOS.Application.Governance;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Application.Governance;

public sealed class GovernedPlanBridgeTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Valid_Wise_Plan_Payload_Should_Reach_Policy_Evaluation_As_Yellow()
    {
        await using var db = CreateDb();
        var bridge = CreateBridge(db);
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
        await using var db = CreateDb();
        var bridge = CreateBridge(db);
        var payload = BasePayload() with { Environment = "Unknown" };

        var preparation = await bridge.PrepareAsync(payload);

        Assert.False(preparation.ProposalBuild.Succeeded);
        Assert.Contains(preparation.ProposalBuild.ContextGaps, gap => gap.Field == "environment");
    }

    [Fact]
    public async Task Invalid_Enum_Value_Should_Throw_Instead_Of_Silently_Defaulting()
    {
        await using var db = CreateDb();
        var bridge = CreateBridge(db);
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

    private static BlueprintOSDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseInMemoryDatabase($"governed-plan-bridge-{Guid.NewGuid():N}")
            .Options;
        return new BlueprintOSDbContext(options);
    }

    private static GovernedPlanBridge CreateBridge(BlueprintOSDbContext db)
    {
        var approvals = new EfApprovalStore(db);
        var audit = new EfGovernanceAuditStore(db);
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
