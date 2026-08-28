#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>What the caller must state to run a governed PED grade adjustment through the recovery-protected
/// live path.</summary>
public sealed record GovernedPedGradeAdjustmentRequest(
    string RequestId,
    string RequestedBy,
    PedGradeAdjustmentRequest Grade,
    IdentityPermissionContext Identity,
    string ConnectionProfile,
    string Server,
    string Database,
    GovernanceEnvironment Environment = GovernanceEnvironment.Development,
    string Purpose = "Ajustar quantidades de grade (posicoes 1-6) em COMPRAS_PRODUTO conforme classificacao de itens PED.");

/// <summary>
/// Entry point for running a PED grade adjustment as a governed, recovery-protected live write.
///
/// Deliberately NOT wired into any controller — this is backend capability plus test homologation only,
/// same convention as <see cref="GovernedGarantirFornecedorService"/>. The actual production rollout is a
/// separate, explicit future step.
/// </summary>
public sealed class GovernedPedGradeAdjustmentService(
    IConfiguration configuration,
    Func<IWriteExecutionAdapter, GovernedWriteExecutionOrchestrator> orchestratorFactory)
{
    public const string ExecutionName = "ped-grade-adjustment";

    public async Task<GovernedWriteExecutionResult> AjustarAsync(
        GovernedPedGradeAdjustmentRequest request,
        ApprovalGrant? approvalGrant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grade = request.Grade;
        var businessKey = $"PEDIDO={grade.Pedido}|PRODUTO={grade.Produto}|COR_PRODUTO={grade.CorProduto}";
        var businessKeys = new[] { businessKey };

        // The write adapter is bound to THIS request only, then discarded.
        var writeAdapter = new PedGradeAdjustmentGovernedWriteAdapter(configuration, grade, request.ConnectionProfile);
        var orchestrator = orchestratorFactory(writeAdapter);

        var fields = new[] { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" };

        var context = new StructuredActionContext(
            request.RequestId,
            request.RequestedBy,
            request.Environment,
            "SOMA/Linx",
            ActionResourceType.DatabaseTable,
            PostWriteValidationRuleCatalog.PedGradeAdjustmentResource,
            OperationIntent.Update,
            [writeAdapter.Capability],
            fields,
            businessKey,
            1,
            request.Purpose,
            // Purchase-order grade/quantity data is company inventory/financial data, not personal data.
            DataClassification.Internal,
            ContainsPersonalData: false,
            ContainsSensitivePersonalData: false,
            ContainsSecrets: false,
            ActionReversibility.Reversible,
            ConnectionProfile: request.ConnectionProfile);

        var routing = new RoutingEvidence(true, writeAdapter.OwnerAgent, [], [], [], []);
        var analysis = new AgentWriteAnalysis(
            writeAdapter.OwnerAgent, writeAdapter.Capability, fields, businessKey, 1, ActionReversibility.Reversible);

        // Only CO1..CO6 (and the totals directly derived from the six desired values, before any live-only
        // input) are knowable ahead of the transaction. QTDE_ENTREGAR/VALOR_ORIGINAL/VALOR_ENTREGAR ultimately
        // depend on QTDE_ENTREGUE/VALOR_ENTREGUE/CUSTO1 read live inside the transaction, so they are not
        // asserted here — and the registered post-write validation rule only compares CO1..CO6 anyway, per
        // the explicit business requirement.
        var expectedAfter = new[]
        {
            new RecoveryDataSet(PostWriteValidationRuleCatalog.PedGradeAdjustmentResource,
            [
                new Dictionary<string, string?>
                {
                    ["PEDIDO"] = grade.Pedido,
                    ["PRODUTO"] = grade.Produto,
                    ["COR_PRODUTO"] = grade.CorProduto,
                    ["CO1"] = grade.Tam1.ToString(),
                    ["CO2"] = grade.Tam2.ToString(),
                    ["CO3"] = grade.Tam3.ToString(),
                    ["CO4"] = grade.Tam4.ToString(),
                    ["CO5"] = grade.Tam5.ToString(),
                    ["CO6"] = grade.Tam6.ToString(),
                },
            ]),
        };

        return await orchestrator.ExecuteAsync(
            new GovernedWriteExecutionRequest(
                context, routing, analysis, request.Identity, ExecutionName,
                request.ConnectionProfile, request.Server, request.Database,
                businessKeys, expectedAfter,
                $"Ajuste de grade PED {grade.Pedido}/{grade.Produto}/{grade.CorProduto} para totais de classificacao.",
                ["LX_MOVIMENTA_COMPRAS_PA", "LX_RECALCULO_RESERVA_MATERIAIS"],
                // This adapter only ever updates an existing row; it never inserts, so a missing before-state
                // is a real capture failure here, never an expected "does not exist yet" case.
                AllowsMissingBeforeState: false),
            approvalGrant,
            writeAdapter,
            cancellationToken);
    }
}
