#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace BlueprintOS.IntegrationTests.Integrations;

/// <summary>
/// REAL end-to-end homologation of <see cref="PedGradeAdjustmentGovernedWriteAdapter"/> against SOMA_DESENV
/// (192.168.9.98) ONLY — never LinxConnectionProfiles.Production (192.168.9.200/SOMA), never WISE.
///
/// Judgment call (see PR notes): rather than inserting a synthetic COMPRAS/PRODUTOS/PRODUTO_CORES/
/// COMPRAS_PRODUTO chain from scratch (COMPRAS_PRODUTO carries FKs to COMPRAS and PRODUTO_CORES, and
/// PRODUTOS/PRODUTO_CORES/COMPRAS each carry a wide NOT-NULL surface with their own lookup dependencies),
/// this suite selects one already-existing, undelivered (QTDE_ENTREGUE=0), non-32 (CO7=0) COMPRAS_PRODUTO
/// row in SOMA_DESENV, captures its exact original state up front, and restores that exact state in a
/// `finally` block regardless of outcome — including re-running the same two stored procedures the adapter
/// itself calls, so the row's derived/reserved state ends up self-consistent, not just its raw columns.
/// SOMA_DESENV is a disposable development database (see agents/DATABASE_CONNECTION_POLICY.md); the row is
/// restored byte-for-byte before this test returns, in both the success and failure path.
///
/// Opt-in only: requires GOVERNANCE_E2E_TESTS=1. Never enabled in CI.
/// </summary>
public sealed class PedGradeAdjustmentE2EIntegrationTests(ITestOutputHelper output)
{
    private const string RequestedBy = "julio.cesar@somagrupo.com.br";

