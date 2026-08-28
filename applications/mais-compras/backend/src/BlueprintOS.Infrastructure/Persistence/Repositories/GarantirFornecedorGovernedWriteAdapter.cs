using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Per-execution bridge between the Tool Gateway's <see cref="IWriteExecutionAdapter"/> contract and the
/// business-level "garantir fornecedor" operation.
///
/// Why a separate type instead of putting ExecuteAsync straight onto SomaGarantirFornecedorErpAdapter:
/// <see cref="IWriteExecutionAdapter.ExecuteAsync"/> receives only the governance request, which carries no
/// business payload (a <see cref="GarantirFornecedorErpRequest"/> holds a CNPJ, a name, an address — data that
/// must not be smuggled through an ActionProposal field that feeds the proposal hash). A DI-shared adapter
/// also cannot hold per-call mutable state safely. So the payload is bound HERE, in an object created for one
/// execution and thrown away after it, while every line of SQL, the transaction, the UPDLOCK/HOLDLOCK
/// reconsult and the commit/rollback stay exactly where they were, in the ERP adapter.
/// </summary>
public sealed class GarantirFornecedorGovernedWriteAdapter(
    IGarantirFornecedorErpAdapter erpAdapter,
    ISnapshotCapableAdapter snapshotSource,
    GarantirFornecedorErpRequest erpRequest,
    string capability = SomaGarantirFornecedorErpAdapter.CapabilityId,
    string ownerAgent = SomaGarantirFornecedorErpAdapter.OwnerAgentId,
    IReadOnlyList<string>? allowedConnectionProfiles = null)
    : IWriteExecutionAdapter, ISnapshotCapableAdapter
{
    private readonly IReadOnlyList<string> _allowedConnectionProfiles =
        allowedConnectionProfiles ?? [WriteVerificationProfileSeeds.LinxDevelopment];

    public string Capability { get; } = capability;

    public string OwnerAgent { get; } = ownerAgent;

    public IReadOnlyList<string> AllowedConnectionProfiles => _allowedConnectionProfiles;

    /// <summary>
    /// Explicitly <see cref="RollbackStrategy.NotSupported"/> — a business rule of "garantir fornecedor", not an
    /// infrastructure limitation of the generic recovery framework. <see cref="SomaGarantirFornecedorErpAdapter"/>
    /// is deliberately non-destructive ("nunca destrói papéis existentes de CADASTRO_CLI_FOR") and offers no
    /// delete; a generic rollback restoring a CREATE to "did not exist" needs exactly that, so this capability
    /// cannot honor a profile that requires rollback support. It must fail BEFORE the write (a recorded
    /// <c>RollbackCapabilityGap</c>, via <c>GovernedWriteExecutionOrchestrator</c>), never at rollback time. Do
    /// not change this to unblock a test — the fix, if this capability ever needs rollback, is a product
    /// decision about what "undo garantir" means for a real supplier record, not a delete added here.
    /// </summary>
    public RollbackStrategy RollbackStrategy => RollbackStrategy.NotSupported;

    /// <summary>The result the ERP adapter reported, available to the caller after a successful execution.</summary>
    public GarantirFornecedorErpResultado? Resultado { get; private set; }

    public Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default) =>
        snapshotSource.CaptureSnapshotAsync(businessKeys, cancellationToken);

    public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(new SomaLinxDryRunPreview(
            request.Proposal.System, request.Proposal.Environment, request.Proposal.Resource, request.Proposal.Operation,
            request.Proposal.Fields, request.Proposal.FilterSummary, request.Proposal.ExpectedAffectedRows,
            request.Proposal.Purpose, request.ConnectionProfile, request.PolicyDecision.RiskClassification,
            request.PolicyDecision.Status, request.ApprovalGrant is null ? "none" : "granted",
            request.Proposal.Reversibility, request.ExecutionMode,
            CredentialResolutionRequired: true, IdentityPermissionCheckRequired: true,
            SqlGenerated: false, ExternalExecutionPerformed: false));
    }

    public async Task<WriteExecutionResult> ExecuteAsync(
        ToolGatewayRequest request,
        RecoveryPackageReceipt? recoveryPackage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Resultado = await erpAdapter.GarantirAsync(erpRequest, cancellationToken);

            // The after-state is re-read by the orchestrator from the snapshot source; what the adapter reports
            // here is the operation it took, not a claim about the stored state.
            return new WriteExecutionResult(
                Succeeded: true,
                RecordsAffected: 1,
                AfterData: [],
                Reasons: ["LIVE_EXECUTION_COMPLETED", $"ERP_OPERATION_{Resultado.Operacao}"],
                ErrorMessage: null,
                ExternalIdentifier: Resultado.IdentificadorExterno);
        }
        catch (ErpFornecedorEscritaException ex)
        {
            // The adapter's typed failure taxonomy is preserved; no SQL, connection string or stack trace
            // crosses this boundary, exactly as before.
            return new WriteExecutionResult(false, 0, [], ["WRITE_FAILED", $"ERP_ERROR_{ex.Tipo}"], ex.Message);
        }
    }
}
