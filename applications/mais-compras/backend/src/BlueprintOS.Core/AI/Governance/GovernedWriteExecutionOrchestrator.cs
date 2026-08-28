#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance;

public enum GovernedWriteExecutionStatus
{
    /// <summary>Governance refused the write. Nothing was executed.</summary>
    Blocked = 1,

    /// <summary>The proposal is valid but needs a human approval that was not supplied. Nothing was executed.</summary>
    AwaitingApproval = 2,

    /// <summary>The write ran and post-write validation passed.</summary>
    Completed = 3,

    /// <summary>The write was permitted and attempted, and the adapter reported failure.</summary>
    ExecutionFailed = 4,

    /// <summary>The write ran, but re-reading the state did not match what was expected. The recovery package
    /// is intact and a rollback is the next governed step.</summary>
    ValidationFailed = 5,
}

/// <summary>Everything the orchestrator needs that the governance stack does not already carry.
/// <c>AllowsMissingBeforeState</c> declares this write as insert-or-update by business key ("garantir X"),
/// decided by the write itself against the state it finds at execution time — so an empty before-state
/// snapshot is expected, not a capture failure, even though the context's OperationIntent is classified as
/// Update for policy/approval purposes (never Merge: that collides with the fixed "MERGE requires an approved
/// runbook" rule, which is about literal SQL MERGE). Defaults to false so every other caller keeps today's
/// strict behavior unchanged.</summary>
public sealed record GovernedWriteExecutionRequest(
    StructuredActionContext Context,
    RoutingEvidence Routing,
    AgentWriteAnalysis Analysis,
    IdentityPermissionContext Identity,
    string ExecutionName,
    string ConnectionProfile,
    string Server,
    string Database,
    IReadOnlyList<string> BusinessKeys,
    IReadOnlyList<RecoveryDataSet> ExpectedAfter,
    string OriginalRequestSummary,
    IReadOnlyList<string> ProceduresInvoked,
    bool AllowsMissingBeforeState = false);

public sealed record GovernedWriteExecutionResult(
    GovernedWriteExecutionStatus Status,
    Guid ExecutionId,
    IReadOnlyList<string> Reasons,
    GovernedWritePreparation? Preparation = null,
    RecoveryPackageReceipt? RecoveryPackage = null,
    PostWriteValidationReport? Validation = null,
    ToolGatewayResult? GatewayResult = null,
    WriteValidationKnowledgeGap? KnowledgeGap = null,
    RollbackCapabilityGap? RollbackGap = null);

