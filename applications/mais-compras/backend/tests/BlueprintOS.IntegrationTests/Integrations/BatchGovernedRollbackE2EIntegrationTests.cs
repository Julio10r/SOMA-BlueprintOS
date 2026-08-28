#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence.Governance;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace BlueprintOS.IntegrationTests.Integrations;

/// <summary>
/// REAL end-to-end homologation of Recovery Package v2 (batch/chunked) — <see cref="BatchRecoveryPackageWriter"/>
/// and <see cref="BatchRollbackOrchestrator"/> — against SOMA_DESENV (192.168.9.98) ONLY, following the exact
/// same judgment call as <see cref="PedGradeAdjustmentE2EIntegrationTests"/>: two already-existing, undelivered
/// COMPRAS_PRODUTO rows are selected, their original state captured up front, and both are restored byte-for-
/// byte in a <c>finally</c> block regardless of outcome.
///
/// There is deliberately no standalone "batch write orchestrator" in this codebase yet (out of this task's
/// scope — see BatchRecoveryPackageWriter's remarks). The forward writes here go through the SAME real,
/// homologated single-item governed write path (<see cref="GovernedWriteExecutionOrchestrator"/> +
/// <see cref="PedGradeAdjustmentGovernedWriteAdapter"/>) used by the 77 production executions — one real write
/// per row — and this test then assembles the batch Recovery Package around those two real writes, which is
/// exactly what item 2 describes as the intended usage: a batch package documents N self-contained items as one
/// logical execution, however those items were produced.
///
/// Opt-in only: requires GOVERNANCE_E2E_TESTS=1. Never enabled in CI.
/// </summary>
public sealed class BatchGovernedRollbackE2EIntegrationTests(ITestOutputHelper output)
{
    private const string RequestedBy = "julio.cesar@somagrupo.com.br";

