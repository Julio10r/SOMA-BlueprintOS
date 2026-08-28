#pragma warning disable CS1591

using System.Security.Cryptography;
using System.Text;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>
/// Governed rollback for a Recovery Package v2 (batch/chunked) execution — the batch analogue of
/// <see cref="RollbackOrchestrator"/>, built on the same DISCOVER != ANALYZE != CONFIRM != EXECUTE separation
/// and the same non-negotiables: a rollback NEVER happens automatically, the original execution's approval is
/// NEVER reused, and every write still goes through <see cref="IToolGateway"/> — nothing here re-implements
/// policy evaluation, approval or the write path itself.
///
/// A batch is indexed in <see cref="IRecoveryIndexStore"/> exactly like a single-item execution — ONE
/// <see cref="RecoveryIndexEntry"/> per batch, keyed by the batch execution id, with
/// <c>BusinessKeys</c> carrying every item's key. That is the item-5 design decision: no second index store, no
/// per-item index rows. A business-key search already finds the batch (via
/// <see cref="RecoveryIndexQuery.Matches"/>'s existing <c>Contains</c> check on <c>BusinessKeys</c>) — it just
/// does not, by itself, say WHICH chunk that key lives in; <see cref="BatchItemsIndex"/> (inside the package)
/// answers that, and is what <see cref="AnalyzeAsync"/> reads to find one item's before/expected-after payload
/// without touching a chunk it does not need.
///
/// SELECTIVE vs FULL rollback is the same method with a different <c>requestedBusinessKeys</c> argument to
/// <see cref="AnalyzeAsync"/>: empty means every item in the batch, non-empty means exactly those items.
///
/// Per-item concurrency is checked exactly like the single-item path (recorded after-state vs a fresh re-read),
/// one item at a time. An item with a concurrency finding is EXCLUDED from <c>ReadyItems</c> — not fatal to the
/// whole batch — but if this leaves a heterogeneous mix of operations (e.g. some items would need Insert,
/// others Update) among what remains, execution refuses outright
/// (<see cref="BatchRollbackAnalysisStatus.MixedOperationsNotSupported"/>): a single governed write proposal
/// cannot honestly describe two different operations at once, and silently splitting into per-operation
/// sub-batches was rejected as more complexity than this host needs today.
///
/// EXECUTION performs ONE policy decision and ONE fresh approval for the whole ready set (a batch rollback is
/// one governed action, not N separate ones), then invokes the Tool Gateway once PER ITEM — necessarily,
/// because each item restores different values, so the <c>gatewayFactory</c> parameter of
/// <see cref="ExecuteAsync"/> builds a gateway wired with the correct adapter instance for that one item. This
/// keeps the orchestrator capability-agnostic: it never knows what a "ped grade adjustment" is, only that the
/// caller can hand it a gateway for any given item's restore.
/// </summary>
public sealed class BatchRollbackOrchestrator(
    IRecoveryIndexStore recoveryIndexStore,
    IBatchRecoveryPackageWriter batchPackageWriter,
    IPostWriteValidationRuleCatalog validationRuleCatalog,
    IAIGovernancePolicyEngine policyEngine,
    IApprovalPolicy approvalPolicy,
    IApprovalStore approvalStore,
    IWriteVerificationProfileStore profileStore,
    IRollbackAuditStore rollbackAuditStore,
    IGovernanceAuditStore governanceAuditStore,
    TimeProvider timeProvider)
{
    public const string NotFoundReason = "ROLLBACK_NOT_FOUND";
    public const string NotAvailableReason = "ROLLBACK_NOT_AVAILABLE";
    public const string ConcurrentChangeReason = "ROLLBACK_BLOCKED_CONCURRENT_CHANGE";
    public const string ConfirmationMismatchReason = "ROLLBACK_CONFIRMATION_MISMATCH";
    public const string MixedOperationsReason = "BATCH_ROLLBACK_MIXED_OPERATIONS_NOT_SUPPORTED";
    public const string ValidationReason = "ROLLBACK_VALIDATION";

    public async Task<RollbackDiscoveryResult> DiscoverAsync(RecoveryIndexQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var candidates = await recoveryIndexStore.FindAsync(query, cancellationToken);
        return candidates.Count switch
        {
            0 => new(RollbackDiscoveryStatus.NotFound, [], [NotFoundReason]),
            1 => new(RollbackDiscoveryStatus.SingleCandidate, candidates, ["ROLLBACK_CANDIDATE_LOCATED", "AWAITING_EXPLICIT_SELECTION"]),
            _ => new(RollbackDiscoveryStatus.MultipleCandidates, candidates,
                ["ROLLBACK_MULTIPLE_CANDIDATES", "AWAITING_EXPLICIT_SELECTION", $"CANDIDATES={candidates.Count}"]),
        };
    }

    public async Task<BatchRollbackSafetyAnalysis> AnalyzeAsync(
        Guid batchExecutionId,
        IReadOnlyList<string> requestedBusinessKeys,
        ISnapshotCapableAdapter snapshotSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestedBusinessKeys);
        ArgumentNullException.ThrowIfNull(snapshotSource);
        var now = timeProvider.GetUtcNow();

        var matches = await recoveryIndexStore.FindAsync(new RecoveryIndexQuery { ExecutionId = batchExecutionId }, cancellationToken);
        if (matches.Count != 1)
        {
            return NotFoundAnalysis(batchExecutionId, "Execucao (lote) nao encontrada no indice de recovery.");
        }

        var entry = matches[0];
        if (!entry.BackupRequired || !entry.RollbackSupported)
        {
            return Analysis(BatchRollbackAnalysisStatus.NotAvailable, batchExecutionId, entry, null, requestedBusinessKeys, [], [],
                "Lote sem backup ou sem suporte a rollback pela politica vigente; nenhuma reconstrucao sera tentada.",
                [NotAvailableReason, entry.BackupRequired ? "ROLLBACK_NOT_SUPPORTED_BY_POLICY" : "NO_BACKUP_WAS_TAKEN"]);
        }

        if (entry.Status == RecoveryPackageStatus.Expired || entry.ExpiresAt <= now || !batchPackageWriter.PackageExists(entry.PackagePath))
        {
            return Analysis(BatchRollbackAnalysisStatus.NotAvailable, batchExecutionId, entry, null, requestedBusinessKeys, [], [],
                "Pacote de recovery (lote) expirado ou removido pela retencao; o audit permanente continua consultavel.",
                [NotAvailableReason, "RECOVERY_PACKAGE_EXPIRED_OR_REMOVED"]);
        }

        var manifest = await batchPackageWriter.ReadManifestAsync(entry.PackagePath, cancellationToken);
        if (manifest is null || !string.Equals(manifest.ManifestChecksumSha256, entry.ManifestChecksumSha256, StringComparison.Ordinal))
        {
            return Analysis(BatchRollbackAnalysisStatus.NotAvailable, batchExecutionId, entry, manifest, requestedBusinessKeys, [], [],
                "Checksum do manifesto do lote nao confere com o indice; o pacote nao pode ser considerado confiavel.",
                [NotAvailableReason, "RECOVERY_PACKAGE_INTEGRITY_FAILED"]);
        }

        var itemsIndex = await batchPackageWriter.ReadItemsIndexAsync(entry.PackagePath, cancellationToken);
        if (itemsIndex is null)
        {
            return Analysis(BatchRollbackAnalysisStatus.NotAvailable, batchExecutionId, entry, manifest, requestedBusinessKeys, [], [],
                "items-index.json ausente; o pacote nao pode ser considerado confiavel.", [NotAvailableReason, "ITEMS_INDEX_MISSING"]);
        }

        var targetKeys = requestedBusinessKeys.Count == 0
            ? itemsIndex.ByPosition.Select(l => l.BusinessKey).ToArray()
            : requestedBusinessKeys;

        var unknownKeys = targetKeys.Where(key => !itemsIndex.ByBusinessKey.ContainsKey(key)).ToArray();
        if (unknownKeys.Length > 0)
        {
            return Analysis(BatchRollbackAnalysisStatus.NotAvailable, batchExecutionId, entry, manifest, requestedBusinessKeys, [], [],
                $"Chave(s) de negocio nao pertencem a este lote: {string.Join(", ", unknownKeys)}.",
                [NotAvailableReason, "BUSINESS_KEY_NOT_IN_BATCH"]);
        }

        // Integrity per chunk actually touched — never trust a chunk file just because the manifest lists it.
        var touchedChunks = targetKeys.Select(key => itemsIndex.ByBusinessKey[key].ChunkNumber).Distinct().ToArray();
        foreach (var chunkNumber in touchedChunks)
        {
            if (!await batchPackageWriter.VerifyChunkIntegrityAsync(entry.PackagePath, manifest, chunkNumber, cancellationToken))
            {
                return Analysis(BatchRollbackAnalysisStatus.NotAvailable, batchExecutionId, entry, manifest, requestedBusinessKeys, [], [],
                    $"Checksum do chunk {chunkNumber} nao confere; o pacote nao pode ser considerado confiavel.",
                    [NotAvailableReason, $"RECOVERY_PACKAGE_CHUNK_{chunkNumber}_INTEGRITY_FAILED"]);
            }
        }

        var beforeByChunk = new Dictionary<int, IReadOnlyList<RecoveryDataSet>>();
        var afterByChunk = new Dictionary<int, IReadOnlyList<RecoveryDataSet>>();
        async Task<IReadOnlyList<RecoveryDataSet>> BeforeChunk(int chunk) =>
            beforeByChunk.TryGetValue(chunk, out var v) ? v : beforeByChunk[chunk] = await batchPackageWriter.ReadChunkBeforeDataAsync(entry.PackagePath, chunk, cancellationToken);
        async Task<IReadOnlyList<RecoveryDataSet>> AfterChunk(int chunk) =>
            afterByChunk.TryGetValue(chunk, out var v) ? v : afterByChunk[chunk] = await batchPackageWriter.ReadChunkAfterDataAsync(entry.PackagePath, chunk, cancellationToken);

        var readyItems = new List<BatchItemRestorePlan>();
        var concurrencyFindings = new List<string>();

        foreach (var key in targetKeys)
        {
            var location = itemsIndex.ByBusinessKey[key];
            var beforeSets = await BeforeChunk(location.ChunkNumber);
            var afterSets = await AfterChunk(location.ChunkNumber);
            var beforeRecord = location.IndexWithinChunk < beforeSets.Count
                ? beforeSets[location.IndexWithinChunk].Records.FirstOrDefault() : null;
            // "Recorded current" is the after-state written once the original write ran (if the caller wrote
            // one); an item whose batch never had after-data recorded has nothing to compare concurrency
            // against, which is treated the same way an empty before-state is for a CREATE: not evidence of
            // anything, so no finding is raised for it — the actual live re-read below is still what
            // ReadyItems is built from.
            var recordedCurrent = location.IndexWithinChunk < afterSets.Count
                ? afterSets[location.IndexWithinChunk].Records.FirstOrDefault() : null;

            var observed = await snapshotSource.CaptureSnapshotAsync([key], cancellationToken);
            var observedRecord = observed.SelectMany(set => set.Records).FirstOrDefault();

            if (recordedCurrent is not null)
            {
                var recordedMatchesObserved = observedRecord is not null && recordedCurrent.All(pair =>
                    observedRecord.TryGetValue(pair.Key, out var value)
                    && string.Equals(value?.Trim() ?? string.Empty, pair.Value?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase));
                if (!recordedMatchesObserved)
                {
                    concurrencyFindings.Add($"{location.Resource}[{key}]: registro esperado [{Describe(recordedCurrent)}] nao corresponde ao estado atual.");
                    continue; // excluded from ReadyItems — the safest response to an unexplained divergence.
                }
            }

            var beforeExists = beforeRecord is not null;
            var currentExists = observedRecord is not null;
            var operation = (beforeExists, currentExists) switch
            {
                (false, true) => ActionOperation.Delete,
                (true, false) => ActionOperation.Insert,
                _ => ActionOperation.Update,
            };

            readyItems.Add(new BatchItemRestorePlan(key, location.Resource, operation, beforeRecord, observedRecord));
        }

        if (readyItems.Count == 0)
        {
            return Analysis(BatchRollbackAnalysisStatus.BlockedConcurrentChange, batchExecutionId, entry, manifest, requestedBusinessKeys, [], concurrencyFindings,
                "Todos os itens visados divergem do estado esperado (alteracao concorrente); nenhuma escrita sera feita.",
                [ConcurrentChangeReason]);
        }

        var distinctOperations = readyItems.Select(i => i.Operation).Distinct().ToArray();
        if (distinctOperations.Length > 1)
        {
            return Analysis(BatchRollbackAnalysisStatus.MixedOperationsNotSupported, batchExecutionId, entry, manifest, requestedBusinessKeys, readyItems, concurrencyFindings,
                $"Os itens prontos exigiriam operacoes diferentes ({string.Join(", ", distinctOperations)}); rollback de lote so executa um lote homogeneo.",
                [MixedOperationsReason]);
        }

        var handle = BuildConfirmationHandle(batchExecutionId, manifest.ManifestChecksumSha256, readyItems.Select(i => i.BusinessKey), now);
        return new BatchRollbackSafetyAnalysis(
            BatchRollbackAnalysisStatus.ReadyForConfirmation, batchExecutionId, entry, manifest, requestedBusinessKeys,
            readyItems, concurrencyFindings,
            $"Rollback de lote '{entry.ExecutionName}' ({batchExecutionId}) em {entry.Database}@{entry.Server}: "
            + $"{readyItems.Count}/{targetKeys.Count} item(ns) prontos, operacao {distinctOperations[0]}. "
            + (concurrencyFindings.Count > 0 ? $"{concurrencyFindings.Count} item(ns) excluido(s) por alteracao concorrente. " : string.Empty)
            + "Confirmacao explicita obrigatoria para executar.",
            handle, ["ROLLBACK_READY_FOR_CONFIRMATION"]);
    }

    public async Task<BatchRollbackExecutionResult> ExecuteAsync(
        BatchRollbackSafetyAnalysis analysis,
        BatchRollbackConfirmation confirmation,
        Func<BatchItemRestorePlan, (IToolGateway Gateway, string Capability)> gatewayFactory,
        Func<string, CancellationToken, Task<IReadOnlyList<RecoveryDataSet>>> captureAfterRollbackAsync,
        RollbackApprovalCallback approvalCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(gatewayFactory);
        ArgumentNullException.ThrowIfNull(approvalCallback);

        var now = timeProvider.GetUtcNow();
        var rollbackExecutionId = Guid.NewGuid();

        if (analysis.Status != BatchRollbackAnalysisStatus.ReadyForConfirmation
            || analysis.ConfirmationHandle is null
            || confirmation.BatchExecutionId != analysis.BatchExecutionId
            || !string.Equals(confirmation.ConfirmationHandle, analysis.ConfirmationHandle, StringComparison.Ordinal))
        {
            var audit = await AuditAsync(rollbackExecutionId, analysis, confirmation, BatchRollbackExecutionStatus.Blocked, [ConfirmationMismatchReason], null, null, 0, cancellationToken);
            return new(BatchRollbackExecutionStatus.Blocked, rollbackExecutionId, analysis.BatchExecutionId, [],
                [ConfirmationMismatchReason, "NEW_CONFIRMATION_REQUIRED"]);
        }

        var entry = analysis.Entry!;
        var profile = await profileStore.ResolveAsync(entry.ConnectionProfile, now, cancellationToken);
        if (profile is null)
        {
            await AuditAsync(rollbackExecutionId, analysis, confirmation, BatchRollbackExecutionStatus.GovernanceBlocked, ["WRITE_VERIFICATION_PROFILE_NOT_FOUND"], null, null, 0, cancellationToken);
            return new(BatchRollbackExecutionStatus.GovernanceBlocked, rollbackExecutionId, entry.ExecutionId, [], ["WRITE_VERIFICATION_PROFILE_NOT_FOUND"]);
        }

        var readyItems = analysis.ReadyItems;
        var resource = readyItems[0].Resource;
        var operation = readyItems[0].Operation;
        var fields = readyItems
            .SelectMany(i => (i.TargetRecord ?? i.CurrentRecord)?.Keys ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var equivalent = new ActionProposal
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            RequestingAgent = entry.AgentId,
            Environment = ResolveEnvironment(entry),
            System = "SOMA/Linx",
            ResourceType = ActionResourceType.DatabaseTable,
            Resource = resource,
            Operation = operation,
            Fields = fields.Length > 0 ? fields : ["*"],
            FilterSummary = string.Join(" AND ", readyItems.Select(i => i.BusinessKey)),
            ExpectedAffectedRows = readyItems.Count,
            Purpose = $"Rollback de lote governado {entry.ExecutionId}: {confirmation.Justification}",
            DataClassification = DataClassification.Internal,
            ContainsPersonalData = false,
            ContainsSensitivePersonalData = false,
            ContainsSecrets = false,
            Reversibility = ActionReversibility.Reversible,
            AdditionalContext = $"batch_rollback_of_execution={entry.ExecutionId}; item_count={readyItems.Count}; original_proposal_hash={entry.ProposalHash}",
            RollbackOfExecutionId = entry.ExecutionId,
        };

        var rollbackProposal = new RollbackActionProposal(entry.ExecutionId, equivalent, confirmation.RequestedBy, confirmation.Justification);
        var decision = policyEngine.Evaluate(equivalent, now);
        await governanceAuditStore.AppendAsync(new GovernanceAuditEvent(
            Guid.NewGuid(), "batch-rollback.policy-evaluated", rollbackExecutionId.ToString("N"), equivalent.Id, equivalent.ProposalHash,
            entry.AgentId, confirmation.RequestedBy, decision.Status.ToString(), [decision.RiskClassification.ToString()], now), cancellationToken);

        if (decision.Status == PolicyDecisionStatus.Blocked || decision.RiskClassification == RiskClassification.Red)
        {
            var audit = await AuditAsync(rollbackExecutionId, analysis, confirmation, BatchRollbackExecutionStatus.GovernanceBlocked, ["POLICY_BLOCKED", .. decision.Reasons], rollbackProposal, decision, 0, cancellationToken);
            return new(BatchRollbackExecutionStatus.GovernanceBlocked, rollbackExecutionId, entry.ExecutionId, [], ["POLICY_BLOCKED"], rollbackProposal, decision);
        }

        ApprovalGrant? grant = null;
        if (decision.Status == PolicyDecisionStatus.RequiresApproval)
        {
            var approvalRequest = new ApprovalRequest(
                Guid.NewGuid(), equivalent.Id, equivalent.ProposalHash, decision.RiskClassification,
                $"Rollback de lote {entry.ExecutionId} ({readyItems.Count} item(ns)): {confirmation.Justification}",
                "authorized-product-owner", now, now.AddHours(1), ApprovalRequestStatus.Pending);
            await approvalStore.SaveRequestAsync(approvalRequest, cancellationToken);
            grant = await approvalCallback(equivalent, decision, approvalRequest, cancellationToken);

            if (grant is null || !approvalPolicy.IsGrantValidFor(equivalent, grant, now))
            {
                var audit = await AuditAsync(rollbackExecutionId, analysis, confirmation, BatchRollbackExecutionStatus.ApprovalRequired, ["VALID_APPROVAL_REQUIRED"], rollbackProposal, decision, 0, cancellationToken);
                return new(BatchRollbackExecutionStatus.ApprovalRequired, rollbackExecutionId, entry.ExecutionId, [], ["VALID_APPROVAL_REQUIRED"], rollbackProposal, decision);
            }
        }

        var rule = validationRuleCatalog.Resolve(operation, resource);
        var outcomes = new List<BatchItemRollbackOutcome>();

        foreach (var item in readyItems)
        {
            var (gateway, capability) = gatewayFactory(item);
            var receipt = new RecoveryPackageReceipt(
                entry.ExecutionId, entry.PackagePath, entry.ManifestChecksumSha256, entry.ExecutedAt, entry.ExpiresAt,
                BeforeState: item.CurrentRecord is null ? BeforeStateStatus.NotExistent : BeforeStateStatus.Captured);

            var itemGatewayRequest = new ToolGatewayRequest(
                capability, entry.AgentId, true, equivalent, decision, grant,
                [], entry.ConnectionProfile, new IdentityPermissionContext(confirmation.RequestedBy, HasEffectivePermission: true),
                GovernedExecutionMode.LiveExecution, receipt, rule, profile);

            var result = await gateway.InvokeAsync(itemGatewayRequest, cancellationToken);
            if (result.Status is ToolGatewayStatus.Blocked or ToolGatewayStatus.LiveExecutionFailed)
            {
                outcomes.Add(new BatchItemRollbackOutcome(item.BusinessKey, false, result.Reasons));
                continue;
            }

            var restored = await captureAfterRollbackAsync(item.BusinessKey, cancellationToken);
            var passed = rule is null
                || (item.TargetRecord is null
                    ? restored.Count == 0
                    : PostWriteValidator.Validate(rule, [new RecoveryDataSet(item.Resource, [item.TargetRecord])], restored, timeProvider.GetUtcNow()).Passed);

            outcomes.Add(new BatchItemRollbackOutcome(item.BusinessKey, passed, passed ? ["PASS"] : ["VALIDATION_FAILED"]));
            await batchPackageWriter.UpdateItemStatusAsync(entry.PackagePath, item.BusinessKey,
                passed ? BatchItemStatus.RolledBack : BatchItemStatus.ValidationFailed, cancellationToken);
        }

        var succeeded = outcomes.Count(o => o.Success);
        var status = succeeded == outcomes.Count
            ? BatchRollbackExecutionStatus.Completed
            : succeeded == 0
                ? BatchRollbackExecutionStatus.ValidationFailed
                : BatchRollbackExecutionStatus.PartiallyCompleted;

        if (status == BatchRollbackExecutionStatus.Completed)
        {
            var itemsIndex = await batchPackageWriter.ReadItemsIndexAsync(entry.PackagePath, cancellationToken);
            if (itemsIndex is not null && itemsIndex.ByPosition.All(l => l.Status == BatchItemStatus.RolledBack))
            {
                await recoveryIndexStore.UpdateStatusAsync(entry.ExecutionId, RecoveryPackageStatus.RolledBack, cancellationToken);
            }
        }

        var finalAudit = await AuditAsync(rollbackExecutionId, analysis, confirmation, status,
            [$"{ValidationReason}={status}"], rollbackProposal, decision, succeeded, cancellationToken);

        return new(status, rollbackExecutionId, entry.ExecutionId, outcomes,
            [$"{ValidationReason}={status}"], rollbackProposal, decision);
    }

    private static GovernanceEnvironment ResolveEnvironment(RecoveryIndexEntry entry) =>
        string.Equals(entry.ConnectionProfile, WriteVerificationProfileSeeds.LinxProduction, StringComparison.Ordinal)
            ? GovernanceEnvironment.Production
            : GovernanceEnvironment.Development;

    private static string Describe(IReadOnlyDictionary<string, string?> record) =>
        string.Join(", ", record.Select(pair => $"{pair.Key}={pair.Value}"));

    private static string BuildConfirmationHandle(Guid batchExecutionId, string manifestChecksum, IEnumerable<string> readyBusinessKeys, DateTimeOffset analyzedAt)
    {
        var payload = $"{batchExecutionId:N}|{manifestChecksum}|{string.Join(",", readyBusinessKeys.OrderBy(k => k, StringComparer.Ordinal))}|{analyzedAt.ToUniversalTime():O}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static BatchRollbackSafetyAnalysis NotFoundAnalysis(Guid batchExecutionId, string summary) =>
        Analysis(BatchRollbackAnalysisStatus.NotFound, batchExecutionId, null, null, [], [], [], summary, [NotFoundReason]);

    private static BatchRollbackSafetyAnalysis Analysis(
        BatchRollbackAnalysisStatus status, Guid batchExecutionId, RecoveryIndexEntry? entry, BatchRecoveryPackageManifest? manifest,
        IReadOnlyList<string> requestedKeys, IReadOnlyList<BatchItemRestorePlan> readyItems, IReadOnlyList<string> concurrencyFindings,
        string summary, IReadOnlyList<string> reasons) =>
        new(status, batchExecutionId, entry, manifest, requestedKeys, readyItems, concurrencyFindings, summary, null, reasons);

    private async Task<RollbackAuditRecord> AuditAsync(
        Guid rollbackExecutionId, BatchRollbackSafetyAnalysis analysis, BatchRollbackConfirmation confirmation,
        BatchRollbackExecutionStatus status, IReadOnlyList<string> errors, RollbackActionProposal? proposal,
        PolicyDecision? decision, int recordsAffected, CancellationToken cancellationToken)
    {
        var record = new RollbackAuditRecord
        {
            RollbackExecutionId = rollbackExecutionId,
            OriginalExecutionId = analysis.BatchExecutionId,
            Requester = confirmation.RequestedBy,
            RequestedAt = timeProvider.GetUtcNow(),
            ExplicitConfirmationReceived = status != BatchRollbackExecutionStatus.Blocked,
            ConfirmedAt = status == BatchRollbackExecutionStatus.Blocked ? null : confirmation.ConfirmedAt,
            Justification = confirmation.Justification,
            TablesAffected = analysis.Entry?.TablesAffected ?? [],
            BusinessKeys = analysis.ReadyItems.Select(i => i.BusinessKey).ToArray(),
            RecordsAffected = recordsAffected,
            ConcurrencyFindings = analysis.ConcurrencyFindings,
            ExpectedStateSummary = $"lote {analysis.BatchExecutionId}: {analysis.ReadyItems.Count} item(ns) alvo",
            ObservedStateSummary = $"{recordsAffected}/{analysis.ReadyItems.Count} item(ns) restaurado(s) com sucesso",
            Status = status switch
            {
                BatchRollbackExecutionStatus.Completed => RollbackExecutionStatus.Completed,
                BatchRollbackExecutionStatus.PartiallyCompleted => RollbackExecutionStatus.Completed,
                BatchRollbackExecutionStatus.ValidationFailed => RollbackExecutionStatus.ValidationFailed,
                BatchRollbackExecutionStatus.ApprovalRequired => RollbackExecutionStatus.ApprovalRequired,
                BatchRollbackExecutionStatus.GovernanceBlocked => RollbackExecutionStatus.GovernanceBlocked,
                _ => RollbackExecutionStatus.Blocked,
            },
            PostRollbackValidationPassed = status == BatchRollbackExecutionStatus.Completed,
            PostRollbackValidationRuleId = null,
            Errors = errors,
            RollbackProposalHash = proposal?.EquivalentProposal.ProposalHash,
        };

        await rollbackAuditStore.AppendAsync(record, cancellationToken);
        await governanceAuditStore.AppendAsync(new GovernanceAuditEvent(
            Guid.NewGuid(), "batch-rollback.completed", rollbackExecutionId.ToString("N"),
            proposal?.EquivalentProposal.Id, proposal?.EquivalentProposal.ProposalHash,
            analysis.Entry?.AgentId, confirmation.RequestedBy, status.ToString(), errors, record.RequestedAt), cancellationToken);
        return record;
    }
}
