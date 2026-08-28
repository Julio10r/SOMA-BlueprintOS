#pragma warning disable CS1591

using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Application.Governance;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.Api.Governance;

/// <summary>
/// The `governed-execute` CLI command family — the real, persistent LIVE-execution path that closes the
/// Capability Gap `governed-plan` (<see cref="GovernedPlanCliHandler"/>) deliberately does NOT close.
/// `governed-plan` stays exactly as it is: an offline, in-memory, always-BLOCKED bridge. This is a SEPARATE
/// process boundary that:
///
///   - persists EVERY governance artifact (ActionProposal build outcome, PolicyDecision, ApprovalRequest,
///     ApprovalGrant, RecoveryPackage, PostWriteValidationReport, WriteExecutionAuditRecord) under the SAME
///     file-based stores <c>AddGovernedWriteStack</c> wires into the real host — <c>runtime/governance/</c>
///     and <c>runtime/backups/</c>, rooted at <c>Governance:RuntimeRoot</c>/<c>Governance:BackupsRoot</c> when
///     configured, else <c>{CurrentDirectory}/runtime/governance</c> / <c>{CurrentDirectory}/runtime/backups</c>;
///   - NEVER synthesizes an <see cref="ApprovalGrant"/> in code — `run` and `rollback` always look one up by
///     id from <see cref="IApprovalStore.GetGrantAsync"/>, and abort with a clear reason when it is missing,
///     expired, or revoked. Hash-vs-proposal match is enforced once, at its one real gate
///     (<c>ToolGateway.Validate</c>/<c>ApprovalPolicy.IsGrantValidFor</c>, and for rollback
///     <c>RollbackOrchestrator.ExecuteAsync</c>'s own identical check) — never re-implemented here a second,
///     divergent way;
///   - registers the one concrete write adapter this command knows how to execute,
///     <see cref="PedGradeAdjustmentGovernedWriteAdapter"/>, built fresh per invocation from the payload's own
///     data (it is not singleton-friendly, so it is never registered in <c>AddGovernedWriteStack</c>'s
///     process-lifetime DI container — see that file's remarks);
///   - validates connection profile/server/database via <see cref="LinxConnectionStringResolver"/> BEFORE
///     doing anything else — never falling back to raw SQL if any governance precondition is unmet;
///   - never triggers a rollback on its own: `rollback-plan`/`rollback` are always a separate, explicit,
///     human-confirmed invocation, exactly like <see cref="RollbackOrchestrator"/> already requires.
///
/// Five sub-modes, selected by <c>args[1]</c>, each reading one JSON payload from stdin and writing one JSON
/// result to stdout (same shape/discipline as `governed-plan`):
///
///   propose       — GovernedWriteStack.PrepareAsync against the file-based stores. Persists the
///                   ApprovalRequest when policy requires one. Executes nothing.
///   approve       — looks up a pending ApprovalRequest by id, calls GovernedWriteStack.GrantAsync, persists
///                   the real ApprovalGrant.
///   run           — looks up the ApprovalGrant by id (if supplied), builds
///                   GovernedWriteExecutionOrchestrator with every file-based store plus a ToolGateway
///                   carrying ONLY PedGradeAdjustmentGovernedWriteAdapter, and calls ExecuteAsync. The only
///                   mode that ever performs a live write.
///   rollback-plan — RollbackOrchestrator.Discover + Analyze for one already-selected execution id. On
///                   success, previews the equivalent rollback proposal (RollbackOrchestrator.PreviewEquivalentProposal)
///                   and — when the policy requires one — persists a real ApprovalRequest for it, so it can be
///                   approved with the SAME `approve` mode as a forward write. Writes nothing else.
///   rollback      — re-Discovers/Analyzes the same execution (so any concurrent change since rollback-plan is
///                   caught fresh) and, given the SAME RequestedBy/Justification used for rollback-plan (the
///                   equivalent proposal's hash depends on them) plus an ApprovalGrantId looked up by id, calls
///                   RollbackOrchestrator.ExecuteAsync. Always a separate, manual command; never invoked by `run`.
/// </summary>
public static class GovernedExecuteCliHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<int> RunAsync(string[] args, TextReader input, TextWriter output, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var mode = args.Length > 1 ? args[1] : null;
        var governanceRoot = ResolveGovernanceRoot(configuration);
        var backupsRoot = ResolveBackupsRoot(configuration);

        try
        {
            return mode switch
            {
                "propose" => await ProposeAsync(input, output, governanceRoot, cancellationToken),
                "approve" => await ApproveAsync(input, output, governanceRoot, cancellationToken),
                "run" => await RunExecuteAsync(input, output, configuration, governanceRoot, backupsRoot, cancellationToken),
                "rollback-plan" => await RollbackPlanAsync(input, output, configuration, governanceRoot, backupsRoot, cancellationToken),
                "rollback" => await RollbackAsync(input, output, configuration, governanceRoot, backupsRoot, cancellationToken),
                _ => await WriteErrorAsync(output, "UNKNOWN_MODE", $"Modo desconhecido: '{mode}'. Use propose, approve, run, rollback-plan ou rollback."),
            };
        }
        catch (JsonException ex)
        {
            return await WriteErrorAsync(output, "INVALID_JSON_PAYLOAD", ex.Message);
        }
    }

    // =========================================================================================================
    // propose
    // =========================================================================================================

    private static async Task<int> ProposeAsync(TextReader input, TextWriter output, string governanceRoot, CancellationToken cancellationToken)
    {
        var payload = await ReadAsync<GovernedPlanPayload>(input, cancellationToken);
        if (payload is null) return await WriteErrorAsync(output, "EMPTY_PAYLOAD");

        var database = GovernanceDatabaseResolver.ResolveForConnectionProfile(payload.ConnectionProfile);
        var writeStack = BuildWriteStack(ForDatabase(governanceRoot, database));
        var bridge = new GovernedPlanBridge(writeStack);

        GovernedWritePreparation preparation;
        try
        {
            preparation = await bridge.PrepareAsync(payload, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return await WriteErrorAsync(output, "INVALID_ENUM_VALUE", ex.Message);
        }

        await WriteAsync(output, new
        {
            requestId = payload.RequestId,
            proposalBuild = new
            {
                succeeded = preparation.ProposalBuild.Succeeded,
                contextGaps = preparation.ProposalBuild.ContextGaps,
                proposalId = preparation.ProposalBuild.Proposal?.Id,
                proposalHash = preparation.ProposalBuild.Proposal?.ProposalHash,
            },
            policyDecision = preparation.PolicyDecision is null ? null : new
            {
                status = preparation.PolicyDecision.Status.ToString(),
                riskClassification = preparation.PolicyDecision.RiskClassification.ToString(),
                reasons = preparation.PolicyDecision.Reasons,
            },
            approvalRequest = preparation.ApprovalRequest is null ? null : new
            {
                id = preparation.ApprovalRequest.Id,
                status = preparation.ApprovalRequest.Status.ToString(),
                expiresAt = preparation.ApprovalRequest.ExpiresAt,
            },
            persisted = true,
            governanceRoot,
            nextStep = preparation.ApprovalRequest is not null
                ? "governed-execute approve com este approvalRequestId, depois governed-execute run com o approvalGrantId resultante."
                : preparation.ProposalBuild.Succeeded
                    ? "governed-execute run (nenhuma aprovacao humana exigida pela politica para este proposal)."
                    : "Resolva os context gaps antes de propor novamente.",
        });
        return 0;
    }

    // =========================================================================================================
    // approve — shared by the forward-write path (propose) and the rollback path (rollback-plan): both persist
    // an ApprovalRequest the same way, so both are approved the same way.
    // =========================================================================================================

    private sealed record ApprovePayload(Guid ApprovalRequestId, string ApprovedBy, DateTimeOffset? ExpiresAt, string Scope, string? Notes);

    private static async Task<int> ApproveAsync(TextReader input, TextWriter output, string governanceRoot, CancellationToken cancellationToken)
    {
        var payload = await ReadAsync<ApprovePayload>(input, cancellationToken);
        if (payload is null) return await WriteErrorAsync(output, "EMPTY_PAYLOAD");
        if (string.IsNullOrWhiteSpace(payload.ApprovedBy)) return await WriteErrorAsync(output, "APPROVED_BY_REQUIRED");

        var located = await FindApprovalStoreForRequestAsync(governanceRoot, payload.ApprovalRequestId, cancellationToken);
        if (located is null) return await WriteErrorAsync(output, "APPROVAL_REQUEST_NOT_FOUND", $"Nenhum ApprovalRequest persistido com id {payload.ApprovalRequestId}.");
        var (approvals, databaseGovernanceRoot) = located.Value;
        var writeStack = BuildWriteStack(databaseGovernanceRoot);
        var request = await approvals.GetRequestAsync(payload.ApprovalRequestId, cancellationToken);
        if (request is null) return await WriteErrorAsync(output, "APPROVAL_REQUEST_NOT_FOUND", $"Nenhum ApprovalRequest persistido com id {payload.ApprovalRequestId}.");
        if (request.Status != ApprovalRequestStatus.Pending) return await WriteErrorAsync(output, "APPROVAL_REQUEST_NOT_PENDING", $"ApprovalRequest {payload.ApprovalRequestId} esta com status {request.Status}, nao Pending.");

        var expiresAt = payload.ExpiresAt ?? request.ExpiresAt;
        ApprovalGrant grant;
        try
        {
            grant = await writeStack.GrantAsync(request, payload.ApprovedBy, expiresAt, payload.Scope, payload.Notes, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return await WriteErrorAsync(output, "INVALID_APPROVAL_INPUT", ex.Message);
        }

        await WriteAsync(output, new
        {
            approvalRequestId = request.Id,
            approvalGrant = new
            {
                id = grant.Id,
                approvedBy = grant.ApprovedBy,
                approvedAt = grant.ApprovedAt,
                expiresAt = grant.ExpiresAt,
                scope = grant.Scope,
                proposalHash = grant.ProposalHash,
            },
            persisted = true,
            governanceRoot,
            nextStep = "governed-execute run (ou rollback) com approvalGrantId = " + grant.Id,
        });
        return 0;
    }

    // =========================================================================================================
    // run — the only mode that ever performs a live write.
    // =========================================================================================================

    public sealed record PedGradeAdjustmentRunPayload(string Pedido, string Produto, string CorProduto, int Tam1, int Tam2, int Tam3, int Tam4, int Tam5, int Tam6);

    public sealed record GovernedExecuteRunPayload(
        GovernedPlanPayload Context,
        string ExecutionName,
        string ConnectionProfile,
        string Server,
        string Database,
        IReadOnlyList<string> BusinessKeys,
        IReadOnlyList<string> ProceduresInvoked,
        string OriginalRequestSummary,
        bool AllowsMissingBeforeState,
        Guid? ApprovalGrantId,
        PedGradeAdjustmentRunPayload PedGradeAdjustment);

    private static async Task<int> RunExecuteAsync(
        TextReader input, TextWriter output, IConfiguration configuration, string governanceRoot, string backupsRoot, CancellationToken cancellationToken)
    {
        var payload = await ReadAsync<GovernedExecuteRunPayload>(input, cancellationToken);
        if (payload is null) return await WriteErrorAsync(output, "EMPTY_PAYLOAD");
        if (payload.Context is null) return await WriteErrorAsync(output, "CONTEXT_REQUIRED");
        if (payload.PedGradeAdjustment is null) return await WriteErrorAsync(output, "PED_GRADE_ADJUSTMENT_PAYLOAD_REQUIRED");

        // This CLI knows how to execute exactly one concrete capability. It never falls back to any other
        // adapter or to raw SQL for anything else — a capability it does not recognize is a hard abort.
        if (!string.Equals(payload.Context.Capability, PedGradeAdjustmentGovernedWriteAdapter.CapabilityId, StringComparison.Ordinal))
        {
            return await WriteErrorAsync(output, "CAPABILITY_NOT_SUPPORTED_BY_THIS_HOST",
                $"governed-execute run so executa a capability '{PedGradeAdjustmentGovernedWriteAdapter.CapabilityId}'. Capability recebida: '{payload.Context.Capability}'.");
        }

        // Connection profile / server / database validated BEFORE anything else — including before a proposal
        // is even built. LinxConnectionStringResolver.Resolve is the single source of truth for this check;
        // it is invoked here, never re-implemented.
        var profileResolution = TryResolveLinxProfile(payload.ConnectionProfile, payload.Server, payload.Database, configuration);
        if (profileResolution.Error is not null) return await WriteErrorAsync(output, profileResolution.Error, profileResolution.Message);

        // Governance for a live write is bucketed under the REAL, validated Database (never the connection
        // profile name) — TryResolveLinxProfile above already proved payload.Database matches the resolved
        // profile's ExpectedDatabase, which itself only reaches here after LinxConnectionStringResolver.Resolve
        // validated the real connection string against it.
        var databaseGovernanceRoot = ForDatabase(governanceRoot, payload.Database);

        // Approval grant — ALWAYS looked up by id from the persisted, file-based store. Never synthesized.
        var approvals = new FileApprovalStore(databaseGovernanceRoot);
        var grantResolution = await ResolveApprovalGrantAsync(approvals, payload.ApprovalGrantId, cancellationToken);
        if (grantResolution.Error is not null) return await WriteErrorAsync(output, grantResolution.Error, grantResolution.Message);

        var (context, routing, analysis) = GovernedPlanBridge.BuildTriple(payload.Context);

        var pedRequest = new PedGradeAdjustmentRequest(
            payload.PedGradeAdjustment.Pedido, payload.PedGradeAdjustment.Produto, payload.PedGradeAdjustment.CorProduto,
            payload.PedGradeAdjustment.Tam1, payload.PedGradeAdjustment.Tam2, payload.PedGradeAdjustment.Tam3,
            payload.PedGradeAdjustment.Tam4, payload.PedGradeAdjustment.Tam5, payload.PedGradeAdjustment.Tam6);
        var adapter = new PedGradeAdjustmentGovernedWriteAdapter(configuration, pedRequest, payload.ConnectionProfile);

        var executionRequest = new GovernedWriteExecutionRequest(
            context, routing, analysis,
            new IdentityPermissionContext(payload.Context.RequestedBy, HasEffectivePermission: true),
            payload.ExecutionName, payload.ConnectionProfile, payload.Server, payload.Database,
            payload.BusinessKeys, [BuildExpectedAfter(payload.PedGradeAdjustment)], payload.OriginalRequestSummary, payload.ProceduresInvoked,
            payload.AllowsMissingBeforeState);

        var orchestrator = BuildExecutionOrchestrator(databaseGovernanceRoot, backupsRoot, adapter);
        var result = await orchestrator.ExecuteAsync(executionRequest, grantResolution.Grant, adapter, cancellationToken);

        await WriteAsync(output, new
        {
            executionId = result.ExecutionId,
            status = result.Status.ToString(),
            reasons = result.Reasons,
            recoveryPackage = result.RecoveryPackage is null ? null : new
            {
                packagePath = result.RecoveryPackage.PackagePath,
                manifestChecksumSha256 = result.RecoveryPackage.ManifestChecksumSha256,
                expiresAt = result.RecoveryPackage.ExpiresAt,
            },
            validation = result.Validation is null ? null : new
            {
                ruleId = result.Validation.RuleId,
                passed = result.Validation.Passed,
                recordsValidated = result.Validation.RecordsValidated,
                recordsWithErrors = result.Validation.RecordsWithErrors,
                mismatches = result.Validation.Mismatches,
            },
            knowledgeGap = result.KnowledgeGap,
            rollbackGap = result.RollbackGap,
            persisted = true,
            governanceRoot,
            backupsRoot,
        });
        return 0;
    }

    private static RecoveryDataSet BuildExpectedAfter(PedGradeAdjustmentRunPayload ped) => new(
        PedGradeAdjustmentGovernedWriteAdapter.TableName,
        [
            new Dictionary<string, string?>
            {
                ["PEDIDO"] = ped.Pedido,
                ["PRODUTO"] = ped.Produto,
                ["COR_PRODUTO"] = ped.CorProduto,
                ["CO1"] = ped.Tam1.ToString(),
                ["CO2"] = ped.Tam2.ToString(),
                ["CO3"] = ped.Tam3.ToString(),
                ["CO4"] = ped.Tam4.ToString(),
                ["CO5"] = ped.Tam5.ToString(),
                ["CO6"] = ped.Tam6.ToString(),
            },
        ]);

    // =========================================================================================================
    // rollback-plan — Discover + Analyze + preview the equivalent proposal + (if required) persist a real
    // ApprovalRequest for it. Writes nothing to the target database.
    // =========================================================================================================

    public sealed record RollbackPlanPayload(Guid ExecutionId, string RequestedBy, string Justification, string ConnectionProfile, PedGradeAdjustmentRunPayload SnapshotKey);

    private static async Task<int> RollbackPlanAsync(
        TextReader input, TextWriter output, IConfiguration configuration, string governanceRoot, string backupsRoot, CancellationToken cancellationToken)
    {
        var payload = await ReadAsync<RollbackPlanPayload>(input, cancellationToken);
        if (payload is null) return await WriteErrorAsync(output, "EMPTY_PAYLOAD");

        // rollback-plan never executes, so the Tool Gateway's write adapter is never actually invoked here —
        // an all-zero placeholder is fine for discovery/analysis (a read-only phase via ISnapshotCapableAdapter).
        var placeholderRestore = new PedGradeAdjustmentRequest(payload.SnapshotKey.Pedido, payload.SnapshotKey.Produto, payload.SnapshotKey.CorProduto, 0, 0, 0, 0, 0, 0);
        var (rollbackOrchestrator, approvals, _) = BuildRollbackOrchestrator(ForDatabase(governanceRoot, GovernanceDatabaseResolver.ResolveForConnectionProfile(payload.ConnectionProfile)), backupsRoot, configuration, payload.ConnectionProfile, placeholderRestore);
        var snapshotAdapter = BuildSnapshotOnlyAdapter(configuration, payload.ConnectionProfile, payload.SnapshotKey);

        var discovery = await rollbackOrchestrator.DiscoverAsync(new RecoveryIndexQuery { ExecutionId = payload.ExecutionId }, cancellationToken);
        if (discovery.Status != RollbackDiscoveryStatus.SingleCandidate)
        {
            await WriteAsync(output, new { status = discovery.Status.ToString(), reasons = discovery.Reasons, candidates = discovery.Candidates.Count });
            return 0;
        }

        var analysis = await rollbackOrchestrator.AnalyzeAsync(payload.ExecutionId, snapshotAdapter, cancellationToken);
        if (analysis.Status != RollbackAnalysisStatus.ReadyForConfirmation || analysis.ConfirmationHandle is null)
        {
            await WriteAsync(output, new { status = analysis.Status.ToString(), reasons = analysis.Reasons, summary = analysis.Summary });
            return 0;
        }

        var now = SaoPauloTimeProvider.Instance.GetUtcNow();
        var confirmation = new RollbackConfirmation(analysis.ExecutionId, analysis.ConfirmationHandle, payload.RequestedBy, payload.Justification, now);
        var equivalent = RollbackOrchestrator.PreviewEquivalentProposal(analysis, confirmation, now);
        var decision = new AIGovernancePolicyEngine().Evaluate(equivalent, now);

        Guid? approvalRequestId = null;
        if (decision.Status == PolicyDecisionStatus.RequiresApproval)
        {
            var request = new ApprovalRequest(Guid.NewGuid(), equivalent.Id, equivalent.ProposalHash, decision.RiskClassification,
                $"Rollback da execucao {payload.ExecutionId}: {payload.Justification}", "authorized-product-owner", now, now.AddHours(1), ApprovalRequestStatus.Pending);
            await approvals.SaveRequestAsync(request, cancellationToken);
            approvalRequestId = request.Id;
        }

        await WriteAsync(output, new
        {
            status = analysis.Status.ToString(),
            confirmationHandle = analysis.ConfirmationHandle,
            summary = analysis.Summary,
            equivalentProposal = new
            {
                operation = equivalent.Operation.ToString(),
                resource = equivalent.Resource,
                proposalHash = equivalent.ProposalHash,
            },
            policyDecision = new { status = decision.Status.ToString(), riskClassification = decision.RiskClassification.ToString(), reasons = decision.Reasons },
            approvalRequestId,
            persisted = approvalRequestId is not null,
            governanceRoot,
            nextStep = approvalRequestId is not null
                ? $"governed-execute approve com approvalRequestId={approvalRequestId}, depois governed-execute rollback com o approvalGrantId resultante e o MESMO RequestedBy/Justification usados aqui."
                : "governed-execute rollback (nenhuma aprovacao humana exigida pela politica para este rollback) com o MESMO RequestedBy/Justification usados aqui.",
        });
        return 0;
    }

    // =========================================================================================================
    // rollback — always a separate, manual, explicitly-confirmed invocation. Never triggered by `run`.
    // =========================================================================================================

    public sealed record RollbackPayload(Guid ExecutionId, string RequestedBy, string Justification, Guid? ApprovalGrantId, string ConnectionProfile, PedGradeAdjustmentRunPayload SnapshotKey);

    private static async Task<int> RollbackAsync(
        TextReader input, TextWriter output, IConfiguration configuration, string governanceRoot, string backupsRoot, CancellationToken cancellationToken)
    {
        var payload = await ReadAsync<RollbackPayload>(input, cancellationToken);
        if (payload is null) return await WriteErrorAsync(output, "EMPTY_PAYLOAD");

        // Phase 1 — discovery/analysis only. The write adapter is never invoked in this phase (it is read-only
        // via ISnapshotCapableAdapter), so an all-zero placeholder is fine here.
        var placeholderRestore = new PedGradeAdjustmentRequest(payload.SnapshotKey.Pedido, payload.SnapshotKey.Produto, payload.SnapshotKey.CorProduto, 0, 0, 0, 0, 0, 0);
        var (discoveryOrchestrator, approvals, _) = BuildRollbackOrchestrator(governanceRoot, backupsRoot, configuration, payload.ConnectionProfile, placeholderRestore);
        var snapshotAdapter = BuildSnapshotOnlyAdapter(configuration, payload.ConnectionProfile, payload.SnapshotKey);

        var discovery = await discoveryOrchestrator.DiscoverAsync(new RecoveryIndexQuery { ExecutionId = payload.ExecutionId }, cancellationToken);
        if (discovery.Status != RollbackDiscoveryStatus.SingleCandidate)
        {
            await WriteAsync(output, new { status = discovery.Status.ToString(), reasons = discovery.Reasons, candidates = discovery.Candidates.Count });
            return 0;
        }

        var analysis = await discoveryOrchestrator.AnalyzeAsync(payload.ExecutionId, snapshotAdapter, cancellationToken);
        if (analysis.Status != RollbackAnalysisStatus.ReadyForConfirmation || analysis.ConfirmationHandle is null)
        {
            await WriteAsync(output, new { status = analysis.Status.ToString(), reasons = analysis.Reasons, summary = analysis.Summary });
            return 0;
        }

        // Phase 2 — the REAL restore quantities, extracted from the Recovery Package's own recorded
        // before-state (never asserted by the caller), feed a freshly-built write adapter used ONLY for the
        // actual execution below. Building the orchestrator/gateway around the wrong (e.g. all-zero) adapter
        // here is exactly the bug the SOMA_DESENV homologation run caught — see BuildRollbackOrchestrator's
        // remarks.
        var restoreRequest = ExtractRestoreRequest(analysis, payload.SnapshotKey);
        var (rollbackOrchestrator, _, writeAdapter) = BuildRollbackOrchestrator(ForDatabase(governanceRoot, GovernanceDatabaseResolver.ResolveForConnectionProfile(payload.ConnectionProfile)), backupsRoot, configuration, payload.ConnectionProfile, restoreRequest);

        var confirmation = new RollbackConfirmation(analysis.ExecutionId, analysis.ConfirmationHandle, payload.RequestedBy, payload.Justification, SaoPauloTimeProvider.Instance.GetUtcNow());

        var grantId = payload.ApprovalGrantId;
        var result = await rollbackOrchestrator.ExecuteAsync(
            analysis, confirmation, snapshotAdapter, writeAdapter,
            async (proposal, decision, request, ct) =>
                grantId is null ? null : await approvals.GetGrantAsync(grantId.Value, ct),
            cancellationToken);

        await WriteAsync(output, new
        {
            rollbackExecutionId = result.RollbackExecutionId,
            originalExecutionId = result.OriginalExecutionId,
            status = result.Status.ToString(),
            reasons = result.Reasons,
            persisted = true,
            governanceRoot,
            backupsRoot,
        });
        return 0;
    }

    // =========================================================================================================
    // Shared wiring — every store below is the same File* implementation AddGovernedWriteStack registers,
    // rooted at the same configurable directory. No In-Memory store is ever used on this path.
    // =========================================================================================================

    private static GovernedWriteStack BuildWriteStack(string governanceRoot)
    {
        IGovernanceAuditStore audit = new FileGovernanceAuditStore(governanceRoot);
        IApprovalStore approvals = new FileApprovalStore(governanceRoot);
        var gateway = new ToolGateway([], new ApprovalPolicy(), audit, SaoPauloTimeProvider.Instance);
        return new GovernedWriteStack(new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, audit, gateway, SaoPauloTimeProvider.Instance);
    }

    private static GovernedWriteExecutionOrchestrator BuildExecutionOrchestrator(string governanceRoot, string backupsRoot, PedGradeAdjustmentGovernedWriteAdapter adapter)
    {
        var audit = new FileGovernanceAuditStore(governanceRoot);
        var approvals = new FileApprovalStore(governanceRoot);
        var profileStore = new FileWriteVerificationProfileStore(governanceRoot);
        var index = new FileRecoveryIndexStore(governanceRoot);
        var writeAudit = new FileWriteExecutionAuditStore(governanceRoot);
        var knowledgeGapStore = new FileWriteValidationKnowledgeGapStore(governanceRoot);
        var rollbackCapabilityGapStore = new FileRollbackCapabilityGapStore(governanceRoot);
        var recoveryWriter = new RecoveryPackageWriter(backupsRoot);

        // The gateway for `run` carries EXACTLY the one concrete adapter this command builds from the payload —
        // never SomaLinxDryRunAdapter/SomaLinxReadOnlyAdapter/WiseGovernedAdapter, which cannot execute live and
        // have no place on a live-execution path.
        var gateway = new ToolGateway([adapter], new ApprovalPolicy(), audit, SaoPauloTimeProvider.Instance);
        var writeStack = new GovernedWriteStack(new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, audit, gateway, SaoPauloTimeProvider.Instance);

        return new GovernedWriteExecutionOrchestrator(
            writeStack, profileStore, new PostWriteValidationRuleCatalog(), knowledgeGapStore,
            recoveryWriter, index, gateway, writeAudit, SaoPauloTimeProvider.Instance, rollbackCapabilityGapStore);
    }

    /// <summary>
    /// Builds a <see cref="RollbackOrchestrator"/> whose Tool Gateway carries a
    /// <see cref="PedGradeAdjustmentGovernedWriteAdapter"/> constructed with <paramref name="restoreRequest"/> —
    /// the EXACT quantities the write must set. This matters because <c>RollbackOrchestrator.ExecuteAsync</c>
    /// does NOT feed the restore values into the adapter itself: it builds an equivalent
    /// <see cref="ActionProposal"/> purely for policy/approval/audit purposes, then invokes whichever adapter
    /// instance the Tool Gateway has registered for the capability — and that instance decides what to WRITE
    /// from its own constructor state, not from the proposal. Passing an adapter built with the wrong
    /// quantities (e.g. all zero, "for identity purposes only") would silently zero the row instead of
    /// restoring it — a real bug caught by the SOMA_DESENV homologation run, fixed here by requiring the
    /// caller to always supply the real target quantities for anything that will actually execute.
    /// </summary>
    private static (RollbackOrchestrator Orchestrator, IApprovalStore Approvals, PedGradeAdjustmentGovernedWriteAdapter WriteAdapter) BuildRollbackOrchestrator(
        string governanceRoot, string backupsRoot, IConfiguration configuration, string connectionProfile, PedGradeAdjustmentRequest restoreRequest)
    {
        var index = new FileRecoveryIndexStore(governanceRoot);
        var writer = new RecoveryPackageWriter(backupsRoot);
        var profileStore = new FileWriteVerificationProfileStore(governanceRoot);
        var writeAudit = new FileWriteExecutionAuditStore(governanceRoot);
        var approvals = new FileApprovalStore(governanceRoot);
        var governanceAudit = new FileGovernanceAuditStore(governanceRoot);
        var rollbackAudit = new FileRollbackAuditStore(governanceRoot);

        var writeAdapter = new PedGradeAdjustmentGovernedWriteAdapter(configuration, restoreRequest, connectionProfile);

        var gateway = new ToolGateway([writeAdapter], new ApprovalPolicy(), governanceAudit, SaoPauloTimeProvider.Instance);
        var orchestrator = new RollbackOrchestrator(
            index, writer, new PostWriteValidationRuleCatalog(), new AIGovernancePolicyEngine(), new ApprovalPolicy(),
            approvals, gateway, profileStore, rollbackAudit, writeAudit, governanceAudit, SaoPauloTimeProvider.Instance);

        return (orchestrator, approvals, writeAdapter);
    }

    private static PedGradeAdjustmentGovernedWriteAdapter BuildSnapshotOnlyAdapter(IConfiguration configuration, string connectionProfile, PedGradeAdjustmentRunPayload snapshotKey) =>
        new(configuration, new PedGradeAdjustmentRequest(snapshotKey.Pedido, snapshotKey.Produto, snapshotKey.CorProduto, 0, 0, 0, 0, 0, 0), connectionProfile);

    /// <summary>
    /// Extracts the restore quantities (CO1..CO6) from the Recovery Package's recorded before-state
    /// (<see cref="RollbackSafetyAnalysis.BeforeData"/>) for the row identified by <paramref name="snapshotKey"/>
    /// — the objective source of "what CO1..CO6 were before the execution being rolled back", never a value
    /// the caller merely asserts. Throws if the before-state does not contain exactly the row expected;
    /// this capability's write is always an UPDATE on an existing row, so an empty/missing before-state here
    /// means the Recovery Package itself is unusable, not a legitimate "nothing to restore" case.
    /// </summary>
    private static PedGradeAdjustmentRequest ExtractRestoreRequest(RollbackSafetyAnalysis analysis, PedGradeAdjustmentRunPayload snapshotKey)
    {
        var record = analysis.BeforeData
            .Where(set => string.Equals(set.Resource, PedGradeAdjustmentGovernedWriteAdapter.TableName, StringComparison.OrdinalIgnoreCase))
            .SelectMany(set => set.Records)
            .FirstOrDefault(r =>
                string.Equals(GetOrDefault(r, "PEDIDO"), snapshotKey.Pedido, StringComparison.Ordinal) &&
                string.Equals(GetOrDefault(r, "PRODUTO"), snapshotKey.Produto, StringComparison.Ordinal) &&
                string.Equals(GetOrDefault(r, "COR_PRODUTO"), snapshotKey.CorProduto, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"O Recovery Package nao contem before-data para PEDIDO={snapshotKey.Pedido}, PRODUTO={snapshotKey.Produto}, COR_PRODUTO={snapshotKey.CorProduto} — rollback abortado sem escrever.");

        return new PedGradeAdjustmentRequest(
            snapshotKey.Pedido, snapshotKey.Produto, snapshotKey.CorProduto,
            ParseIntOrZero(record, "CO1"), ParseIntOrZero(record, "CO2"), ParseIntOrZero(record, "CO3"),
            ParseIntOrZero(record, "CO4"), ParseIntOrZero(record, "CO5"), ParseIntOrZero(record, "CO6"));
    }

    private static string? GetOrDefault(IReadOnlyDictionary<string, string?> record, string key) => record.TryGetValue(key, out var value) ? value : null;

    private static int ParseIntOrZero(IReadOnlyDictionary<string, string?> record, string key) =>
        int.TryParse(GetOrDefault(record, key), out var value) ? value : 0;

    private sealed record ProfileResolution(string? Error, string? Message);

    private static ProfileResolution TryResolveLinxProfile(string connectionProfile, string server, string database, IConfiguration configuration)
    {
        LinxConnectionProfile profile;
        try
        {
            profile = connectionProfile switch
            {
                WriteVerificationProfileSeeds.LinxDevelopment => LinxConnectionProfiles.Development,
                WriteVerificationProfileSeeds.LinxProduction => LinxConnectionProfiles.Production,
                _ => throw new InvalidOperationException($"Connection profile nao governado para escrita: '{connectionProfile}'."),
            };
        }
        catch (InvalidOperationException ex)
        {
            return new("CONNECTION_PROFILE_NOT_GOVERNED", ex.Message);
        }

        if (!string.Equals(server, profile.ExpectedServer, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(database, profile.ExpectedDatabase, StringComparison.OrdinalIgnoreCase))
        {
            return new("SERVER_OR_DATABASE_MISMATCH",
                $"Payload declara server='{server}' database='{database}', mas o profile '{connectionProfile}' exige server='{profile.ExpectedServer}' database='{profile.ExpectedDatabase}'.");
        }

        string connectionString;
        try
        {
            connectionString = LinxConnectionStringResolver.Resolve(configuration, profile);
        }
        catch (InvalidOperationException ex)
        {
            return new("CONNECTION_STRING_VALIDATION_FAILED", ex.Message);
        }

        return string.IsNullOrWhiteSpace(connectionString)
            ? new("CONNECTION_STRING_NOT_CONFIGURED", $"Nenhuma connection string configurada para o profile '{connectionProfile}'.")
            : new(null, null);
    }

    private sealed record GrantResolution(ApprovalGrant? Grant, string? Error, string? Message);

    private static async Task<GrantResolution> ResolveApprovalGrantAsync(IApprovalStore approvals, Guid? approvalGrantId, CancellationToken cancellationToken)
    {
        if (approvalGrantId is not { } grantId) return new(null, null, null);

        var grant = await approvals.GetGrantAsync(grantId, cancellationToken);
        if (grant is null) return new(null, "APPROVAL_GRANT_NOT_FOUND", $"Nenhum ApprovalGrant persistido com id {grantId}.");

        var now = SaoPauloTimeProvider.Instance.GetUtcNow();
        if (grant.RevokedAt is not null) return new(null, "APPROVAL_GRANT_REVOKED", $"ApprovalGrant {grantId} foi revogado em {grant.RevokedAt}.");
        if (now > grant.ExpiresAt) return new(null, "APPROVAL_GRANT_EXPIRED", $"ApprovalGrant {grantId} expirou em {grant.ExpiresAt}.");

        // Hash-vs-proposal match is verified by ToolGateway.Validate/ApprovalPolicy.IsGrantValidFor once the
        // proposal is built inside the orchestrator — deliberately not re-checked here a second, divergent way.
        return new(grant, null, null);
    }

    /// <summary>Base governance root (NOT yet split by database) — <c>{repository-root}/runtime/governance</c>
    /// by default, or the configured override. Every call site below combines this with a resolved
    /// <c>database</c> bucket (<see cref="GovernanceDatabaseResolver"/> pre-execution, or the real validated
    /// <c>Database</c> once a live write is in play) before constructing any File* store.</summary>
    private static string ResolveGovernanceRoot(IConfiguration configuration)
    {
        var configuredRoot = configuration["Governance:RuntimeRoot"];
        return string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(RuntimeRootLocator.ResolveRuntimeRoot(), "governance")
            : configuredRoot;
    }

    private static string ResolveBackupsRoot(IConfiguration configuration)
    {
        var configuredRoot = configuration["Governance:BackupsRoot"];
        return string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(RuntimeRootLocator.ResolveRuntimeRoot(), "backups")
            : configuredRoot;
    }

    /// <summary>Combines the base governance root with the database bucket — <c>{governanceRoot}/{database}</c>
    /// — the "effective" root every File* governance store below is actually constructed with.</summary>
    private static string ForDatabase(string governanceRoot, string database) => Path.Combine(governanceRoot, database);

    /// <summary>
    /// <c>approve</c> only receives an <c>ApprovalRequestId</c> — no connection profile/database — because the
    /// request may have been persisted by `propose` (pre-execution, database resolved via
    /// <see cref="GovernanceDatabaseResolver"/>) or by `rollback-plan` (same resolver). Rather than requiring the
    /// caller to also pass the database back in (a value it may not have on hand), this looks the request up
    /// across every existing database bucket under the governance root — inexpensive at this project's volume
    /// (a handful of buckets: SOMA, SOMA_DESENV, wise).
    /// </summary>
    private static async Task<(FileApprovalStore Store, string DatabaseGovernanceRoot)?> FindApprovalStoreForRequestAsync(
        string governanceRoot, Guid requestId, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(governanceRoot)) return null;
        foreach (var bucket in Directory.GetDirectories(governanceRoot))
        {
            var store = new FileApprovalStore(bucket);
            var found = await store.GetRequestAsync(requestId, cancellationToken);
            if (found is not null) return (store, bucket);
        }

        return null;
    }

    private static async Task<T?> ReadAsync<T>(TextReader input, CancellationToken cancellationToken)
    {
        var raw = await input.ReadToEndAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(raw, JsonOptions);
    }

    private static Task WriteAsync(TextWriter output, object value) =>
        output.WriteLineAsync(JsonSerializer.Serialize(value, JsonOptions));

    private static async Task<int> WriteErrorAsync(TextWriter output, string error, string? message = null)
    {
        await WriteAsync(output, new { error, message });
        return 1;
    }
}