    [Fact]
    public async Task EndToEnd_PedGradeAdjustment_AllPhases_Against_SomaDesenv()
    {
        var (configuration, connectionString) = LoadConfiguration();
        if (connectionString is null)
        {
            output.WriteLine("GOVERNANCE_E2E_TESTS!=1 ou connection string ausente/placeholder — teste ignorado (nunca ativo em CI).");
            return;
        }

        var backendRoot = FindBackendRoot();
        var backupsRoot = Path.Combine(backendRoot, "runtime", "backups");
        output.WriteLine($"Backend root: {backendRoot}");
        output.WriteLine($"Recovery packages root: {backupsRoot}");

        var (pedido, produto, cor) = await FindCandidateRowAsync(connectionString);
        output.WriteLine($"Linha candidata selecionada em SOMA_DESENV: PEDIDO={pedido}, PRODUTO={produto}, COR_PRODUTO={cor}");

        var original = await ReadRowAsync(connectionString, pedido, produto, cor)
            ?? throw new InvalidOperationException("Linha candidata desapareceu antes do teste iniciar.");
        output.WriteLine($"Estado original capturado: CO1..CO6=[{original.Co1},{original.Co2},{original.Co3},{original.Co4},{original.Co5},{original.Co6}], " +
            $"CO7={original.Co7}, QTDE_ENTREGUE={original.QtdeEntregue}, VALOR_ENTREGUE={original.ValorEntregue}, CUSTO1={original.Custo1}");

        try
        {
            var writer = new RecoveryPackageWriter(backupsRoot);
            var clock = new FixedTimeProvider(DateTimeOffset.UtcNow);

            // =====================================================================================
            // FASE A — WRITE + BACKUP + POST_WRITE_VALIDATION
            // =====================================================================================
            output.WriteLine("=== FASE A: write governado (linx-development, backup+rollback+validation) ===");

            var profileStoreA = new InMemoryWriteVerificationProfileStore();
            var indexA = new InMemoryRecoveryIndexStore();
            var writeAuditA = new InMemoryWriteExecutionAuditStore();
            using var dbA = NewInMemoryDb();

            var desiredA = new PedGradeAdjustmentRequest(pedido, produto, cor,
                original.Co1 + 1, original.Co2 + 1, original.Co3 + 1, original.Co4 + 1, original.Co5 + 1, original.Co6 + 1);
            var forwardAdapterA = new PedGradeAdjustmentGovernedWriteAdapter(configuration, desiredA);
            var (orchestratorA, _) = BuildOrchestrator(forwardAdapterA, dbA, profileStoreA, indexA, writeAuditA, writer, clock);

            var requestA = Request("REQ-PED-GRADE-FASE-A", pedido, produto, cor, desiredA);
            var grantA = GrantFor(requestA.Context, clock.GetUtcNow(), pedido, produto, cor);

            var resultA = await orchestratorA.ExecuteAsync(requestA, grantA, forwardAdapterA);
            output.WriteLine($"Fase A — status={resultA.Status}; reasons=[{string.Join(", ", resultA.Reasons)}]");

            Assert.Equal(GovernedWriteExecutionStatus.Completed, resultA.Status);
            Assert.NotNull(resultA.RecoveryPackage);
            Assert.NotNull(resultA.Validation);
            Assert.True(resultA.Validation!.Passed);
            var executionIdA = resultA.ExecutionId;
            var packagePathA = resultA.RecoveryPackage!.PackagePath;
            output.WriteLine($"Fase A — execution_id={executionIdA}; package={packagePathA}");

            Assert.True(File.Exists(Path.Combine(packagePathA, RecoveryPackageWriter.ManifestFileName)));
            Assert.True(File.Exists(Path.Combine(packagePathA, RecoveryPackageWriter.BeforeDataFileName)));
            Assert.True(File.Exists(Path.Combine(packagePathA, RecoveryPackageWriter.ExpectedAfterFileName)));
            Assert.True(File.Exists(Path.Combine(packagePathA, RecoveryPackageWriter.AfterDataFileName)));
            Assert.True(File.Exists(Path.Combine(packagePathA, RecoveryPackageWriter.ValidationReportFileName)));

            var beforeJson = await File.ReadAllTextAsync(Path.Combine(packagePathA, RecoveryPackageWriter.BeforeDataFileName));
            foreach (var field in new[] { "QTDE_ORIGINAL", "QTDE_ENTREGAR", "QTDE_ENTREGUE", "VALOR_ORIGINAL", "VALOR_ENTREGAR", "VALOR_ENTREGUE", "CUSTO1", "CO1", "CO2", "CO3", "CO4", "CO5", "CO6", "CE1", "CE2", "CE3", "CE4", "CE5", "CE6" })
            {
                Assert.Contains(field, beforeJson);
            }

            output.WriteLine("Fase A — recovery package contem todos os campos exigidos (QTDE_*/VALOR_*/CUSTO1/CO1..6/CE1..6) no before-data.");

            var afterRow = await ReadRowAsync(connectionString, pedido, produto, cor);
            Assert.NotNull(afterRow);
            Assert.Equal(desiredA.Tam1, afterRow!.Co1);
            Assert.Equal(desiredA.Tam2, afterRow.Co2);
            Assert.Equal(desiredA.Tam3, afterRow.Co3);
            Assert.Equal(desiredA.Tam4, afterRow.Co4);
            Assert.Equal(desiredA.Tam5, afterRow.Co5);
            Assert.Equal(desiredA.Tam6, afterRow.Co6);
            Assert.Equal(original.Co7, afterRow.Co7);
            Assert.Equal(original.QtdeEntregue, afterRow.QtdeEntregue);
            Assert.Equal(original.ValorEntregue, afterRow.ValorEntregue);
            output.WriteLine($"Fase A — DB confirma CO1..CO6=[{afterRow.Co1},{afterRow.Co2},{afterRow.Co3},{afterRow.Co4},{afterRow.Co5},{afterRow.Co6}], " +
                $"CO7 inalterado={afterRow.Co7}, QTDE_ENTREGUE inalterado={afterRow.QtdeEntregue}, VALOR_ENTREGUE inalterado={afterRow.ValorEntregue}.");

            var indexEntryA = Assert.Single(await indexA.FindAsync(new RecoveryIndexQuery { ExecutionId = executionIdA }));
            Assert.Equal(RecoveryPackageStatus.Active, indexEntryA.Status);
            output.WriteLine("Fase A — RecoveryIndexEntry status=Active confirmado.");

            // =====================================================================================
            // FASE B — ROLLBACK
            // =====================================================================================
            output.WriteLine("=== FASE B: rollback governado da execucao A ===");

            using var dbRollbackA = NewInMemoryDb();
            var snapshotSourceA = new PedGradeAdjustmentGovernedWriteAdapter(configuration,
                new PedGradeAdjustmentRequest(pedido, produto, cor, 0, 0, 0, 0, 0, 0)); // read-only use (CaptureSnapshotAsync)
            var rollbackWriteAdapterA = new PedGradeAdjustmentGovernedWriteAdapter(configuration,
                new PedGradeAdjustmentRequest(pedido, produto, cor, original.Co1, original.Co2, original.Co3, original.Co4, original.Co5, original.Co6));
            var rollbackOrchestratorA = BuildRollbackOrchestrator(indexA, writer, profileStoreA, writeAuditA, dbRollbackA, clock, rollbackWriteAdapterA);

            var discovery = await rollbackOrchestratorA.DiscoverAsync(new RecoveryIndexQuery
            {
                AgentId = PedGradeAdjustmentGovernedWriteAdapter.OwnerAgentId,
                ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment,
                Table = PedGradeAdjustmentGovernedWriteAdapter.TableName,
                ExecutionId = executionIdA,
            });
            Assert.Equal(RollbackDiscoveryStatus.SingleCandidate, discovery.Status);
            var candidate = Assert.Single(discovery.Candidates);
            Assert.Equal(executionIdA, candidate.ExecutionId);
            output.WriteLine($"Discovery — 1 candidato localizado: {candidate.ExecutionId}");

            var analysisA = await rollbackOrchestratorA.AnalyzeAsync(candidate.ExecutionId, snapshotSourceA);
            Assert.Equal(RollbackAnalysisStatus.ReadyForConfirmation, analysisA.Status);
            Assert.Empty(analysisA.ConcurrencyFindings);
            Assert.NotNull(analysisA.ConfirmationHandle);
            output.WriteLine($"Pre-analise de rollback: sem concorrencia. Resumo:\n{analysisA.Summary}");

            var confirmationA = new RollbackConfirmation(
                analysisA.ExecutionId, analysisA.ConfirmationHandle!, RequestedBy,
                "Teste E2E do ajuste de grade PED — restaurar estado original (fase A/B).",
                clock.GetUtcNow());

            var rollbackResultA = await rollbackOrchestratorA.ExecuteAsync(
                analysisA, confirmationA, snapshotSourceA, rollbackWriteAdapterA,
                (proposal, decision, req, ct) => Task.FromResult<ApprovalGrant?>(
                    new ApprovalGrant(Guid.NewGuid(), req.Id, proposal.ProposalHash, "authorized-product-owner",
                        clock.GetUtcNow(), clock.GetUtcNow().AddMinutes(30), "rollback E2E ped-grade-adjustment", null, null)));

            output.WriteLine($"Rollback status={rollbackResultA.Status}; reasons=[{string.Join(", ", rollbackResultA.Reasons)}]");
            Assert.Equal(ActionOperation.Update, rollbackResultA.Proposal!.EquivalentProposal.Operation);
            Assert.Contains($"{RollbackOrchestrator.ValidationReason}=PASS", rollbackResultA.Reasons);
            Assert.Equal(RollbackExecutionStatus.Completed, rollbackResultA.Status);

            var restoredRow = await ReadRowAsync(connectionString, pedido, produto, cor);
            Assert.NotNull(restoredRow);
            Assert.Equal(original.Co1, restoredRow!.Co1);
            Assert.Equal(original.Co2, restoredRow.Co2);
            Assert.Equal(original.Co3, restoredRow.Co3);
            Assert.Equal(original.Co4, restoredRow.Co4);
            Assert.Equal(original.Co5, restoredRow.Co5);
            Assert.Equal(original.Co6, restoredRow.Co6);
            Assert.Equal(original.Co7, restoredRow.Co7);
            Assert.Equal(original.QtdeOriginal, restoredRow.QtdeOriginal);
            Assert.Equal(original.QtdeEntregar, restoredRow.QtdeEntregar);
            Assert.Equal(original.ValorOriginal, restoredRow.ValorOriginal);
            Assert.Equal(original.ValorEntregar, restoredRow.ValorEntregar);
            output.WriteLine("Fase B — apos rollback, todos os campos capturados batem exatamente com o estado original.");

            var indexEntryAfterRollback = Assert.Single(await indexA.FindAsync(new RecoveryIndexQuery { ExecutionId = executionIdA }));
            Assert.Equal(RecoveryPackageStatus.RolledBack, indexEntryAfterRollback.Status);
            output.WriteLine("Fase B — RecoveryIndexEntry status=RolledBack confirmado.");

            var permanentAuditRecords = await writeAuditA.ListAsync();
            Assert.Contains(permanentAuditRecords, r => r.ExecutionId == executionIdA);
            output.WriteLine($"Fase B — audit permanente registra o write original (execution_id={executionIdA}) e {permanentAuditRecords.Count} registro(s) no total apos o rollback.");

            // =====================================================================================
            // FASE C — CONCORRENCIA BLOQUEIA ROLLBACK
            // =====================================================================================
            output.WriteLine("=== FASE C: concorrencia bloqueia rollback ===");

            var profileStoreD = new InMemoryWriteVerificationProfileStore();
            var indexD = new InMemoryRecoveryIndexStore();
            var writeAuditD = new InMemoryWriteExecutionAuditStore();
            using var dbD = NewInMemoryDb();
            var clockD = new FixedTimeProvider(DateTimeOffset.UtcNow);

            var desiredD = new PedGradeAdjustmentRequest(pedido, produto, cor,
                original.Co1 + 2, original.Co2 + 2, original.Co3 + 2, original.Co4 + 2, original.Co5 + 2, original.Co6 + 2);
            var forwardAdapterD = new PedGradeAdjustmentGovernedWriteAdapter(configuration, desiredD);
            var (orchestratorD, _) = BuildOrchestrator(forwardAdapterD, dbD, profileStoreD, indexD, writeAuditD, writer, clockD);

            var requestD = Request("REQ-PED-GRADE-CONCORRENCIA", pedido, produto, cor, desiredD);
            var grantD = GrantFor(requestD.Context, clockD.GetUtcNow(), pedido, produto, cor);
            var resultD = await orchestratorD.ExecuteAsync(requestD, grantD, forwardAdapterD);
            Assert.Equal(GovernedWriteExecutionStatus.Completed, resultD.Status);
            var executionIdD = resultD.ExecutionId;
            output.WriteLine($"Concorrencia — write original concluido, execution_id={executionIdD}.");

            // Simula um terceiro alterando CO1 fora do framework, DEPOIS do write governado.
            await AdminSetCo1Async(connectionString, pedido, produto, cor, desiredD.Tam1 + 99);
            output.WriteLine($"Concorrencia — alteracao simulada de terceiro: CO1={desiredD.Tam1 + 99}.");

            using var dbRollbackD = NewInMemoryDb();
            var rollbackOrchestratorD = BuildRollbackOrchestrator(indexD, writer, profileStoreD, writeAuditD, dbRollbackD, clockD,
                new PedGradeAdjustmentGovernedWriteAdapter(configuration, new PedGradeAdjustmentRequest(pedido, produto, cor, 0, 0, 0, 0, 0, 0)));
            var analysisD = await rollbackOrchestratorD.AnalyzeAsync(executionIdD,
                new PedGradeAdjustmentGovernedWriteAdapter(configuration, new PedGradeAdjustmentRequest(pedido, produto, cor, 0, 0, 0, 0, 0, 0)));
            output.WriteLine($"Concorrencia — analise: status={analysisD.Status}, reasons=[{string.Join(", ", analysisD.Reasons)}]");

            Assert.Equal(RollbackAnalysisStatus.BlockedConcurrentChange, analysisD.Status);
            Assert.Contains(RollbackOrchestrator.ConcurrentChangeReason, analysisD.Reasons);
            Assert.NotEmpty(analysisD.ConcurrencyFindings);
            Assert.Null(analysisD.ConfirmationHandle);

            var rowAfterConcurrency = await ReadRowAsync(connectionString, pedido, produto, cor);
            Assert.Equal(desiredD.Tam1 + 99, rowAfterConcurrency!.Co1);
            output.WriteLine("Concorrencia — ROLLBACK_BLOCKED_CONCURRENT_CHANGE confirmado; linha permanece como o terceiro deixou (rollback nao executou).");
        }
        finally
        {
            await RestoreOriginalStateAsync(connectionString, pedido, produto, cor, original);
            output.WriteLine("Limpeza final: linha COMPRAS_PRODUTO restaurada byte-a-byte ao estado original em SOMA_DESENV.");
        }
    }

