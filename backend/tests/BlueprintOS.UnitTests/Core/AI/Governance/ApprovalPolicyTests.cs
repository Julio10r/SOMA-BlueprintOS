using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class ApprovalPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);
    private readonly ApprovalPolicy _policy = new();

    [Fact]
    public void ApprovalGrant_With_Same_ProposalHash_Should_Be_Valid()
    {
        var proposal = Proposal("PRODUTOS", 417);
        var grant = Grant(proposal.ProposalHash);

        Assert.True(_policy.IsGrantValidFor(proposal, grant, Now));
    }

    [Fact]
    public void ApprovalGrant_With_Different_ProposalHash_Should_Be_Invalid()
    {
        var original = Proposal("PRODUTOS", 417);
        var changed = Proposal("PRODUTOS", 418);
        var grant = Grant(original.ProposalHash);

        Assert.False(_policy.IsGrantValidFor(changed, grant, Now));
    }

    [Fact]
    public void Expired_ApprovalGrant_Should_Be_Invalid()
    {
        var proposal = Proposal("PRODUTOS", 417);
        var grant = Grant(proposal.ProposalHash) with { ExpiresAt = Now.AddSeconds(-1) };

        Assert.False(_policy.IsGrantValidFor(proposal, grant, Now));
    }

    [Fact]
    public void Revoked_ApprovalGrant_Should_Be_Invalid()
    {
        var proposal = Proposal("PRODUTOS", 417);
        var grant = Grant(proposal.ProposalHash) with { RevokedAt = Now.AddMinutes(-1) };

        Assert.False(_policy.IsGrantValidFor(proposal, grant, Now));
    }

    [Fact]
    public void ProposalHash_Should_Change_When_Material_Context_Changes()
    {
        var original = Proposal("PRODUTOS", 417);
        var changedResource = Proposal("FORNECEDORES", 417);

        Assert.NotEqual(original.ProposalHash, changedResource.ProposalHash);
    }

    private static ApprovalGrant Grant(string proposalHash) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        proposalHash,
        "product-owner",
        Now,
        Now.AddMinutes(30),
        "acao especifica",
        null,
        null);

    private static ActionProposal Proposal(string resource, int expectedRows) => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = Now,
        RequestingAgent = "linx-agent",
        Environment = GovernanceEnvironment.Production,
        System = "SOMA/Linx",
        ResourceType = ActionResourceType.DatabaseTable,
        Resource = resource,
        Operation = ActionOperation.Update,
        Fields = ["ENVIA_ATACADO_INTERNET"],
        FilterSummary = "conjunto validado da planilha",
        ExpectedAffectedRows = expectedRows,
        Purpose = "integracao diaria Linx/WISE",
        DataClassification = DataClassification.Internal,
        ContainsPersonalData = false,
        ContainsSensitivePersonalData = false,
        ContainsSecrets = false,
        Reversibility = ActionReversibility.Reversible,
        RunbookReference = "docs/operations/LinxWiseDailyIntegrationRunbook.md",
        IsRunbookApprovedOperation = true,
        RunbookExpectedAffectedRows = 400,
    };
}

