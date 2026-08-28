using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>What the caller must state to run a governed "garantir fornecedor" through the recovery-protected
/// live path.</summary>
public sealed record GovernedGarantirFornecedorRequest(
    string RequestId,
    string RequestedBy,
    GarantirFornecedorErpRequest Erp,
    IdentityPermissionContext Identity,
    string ConnectionProfile,
    string Server,
    string Database,
    GovernanceEnvironment Environment = GovernanceEnvironment.Development,
    string Purpose = "Garantir cadastro de fornecedor no ERP a partir do CNPJ.");

public sealed record GovernedGarantirFornecedorResult(
    GovernedWriteExecutionResult Execution,
    GarantirFornecedorErpResultado? Erp);

/// <summary>
/// Entry point for running "garantir fornecedor" as a governed, recovery-protected live write.
///
/// It is deliberately NOT wired into any controller or handler yet: the existing ungoverned path through
/// <see cref="IGarantirFornecedorErpAdapter"/> is untouched and still serves production traffic. This class
/// exists so the governed path can be exercised and validated in isolation first. Switching the real callers
/// over is a separate, explicit decision.
/// </summary>
public sealed class GovernedGarantirFornecedorService(
    IGarantirFornecedorErpAdapter erpAdapter,
    ISnapshotCapableAdapter snapshotSource,
    Func<IWriteExecutionAdapter, GovernedWriteExecutionOrchestrator> orchestratorFactory)
{
    public const string ExecutionName = "garantir-fornecedor";

    public async Task<GovernedGarantirFornecedorResult> GarantirAsync(
        GovernedGarantirFornecedorRequest request,
        ApprovalGrant? approvalGrant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documento = SomaGarantirFornecedorErpAdapter.ExtrairCnpjDaChaveDeNegocio(request.Erp.DocumentoFiscal);
        var businessKeys = new[] { $"CGC_CPF={documento}" };

        // The write adapter is bound to THIS request only, then discarded.
        var writeAdapter = new GarantirFornecedorGovernedWriteAdapter(erpAdapter, snapshotSource, request.Erp);
        var orchestrator = orchestratorFactory(writeAdapter);

        var context = new StructuredActionContext(
            request.RequestId,
            request.RequestedBy,
            request.Environment,
            "SOMA/Linx",
            ActionResourceType.DatabaseTable,
            PostWriteValidationRuleCatalog.FornecedoresResource,
            OperationIntent.Update,
            [writeAdapter.Capability],
            ["FORNECEDOR", "CGC_CPF", "INATIVO"],
            $"CGC_CPF={documento}",
            1,
            request.Purpose,
            // A supplier registration is company data, not personal data; the CNPJ is a public company
            // identifier. Classification is stated explicitly rather than left Unknown, which the policy
            // engine would otherwise escalate.
            DataClassification.Internal,
            ContainsPersonalData: false,
            ContainsSensitivePersonalData: false,
            ContainsSecrets: false,
            ActionReversibility.Reversible,
            ConnectionProfile: request.ConnectionProfile);

        var routing = new RoutingEvidence(true, writeAdapter.OwnerAgent, [], [], [], []);
        var analysis = new AgentWriteAnalysis(
            writeAdapter.OwnerAgent, writeAdapter.Capability,
            ["FORNECEDOR", "CGC_CPF", "INATIVO"], $"CGC_CPF={documento}", 1, ActionReversibility.Reversible);

        var expectedAfter = new[]
        {
            new RecoveryDataSet(PostWriteValidationRuleCatalog.FornecedoresResource,
            [
                new Dictionary<string, string?>
                {
                    ["CGC_CPF"] = documento,
                    ["INATIVO"] = request.Erp.Ativo ? "0" : "1",
                },
            ]),
        };

        var execution = await orchestrator.ExecuteAsync(
            new GovernedWriteExecutionRequest(
                context, routing, analysis, request.Identity, ExecutionName,
                request.ConnectionProfile, request.Server, request.Database,
                businessKeys, expectedAfter,
                $"Garantir fornecedor {documento} na BU {request.Erp.BusinessUnit}.",
                ["LX_SEQUENCIAL"],
                // "Garantir" is insert-or-update by CNPJ, decided by the ERP adapter itself at execution time
                // (CREATE / ADD_ROLE / UPDATE) — an empty before-state snapshot means the supplier does not
                // exist yet, which is an expected input, not a capture failure.
                AllowsMissingBeforeState: true),
            approvalGrant,
            writeAdapter,
            cancellationToken);

        return new(execution, writeAdapter.Resultado);
    }
}
