#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>
/// Formal Gateway-registered adapter for the B3/Bloco 5A.9 "Gate A" LiveRead capability: a real, non-mutating,
/// pre-registered-dataset bulk read of Linx, streamed straight into a RAW staging table — never SQL supplied
/// by the caller (see <see cref="IReadDatasetCatalog"/>).
///
/// Deliberately NOT registered in the general web/API DI composition (<c>AddGovernedWriteStack</c>): like the
/// existing live-write adapters, real (non-dry-run) execution is reachable only from a dedicated CLI entry
/// point built when Gate B authorizes real execution against SOMA_DESENV — never from a web request pipeline.
/// Only the dry-run/preview path is safe to expose broadly, exactly as for the write side.
/// </summary>
public sealed class LinxDatasetSnapshotReadAdapter(
    IReadDatasetCatalog catalog,
    ISomaLinxDatasetBulkReader bulkReader,
    IDatasetLoadGate loadGate) : IGovernedToolAdapter, IReadExecutionAdapter
{
    public const string Capability = "linx-dataset-snapshot-read";
    public const string OwnerAgent = "linx-database-specialist-agent";

    string IGovernedToolAdapter.Capability => Capability;
    string IGovernedToolAdapter.OwnerAgent => OwnerAgent;

    // Named distinctly from "linx-erp-governed-read" (SomaLinxReadOnlyAdapter, dry-run-only preview of schema
    // discovery/lookups) and "linx-erp-governed-write" (live write) — this is the only profile allowed to
    // execute a REAL bulk read, so it must never be confused with either.
    public IReadOnlyList<string> AllowedConnectionProfiles { get; } = ["linx-erp-governed-live-read"];

    public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var proposal = request.Proposal;
        var preview = new SomaLinxDryRunPreview(
            proposal.System,
            proposal.Environment,
            proposal.Resource,
            proposal.Operation,
            proposal.Fields,
            proposal.FilterSummary,
            proposal.ExpectedAffectedRows,
            proposal.Purpose,
            request.ConnectionProfile,
            request.PolicyDecision.RiskClassification,
            request.PolicyDecision.Status,
            request.ApprovalGrant is null ? "not-required-or-not-present" : "valid-grant-present",
            proposal.Reversibility,
            GovernedExecutionMode.DryRun,
            CredentialResolutionRequired: true,
            IdentityPermissionCheckRequired: true,
            SqlGenerated: false,
            ExternalExecutionPerformed: false);
        return Task.FromResult(preview);
    }

    /// <summary>
    /// Resolves <see cref="ActionProposal.Resource"/> against the fixed dataset catalog and, only if it
    /// matches a registered dataset, delegates the actual streaming to <see cref="ISomaLinxDatasetBulkReader"/>.
    /// An unrecognized resource — including one that looks like a SQL fragment — is never interpreted, only
    /// rejected as an unknown dataset: there is no code path here that turns caller-supplied text into SQL.
    ///
    /// The load mode (<see cref="DatasetLoadKind.Full"/> or <see cref="DatasetLoadKind.Incremental"/>) is
    /// decoded from <see cref="ActionProposal.AdditionalContext"/> (<see cref="DatasetLoadModeContext"/>) —
    /// fail-closed (B3/Bloco 5A, decisão do PO): ausente, malformado ou não reconhecido é um erro de
    /// contrato rejeitado ANTES de qualquer acesso ao Linx, nunca uma escolha implícita de Full. Full é
    /// seguro quanto a perda de alterações, mas continua sendo uma operação real e potencialmente pesada —
    /// um typo/configuração incorreta nunca deve disparar uma carga completa silenciosamente. Incremental
    /// segue adicionalmente gated pelo bootstrap obrigatório do dataset: um dataset cuja baseline Full nunca
    /// foi reconciliada e homologada é rejeitado aqui, também antes de tocar o Linx.
    /// </summary>
    public async Task<ReadExecutionResult> ExecuteAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default)
    {
        if (!catalog.TryGet(request.Proposal.Resource, out var dataset) || dataset is null)
        {
            return new ReadExecutionResult(
                Succeeded: false, RowsRead: 0, RowsWritten: 0, IsolationLevelUsed: "N/A", Duration: TimeSpan.Zero,
                Reasons: ["DATASET_UNKNOWN"],
                ErrorMessage: $"Dataset '{request.Proposal.Resource}' não está registrado no catálogo governado.");
        }

        if (!DatasetLoadModeContext.TryDecode(request.Proposal.AdditionalContext, out var modo))
        {
            return new ReadExecutionResult(
                Succeeded: false, RowsRead: 0, RowsWritten: 0, IsolationLevelUsed: "N/A", Duration: TimeSpan.Zero,
                Reasons: ["LOAD_MODE_INVALID"],
                ErrorMessage: $"AdditionalContext '{request.Proposal.AdditionalContext}' não codifica um DatasetLoadKind válido (Full|Incremental). Execução rejeitada — fail-closed, nunca Full por omissão.");
        }

        DateTimeOffset? watermark = null;
        if (modo == DatasetLoadKind.Incremental)
        {
            var overlap = dataset.Watermark?.OverlapWindow ?? TimeSpan.Zero;
            var autorizacao = await loadGate.AuthorizeIncrementalAsync(dataset.Name, overlap, cancellationToken);
            if (!autorizacao.Permitido)
            {
                return new ReadExecutionResult(
                    Succeeded: false, RowsRead: 0, RowsWritten: 0, IsolationLevelUsed: "N/A", Duration: TimeSpan.Zero,
                    Reasons: ["INCREMENTAL_BLOCKED_BOOTSTRAP_PENDING"],
                    ErrorMessage: $"Dataset '{dataset.Name}': bootstrap Full ainda nao homologado — incremental permanece bloqueado.");
            }

            watermark = autorizacao.WatermarkEfetivo;
        }

        return await bulkReader.StreamAsync(dataset, request.Proposal.Id, modo, watermark, cancellationToken);
    }
}