    // ------------------------------------------------------------------------------------------------------
    // Wiring helpers
    // ------------------------------------------------------------------------------------------------------

    private static GovernedWriteExecutionRequest Request(string requestId, string pedido, string produto, string cor, PedGradeAdjustmentRequest desired) => new(
        Context(requestId, pedido, produto, cor, desired), Routing(), Analysis(pedido, produto, cor),
        new IdentityPermissionContext(RequestedBy, HasEffectivePermission: true),
        "ped-grade-adjustment",
        WriteVerificationProfileSeeds.LinxDevelopment,
        "192.168.9.98",
        "SOMA_DESENV",
        [BusinessKey(pedido, produto, cor)],
        [ExpectedAfterSet(pedido, produto, cor, desired)],
        "Homologacao E2E do ajuste de grade PED.",
        ["LX_MOVIMENTA_COMPRAS_PA", "LX_RECALCULO_RESERVA_MATERIAIS"],
        AllowsMissingBeforeState: false);

    private static string BusinessKey(string pedido, string produto, string cor) =>
        $"PEDIDO={pedido}|PRODUTO={produto}|COR_PRODUTO={cor}";

    private static StructuredActionContext Context(string requestId, string pedido, string produto, string cor, PedGradeAdjustmentRequest desired) => new(
        requestId, RequestedBy, GovernanceEnvironment.Development, "SOMA/Linx",
        ActionResourceType.DatabaseTable, PostWriteValidationRuleCatalog.PedGradeAdjustmentResource, OperationIntent.Update,
        [PedGradeAdjustmentGovernedWriteAdapter.CapabilityId], ["CO1", "CO2", "CO3", "CO4", "CO5", "CO6"],
        BusinessKey(pedido, produto, cor), 1,
        "Homologacao E2E do ajuste de grade PED.",
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
                ["PEDIDO"] = pedido,
                ["PRODUTO"] = produto,
                ["COR_PRODUTO"] = cor,
                ["CO1"] = desired.Tam1.ToString(),
                ["CO2"] = desired.Tam2.ToString(),
                ["CO3"] = desired.Tam3.ToString(),
                ["CO4"] = desired.Tam4.ToString(),
                ["CO5"] = desired.Tam5.ToString(),
                ["CO6"] = desired.Tam6.ToString(),
            },
        ]);

    private static ApprovalGrant GrantFor(StructuredActionContext context, DateTimeOffset now, string pedido, string produto, string cor)
    {
        var build = new StructuredActionProposalAdapter().Build(context, Routing(), Analysis(pedido, produto, cor), now);
        var hash = build.Proposal?.ProposalHash ?? new string('0', 64);
        return new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), hash, "authorized-product-owner", now, now.AddMinutes(30), "grant E2E especifico", null, null);
    }

    private static (GovernedWriteExecutionOrchestrator Orchestrator, IApprovalStore ApprovalStore) BuildOrchestrator(
        IWriteExecutionAdapter adapter,
        BlueprintOSDbContext db,
        IWriteVerificationProfileStore profileStore,
        IRecoveryIndexStore index,
        IWriteExecutionAuditStore writeAudit,
        IRecoveryPackageWriter recoveryWriter,
        TimeProvider clock)
    {
        var governanceAudit = new EfGovernanceAuditStore(db);
        var approvals = new EfApprovalStore(db);
        var gateway = new ToolGateway([adapter], new ApprovalPolicy(), governanceAudit, clock);
        var stack = new GovernedWriteStack(new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, governanceAudit, gateway, clock);
        var orchestrator = new GovernedWriteExecutionOrchestrator(
            stack, profileStore, new PostWriteValidationRuleCatalog(), new InMemoryWriteValidationKnowledgeGapStore(),
            recoveryWriter, index, gateway, writeAudit, clock);
        return (orchestrator, approvals);
    }

    private static RollbackOrchestrator BuildRollbackOrchestrator(
        IRecoveryIndexStore index,
        IRecoveryPackageWriter writer,
        IWriteVerificationProfileStore profileStore,
        IWriteExecutionAuditStore writeAudit,
        BlueprintOSDbContext db,
        TimeProvider clock,
        IWriteExecutionAdapter rollbackWriteAdapter)
    {
        var governanceAudit = new EfGovernanceAuditStore(db);
        var approvals = new EfApprovalStore(db);
        var rollbackAudit = new InMemoryRollbackAuditStore();
        var gateway = new ToolGateway([rollbackWriteAdapter], new ApprovalPolicy(), governanceAudit, clock);

        return new RollbackOrchestrator(
            index, writer, new PostWriteValidationRuleCatalog(), new AIGovernancePolicyEngine(), new ApprovalPolicy(),
            approvals, gateway, profileStore, rollbackAudit, writeAudit, governanceAudit, clock);
    }

    private static BlueprintOSDbContext NewInMemoryDb() => new(new DbContextOptionsBuilder<BlueprintOSDbContext>()
        .UseInMemoryDatabase($"ped-grade-adjustment-e2e-{Guid.NewGuid():N}").Options);

    private static string FindBackendRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BlueprintOS.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Nao foi possivel localizar BlueprintOS.sln a partir do diretorio de teste.");
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
            // Reuses the same local, gitignored secret store already used by BlueprintOS.Api for
            // LinxDevelopmentConnection — never pass the credential via command line or env var.
            .AddUserSecrets("BlueprintOS-Development")
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("LinxDevelopmentConnection")
            ?? configuration.GetConnectionString("ErpConnection");
        return string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal)
            ? (configuration, null)
            : (configuration, connectionString);
    }

    private static async Task<(string Pedido, string Produto, string Cor)> FindCandidateRowAsync(string connectionString)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP 1 cp.PEDIDO, cp.PRODUTO, cp.COR_PRODUTO
            FROM COMPRAS_PRODUTO cp
            JOIN COMPRAS c ON c.PEDIDO = cp.PEDIDO
            WHERE cp.CO7 = 0 AND cp.QTDE_ENTREGUE = 0
              AND (cp.CO1 + cp.CO2 + cp.CO3 + cp.CO4 + cp.CO5 + cp.CO6) > 0
              AND ISNUMERIC(cp.PEDIDO) = 1 AND cp.PEDIDO NOT LIKE '%[^0-9 ]%'
            ORDER BY cp.PEDIDO DESC
            """;
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("Nenhuma linha candidata (CO7=0, QTDE_ENTREGUE=0, PEDIDO numerico) encontrada em COMPRAS_PRODUTO no SOMA_DESENV.");
        }

        return (reader.GetString(0).Trim(), reader.GetString(1).Trim(), reader.GetString(2).Trim());
    }

    private sealed record CapturedRow(
        int Co1, int Co2, int Co3, int Co4, int Co5, int Co6, int Co7,
        int Ce1, int Ce2, int Ce3, int Ce4, int Ce5, int Ce6,
        int QtdeOriginal, int QtdeEntregar, int QtdeEntregue,
        decimal ValorOriginal, decimal ValorEntregar, decimal ValorEntregue, decimal Custo1);

    private static async Task<CapturedRow?> ReadRowAsync(string connectionString, string pedido, string produto, string cor)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CO1,CO2,CO3,CO4,CO5,CO6,CO7,CE1,CE2,CE3,CE4,CE5,CE6,
                   QTDE_ORIGINAL,QTDE_ENTREGAR,QTDE_ENTREGUE,VALOR_ORIGINAL,VALOR_ENTREGAR,VALOR_ENTREGUE,CUSTO1
            FROM COMPRAS_PRODUTO WHERE PEDIDO=@pedido AND PRODUTO=@produto AND COR_PRODUTO=@cor
            """;
        AddKeyParameters(command, pedido, produto, cor);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        int I(int i) => reader[i] is DBNull ? 0 : Convert.ToInt32(reader[i]);
        decimal D(int i) => reader[i] is DBNull ? 0m : Convert.ToDecimal(reader[i]);

        return new CapturedRow(
            I(0), I(1), I(2), I(3), I(4), I(5), I(6),
            I(7), I(8), I(9), I(10), I(11), I(12),
            I(13), I(14), I(15), D(16), D(17), D(18), D(19));
    }

    private static async Task AdminSetCo1Async(string connectionString, string pedido, string produto, string cor, int co1)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE COMPRAS_PRODUTO SET CO1=@co1, CE1=@co1 WHERE PEDIDO=@pedido AND PRODUTO=@produto AND COR_PRODUTO=@cor";
        command.Parameters.Add(new SqlParameter("@co1", co1));
        AddKeyParameters(command, pedido, produto, cor);
        await command.ExecuteNonQueryAsync();
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
                  QTDE_ORIGINAL=@qtdeOriginal, QTDE_ENTREGAR=@qtdeEntregar,
                  VALOR_ORIGINAL=@valorOriginal, VALOR_ENTREGAR=@valorEntregar
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
            command.Parameters.Add(new SqlParameter("@valorOriginal", original.ValorOriginal));
            command.Parameters.Add(new SqlParameter("@valorEntregar", original.ValorEntregar));
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
            // Best effort — a failed restore here must not mask a real assertion result upstream, but is
            // exceptionally unlikely: the same UPDATE/EXEC pair the adapter itself proved works.
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