/// <summary>
/// Coordinates a real governed write end to end. It does NOT reimplement governance: proposal construction,
/// policy evaluation and approval all still go through the existing <see cref="GovernedWriteStack"/>, and the
/// write itself still goes through <see cref="IToolGateway"/>. What this class adds is the recovery discipline
/// around that call:
///
///   prepare (proposal → policy → approval)
///     → resolve the write verification profile from the store
///     → resolve the post-write validation rule, or stop and record a knowledge gap
///     → capture the before-state and write the recovery package, then index it
///     → invoke the Tool Gateway in LiveExecution with the receipt, rule and profile attached
///     → re-read the after-state, apply the rule, persist the validation report
///     → append the permanent write execution audit
///
/// The order matters and is enforced by construction: the recovery package is written BEFORE the gateway call,
/// because a backup taken after a write is not a backup.
/// </summary>
public sealed class GovernedWriteExecutionOrchestrator(
    GovernedWriteStack governedWriteStack,
    IWriteVerificationProfileStore profileStore,
    IPostWriteValidationRuleCatalog validationRuleCatalog,
    IWriteValidationKnowledgeGapStore knowledgeGapStore,
    IRecoveryPackageWriter recoveryPackageWriter,
    IRecoveryIndexStore recoveryIndexStore,
    IToolGateway toolGateway,
    IWriteExecutionAuditStore writeExecutionAuditStore,
    TimeProvider timeProvider,
    IRollbackCapabilityGapStore? rollbackCapabilityGapStore = null)
{
    private readonly IRollbackCapabilityGapStore rollbackCapabilityGapStore = rollbackCapabilityGapStore ?? new InMemoryRollbackCapabilityGapStore();

    public async Task<GovernedWriteExecutionResult> ExecuteAsync(
        GovernedWriteExecutionRequest request,
        ApprovalGrant? approvalGrant,
        ISnapshotCapableAdapter snapshotSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(snapshotSource);

        var startedAt = timeProvider.GetUtcNow();
        var executionId = Guid.NewGuid();

        // 1. Ordinary governed write preparation — unchanged, reused, not reimplemented.
        var preparation = await governedWriteStack.PrepareAsync(request.Context, request.Routing, request.Analysis, cancellationToken);
        if (!preparation.ProposalBuild.Succeeded || preparation.PolicyDecision is null)
        {
            return Blocked(executionId, preparation, preparation.ProposalBuild.ContextGaps.Select(gap => $"{gap.Code}:{gap.Field}").ToArray());
        }

        var proposal = preparation.ProposalBuild.Proposal!;
        var decision = preparation.PolicyDecision;
        if (decision.Status == PolicyDecisionStatus.Blocked || decision.RiskClassification == RiskClassification.Red)
        {
            await AuditOutcomeAsync(request, executionId, proposal, null, null, null, startedAt, WriteExecutionOutcome.Blocked, ["POLICY_BLOCKED"], [], cancellationToken);
            return Blocked(executionId, preparation, ["POLICY_BLOCKED"]);
        }

        if (decision.Status == PolicyDecisionStatus.RequiresApproval && approvalGrant is null)
        {
            return new(GovernedWriteExecutionStatus.AwaitingApproval, executionId, ["VALID_APPROVAL_REQUIRED"], preparation);
        }

        // 2. Write verification policy. Absent policy means "not governed for live writes", never "no guarantees".
        var profile = await profileStore.ResolveAsync(request.ConnectionProfile, startedAt, cancellationToken);
        if (profile is null)
        {
            await AuditOutcomeAsync(request, executionId, proposal, null, null, null, startedAt, WriteExecutionOutcome.Blocked, ["WRITE_VERIFICATION_PROFILE_NOT_FOUND"], [], cancellationToken);
            return Blocked(executionId, preparation, ["WRITE_VERIFICATION_PROFILE_NOT_FOUND"]);
        }

        // 2.5. Rollback capability gate — BEFORE any backup or write, never discovered later at rollback time.
        // The profile says the ENVIRONMENT requires rollback support; the adapter says what THIS capability can
        // actually offer. A mismatch is a capability gap, not a knowledge gap: the framework knows exactly how
        // it would roll back (RollbackOrchestrator), the capability's own business rules just do not allow it.
        if (profile.RollbackSupported && snapshotSource is IWriteExecutionAdapter { RollbackStrategy: RollbackStrategy.NotSupported } capableAdapter)
        {
            var gap = new RollbackCapabilityGap(
                Guid.NewGuid(), request.Context.RequestId, request.Analysis.AgentId, request.ConnectionProfile,
                capableAdapter.Capability, request.Context.Resource, RollbackCapabilityGap.ReasonCode, proposal.Id, startedAt);
            await rollbackCapabilityGapStore.RecordAsync(gap, cancellationToken);
            await AuditOutcomeAsync(request, executionId, proposal, profile, null, null, startedAt, WriteExecutionOutcome.Blocked,
                [RollbackCapabilityGap.ReasonCode], [RollbackCapabilityGap.ReasonCode], cancellationToken);
            return new(GovernedWriteExecutionStatus.Blocked, executionId, [RollbackCapabilityGap.ReasonCode], preparation, RollbackGap: gap);
        }

        // 3. Post-write validation rule, or a recorded knowledge gap and a hard stop.
        PostWriteValidationRule? rule = validationRuleCatalog.Resolve(proposal.Operation, proposal.Resource);
        if (profile.PostWriteValidationRequired && rule is null)
        {
            var gap = new WriteValidationKnowledgeGap(
                Guid.NewGuid(), request.Context.RequestId, request.Analysis.AgentId, request.ConnectionProfile,
                proposal.Resource, proposal.Operation, WriteValidationKnowledgeGap.ReasonCode, proposal.Id, startedAt);
            await knowledgeGapStore.RecordAsync(gap, cancellationToken);
            await AuditOutcomeAsync(request, executionId, proposal, profile, null, null, startedAt, WriteExecutionOutcome.Blocked,
                [WriteValidationKnowledgeGap.ReasonCode], [WriteValidationKnowledgeGap.ReasonCode], cancellationToken);
            return new(GovernedWriteExecutionStatus.Blocked, executionId, [WriteValidationKnowledgeGap.ReasonCode], preparation, KnowledgeGap: gap);
        }

        // 4. Recovery package — written BEFORE the write, never after.
        RecoveryPackageReceipt? receipt = null;
        IReadOnlyList<RecoveryDataSet> beforeData = [];
        if (profile.BackupRequired)
        {
            beforeData = await snapshotSource.CaptureSnapshotAsync(request.BusinessKeys, cancellationToken);
            var manifest = new RecoveryPackageManifest
            {
                ExecutionId = executionId,
                ExecutionName = request.ExecutionName,
                AgentId = request.Analysis.AgentId,
                ConnectionProfile = request.ConnectionProfile,
                Server = request.Server,
                Database = request.Database,
                ExecutedAt = startedAt,
                Requester = request.Context.RequestedBy,
                OriginalRequestSummary = request.OriginalRequestSummary,
                OperationTypes = [proposal.Operation],
                TablesAffected = [proposal.Resource],
                BusinessKeys = request.BusinessKeys,
                RecordsExpectedToChange = proposal.ExpectedAffectedRows ?? request.ExpectedAfter.Sum(set => set.Records.Count),
                BackupRequired = profile.BackupRequired,
                RollbackSupported = profile.RollbackSupported,
                RetentionDays = profile.BackupRetentionDays,
                ExpiresAt = startedAt.AddDays(profile.BackupRetentionDays),
                ValidationRuleId = rule?.RuleId ?? "none",
                ProposalHash = proposal.ProposalHash,
            };

            receipt = await recoveryPackageWriter.CreateAsync(manifest, beforeData, request.ExpectedAfter, request.AllowsMissingBeforeState, cancellationToken);
            await recoveryIndexStore.AppendAsync(new RecoveryIndexEntry(
                executionId, manifest.ExecutionName, manifest.AgentId, manifest.ConnectionProfile, manifest.Server,
                manifest.Database, manifest.ExecutedAt, manifest.Requester, manifest.OperationTypes, manifest.TablesAffected,
                manifest.BusinessKeys, manifest.RecordsExpectedToChange, manifest.BackupRequired, manifest.RollbackSupported,
                manifest.RetentionDays, manifest.ExpiresAt, receipt.PackagePath, receipt.ManifestChecksumSha256,
                RecoveryPackageStatus.Active, manifest.ProposalHash, manifest.ValidationRuleId), cancellationToken);
        }

        // 5. The write itself, through the same Tool Gateway as every other governed action.
        var gatewayRequest = new ToolGatewayRequest(
            request.Analysis.Capability, request.Analysis.AgentId, request.Routing.RoutingResolved,
            proposal, decision, approvalGrant, request.Routing.CrossCuttingAgents, request.ConnectionProfile,
            request.Identity, GovernedExecutionMode.LiveExecution, receipt, rule, profile);

        var gatewayResult = await toolGateway.InvokeAsync(gatewayRequest, cancellationToken);
        if (gatewayResult.Status == ToolGatewayStatus.Blocked)
        {
            await AuditOutcomeAsync(request, executionId, proposal, profile, receipt, null, startedAt, WriteExecutionOutcome.Blocked, gatewayResult.Reasons, [], cancellationToken);
            return new(GovernedWriteExecutionStatus.Blocked, executionId, gatewayResult.Reasons, preparation, receipt, null, gatewayResult);
        }

        if (gatewayResult.Status == ToolGatewayStatus.LiveExecutionFailed)
        {
            await AuditOutcomeAsync(request, executionId, proposal, profile, receipt, null, startedAt, WriteExecutionOutcome.ExecutionFailed,
                gatewayResult.Execution?.ErrorMessage is { } error ? [.. gatewayResult.Reasons, error] : gatewayResult.Reasons, [], cancellationToken);
            return new(GovernedWriteExecutionStatus.ExecutionFailed, executionId, gatewayResult.Reasons, preparation, receipt, null, gatewayResult);
        }

        // 6. Re-read the real state and prove the write actually landed. The adapter's own reported after-data
        //    is only a fallback: a fresh re-read is what makes the validation independent of the writer.
        var afterData = await snapshotSource.CaptureSnapshotAsync(request.BusinessKeys, cancellationToken);
        if (afterData.Count == 0) afterData = gatewayResult.Execution?.AfterData ?? [];

        PostWriteValidationReport? report = null;
        if (rule is not null)
        {
            report = PostWriteValidator.Validate(rule, request.ExpectedAfter, afterData, timeProvider.GetUtcNow());
        }

        if (receipt is not null)
        {
            await recoveryPackageWriter.WriteAfterDataAsync(receipt, afterData, cancellationToken);
            if (report is not null) await recoveryPackageWriter.WriteValidationReportAsync(receipt, report, cancellationToken);
        }

        var passed = report?.Passed ?? !profile.PostWriteValidationRequired;
        var outcome = passed ? WriteExecutionOutcome.Completed : WriteExecutionOutcome.ValidationFailed;
        await AuditOutcomeAsync(request, executionId, proposal, profile, receipt, report, startedAt, outcome,
            passed ? [] : report?.Mismatches ?? ["POST_WRITE_VALIDATION_FAILED"], [], cancellationToken,
            gatewayResult.Execution?.RecordsAffected ?? 0, beforeData, afterData);

        return new(
            passed ? GovernedWriteExecutionStatus.Completed : GovernedWriteExecutionStatus.ValidationFailed,
            executionId,
            passed ? ["LIVE_EXECUTION_COMPLETED", "POST_WRITE_VALIDATION_PASSED"] : ["POST_WRITE_VALIDATION_FAILED"],
            preparation, receipt, report, gatewayResult);
    }

    private static GovernedWriteExecutionResult Blocked(Guid executionId, GovernedWritePreparation preparation, IReadOnlyList<string> reasons) =>
        new(GovernedWriteExecutionStatus.Blocked, executionId, reasons, preparation);

    private Task AuditOutcomeAsync(
        GovernedWriteExecutionRequest request,
        Guid executionId,
        ActionProposal proposal,
        WriteVerificationProfile? profile,
        RecoveryPackageReceipt? receipt,
        PostWriteValidationReport? report,
        DateTimeOffset startedAt,
        WriteExecutionOutcome outcome,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> knowledgeGaps,
        CancellationToken cancellationToken,
        int recordsAffected = 0,
        IReadOnlyList<RecoveryDataSet>? beforeData = null,
        IReadOnlyList<RecoveryDataSet>? afterData = null) =>
        writeExecutionAuditStore.AppendAsync(new WriteExecutionAuditRecord
        {
            ExecutionId = executionId,
            ExecutionName = request.ExecutionName,
            AgentId = request.Analysis.AgentId,
            ConnectionProfile = request.ConnectionProfile,
            WriteVerificationPolicyVersion = profile?.PolicyVersion ?? "<unresolved>",
            Server = request.Server,
            Database = request.Database,
            StartedAt = startedAt,
            CompletedAt = timeProvider.GetUtcNow(),
            Requester = request.Context.RequestedBy,
            Intent = request.Context.Purpose,
            Operations = [proposal.Operation],
            TablesAffected = [proposal.Resource],
            BusinessKeys = request.BusinessKeys,
            RecordsAffected = recordsAffected,
            ProceduresInvoked = request.ProceduresInvoked,
            BeforeAfterSummary = Summarize(beforeData, afterData),
            ChangedFields = proposal.Fields,
            ValidationRuleId = report?.RuleId ?? "none",
            RecordsValidated = report?.RecordsValidated ?? 0,
            RecordsWithErrors = report?.RecordsWithErrors ?? 0,
            PostWriteValidationPassed = report?.Passed ?? false,
            BackupRequired = profile?.BackupRequired ?? false,
            BackupCreated = receipt is not null,
            RetentionDays = profile?.BackupRetentionDays ?? 0,
            BackupExpiresAt = receipt?.ExpiresAt,
            RecoveryPackageStatus = receipt is null ? RecoveryPackageStatus.Expired : RecoveryPackageStatus.Active,
            RollbackAvailable = receipt is not null && (profile?.RollbackSupported ?? false),
            Errors = errors,
            KnowledgeGaps = knowledgeGaps,
            Outcome = outcome,
            ProposalHash = proposal.ProposalHash,
        }, cancellationToken);

    /// <summary>Compact, non-payload summary: counts and resources only, so the permanent audit never becomes a
    /// long-lived copy of business (or personal) data.</summary>
    private static string Summarize(IReadOnlyList<RecoveryDataSet>? before, IReadOnlyList<RecoveryDataSet>? after)
    {
        var beforeCount = before?.Sum(set => set.Records.Count) ?? 0;
        var afterCount = after?.Sum(set => set.Records.Count) ?? 0;
        var resources = (before ?? []).Concat(after ?? []).Select(set => set.Resource).Distinct(StringComparer.OrdinalIgnoreCase);
        return $"before={beforeCount} registro(s); after={afterCount} registro(s); recursos=[{string.Join(", ", resources)}]";
    }
}
