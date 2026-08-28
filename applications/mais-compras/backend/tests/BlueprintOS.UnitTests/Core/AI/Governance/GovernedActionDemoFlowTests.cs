using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class GovernedActionDemoFlowTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Demonstration_Flow_Should_Block_Red_Action()
    {
        var audit = new InMemoryGovernanceAuditRecorder();
        var flow = new GovernedActionDemoFlow(new AIGovernancePolicyEngine(), new ApprovalPolicy(), audit);

        var result = flow.Evaluate(Proposal(ActionOperation.Truncate), null, Now);

        Assert.False(result.CanExecute);
        Assert.Equal(RiskClassification.Red, result.Decision.RiskClassification);
        Assert.Single(audit.Entries);
    }

    [Fact]
    public void Demonstration_Flow_Should_Allow_Yellow_Action_With_Matching_Approval()
    {
        var audit = new InMemoryGovernanceAuditRecorder();
        var flow = new GovernedActionDemoFlow(new AIGovernancePolicyEngine(), new ApprovalPolicy(), audit);
        var proposal = Proposal(ActionOperation.Update);
        var grant = new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), proposal.ProposalHash, "po", Now, Now.AddMinutes(30), "acao especifica", null, null);

        var result = flow.Evaluate(proposal, grant, Now);

        Assert.True(result.CanExecute);
        Assert.Equal(PolicyDecisionStatus.RequiresApproval, result.Decision.Status);
        Assert.Equal("approved", audit.Entries[0].HumanDecision);
    }

    private static ActionProposal Proposal(ActionOperation operation) => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = Now,
        RequestingAgent = "linx-agent",
        Environment = GovernanceEnvironment.Production,
        System = "SOMA/Linx",
        ResourceType = ActionResourceType.DatabaseTable,
        Resource = "PRODUTOS",
        Operation = operation,
        Fields = ["ENVIA_ATACADO_INTERNET"],
        FilterSummary = operation == ActionOperation.Update ? "conjunto validado da planilha" : null,
        ExpectedAffectedRows = operation == ActionOperation.Update ? 417 : null,
        Purpose = "integracao diaria Linx/WISE",
        DataClassification = DataClassification.Internal,
        ContainsPersonalData = false,
        ContainsSensitivePersonalData = false,
        ContainsSecrets = false,
        Reversibility = ActionReversibility.Reversible,
        RunbookReference = operation == ActionOperation.Update ? "docs/operations/LinxWiseDailyIntegrationRunbook.md" : null,
        IsRunbookApprovedOperation = operation == ActionOperation.Update,
        RunbookExpectedAffectedRows = operation == ActionOperation.Update ? 400 : null,
    };
}

