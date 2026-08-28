#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

/// <summary>
/// Unit coverage for the new PED grade adjustment capability that does NOT require a live database:
/// post-write validation rule registration/resolution, the adapter's RollbackStrategy declaration, its
/// allowed-connection-profile restriction (never linx-production/wise in this PR), and its guard clauses
/// (negative desired quantities, missing row) — all of which are checkable without opening a real SqlConnection.
/// </summary>
public sealed class PedGradeAdjustmentGovernanceTests
{
    [Fact]
    public void Catalog_Resolves_PedGradeAdjustmentRule_For_Update_On_ComprasProduto()
    {
        var catalog = new PostWriteValidationRuleCatalog();

        var rule = catalog.Resolve(ActionOperation.Update, PostWriteValidationRuleCatalog.PedGradeAdjustmentResource);

        Assert.NotNull(rule);
        Assert.Equal("post-write-validation.ped-grade-adjustment.v1", rule!.RuleId);
        Assert.Equal(["PEDIDO", "PRODUTO", "COR_PRODUTO"], rule.BusinessKeyFields);
        Assert.Equal(["CO1", "CO2", "CO3", "CO4", "CO5", "CO6"], rule.FieldsToCompare);
        Assert.DoesNotContain("CO7", rule.FieldsToCompare);
        Assert.DoesNotContain("CO8", rule.FieldsToCompare);
        Assert.DoesNotContain("CO9", rule.FieldsToCompare);
    }

    [Fact]
    public void Catalog_Does_Not_Resolve_PedGradeAdjustmentRule_For_Insert()
    {
        var catalog = new PostWriteValidationRuleCatalog();

        var rule = catalog.Resolve(ActionOperation.Insert, PostWriteValidationRuleCatalog.PedGradeAdjustmentResource);

        Assert.Null(rule);
    }

    [Fact]
    public void Adapter_Declares_RestoreBeforeState_RollbackStrategy()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var request = new PedGradeAdjustmentRequest("000001", "PROD001", "01", 1, 2, 3, 4, 5, 6);
        var adapter = new PedGradeAdjustmentGovernedWriteAdapter(configuration, request);

        Assert.Equal(RollbackStrategy.RestoreBeforeState, adapter.RollbackStrategy);
    }

    [Fact]
    public void Adapter_AllowedConnectionProfiles_Excludes_Production_And_Wise()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var request = new PedGradeAdjustmentRequest("000001", "PROD001", "01", 1, 2, 3, 4, 5, 6);
        var adapter = new PedGradeAdjustmentGovernedWriteAdapter(configuration, request);

        Assert.Contains(WriteVerificationProfileSeeds.LinxDevelopment, adapter.AllowedConnectionProfiles);
        Assert.DoesNotContain(WriteVerificationProfileSeeds.LinxProduction, adapter.AllowedConnectionProfiles);
        Assert.DoesNotContain(WriteVerificationProfileSeeds.Wise, adapter.AllowedConnectionProfiles);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, 0, 0)]
    [InlineData(0, -5, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 0, -1)]
    public async Task ExecuteAsync_Fails_Cleanly_On_Negative_Desired_Quantity_Without_Touching_The_Database(
        int t1, int t2, int t3, int t4, int t5, int t6)
    {
        // No real connection string is configured, so if the guard clause did not short-circuit first, this
        // would throw from LinxConnectionStringResolver instead of returning a typed failure result.
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var request = new PedGradeAdjustmentRequest("000001", "PROD001", "01", t1, t2, t3, t4, t5, t6);
        var adapter = new PedGradeAdjustmentGovernedWriteAdapter(configuration, request);

        var result = await adapter.ExecuteAsync(BuildGatewayRequest(), recoveryPackage: null);

        Assert.False(result.Succeeded);
        Assert.Equal(0, result.RecordsAffected);
        Assert.Contains("NEGATIVE_GRADE_QUANTITY", result.Reasons);
        Assert.NotNull(result.ErrorMessage);
        Assert.DoesNotContain("Server=", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_With_Unconfigured_Connection_Returns_Typed_Failure_Not_An_Unhandled_Exception()
    {
        // Valid (non-negative) quantities but no connection string configured at all: OpenAsync's resolver
        // throws InvalidOperationException, which must surface as a typed WriteExecutionResult failure, never
        // an unhandled exception out of ExecuteAsync, and must never leak a connection string.
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var request = new PedGradeAdjustmentRequest("000001", "PROD001", "01", 1, 2, 3, 4, 5, 6);
        var adapter = new PedGradeAdjustmentGovernedWriteAdapter(configuration, request);

        var result = await adapter.ExecuteAsync(BuildGatewayRequest(), recoveryPackage: null);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
        Assert.DoesNotContain("Server=", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Request_Total_Sums_All_Six_Grade_Positions()
    {
        var request = new PedGradeAdjustmentRequest("000001", "PROD001", "01", 1, 2, 3, 4, 5, 6);

        Assert.Equal(21, request.Total);
    }

    private static ToolGatewayRequest BuildGatewayRequest()
    {
        var context = new StructuredActionContext(
            "REQ-1", "julio.cesar@somagrupo.com.br", GovernanceEnvironment.Development, "SOMA/Linx",
            ActionResourceType.DatabaseTable, PostWriteValidationRuleCatalog.PedGradeAdjustmentResource,
            OperationIntent.Update, [PedGradeAdjustmentGovernedWriteAdapter.CapabilityId],
            ["CO1", "CO2", "CO3", "CO4", "CO5", "CO6"], "PEDIDO=000001|PRODUTO=PROD001|COR_PRODUTO=01", 1,
            "Teste unitario.", DataClassification.Internal, false, false, false, ActionReversibility.Reversible,
            ConnectionProfile: WriteVerificationProfileSeeds.LinxDevelopment);

        var routing = new RoutingEvidence(true, PedGradeAdjustmentGovernedWriteAdapter.OwnerAgentId, [], [], [], []);
        var analysis = new AgentWriteAnalysis(
            PedGradeAdjustmentGovernedWriteAdapter.OwnerAgentId, PedGradeAdjustmentGovernedWriteAdapter.CapabilityId,
            ["CO1", "CO2", "CO3", "CO4", "CO5", "CO6"], "PEDIDO=000001|PRODUTO=PROD001|COR_PRODUTO=01", 1,
            ActionReversibility.Reversible);

        var now = DateTimeOffset.UtcNow;
        var build = new StructuredActionProposalAdapter().Build(context, routing, analysis, now);
        var proposal = build.Proposal!;
        var decision = new PolicyDecision(
            Guid.NewGuid(), proposal.Id, proposal.ProposalHash, RiskClassification.Yellow,
            PolicyDecisionStatus.Allowed, [], now, false, false);
        var grant = new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), proposal.ProposalHash,
            "subject-product-owner-001", now, now.AddMinutes(30), "unit test grant", null, null);

        return new ToolGatewayRequest(
            PedGradeAdjustmentGovernedWriteAdapter.CapabilityId, PedGradeAdjustmentGovernedWriteAdapter.OwnerAgentId,
            true, proposal, decision, grant, [], WriteVerificationProfileSeeds.LinxDevelopment,
            new IdentityPermissionContext("julio.cesar@somagrupo.com.br", true), GovernedExecutionMode.LiveExecution);
    }
}