    [Fact]
    public async Task EndToEnd_BatchRecoveryPackage_FullRollback_Then_SelectiveRollback_Against_SomaDesenv()
    {
        var (configuration, connectionString) = LoadConfiguration();
        if (connectionString is null)
        {
            output.WriteLine("GOVERNANCE_E2E_TESTS!=1 ou connection string ausente/placeholder — teste ignorado (nunca ativo em CI).");
            return;
        }

        var backupsRoot = Path.Combine(RuntimeRootLocator.ResolveRuntimeRoot(), "backups", "e2e-batch-test-scratch");
        var rows = await FindCandidateRowsAsync(connectionString, count: 2);
        output.WriteLine($"Linhas candidatas: {string.Join(" | ", rows.Select(r => $"PEDIDO={r.Pedido},PRODUTO={r.Produto},COR={r.Cor}"))}");

        var originals = new Dictionary<(string, string, string), CapturedRow>();
        foreach (var r in rows)
        {
            originals[r] = await ReadRowAsync(connectionString, r.Pedido, r.Produto, r.Cor)
                ?? throw new InvalidOperationException("Linha candidata desapareceu antes do teste iniciar.");
        }

        try
        {
            var batchWriter = new BatchRecoveryPackageWriter(backupsRoot);
            var index = new InMemoryRecoveryIndexStore();
            var rollbackAudit = new InMemoryRollbackAuditStore();
            var governanceRoot = NewGovernanceRoot(backupsRoot);
            var governanceAudit = new FileGovernanceAuditStore(governanceRoot);
            var approvals = new FileApprovalStore(governanceRoot);
            var profileStore = new InMemoryWriteVerificationProfileStore();
            var clock = new FixedTimeProvider(DateTimeOffset.UtcNow);

            var orchestrator = new BatchRollbackOrchestrator(
                index, batchWriter, new PostWriteValidationRuleCatalog(), new AIGovernancePolicyEngine(), new ApprovalPolicy(),
                approvals, profileStore, rollbackAudit, governanceAudit, clock);

            // =====================================================================================
            // FASE A — dois writes reais governados (path single-item ja homologado), agrupados em UM
            // Recovery Package v2 (batch).
            // =====================================================================================
            output.WriteLine("=== FASE A: dois writes governados reais + montagem do batch package ===");

            var batchExecutionId = Guid.NewGuid();
            var executedAt = clock.GetUtcNow();
            var items = new List<BatchRecoveryItem>();

            foreach (var r in rows)
            {
                var original = originals[r];
                var writeAuditA = new InMemoryWriteExecutionAuditStore();
                var singleIndexA = new InMemoryRecoveryIndexStore();
                var singleRoot = NewGovernanceRoot(backupsRoot);
                var writer = new RecoveryPackageWriter(Path.Combine(backupsRoot, "single-item-scratch"));

                var desired = new PedGradeAdjustmentRequest(r.Pedido, r.Produto, r.Cor,
                    original.Co1 + 1, original.Co2 + 1, original.Co3 + 1, original.Co4 + 1, original.Co5 + 1, original.Co6 + 1);
                var forwardAdapter = new PedGradeAdjustmentGovernedWriteAdapter(configuration, desired);
                var (fwOrchestrator, _) = BuildOrchestrator(forwardAdapter, singleRoot, profileStore, singleIndexA, writeAuditA, writer, clock);

                var request = Request(r.Pedido, r.Produto, r.Cor, desired);
                var grant = GrantFor(request.Context, clock.GetUtcNow(), r.Pedido, r.Produto, r.Cor);
                var result = await fwOrchestrator.ExecuteAsync(request, grant, forwardAdapter);
                Assert.Equal(GovernedWriteExecutionStatus.Completed, result.Status);
                output.WriteLine($"Write real concluido para {BusinessKey(r.Pedido, r.Produto, r.Cor)}: execution_id={result.ExecutionId}");

                var afterRow = await ReadRowAsync(connectionString, r.Pedido, r.Produto, r.Cor);
                Assert.NotNull(afterRow);

                items.Add(new BatchRecoveryItem(
                    BusinessKey(r.Pedido, r.Produto, r.Cor), PedGradeAdjustmentGovernedWriteAdapter.TableName,
                    RowSet(r.Pedido, r.Produto, r.Cor, original), RowSet(r.Pedido, r.Produto, r.Cor, afterRow!)));
            }

            var manifest = new BatchRecoveryPackageManifest
            {
                BatchExecutionId = batchExecutionId,
                ExecutionName = "ajuste-grade-lote-e2e",
                AgentId = PedGradeAdjustmentGovernedWriteAdapter.OwnerAgentId,
                Capability = PedGradeAdjustmentGovernedWriteAdapter.CapabilityId,
                ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment,
                Server = "192.168.9.98",
                Database = "SOMA_DESENV",
                ExecutedAt = executedAt,
                Requester = RequestedBy,
                Origin = "BatchGovernedRollbackE2EIntegrationTests",
                OriginalRequestSummary = "Homologacao E2E do Recovery Package v2 (lote).",
                OperationTypes = [ActionOperation.Update],
                TablesAffected = [PedGradeAdjustmentGovernedWriteAdapter.TableName],
                TotalItems = 0,
                ChunkCount = 0,
                MaxItemsPerChunk = 0,
                MaxChunkSizeBytes = 0,
                BackupRequired = true,
                RollbackSupported = true,
                RetentionDays = 30,
                ExpiresAt = executedAt.AddDays(30),
                ValidationRuleId = PostWriteValidationRuleCatalog.PedGradeAdjustmentRule.RuleId,
                ProposalHash = new string('e', 64),
                Status = BatchStatus.Active,
                ChunkBeforeDataChecksumsSha256 = new Dictionary<int, string>(),
            };

            var receipt = await batchWriter.CreateBatchAsync(manifest, items);
            await batchWriter.WriteChunkAfterDataAsync(receipt.PackagePath, 1, items.Select(i => i.ExpectedAfter).ToArray());
            await index.AppendAsync(new RecoveryIndexEntry(
                batchExecutionId, manifest.ExecutionName, manifest.AgentId, manifest.ConnectionProfile, manifest.Server,
                manifest.Database, manifest.ExecutedAt, manifest.Requester, manifest.OperationTypes, manifest.TablesAffected,
                items.Select(i => i.BusinessKey).ToArray(), items.Count, true, true, 30, manifest.ExpiresAt,
                receipt.PackagePath, receipt.ManifestChecksumSha256, RecoveryPackageStatus.Active, manifest.ProposalHash, manifest.ValidationRuleId));

            output.WriteLine($"Batch package criado: {receipt.PackagePath} ({receipt.ChunkCount} chunk(s), {receipt.TotalItems} item(ns)).");
            Assert.True(File.Exists(Path.Combine(receipt.PackagePath, BatchRecoveryPackageWriter.ItemsIndexFileName)));

            // =====================================================================================
            // FASE B — ROLLBACK COMPLETO DO LOTE (2 itens)
            // =====================================================================================
            output.WriteLine("=== FASE B: rollback completo do lote ===");

            var snapshotAdapters = rows.ToDictionary(r => r, r => new PedGradeAdjustmentGovernedWriteAdapter(configuration,
                new PedGradeAdjustmentRequest(r.Pedido, r.Produto, r.Cor, 0, 0, 0, 0, 0, 0)));
            var combinedSnapshot = new RoutedSnapshotAdapter(rows, snapshotAdapters);

            var analysisFull = await orchestrator.AnalyzeAsync(batchExecutionId, [], combinedSnapshot);
            output.WriteLine($"Analise (completo): status={analysisFull.Status}, ready={analysisFull.ReadyItems.Count}, findings={analysisFull.ConcurrencyFindings.Count}");
            Assert.Equal(BatchRollbackAnalysisStatus.ReadyForConfirmation, analysisFull.Status);
            Assert.Equal(2, analysisFull.ReadyItems.Count);
            Assert.Empty(analysisFull.ConcurrencyFindings);

            var confirmationFull = new BatchRollbackConfirmation(batchExecutionId, analysisFull.ConfirmationHandle!, RequestedBy,
                "Teste E2E do Recovery Package v2 — rollback completo do lote.", clock.GetUtcNow());

            var resultFull = await orchestrator.ExecuteAsync(
                analysisFull, confirmationFull,
                item => BuildRestoreGateway(configuration, item, rows, originals, governanceAudit, clock),
                (key, ct) => CaptureByKeyAsync(rows, snapshotAdapters, key, ct),
                (proposal, decision, req, ct) => Task.FromResult<ApprovalGrant?>(
                    new ApprovalGrant(Guid.NewGuid(), req.Id, proposal.ProposalHash, "authorized-product-owner",
                        clock.GetUtcNow(), clock.GetUtcNow().AddMinutes(30), "rollback de lote E2E", null, null)));

            output.WriteLine($"Rollback completo — status={resultFull.Status}; itens={string.Join(", ", resultFull.ItemOutcomes.Select(o => $"{o.BusinessKey}={o.Success}"))}");
            Assert.Equal(BatchRollbackExecutionStatus.Completed, resultFull.Status);
            Assert.All(resultFull.ItemOutcomes, o => Assert.True(o.Success));

            foreach (var r in rows)
            {
                var restored = await ReadRowAsync(connectionString, r.Pedido, r.Produto, r.Cor);
                var original = originals[r];
                Assert.Equal(original.Co1, restored!.Co1);
                Assert.Equal(original.Co2, restored.Co2);
                Assert.Equal(original.Co3, restored.Co3);
                Assert.Equal(original.Co4, restored.Co4);
                Assert.Equal(original.Co5, restored.Co5);
                Assert.Equal(original.Co6, restored.Co6);
            }
            output.WriteLine("Rollback completo — ambas as linhas confirmadas restauradas ao estado original em SOMA_DESENV.");

            var entryAfterFull = Assert.Single(await index.FindAsync(new RecoveryIndexQuery { ExecutionId = batchExecutionId }));
            Assert.Equal(RecoveryPackageStatus.RolledBack, entryAfterFull.Status);
            output.WriteLine("Rollback completo — RecoveryIndexEntry do lote = RolledBack.");

            // =====================================================================================
            // FASE C — SEGUNDO LOTE (2 novos writes reais) + ROLLBACK SELETIVO (so 1 dos 2 itens)
            // =====================================================================================
            output.WriteLine("=== FASE C: segundo lote + rollback seletivo ===");

            var batchExecutionId2 = Guid.NewGuid();
            var items2 = new List<BatchRecoveryItem>();
            foreach (var r in rows)
            {
                var baseline = originals[r]; // rows are back to original after phase B
                var writeAuditC = new InMemoryWriteExecutionAuditStore();
                var singleIndexC = new InMemoryRecoveryIndexStore();
                var singleRootC = NewGovernanceRoot(backupsRoot);
                var writerC = new RecoveryPackageWriter(Path.Combine(backupsRoot, "single-item-scratch"));

                var desired = new PedGradeAdjustmentRequest(r.Pedido, r.Produto, r.Cor,
                    baseline.Co1 + 2, baseline.Co2 + 2, baseline.Co3 + 2, baseline.Co4 + 2, baseline.Co5 + 2, baseline.Co6 + 2);
                var forwardAdapter = new PedGradeAdjustmentGovernedWriteAdapter(configuration, desired);
                var (fwOrchestrator, _) = BuildOrchestrator(forwardAdapter, singleRootC, profileStore, singleIndexC, writeAuditC, writerC, clock);

                var request = Request(r.Pedido, r.Produto, r.Cor, desired);
                var grant = GrantFor(request.Context, clock.GetUtcNow(), r.Pedido, r.Produto, r.Cor);
                var result = await fwOrchestrator.ExecuteAsync(request, grant, forwardAdapter);
                Assert.Equal(GovernedWriteExecutionStatus.Completed, result.Status);

                var afterRow = await ReadRowAsync(connectionString, r.Pedido, r.Produto, r.Cor);
                items2.Add(new BatchRecoveryItem(
                    BusinessKey(r.Pedido, r.Produto, r.Cor), PedGradeAdjustmentGovernedWriteAdapter.TableName,
                    RowSet(r.Pedido, r.Produto, r.Cor, baseline), RowSet(r.Pedido, r.Produto, r.Cor, afterRow!)));
            }

            var manifest2 = manifest with { BatchExecutionId = batchExecutionId2, ProposalHash = new string('f', 64) };
            var receipt2 = await batchWriter.CreateBatchAsync(manifest2, items2);
            await batchWriter.WriteChunkAfterDataAsync(receipt2.PackagePath, 1, items2.Select(i => i.ExpectedAfter).ToArray());
            await index.AppendAsync(new RecoveryIndexEntry(
                batchExecutionId2, manifest2.ExecutionName, manifest2.AgentId, manifest2.ConnectionProfile, manifest2.Server,
                manifest2.Database, manifest2.ExecutedAt, manifest2.Requester, manifest2.OperationTypes, manifest2.TablesAffected,
                items2.Select(i => i.BusinessKey).ToArray(), items2.Count, true, true, 30, manifest2.ExpiresAt,
                receipt2.PackagePath, receipt2.ManifestChecksumSha256, RecoveryPackageStatus.Active, manifest2.ProposalHash, manifest2.ValidationRuleId));

            var selectiveKey = BusinessKey(rows[0].Pedido, rows[0].Produto, rows[0].Cor);
            var analysisSelective = await orchestrator.AnalyzeAsync(batchExecutionId2, [selectiveKey], combinedSnapshot);
            Assert.Equal(BatchRollbackAnalysisStatus.ReadyForConfirmation, analysisSelective.Status);
            Assert.Single(analysisSelective.ReadyItems);

            var confirmationSelective = new BatchRollbackConfirmation(batchExecutionId2, analysisSelective.ConfirmationHandle!, RequestedBy,
                "Teste E2E do Recovery Package v2 — rollback seletivo de 1 item do lote.", clock.GetUtcNow());

            var resultSelective = await orchestrator.ExecuteAsync(
                analysisSelective, confirmationSelective,
                item => BuildRestoreGateway(configuration, item, rows, originals, governanceAudit, clock),
                (key, ct) => CaptureByKeyAsync(rows, snapshotAdapters, key, ct),
                (proposal, decision, req, ct) => Task.FromResult<ApprovalGrant?>(
                    new ApprovalGrant(Guid.NewGuid(), req.Id, proposal.ProposalHash, "authorized-product-owner",
                        clock.GetUtcNow(), clock.GetUtcNow().AddMinutes(30), "rollback seletivo E2E", null, null)));

            output.WriteLine($"Rollback seletivo — status={resultSelective.Status}; item={selectiveKey}");
            Assert.Equal(BatchRollbackExecutionStatus.Completed, resultSelective.Status);
            Assert.Single(resultSelective.ItemOutcomes);

            var restoredRow0 = await ReadRowAsync(connectionString, rows[0].Pedido, rows[0].Produto, rows[0].Cor);
            Assert.Equal(originals[rows[0]].Co1, restoredRow0!.Co1);
            output.WriteLine("Rollback seletivo — linha 0 restaurada ao original; linha 1 permanece com o valor do segundo write (nao solicitado).");

            var row1AfterSelective = await ReadRowAsync(connectionString, rows[1].Pedido, rows[1].Produto, rows[1].Cor);
            Assert.NotEqual(originals[rows[1]].Co1, row1AfterSelective!.Co1);
            output.WriteLine("Rollback seletivo confirmado: apenas o item solicitado foi restaurado; o outro item do lote ficou intacto.");
        }
        finally
        {
            foreach (var r in rows)
            {
                await RestoreOriginalStateAsync(connectionString, r.Pedido, r.Produto, r.Cor, originals[r]);
            }
            output.WriteLine("Limpeza final: ambas as linhas COMPRAS_PRODUTO restauradas byte-a-byte ao estado original em SOMA_DESENV.");
            foreach (var root in GovernanceRoots)
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
                catch (IOException) { }
            }
            try { if (Directory.Exists(backupsRoot)) Directory.Delete(backupsRoot, recursive: true); }
            catch (IOException) { }
        }
    }

    // ------------------------------------------------------------------------------------------------------
    // Wiring helpers
    // ------------------------------------------------------------------------------------------------------

    private static (IToolGateway Gateway, string Capability) BuildRestoreGateway(
        IConfiguration configuration, BatchItemRestorePlan item,
        List<(string Pedido, string Produto, string Cor)> rows,
        Dictionary<(string, string, string), CapturedRow> originals,
        IGovernanceAuditStore governanceAudit, TimeProvider clock)
    {
        var row = rows.Single(r => BusinessKey(r.Pedido, r.Produto, r.Cor) == item.BusinessKey);
        var target = item.TargetRecord!; // restoring means the target record always exists here (Update).
        var restoreRequest = new PedGradeAdjustmentRequest(
            row.Pedido, row.Produto, row.Cor,
            int.Parse(target["CO1"]!), int.Parse(target["CO2"]!), int.Parse(target["CO3"]!),
            int.Parse(target["CO4"]!), int.Parse(target["CO5"]!), int.Parse(target["CO6"]!));
        var adapter = new PedGradeAdjustmentGovernedWriteAdapter(configuration, restoreRequest);
        var gateway = new ToolGateway([adapter], new ApprovalPolicy(), governanceAudit, clock);
        return (gateway, PedGradeAdjustmentGovernedWriteAdapter.CapabilityId);
    }

    private static async Task<IReadOnlyList<RecoveryDataSet>> CaptureByKeyAsync(
        List<(string Pedido, string Produto, string Cor)> rows,
        Dictionary<(string, string, string), PedGradeAdjustmentGovernedWriteAdapter> snapshotAdapters,
        string businessKey, CancellationToken ct)
    {
        var row = rows.Single(r => BusinessKey(r.Pedido, r.Produto, r.Cor) == businessKey);
        return await snapshotAdapters[row].CaptureSnapshotAsync([businessKey], ct);
    }

    /// <summary>
    /// Routes a snapshot request to the ONE adapter bound to the requested business key. This matters because
    /// <see cref="PedGradeAdjustmentGovernedWriteAdapter.CaptureSnapshotAsync"/> ignores its own
    /// <c>businessKeys</c> argument and always returns the row for whichever PEDIDO/PRODUTO/COR_PRODUTO it was
    /// constructed with (see <c>BuildRestoreGateway</c>'s remarks and the adapter's own doc comment) — so
    /// concatenating every adapter's result regardless of which key was actually asked for would silently hand
    /// <see cref="BatchRollbackOrchestrator.AnalyzeAsync"/> the wrong row for a per-item concurrency check.
    /// </summary>
    private sealed class RoutedSnapshotAdapter(
        List<(string Pedido, string Produto, string Cor)> rows,
        Dictionary<(string, string, string), PedGradeAdjustmentGovernedWriteAdapter> adaptersByRow) : ISnapshotCapableAdapter
    {
        public Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default)
        {
            var key = businessKeys[0];
            var row = rows.Single(r => BusinessKey(r.Pedido, r.Produto, r.Cor) == key);
            return adaptersByRow[row].CaptureSnapshotAsync(businessKeys, cancellationToken);
        }
    }

    private static string BusinessKey(string pedido, string produto, string cor) => $"PEDIDO={pedido}|PRODUTO={produto}|COR_PRODUTO={cor}";

    private static RecoveryDataSet RowSet(string pedido, string produto, string cor, CapturedRow row) => new(
        PedGradeAdjustmentGovernedWriteAdapter.TableName,
        [new Dictionary<string, string?>
        {
            ["PEDIDO"] = pedido, ["PRODUTO"] = produto, ["COR_PRODUTO"] = cor,
            ["CO1"] = row.Co1.ToString(), ["CO2"] = row.Co2.ToString(), ["CO3"] = row.Co3.ToString(),
            ["CO4"] = row.Co4.ToString(), ["CO5"] = row.Co5.ToString(), ["CO6"] = row.Co6.ToString(),
        }]);

    private static GovernedWriteExecutionRequest Request(string pedido, string produto, string cor, PedGradeAdjustmentRequest desired) => new(
        Context(pedido, produto, cor, desired), Routing(), Analysis(pedido, produto, cor),
        new IdentityPermissionContext(RequestedBy, HasEffectivePermission: true),
        "ped-grade-adjustment",
        WriteVerificationProfileSeeds.LinxDevelopment,
        "192.168.9.98",
        "SOMA_DESENV",
        [BusinessKey(pedido, produto, cor)],
        [ExpectedAfterSet(pedido, produto, cor, desired)],
        "Homologacao E2E do Recovery Package v2 (lote).",
        ["LX_MOVIMENTA_COMPRAS_PA", "LX_RECALCULO_RESERVA_MATERIAIS"],
        AllowsMissingBeforeState: false);

    private static StructuredActionContext Context(string pedido, string produto, string cor, PedGradeAdjustmentRequest desired) => new(
        $"REQ-BATCH-E2E-{pedido}-{produto}-{cor}", RequestedBy, GovernanceEnvironment.Development, "SOMA/Linx",
        ActionResourceType.DatabaseTable, PostWriteValidationRuleCatalog.PedGradeAdjustmentResource, OperationIntent.Update,
        [PedGradeAdjustmentGovernedWriteAdapter.CapabilityId], ["CO1", "CO2", "CO3", "CO4", "CO5", "CO6"],
        BusinessKey(pedido, produto, cor), 1,
        "Homologacao E2E do Recovery Package v2 (lote).",
        DataClassification.Internal, false, false, false, ActionReversibility.Reversible,
        ConnectionProfile: WriteVerificationProfileSeeds.LinxDevelopment);

    private static RoutingEvidence Routing() => new(true, PedGradeAdjustmentGovernedWriteAdapter.OwnerAgentId, [], [], [], []);

    private static AgentWriteAnalysis Analysis(string pedido, string produto, string cor) => new(
        PedGradeAdjustmentGovernedWriteAdapter.OwnerAgentId, PedGradeAdjustmentGovernedWriteAdapter.CapabilityId,
        ["CO1", "CO2", "CO3", "CO4", "CO5", "CO6"], BusinessKey(pedido, produto, cor), 1, ActionReversibility.Reversible);

    private static RecoveryDataSet ExpectedAfterSet(string pedido, string produto, string cor, PedGradeAdjustmentRequest desired) =>
        new(PedGradeAdjustmentGovernedWriteAdapter.TableName,
        [
            new Dictionary<string, string?>
            {
                ["PEDIDO"] = pedido, ["PRODUTO"] = produto, ["COR_PRODUTO"] = cor,
                ["CO1"] = desired.Tam1.ToString(), ["CO2"] = desired.Tam2.ToString(), ["CO3"] = desired.Tam3.ToString(),
                ["CO4"] = desired.Tam4.ToString(), ["CO5"] = desired.Tam5.ToString(), ["CO6"] = desired.Tam6.ToString(),
            },
        ]);

    private static ApprovalGrant GrantFor(StructuredActionContext context, DateTimeOffset now, string pedido, string produto, string cor)
    {
        var build = new StructuredActionProposalAdapter().Build(context, Routing(), Analysis(pedido, produto, cor), now);
        var hash = build.Proposal?.ProposalHash ?? new string('0', 64);
        return new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), hash, "authorized-product-owner", now, now.AddMinutes(30), "grant E2E lote", null, null);
    }

    private static (GovernedWriteExecutionOrchestrator Orchestrator, IApprovalStore ApprovalStore) BuildOrchestrator(
        IWriteExecutionAdapter adapter, string governanceRoot, IWriteVerificationProfileStore profileStore,
        IRecoveryIndexStore index, IWriteExecutionAuditStore writeAudit, IRecoveryPackageWriter recoveryWriter, TimeProvider clock)
    {
        var governanceAudit = new FileGovernanceAuditStore(governanceRoot);
        var approvals = new FileApprovalStore(governanceRoot);
        var gateway = new ToolGateway([adapter], new ApprovalPolicy(), governanceAudit, clock);
        var stack = new GovernedWriteStack(new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, governanceAudit, gateway, clock);
        var orchestrator = new GovernedWriteExecutionOrchestrator(
            stack, profileStore, new PostWriteValidationRuleCatalog(), new InMemoryWriteValidationKnowledgeGapStore(),
            recoveryWriter, index, gateway, writeAudit, clock);
        return (orchestrator, approvals);
    }

    private static readonly List<string> GovernanceRoots = [];

    private static string NewGovernanceRoot(string backupsRoot)
    {
        var root = Path.Combine(backupsRoot, "governance-scratch", Guid.NewGuid().ToString("N"));
        GovernanceRoots.Add(root);
        return root;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    // ------------------------------------------------------------------------------------------------------
    // Direct-SQL helpers — candidate selection, snapshotting, restoration. Never the governed write path.
    // ------------------------------------------------------------------------------------------------------

    private static (IConfiguration Configuration, string? ConnectionString) LoadConfiguration()
    {
        if (Environment.GetEnvironmentVariable("GOVERNANCE_E2E_TESTS") != "1") return (null!, null);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets("BlueprintOS-Development")
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("LinxDevelopmentConnection")
            ?? configuration.GetConnectionString("ErpConnection");
        return string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal)
            ? (configuration, null)
            : (configuration, connectionString);
    }

    private static async Task<List<(string Pedido, string Produto, string Cor)>> FindCandidateRowsAsync(string connectionString, int count)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT TOP {count} cp.PEDIDO, cp.PRODUTO, cp.COR_PRODUTO
            FROM COMPRAS_PRODUTO cp
            JOIN COMPRAS c ON c.PEDIDO = cp.PEDIDO
            WHERE cp.CO7 = 0 AND cp.QTDE_ENTREGUE = 0
              AND (cp.CO1 + cp.CO2 + cp.CO3 + cp.CO4 + cp.CO5 + cp.CO6) > 0
              AND ISNUMERIC(cp.PEDIDO) = 1 AND cp.PEDIDO NOT LIKE '%[^0-9 ]%'
            ORDER BY cp.PEDIDO DESC
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<(string, string, string)>();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0).Trim(), reader.GetString(1).Trim(), reader.GetString(2).Trim()));
        }

        if (results.Count < count)
        {
            throw new InvalidOperationException($"Menos de {count} linhas candidatas encontradas em COMPRAS_PRODUTO no SOMA_DESENV.");
        }

        return results;
    }

    private sealed record CapturedRow(int Co1, int Co2, int Co3, int Co4, int Co5, int Co6, int Co7,
        int QtdeOriginal, int QtdeEntregar);

    private static async Task<CapturedRow?> ReadRowAsync(string connectionString, string pedido, string produto, string cor)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CO1,CO2,CO3,CO4,CO5,CO6,CO7,QTDE_ORIGINAL,QTDE_ENTREGAR
            FROM COMPRAS_PRODUTO WHERE PEDIDO=@pedido AND PRODUTO=@produto AND COR_PRODUTO=@cor
            """;
        AddKeyParameters(command, pedido, produto, cor);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        int I(int i) => reader[i] is DBNull ? 0 : Convert.ToInt32(reader[i]);
        return new CapturedRow(I(0), I(1), I(2), I(3), I(4), I(5), I(6), I(7), I(8));
    }

    private static async Task RestoreOriginalStateAsync(string? connectionString, string pedido, string produto, string cor, CapturedRow original)
    {
        if (connectionString is null) return;
        try
        {
            await using var connection = await OpenAsync(connectionString);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE COMPRAS_PRODUTO SET
                  CO1=@co1, CO2=@co2, CO3=@co3, CO4=@co4, CO5=@co5, CO6=@co6,
                  CE1=@co1, CE2=@co2, CE3=@co3, CE4=@co4, CE5=@co5, CE6=@co6,
                  QTDE_ORIGINAL=@qtdeOriginal, QTDE_ENTREGAR=@qtdeEntregar
                WHERE PEDIDO=@pedido AND PRODUTO=@produto AND COR_PRODUTO=@cor
                """;
            command.Parameters.Add(new SqlParameter("@co1", original.Co1));
            command.Parameters.Add(new SqlParameter("@co2", original.Co2));
            command.Parameters.Add(new SqlParameter("@co3", original.Co3));
            command.Parameters.Add(new SqlParameter("@co4", original.Co4));
            command.Parameters.Add(new SqlParameter("@co5", original.Co5));
            command.Parameters.Add(new SqlParameter("@co6", original.Co6));
            command.Parameters.Add(new SqlParameter("@qtdeOriginal", original.QtdeOriginal));
            command.Parameters.Add(new SqlParameter("@qtdeEntregar", original.QtdeEntregar));
            AddKeyParameters(command, pedido, produto, cor);
            await command.ExecuteNonQueryAsync();

            await using var movimenta = connection.CreateCommand();
            movimenta.CommandText = "EXEC LX_MOVIMENTA_COMPRAS_PA @PEDIDO";
            movimenta.Parameters.Add(new SqlParameter("@PEDIDO", pedido));
            await movimenta.ExecuteNonQueryAsync();

            await using var recalculo = connection.CreateCommand();
            recalculo.CommandText = "EXEC LX_RECALCULO_RESERVA_MATERIAIS @PRODUTO=@produto, @XORDEM_PRODUCAO=@pedido";
            recalculo.Parameters.Add(new SqlParameter("@produto", produto));
            recalculo.Parameters.Add(new SqlParameter("@pedido", pedido));
            await recalculo.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best effort — mirrors PedGradeAdjustmentE2EIntegrationTests's own restore helper.
        }
    }

    private static void AddKeyParameters(SqlCommand command, string pedido, string produto, string cor)
    {
        command.Parameters.Add(new SqlParameter("@pedido", pedido));
        command.Parameters.Add(new SqlParameter("@produto", produto));
        command.Parameters.Add(new SqlParameter("@cor", cor));
    }

    private static async Task<SqlConnection> OpenAsync(string connectionString)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
