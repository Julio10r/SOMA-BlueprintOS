using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class AIGovernancePolicyEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly AIGovernancePolicyEngine _engine = new();

    [Fact]
    public void Select_Should_Be_Green_And_Allowed()
    {
        var decision = _engine.Evaluate(Proposal(ActionOperation.Select, DataClassification.Internal), Now);

        Assert.Equal(RiskClassification.Green, decision.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.Allowed, decision.Status);
    }

    [Fact]
    public void Contextual_Update_Should_Be_Yellow_And_Require_Approval()
    {
        var proposal = Proposal(
            ActionOperation.Update,
            DataClassification.Internal,
            fields: ["ENVIA_ATACADO_INTERNET"],
            filterSummary: "PRODUTO in conjunto validado da planilha",
            expectedAffectedRows: 417,
            runbookReference: "docs/operations/LinxWiseDailyIntegrationRunbook.md",
            isRunbookApprovedOperation: true,
            runbookExpectedAffectedRows: 400);

        var decision = _engine.Evaluate(proposal, Now);

        Assert.Equal(RiskClassification.Yellow, decision.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.RequiresApproval, decision.Status);
        Assert.False(decision.IsMaterialDeviation);
    }

    [Fact]
    public void Update_Without_Context_Should_Be_Red_And_Blocked()
    {
        var decision = _engine.Evaluate(Proposal(ActionOperation.Update, DataClassification.Internal), Now);

        Assert.Equal(RiskClassification.Red, decision.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.Blocked, decision.Status);
    }

    [Theory]
    [InlineData(ActionOperation.Delete)]
    [InlineData(ActionOperation.Truncate)]
    [InlineData(ActionOperation.Drop)]
    [InlineData(ActionOperation.Grant)]
    [InlineData(ActionOperation.Revoke)]
    public void Destructive_Or_Privilege_Operations_Should_Be_Red(ActionOperation operation)
    {
        var decision = _engine.Evaluate(Proposal(operation, DataClassification.Internal), Now);

        Assert.Equal(RiskClassification.Red, decision.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.Blocked, decision.Status);
    }

    [Fact]
    public void Secret_Exposure_Should_Be_Red()
    {
        var proposal = Proposal(ActionOperation.PromptWithSecret, DataClassification.SecretCredential) with
        {
            ContainsSecrets = true,
        };

        var decision = _engine.Evaluate(proposal, Now);

        Assert.Equal(RiskClassification.Red, decision.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.Blocked, decision.Status);
    }

    [Fact]
    public void Massive_Pii_Export_Should_Be_Red()
    {
        var proposal = Proposal(ActionOperation.Export, DataClassification.PersonalData, expectedAffectedRows: 50000) with
        {
            ContainsPersonalData = true,
            ResourceType = ActionResourceType.FileExport,
            Resource = "clientes.xlsx",
        };

        var decision = _engine.Evaluate(proposal, Now);

        Assert.Equal(RiskClassification.Red, decision.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.Blocked, decision.Status);
    }

    [Fact]
    public void Runbook_Operation_Can_Reduce_Write_Risk_To_Yellow()
    {
        var proposal = Proposal(
            ActionOperation.Update,
            DataClassification.Internal,
            fields: ["ESTOQUE", "ES1"],
            filterSummary: "ID_CAMPANHA = 54 e produto/cor aprovados",
            expectedAffectedRows: 430,
            runbookReference: "docs/operations/LinxWiseDailyIntegrationRunbook.md",
            isRunbookApprovedOperation: true,
            runbookExpectedAffectedRows: 400);

        var decision = _engine.Evaluate(proposal, Now);

        Assert.Equal(RiskClassification.Yellow, decision.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.RequiresApproval, decision.Status);
    }

    [Fact]
    public void Runbook_Operation_With_Material_Volume_Deviation_Should_Require_Reevaluation()
    {
        var proposal = Proposal(
            ActionOperation.Update,
            DataClassification.Internal,
            fields: ["ESTOQUE", "ES1"],
            filterSummary: "ID_CAMPANHA = 54 e produto/cor aprovados",
            expectedAffectedRows: 400000,
            runbookReference: "docs/operations/LinxWiseDailyIntegrationRunbook.md",
            isRunbookApprovedOperation: true,
            runbookExpectedAffectedRows: 400);

        var decision = _engine.Evaluate(proposal, Now);

        Assert.Equal(RiskClassification.Yellow, decision.RiskClassification);
        Assert.True(decision.IsMaterialDeviation);
        Assert.Contains(decision.Reasons, reason => reason.Contains("diverge materialmente", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_Data_Classification_Should_Be_Yellow()
    {
        var decision = _engine.Evaluate(Proposal(ActionOperation.Select, DataClassification.Unknown), Now);

        Assert.Equal(RiskClassification.Yellow, decision.RiskClassification);
        Assert.Equal(PolicyDecisionStatus.RequiresApproval, decision.Status);
    }

    private static ActionProposal Proposal(
        ActionOperation operation,
        DataClassification dataClassification,
        IReadOnlyList<string>? fields = null,
        string? filterSummary = null,
        int? expectedAffectedRows = null,
        string? runbookReference = null,
        bool isRunbookApprovedOperation = false,
        int? runbookExpectedAffectedRows = null) => new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = Now,
            RequestingAgent = "linx-agent",
            Environment = GovernanceEnvironment.Production,
            System = "SOMA/Linx",
            ResourceType = ActionResourceType.DatabaseTable,
            Resource = "PRODUTOS",
            Operation = operation,
            Fields = fields ?? Array.Empty<string>(),
            FilterSummary = filterSummary,
            ExpectedAffectedRows = expectedAffectedRows,
            Purpose = "validacao de governanca",
            DataClassification = dataClassification,
            ContainsPersonalData = dataClassification == DataClassification.PersonalData,
            ContainsSensitivePersonalData = dataClassification == DataClassification.SensitivePersonalData,
            ContainsSecrets = dataClassification == DataClassification.SecretCredential,
            Reversibility = ActionReversibility.Reversible,
            RunbookReference = runbookReference,
            IsRunbookApprovedOperation = isRunbookApprovedOperation,
            RunbookExpectedAffectedRows = runbookExpectedAffectedRows,
        };
}

