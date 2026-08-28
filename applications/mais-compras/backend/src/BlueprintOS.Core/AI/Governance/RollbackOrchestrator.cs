#pragma warning disable CS1591

using System.Security.Cryptography;
using System.Text;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>Called to obtain a FRESH approval for a rollback. The original execution's approval is never
/// reusable, so a rollback always passes through a new authorization decision.</summary>
public delegate Task<ApprovalGrant?> RollbackApprovalCallback(
    ActionProposal proposal,
    PolicyDecision decision,
    ApprovalRequest request,
    CancellationToken cancellationToken);

/// <summary>
/// Governed rollback, built around one principle: DISCOVER != SELECT != AUTHORIZE != EXECUTE. Each of those is
/// a separate call with a separate result type, and no call performs the next one's job:
///
/// 1. <see cref="DiscoverAsync"/> — automatic. Searches the recovery index. Zero results is ROLLBACK_NOT_FOUND;
///    many results are all returned; one result is returned, not executed. It NEVER chooses.
/// 2. Selection — not a method here at all. The caller picks an execution id and passes it to step 3. That is
///    the point: selection is a human act.
/// 3. <see cref="AnalyzeAsync"/> — automatic, for one already-selected execution. Verifies package integrity,
///    expiry, rollback support, and re-reads the current state to detect concurrent change. On success it
///    issues a confirmation handle. It still writes nothing.
/// 4. <see cref="ExecuteAsync"/> — runs only when handed back the exact handle for the exact execution that was
///    analyzed. Builds a RollbackActionProposal from the before-data, evaluates it through the policy engine,
///    obtains a NEW approval, writes through the Tool Gateway's ordinary live path, then validates the result.
///
/// The confirmation handle is derived from the execution id plus the manifest checksum plus the analysis
/// instant, so it is bound to one analysis of one execution and cannot be replayed against another.
/// </summary>
public sealed class RollbackOrchestrator(
    IRecoveryIndexStore recoveryIndexStore,
    IRecoveryPackageWriter recoveryPackageWriter,
    IPostWriteValidationRuleCatalog validationRuleCatalog,
    IAIGovernancePolicyEngine policyEngine,
    IApprovalPolicy approvalPolicy,
    IApprovalStore approvalStore,
    IToolGateway toolGateway,
    IWriteVerificationProfileStore profileStore,
    IRollbackAuditStore rollbackAuditStore,
    IWriteExecutionAuditStore writeExecutionAuditStore,
    IGovernanceAuditStore governanceAuditStore,
    TimeProvider timeProvider)
{
    public const string NotFoundReason = "ROLLBACK_NOT_FOUND";
    public const string NotAvailableReason = "ROLLBACK_NOT_AVAILABLE";
    public const string ConcurrentChangeReason = "ROLLBACK_BLOCKED_CONCURRENT_CHANGE";
    public const string ConfirmationMismatchReason = "ROLLBACK_CONFIRMATION_MISMATCH";
    public const string ValidationReason = "ROLLBACK_VALIDATION";

    // ---------------------------------------------------------------------------------------------------
    // 1. DISCOVERY — automatic, stateless, never selects.
    // ---------------------------------------------------------------------------------------------------

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

    // ---------------------------------------------------------------------------------------------------
    // 3. SAFETY PRE-ANALYSIS — automatic, for one explicitly selected execution. Writes nothing.
    // ---------------------------------------------------------------------------------------------------

    public async Task<RollbackSafetyAnalysis> AnalyzeAsync(
        Guid executionId,
        ISnapshotCapableAdapter snapshotSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshotSource);
        var now = timeProvider.GetUtcNow();

        var matches = await recoveryIndexStore.FindAsync(new RecoveryIndexQuery { ExecutionId = executionId }, cancellationToken);
        if (matches.Count != 1)
        {
            return Analysis(RollbackAnalysisStatus.NotFound, executionId, null, null, [NotFoundReason],
                "Execucao nao encontrada no indice de recovery.");
        }

        var entry = matches[0];

        // Guarantees that must hold before anything is read from disk. A package that was never taken, or that
        // the policy says cannot be rolled back, is not a candidate at all — there is nothing to reconstruct
        // from, and reconstructing state by inference is exactly what this design forbids.
        if (!entry.BackupRequired || !entry.RollbackSupported)
        {
            return Analysis(RollbackAnalysisStatus.NotAvailable, executionId, entry, null,
                [NotAvailableReason, entry.BackupRequired ? "ROLLBACK_NOT_SUPPORTED_BY_POLICY" : "NO_BACKUP_WAS_TAKEN"],
                "Execucao sem backup ou sem suporte a rollback pela politica vigente; nenhuma reconstrucao sera tentada.");
        }

        if (entry.Status == RecoveryPackageStatus.Expired || entry.ExpiresAt <= now || !recoveryPackageWriter.PackageExists(entry.PackagePath))
        {
            return Analysis(RollbackAnalysisStatus.NotAvailable, executionId, entry, null,
                [NotAvailableReason, "RECOVERY_PACKAGE_EXPIRED_OR_REMOVED"],
                "Pacote de recovery expirado ou removido pela retencao; o audit permanente continua consultavel.");
        }

        var manifest = await recoveryPackageWriter.ReadManifestAsync(entry.PackagePath, cancellationToken);
        if (manifest is null || !string.Equals(manifest.ManifestChecksumSha256, entry.ManifestChecksumSha256, StringComparison.Ordinal))
        {
            return Analysis(RollbackAnalysisStatus.NotAvailable, executionId, entry, manifest,
                [NotAvailableReason, "RECOVERY_PACKAGE_INTEGRITY_FAILED"],
                "Checksum do manifesto nao confere com o indice; o pacote nao pode ser considerado confiavel.");
        }

        var beforeData = await recoveryPackageWriter.ReadBeforeDataAsync(entry.PackagePath, cancellationToken);
        var expectedCurrent = await recoveryPackageWriter.ReadAfterDataAsync(entry.PackagePath, cancellationToken);
        var observedCurrent = await snapshotSource.CaptureSnapshotAsync(entry.BusinessKeys, cancellationToken);

        var findings = DetectConcurrentChange(expectedCurrent, observedCurrent);
        if (findings.Count > 0)
        {
            return new RollbackSafetyAnalysis(
                RollbackAnalysisStatus.BlockedConcurrentChange, executionId, entry, manifest, beforeData,
                expectedCurrent, observedCurrent, findings,
                "O estado atual diverge do estado deixado pela execucao original: houve alteracao concorrente. Nenhuma escrita sera feita.",
                null, [ConcurrentChangeReason]);
        }

        var handle = BuildConfirmationHandle(executionId, manifest.ManifestChecksumSha256, now);
        return new RollbackSafetyAnalysis(
            RollbackAnalysisStatus.ReadyForConfirmation, executionId, entry, manifest, beforeData,
            expectedCurrent, observedCurrent, [],
            $"Rollback de '{entry.ExecutionName}' ({entry.ExecutionId}) em {entry.Database}@{entry.Server}, tabelas [{string.Join(", ", entry.TablesAffected)}], "
            + $"chaves [{string.Join(", ", entry.BusinessKeys)}], {entry.RecordsAffected} registro(s), executada em {entry.ExecutedAt:u} por {entry.Requester}. "
            + "Integridade verificada, sem alteracao concorrente. Confirmacao explicita obrigatoria para executar.",
            handle, ["ROLLBACK_READY_FOR_CONFIRMATION"]);
    }

    // ---------------------------------------------------------------------------------------------------
    // 4. EXECUTION — only with the exact handle issued for the exact execution that was analyzed.
    // ---------------------------------------------------------------------------------------------------

    public async Task<RollbackExecutionResult> ExecuteAsync(
        RollbackSafetyAnalysis analysis,
        RollbackConfirmation confirmation,
        ISnapshotCapableAdapter snapshotSource,
        IWriteExecutionAdapter writeAdapter,
        RollbackApprovalCallback approvalCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(snapshotSource);
        ArgumentNullException.ThrowIfNull(writeAdapter);
        ArgumentNullException.ThrowIfNull(approvalCallback);

        var now = timeProvider.GetUtcNow();
        var rollbackExecutionId = Guid.NewGuid();

        if (analysis.Status != RollbackAnalysisStatus.ReadyForConfirmation
            || analysis.ConfirmationHandle is null
            || confirmation.ExecutionId != analysis.ExecutionId
            || !string.Equals(confirmation.ConfirmationHandle, analysis.ConfirmationHandle, StringComparison.Ordinal))
        {
            var audit = await AuditAsync(rollbackExecutionId, analysis, confirmation, RollbackExecutionStatus.Blocked,
                [ConfirmationMismatchReason], null, null, 0, cancellationToken);
            return new(RollbackExecutionStatus.Blocked, rollbackExecutionId, analysis.ExecutionId,
                [ConfirmationMismatchReason, "NEW_CONFIRMATION_REQUIRED"], Audit: audit);
        }

        var entry = analysis.Entry!;
        var profile = await profileStore.ResolveAsync(entry.ConnectionProfile, now, cancellationToken);
        if (profile is null)
        {
            var audit = await AuditAsync(rollbackExecutionId, analysis, confirmation, RollbackExecutionStatus.GovernanceBlocked,
                ["WRITE_VERIFICATION_PROFILE_NOT_FOUND"], null, null, 0, cancellationToken);
            return new(RollbackExecutionStatus.GovernanceBlocked, rollbackExecutionId, entry.ExecutionId,
                ["WRITE_VERIFICATION_PROFILE_NOT_FOUND"], Audit: audit);
        }

        // The rollback is a NEW write, described by the state we want to restore.
        var equivalent = BuildEquivalentProposal(entry, analysis, confirmation, now);
        var rollbackProposal = new RollbackActionProposal(entry.ExecutionId, equivalent, confirmation.RequestedBy, confirmation.Justification);
        var decision = policyEngine.Evaluate(equivalent, now);
        await governanceAuditStore.AppendAsync(new GovernanceAuditEvent(
            Guid.NewGuid(), "rollback.policy-evaluated", rollbackExecutionId.ToString("N"), equivalent.Id, equivalent.ProposalHash,
            entry.AgentId, confirmation.RequestedBy, decision.Status.ToString(), [decision.RiskClassification.ToString()], now), cancellationToken);

        if (decision.Status == PolicyDecisionStatus.Blocked || decision.RiskClassification == RiskClassification.Red)
        {
            var audit = await AuditAsync(rollbackExecutionId, analysis, confirmation, RollbackExecutionStatus.GovernanceBlocked,
                ["POLICY_BLOCKED", .. decision.Reasons], rollbackProposal, decision, 0, cancellationToken);
            return new(RollbackExecutionStatus.GovernanceBlocked, rollbackExecutionId, entry.ExecutionId,
                ["POLICY_BLOCKED"], rollbackProposal, decision, Audit: audit);
        }

        // A brand-new approval event. The original execution's grant is never consulted.
        ApprovalGrant? grant = null;
        if (decision.Status == PolicyDecisionStatus.RequiresApproval)
        {
            var approvalRequest = new ApprovalRequest(
                Guid.NewGuid(), equivalent.Id, equivalent.ProposalHash, decision.RiskClassification,
                $"Rollback da execucao {entry.ExecutionId}: {confirmation.Justification}",
                "authorized-product-owner", now, now.AddHours(1), ApprovalRequestStatus.Pending);
            await approvalStore.SaveRequestAsync(approvalRequest, cancellationToken);
            grant = await approvalCallback(equivalent, decision, approvalRequest, cancellationToken);

            if (grant is null || !approvalPolicy.IsGrantValidFor(equivalent, grant, now))
            {
                var audit = await AuditAsync(rollbackExecutionId, analysis, confirmation, RollbackExecutionStatus.ApprovalRequired,
                    ["VALID_APPROVAL_REQUIRED"], rollbackProposal, decision, 0, cancellationToken);
                return new(RollbackExecutionStatus.ApprovalRequired, rollbackExecutionId, entry.ExecutionId,
                    ["VALID_APPROVAL_REQUIRED"], rollbackProposal, decision, Audit: audit);
            }
        }

        // The restoring write goes through the ordinary live path — same gateway, same guarantees. The recovery
        // package of the ORIGINAL execution is the backup that protects this write, which is why the receipt is
        // rebuilt from the verified manifest rather than taken on trust from the caller.
        var rule = validationRuleCatalog.Resolve(equivalent.Operation, equivalent.Resource);
        // The receipt's before-state must describe what is true right before THIS write (the rollback) runs —
        // that is analysis.ObservedCurrentState (the live re-read AnalyzeAsync just took), never
        // analysis.BeforeData (the ORIGINAL execution's before-data, i.e. the state being restored TO, which is
        // legitimately empty when the original write was a CREATE and would otherwise look like a capture
        // failure here).
        var receipt = new RecoveryPackageReceipt(
            entry.ExecutionId, entry.PackagePath, entry.ManifestChecksumSha256,
            entry.ExecutedAt, entry.ExpiresAt, BeforeState: BeforeStateEvaluator.Evaluate(equivalent.Operation, analysis.ObservedCurrentState));

        var gatewayRequest = new ToolGatewayRequest(
            writeAdapter.Capability, writeAdapter.OwnerAgent, true, equivalent, decision, grant,
            [], entry.ConnectionProfile,
            new IdentityPermissionContext(confirmation.RequestedBy, HasEffectivePermission: true),
            GovernedExecutionMode.LiveExecution, receipt, rule, profile);

        var gatewayResult = await toolGateway.InvokeAsync(gatewayRequest, cancellationToken);
        if (gatewayResult.Status is ToolGatewayStatus.Blocked or ToolGatewayStatus.LiveExecutionFailed)
        {
            var status = gatewayResult.Status == ToolGatewayStatus.Blocked
                ? RollbackExecutionStatus.GovernanceBlocked
                : RollbackExecutionStatus.ExecutionFailed;
            var audit = await AuditAsync(rollbackExecutionId, analysis, confirmation, status, gatewayResult.Reasons, rollbackProposal, decision, 0, cancellationToken);
            return new(status, rollbackExecutionId, entry.ExecutionId, gatewayResult.Reasons, rollbackProposal, decision, Audit: audit);
        }

        // Post-rollback validation: re-read and compare against the ORIGINAL before-data — the state we claimed
        // to be restoring. When that target is "the resource did not exist" (rolling back a CREATE), there is
        // nothing to field-compare, and PostWriteValidator.Validate correctly refuses to call an empty
        // comparison a pass ("a validation that compared nothing has proven nothing"). That refusal is right
        // for an ordinary write, where an empty expectation means nobody set one — but here it is the actual,
        // deliberate goal, so the check that proves it is a direct absence assertion, not a field comparison.
        var restored = await snapshotSource.CaptureSnapshotAsync(entry.BusinessKeys, cancellationToken);
        var targetIsAbsence = analysis.BeforeData.Sum(set => set.Records.Count) == 0;
        PostWriteValidationReport? validation = targetIsAbsence
            ? ValidateAbsence(entry.TablesAffected, restored, timeProvider.GetUtcNow())
            : rule is not null
                ? PostWriteValidator.Validate(rule, analysis.BeforeData, restored, timeProvider.GetUtcNow())
                : null;

        var passed = validation?.Passed ?? false;
        var finalStatus = passed ? RollbackExecutionStatus.Completed : RollbackExecutionStatus.ValidationFailed;
        var recordsAffected = gatewayResult.Execution?.RecordsAffected ?? 0;

        var finalAudit = await AuditAsync(rollbackExecutionId, analysis, confirmation, finalStatus,
            passed ? [$"{ValidationReason}=PASS"] : [$"{ValidationReason}=FAIL", .. validation?.Mismatches ?? []],
            rollbackProposal, decision, recordsAffected, cancellationToken, validation, restored);

        if (passed)
        {
            await recoveryIndexStore.UpdateStatusAsync(entry.ExecutionId, RecoveryPackageStatus.RolledBack, cancellationToken);
        }

        await writeExecutionAuditStore.MarkRollbackAsync(entry.ExecutionId, rollbackExecuted: true,
            rollbackResult: finalStatus.ToString(),
            packageStatus: passed ? RecoveryPackageStatus.RolledBack : RecoveryPackageStatus.Active, cancellationToken);

        return new(finalStatus, rollbackExecutionId, entry.ExecutionId,
            passed ? [$"{ValidationReason}=PASS"] : [$"{ValidationReason}=FAIL"],
            rollbackProposal, decision, validation, finalAudit);
    }

    // ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds the proposal that describes restoring the captured before-state. The physical operation is
    /// derived OBJECTIVELY from what the Recovery Package and the live re-read prove — never assumed, never
    /// picked for convenience:
    ///
    ///   before did NOT exist, current DOES  → Delete  (the execution created it; undo = remove it)
    ///   before DID exist,     current DOES  → Update  (the execution changed it; undo = restore old values)
    ///   before DID exist,     current does NOT → Insert (the execution removed it; undo = recreate it)
    ///   before did NOT exist, current does NOT → Update (degenerate/no-op case; still goes through the
    ///     ordinary governed path rather than being special-cased away)
    ///
    /// A resulting Delete is marked with <see cref="ActionProposal.RollbackOfExecutionId"/>, which is the ONLY
    /// thing that keeps <c>AIGovernancePolicyEngine</c>'s "DELETE is always Red/Blocked" rule from applying to
    /// it — that rule still applies unconditionally to every delete that is not provably this. Either way the
    /// proposal carries an explicit filter and an explicit expected row count, precisely so the policy engine's
    /// existing rules (no blind write, no unbounded write) apply to a rollback exactly as they do to any other
    /// write. This is the generic mechanism (<see cref="Contracts.RollbackStrategy.RestoreBeforeState"/>);
    /// specific business rules for a capability declaring <see cref="Contracts.RollbackStrategy.NotSupported"/>
    /// are enforced upstream, before rollback ever reaches this method.
    /// </summary>
    private static ActionProposal BuildEquivalentProposal(
        RecoveryIndexEntry entry,
        RollbackSafetyAnalysis analysis,
        RollbackConfirmation confirmation,
        DateTimeOffset now)
    {
        var resource = entry.TablesAffected.Count > 0 ? entry.TablesAffected[0] : "unknown";
        var beforeRecords = analysis.BeforeData
            .Where(set => string.Equals(set.Resource, resource, StringComparison.OrdinalIgnoreCase))
            .SelectMany(set => set.Records)
            .ToList();
        var currentRecords = analysis.ObservedCurrentState
            .Where(set => string.Equals(set.Resource, resource, StringComparison.OrdinalIgnoreCase))
            .SelectMany(set => set.Records)
            .ToList();

        var beforeExists = beforeRecords.Count > 0;
        var currentExists = currentRecords.Count > 0;
        var operation = (beforeExists, currentExists) switch
        {
            (false, true) => ActionOperation.Delete,
            (true, false) => ActionOperation.Insert,
            _ => ActionOperation.Update,
        };

        // Fields to restore/recreate come from whichever side actually has the data: the target (before) state
        // when there is one, the current state's shape when deleting it (a delete has no "values" to carry,
        // but the field list still documents what is being removed).
        var referenceRecords = beforeExists ? beforeRecords : currentRecords;
        var fields = referenceRecords
            .SelectMany(record => record.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var expectedAffectedRows = operation == ActionOperation.Delete
            ? Math.Max(currentRecords.Count, 1)
            : Math.Max(beforeRecords.Count, 1);

        return new ActionProposal
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
            FilterSummary = string.Join(" AND ", entry.BusinessKeys),
            ExpectedAffectedRows = expectedAffectedRows,
            Purpose = $"Rollback governado da execucao {entry.ExecutionId}: {confirmation.Justification}",
            DataClassification = DataClassification.Internal,
            ContainsPersonalData = false,
            ContainsSensitivePersonalData = false,
            ContainsSecrets = false,
            Reversibility = ActionReversibility.Reversible,
            AdditionalContext = $"rollback_of_execution={entry.ExecutionId}; original_proposal_hash={entry.ProposalHash}",
            RollbackOfExecutionId = entry.ExecutionId,
        };
    }

    /// <summary>Proves that a rollback restoring "the resource did not exist" actually achieved that, by direct
    /// reconsultation — never by trusting that the write completed without exception. Only the tables the
    /// original execution actually touched are checked, so an unrelated resource with records is not mistaken
    /// for a rollback failure.</summary>
    private static PostWriteValidationReport ValidateAbsence(
        IReadOnlyList<string> tablesAffected, IReadOnlyList<RecoveryDataSet> restored, DateTimeOffset validatedAt)
    {
        var remaining = restored
            .Where(set => tablesAffected.Any(table => string.Equals(table, set.Resource, StringComparison.OrdinalIgnoreCase)))
            .Sum(set => set.Records.Count);
        var mismatches = remaining > 0
            ? [$"{string.Join(", ", tablesAffected)}: esperado ausencia (rollback de criacao), mas {remaining} registro(s) ainda presente(s)."]
            : Array.Empty<string>();
        return new PostWriteValidationReport("rollback-absence-check.v1", remaining == 0, 1, remaining > 0 ? 1 : 0, mismatches, validatedAt);
    }

    /// <summary>The environment comes from the recorded execution's own profile policy, not from a database
    /// name: an execution recorded against a Production profile is evaluated as Production.</summary>
    private static GovernanceEnvironment ResolveEnvironment(RecoveryIndexEntry entry) =>
        string.Equals(entry.ConnectionProfile, WriteVerificationProfileSeeds.LinxProduction, StringComparison.Ordinal)
            ? GovernanceEnvironment.Production
            : GovernanceEnvironment.Development;

    private static List<string> DetectConcurrentChange(
        IReadOnlyList<RecoveryDataSet> expected,
        IReadOnlyList<RecoveryDataSet> observed)
    {
        var findings = new List<string>();
        foreach (var expectedSet in expected)
        {
            var observedRecords = observed
                .Where(set => string.Equals(set.Resource, expectedSet.Resource, StringComparison.OrdinalIgnoreCase))
                .SelectMany(set => set.Records)
                .ToList();

            foreach (var expectedRecord in expectedSet.Records)
            {
                var match = observedRecords.FirstOrDefault(record =>
                    expectedRecord.All(pair => record.TryGetValue(pair.Key, out var value)
                        && string.Equals(value?.Trim() ?? string.Empty, pair.Value?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)));
                if (match is null)
                {
                    findings.Add($"{expectedSet.Resource}: registro esperado [{Describe(expectedRecord)}] nao corresponde ao estado atual.");
                }
            }
        }

        return findings;
    }

    private static string Describe(IReadOnlyDictionary<string, string?> record) =>
        string.Join(", ", record.Select(pair => $"{pair.Key}={pair.Value}"));

    /// <summary>Handle bound to (execution, manifest checksum, analysis instant). A handle from a different
    /// execution or a different analysis cannot match.</summary>
    private static string BuildConfirmationHandle(Guid executionId, string manifestChecksum, DateTimeOffset analyzedAt)
    {
        var payload = $"{executionId:N}|{manifestChecksum}|{analyzedAt.ToUniversalTime():O}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static RollbackSafetyAnalysis Analysis(
        RollbackAnalysisStatus status,
        Guid executionId,
        RecoveryIndexEntry? entry,
        RecoveryPackageManifest? manifest,
        IReadOnlyList<string> reasons,
        string summary) =>
        new(status, executionId, entry, manifest, [], [], [], [], summary, null, reasons);

    private async Task<RollbackAuditRecord> AuditAsync(
        Guid rollbackExecutionId,
        RollbackSafetyAnalysis analysis,
        RollbackConfirmation confirmation,
        RollbackExecutionStatus status,
        IReadOnlyList<string> errors,
        RollbackActionProposal? proposal,
        PolicyDecision? decision,
        int recordsAffected,
        CancellationToken cancellationToken,
        PostWriteValidationReport? validation = null,
        IReadOnlyList<RecoveryDataSet>? observedAfterRollback = null)
    {
        var record = new RollbackAuditRecord
        {
            RollbackExecutionId = rollbackExecutionId,
            OriginalExecutionId = analysis.ExecutionId,
            Requester = confirmation.RequestedBy,
            RequestedAt = timeProvider.GetUtcNow(),
            ExplicitConfirmationReceived = status != RollbackExecutionStatus.Blocked,
            ConfirmedAt = status == RollbackExecutionStatus.Blocked ? null : confirmation.ConfirmedAt,
            Justification = confirmation.Justification,
            TablesAffected = analysis.Entry?.TablesAffected ?? [],
            BusinessKeys = analysis.Entry?.BusinessKeys ?? [],
            RecordsAffected = recordsAffected,
            ConcurrencyFindings = analysis.ConcurrencyFindings,
            ExpectedStateSummary = SummarizeState(analysis.BeforeData),
            ObservedStateSummary = SummarizeState(observedAfterRollback ?? analysis.ObservedCurrentState),
            Status = status,
            PostRollbackValidationPassed = validation?.Passed ?? false,
            PostRollbackValidationRuleId = validation?.RuleId,
            Errors = errors,
            RollbackProposalHash = proposal?.EquivalentProposal.ProposalHash,
        };

        await rollbackAuditStore.AppendAsync(record, cancellationToken);
        await governanceAuditStore.AppendAsync(new GovernanceAuditEvent(
            Guid.NewGuid(), "rollback.completed", rollbackExecutionId.ToString("N"),
            proposal?.EquivalentProposal.Id, proposal?.EquivalentProposal.ProposalHash,
            analysis.Entry?.AgentId, confirmation.RequestedBy, status.ToString(), errors, record.RequestedAt), cancellationToken);
        return record;
    }

    private static string SummarizeState(IReadOnlyList<RecoveryDataSet> sets) =>
        $"{sets.Sum(set => set.Records.Count)} registro(s) em [{string.Join(", ", sets.Select(set => set.Resource))}]";
}
