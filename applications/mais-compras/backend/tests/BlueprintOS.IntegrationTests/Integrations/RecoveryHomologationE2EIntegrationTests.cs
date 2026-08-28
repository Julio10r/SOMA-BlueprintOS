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
/// REAL end-to-end validation of the "Production Write Verification &amp; Recovery Policy"
/// (agents/DATABASE_CONNECTION_POLICY.md §24) against SOMA_DESENV (192.168.9.98) ONLY — never
/// LinxConnectionProfiles.Production (192.168.9.200/SOMA), never WISE.
///
/// Deliberately GENERIC: unlike an earlier attempt at this suite, it does NOT use
/// GarantirFornecedorGovernedWriteAdapter to prove rollback. That capability has its own real business rule
/// (never destroy an existing supplier/role — see <see cref="GarantirFornecedorGovernedWriteAdapter"/>'s own
/// RollbackStrategy.NotSupported) and is not a generic rollback fixture. Instead this suite creates a disposable,
/// non-business table — <see cref="RecoveryHomologationWriteAdapter.TableName"/> — that exists ONLY in
/// SOMA_DESENV and ONLY for the duration of this run, dropped at the end regardless of outcome.
///
/// Opt-in only: requires <c>GOVERNANCE_E2E_TESTS=1</c> AND a real <c>ConnectionStrings:ErpConnection</c> (or the
/// legacy fallback already used elsewhere) pointing at SOMA_DESENV. Never enable in CI.
/// </summary>
public sealed class RecoveryHomologationE2EIntegrationTests(ITestOutputHelper output)
{
    private const string RowId = "1";
    private const string Antes = "ANTES";
    private const string Depois = "DEPOIS";
    private const string RequestedBy = "julio.cesar@somagrupo.com.br";
    private static readonly string Table = RecoveryHomologationWriteAdapter.TableName;

    [Fact]
    public async Task EndToEnd_Generic_Recovery_Homologation_AllPhases_Against_SomaDesenv()
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
        output.WriteLine($"Tabela de homologacao (SOMA_DESENV apenas): {Table}");

        await CreateTableAsync(connectionString);
        try
        {
            await ResetRowAsync(connectionString, Antes);

            // =====================================================================================
            // FASE A — backup_required=true, rollback_supported=true
            // =====================================================================================
            output.WriteLine("=== FASE A: write governado com backup+rollback (tabela generica) ===");

            var writer = new RecoveryPackageWriter(backupsRoot);
            var clockA = new FixedTimeProvider(DateTimeOffset.UtcNow);
            var profileStoreA = new InMemoryWriteVerificationProfileStore(); // seeds = phase A effective today
            var indexA = new InMemoryRecoveryIndexStore();
            var writeAuditA = new InMemoryWriteExecutionAuditStore();

            using var dbA = NewInMemoryDb();
            var forwardAdapterA = new RecoveryHomologationWriteAdapter(configuration, RowId, Depois);
            var (orchestratorA, approvalsA) = BuildOrchestrator(forwardAdapterA, dbA, profileStoreA, indexA, writeAuditA, writer, clockA);

            var requestA = Request("REQ-HOMOLOG-FASE-A");
            var grantA = GrantFor(requestA.Context, clockA.GetUtcNow());

            var resultA = await orchestratorA.ExecuteAsync(requestA, grantA, forwardAdapterA);
            output.WriteLine($"Fase A — status={resultA.Status}; reasons=[{string.Join(", ", resultA.Reasons)}]");

            Assert.Equal(GovernedWriteExecutionStatus.Completed, resultA.Status);
            Assert.NotNull(resultA.RecoveryPackage);
            Assert.True(resultA.Validation!.Passed);
            var executionIdA = resultA.ExecutionId;
            var packagePathA = resultA.RecoveryPackage!.PackagePath;
            output.WriteLine($"Fase A — execution_id={executionIdA}");
            output.WriteLine($"Fase A — recovery package path={packagePathA}");

            Assert.True(File.Exists(Path.Combine(packagePathA, RecoveryPackageWriter.ManifestFileName)));
            Assert.True(File.Exists(Path.Combine(packagePathA, RecoveryPackageWriter.BeforeDataFileName)));
            Assert.True(File.Exists(Path.Combine(packagePathA, RecoveryPackageWriter.AfterDataFileName)));
            Assert.True(File.Exists(Path.Combine(packagePathA, RecoveryPackageWriter.ValidationReportFileName)));
            Assert.Contains(RecoveryHomologationWriteAdapter.OwnerAgentId, packagePathA);
            Assert.Contains(WriteVerificationProfileSeeds.LinxDevelopment, packagePathA);

            var beforeJson = await File.ReadAllTextAsync(Path.Combine(packagePathA, RecoveryPackageWriter.BeforeDataFileName));
            var afterJson = await File.ReadAllTextAsync(Path.Combine(packagePathA, RecoveryPackageWriter.AfterDataFileName));
            Assert.Contains(Antes, beforeJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(Depois, afterJson, StringComparison.OrdinalIgnoreCase);

            var indexEntryA = Assert.Single(await indexA.FindAsync(new RecoveryIndexQuery { ExecutionId = executionIdA }));
            Assert.Equal(RecoveryPackageStatus.Active, indexEntryA.Status);

            Assert.Equal(Depois, await ReadValorAsync(connectionString, RowId));
            output.WriteLine("Fase A — VALOR confirmado = DEPOIS em SOMA_DESENV apos o write governado.");

            // ---- ROLLBACK completo da execucao A --------------------------------------------------
            output.WriteLine("=== FASE A: rollback governado ===");

            using var dbRollbackA = NewInMemoryDb();
            var snapshotSourceA = new RecoveryHomologationWriteAdapter(configuration, RowId, targetValue: null); // read-only use here
            var rollbackWriteAdapterA = new RecoveryHomologationWriteAdapter(configuration, RowId, Antes);
            var rollbackOrchestratorA = BuildRollbackOrchestrator(indexA, writer, profileStoreA, writeAuditA, dbRollbackA, clockA, rollbackWriteAdapterA);

            var discovery = await rollbackOrchestratorA.DiscoverAsync(new RecoveryIndexQuery
            {
                AgentId = RecoveryHomologationWriteAdapter.OwnerAgentId,
                ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment,
                Table = Table,
            });
            Assert.Equal(RollbackDiscoveryStatus.SingleCandidate, discovery.Status);
            var candidate = Assert.Single(discovery.Candidates);
            Assert.Equal(executionIdA, candidate.ExecutionId);
            output.WriteLine($"Discovery — 1 candidato localizado: {candidate.ExecutionId}");

            var analysisA = await rollbackOrchestratorA.AnalyzeAsync(candidate.ExecutionId, snapshotSourceA);
            Assert.Equal(RollbackAnalysisStatus.ReadyForConfirmation, analysisA.Status);
            Assert.Empty(analysisA.ConcurrencyFindings);
            Assert.NotNull(analysisA.ConfirmationHandle);
            output.WriteLine($"Pre-analise de seguranca: sem concorrencia. Resumo:\n{analysisA.Summary}");

            var confirmationA = new RollbackConfirmation(
                analysisA.ExecutionId, analysisA.ConfirmationHandle!, RequestedBy,
                "Teste E2E generico do framework de Recovery — restaurar VALOR=ANTES (fase A).",
                clockA.GetUtcNow());

            var rollbackResultA = await rollbackOrchestratorA.ExecuteAsync(
                analysisA, confirmationA, snapshotSourceA, rollbackWriteAdapterA,
                (proposal, decision, req, ct) => Task.FromResult<ApprovalGrant?>(
                    new ApprovalGrant(Guid.NewGuid(), req.Id, proposal.ProposalHash, "authorized-product-owner",
                        clockA.GetUtcNow(), clockA.GetUtcNow().AddMinutes(30), "rollback E2E fase A", null, null)));

            output.WriteLine($"Rollback status={rollbackResultA.Status}; reasons=[{string.Join(", ", rollbackResultA.Reasons)}]");
            // The physical operation is UPDATE here (before=ANTES existed, current=DEPOIS exists) — objectively
            // derived by BuildEquivalentProposal, not assumed. Confirm that derivation, not just the outcome.
            Assert.Equal(ActionOperation.Update, rollbackResultA.Proposal!.EquivalentProposal.Operation);
            Assert.Contains($"{RollbackOrchestrator.ValidationReason}=PASS", rollbackResultA.Reasons);
            Assert.Equal(RollbackExecutionStatus.Completed, rollbackResultA.Status);

            Assert.Equal(Antes, await ReadValorAsync(connectionString, RowId));
            output.WriteLine("Fase A — apos rollback, VALOR confirmado = ANTES em SOMA_DESENV.");

            // =====================================================================================
            // FASE B — backup_required=false, rollback_supported=false
            // =====================================================================================
            output.WriteLine("=== FASE B: write governado sem backup/rollback (post-write validation continua) ===");

            var now = clockA.GetUtcNow();
            var profileStoreB = new InMemoryWriteVerificationProfileStore(
            [
                WriteVerificationProfileSeeds.LinxDevelopmentPhaseA,
                WriteVerificationProfileSeeds.LinxDevelopmentPhaseB with { EffectiveFrom = now.AddSeconds(-1) },
            ]);
            var resolvedB = await profileStoreB.ResolveAsync(WriteVerificationProfileSeeds.LinxDevelopment, now);
            Assert.NotNull(resolvedB);
            Assert.False(resolvedB!.BackupRequired);
            Assert.False(resolvedB.RollbackSupported);
            Assert.True(resolvedB.PostWriteValidationRequired);
            output.WriteLine($"Fase B — perfil resolvido: PolicyVersion={resolvedB.PolicyVersion}, BackupRequired=false, RollbackSupported=false");

            var indexB = new InMemoryRecoveryIndexStore();
            var writeAuditB = new InMemoryWriteExecutionAuditStore();
            using var dbB = NewInMemoryDb();
            var forwardAdapterB = new RecoveryHomologationWriteAdapter(configuration, RowId, Depois);
            var (orchestratorB, _) = BuildOrchestrator(forwardAdapterB, dbB, profileStoreB, indexB, writeAuditB, writer, clockA);

            var requestB = Request("REQ-HOMOLOG-FASE-B");
            var grantB = GrantFor(requestB.Context, now);
            var resultB = await orchestratorB.ExecuteAsync(requestB, grantB, forwardAdapterB);

            Assert.Equal(GovernedWriteExecutionStatus.Completed, resultB.Status);
            Assert.NotNull(resultB.Validation);
            Assert.True(resultB.Validation!.Passed);
            Assert.Null(resultB.RecoveryPackage);
            Assert.Empty(await indexB.FindAsync(new RecoveryIndexQuery()));
            output.WriteLine($"Fase B — execution_id={resultB.ExecutionId}; RecoveryPackage=null; ValidationPassed=true");

            var auditRecordsB = await writeAuditB.ListAsync();
            var auditB = Assert.Single(auditRecordsB);
            Assert.False(auditB.BackupRequired);
            Assert.False(auditB.BackupCreated);
            output.WriteLine($"Fase B — audit permanente: BackupRequired=false, BackupCreated=false, Outcome={auditB.Outcome}");

            Assert.Equal(Depois, await ReadValorAsync(connectionString, RowId));

            using var dbRollbackB = NewInMemoryDb();
            var rollbackOrchestratorB = BuildRollbackOrchestrator(indexB, writer, profileStoreB, writeAuditB, dbRollbackB, clockA,
                new RecoveryHomologationWriteAdapter(configuration, RowId, targetValue: null));
            var discoveryB = await rollbackOrchestratorB.DiscoverAsync(new RecoveryIndexQuery { ExecutionId = resultB.ExecutionId });
            output.WriteLine($"Fase B — tentativa de rollback (discovery): status={discoveryB.Status}, reasons=[{string.Join(", ", discoveryB.Reasons)}]");

            // ROLLBACK_NOT_AVAILABLE: sem backup_required, GovernedWriteExecutionOrchestrator nunca cria uma
            // entrada de indice para esta execucao (passo 4 e pulado inteiro) — a Discovery, que so enxerga o
            // indice, retorna NotFound (zero candidatos). Nenhuma escrita ocorre em nenhum dos dois casos; a
            // garantia de seguranca ("nao ha rollback sem backup") se mantem.
            Assert.Equal(RollbackDiscoveryStatus.NotFound, discoveryB.Status);
            Assert.Contains(RollbackOrchestrator.NotFoundReason, discoveryB.Reasons);
            Assert.Empty(discoveryB.Candidates);

            // Restauracao administrativa (fora do framework, por SQL direto) para permitir continuidade do teste.
            await AdminSetValorAsync(connectionString, RowId, Antes);
            Assert.Equal(Antes, await ReadValorAsync(connectionString, RowId));
            output.WriteLine("Fase B — restauracao administrativa concluida; VALOR confirmado = ANTES.");

            // =====================================================================================
            // FASE C — Retencao
            // =====================================================================================
            output.WriteLine("=== FASE C: retencao de recovery package ===");

            var profileStoreC = new InMemoryWriteVerificationProfileStore(); // volta ao seed padrao (fase A)
            var indexC = new InMemoryRecoveryIndexStore();
            var writeAuditC = new InMemoryWriteExecutionAuditStore();
            using var dbC = NewInMemoryDb();
            var creationTime = clockA.GetUtcNow();
            var clockC = new FixedTimeProvider(creationTime);
            var forwardAdapterC = new RecoveryHomologationWriteAdapter(configuration, RowId, Depois);
            var (orchestratorC, _) = BuildOrchestrator(forwardAdapterC, dbC, profileStoreC, indexC, writeAuditC, writer, clockC);

            var requestC = Request("REQ-HOMOLOG-FASE-C");
            var grantC = GrantFor(requestC.Context, creationTime);
            var resultC = await orchestratorC.ExecuteAsync(requestC, grantC, forwardAdapterC);

            Assert.Equal(GovernedWriteExecutionStatus.Completed, resultC.Status);
            Assert.NotNull(resultC.RecoveryPackage);
            var executionIdC = resultC.ExecutionId;
            var packagePathC = resultC.RecoveryPackage!.PackagePath;
            output.WriteLine($"Fase C — execution_id={executionIdC}; package={packagePathC}");
            Assert.True(Directory.Exists(packagePathC));
            Assert.Equal(Depois, await ReadValorAsync(connectionString, RowId));

            var retentionService = new RecoveryRetentionCleanupService(indexC, writer);
            var report = await retentionService.RunOnceAsync(creationTime.AddDays(31));
            output.WriteLine($"Fase C — retention run: inspected={report.Inspected}, expired={report.Expired}, errors=[{string.Join(", ", report.Errors)}]");

            Assert.Contains(executionIdC, report.ExpiredExecutionIds);
            Assert.False(Directory.Exists(packagePathC));
            var entryC = Assert.Single(await indexC.FindAsync(new RecoveryIndexQuery { ExecutionId = executionIdC }));
            Assert.Equal(RecoveryPackageStatus.Expired, entryC.Status);

            var permanentAuditC = Assert.Single(await writeAuditC.ListAsync());
            Assert.Equal(executionIdC, permanentAuditC.ExecutionId);
            output.WriteLine("Fase C — audit permanente continua consultavel apos expiracao do recovery package.");

            using var dbRollbackC = NewInMemoryDb();
            var rollbackOrchestratorC = BuildRollbackOrchestrator(indexC, writer, profileStoreC, writeAuditC, dbRollbackC, clockC,
                new RecoveryHomologationWriteAdapter(configuration, RowId, targetValue: null));
            var analysisC = await rollbackOrchestratorC.AnalyzeAsync(executionIdC, new RecoveryHomologationWriteAdapter(configuration, RowId, targetValue: null));
            output.WriteLine($"Fase C — tentativa de rollback pos-expiracao: status={analysisC.Status}, reasons=[{string.Join(", ", analysisC.Reasons)}]");
            Assert.Equal(RollbackAnalysisStatus.NotAvailable, analysisC.Status);
            Assert.Contains(RollbackOrchestrator.NotAvailableReason, analysisC.Reasons);
            Assert.Contains("RECOVERY_PACKAGE_EXPIRED_OR_REMOVED", analysisC.Reasons);
            Assert.Null(analysisC.ConfirmationHandle);

            await AdminSetValorAsync(connectionString, RowId, Antes);
            output.WriteLine("Fase C — restauracao administrativa concluida; VALOR confirmado = ANTES.");

            // =====================================================================================
            // TESTE DE CONCORRENCIA
            // =====================================================================================
            output.WriteLine("=== TESTE DE CONCORRENCIA ===");

            var profileStoreD = new InMemoryWriteVerificationProfileStore();
            var indexD = new InMemoryRecoveryIndexStore();
            var writeAuditD = new InMemoryWriteExecutionAuditStore();
            using var dbD = NewInMemoryDb();
            var clockD = new FixedTimeProvider(DateTimeOffset.UtcNow);
            var forwardAdapterD = new RecoveryHomologationWriteAdapter(configuration, RowId, Depois);
            var (orchestratorD, _) = BuildOrchestrator(forwardAdapterD, dbD, profileStoreD, indexD, writeAuditD, writer, clockD);

            var requestD = Request("REQ-HOMOLOG-CONCORRENCIA");
            var grantD = GrantFor(requestD.Context, clockD.GetUtcNow());
            var resultD = await orchestratorD.ExecuteAsync(requestD, grantD, forwardAdapterD);
            Assert.Equal(GovernedWriteExecutionStatus.Completed, resultD.Status);
            var executionIdD = resultD.ExecutionId;
            output.WriteLine($"Concorrencia — write original concluido, execution_id={executionIdD}, VALOR=DEPOIS.");

            // Simula um terceiro alterando o mesmo dado DEPOIS da execucao original, por fora do framework.
            const string terceiro = "ALTERADO_POR_TERCEIRO";
            await AdminSetValorAsync(connectionString, RowId, terceiro);
            output.WriteLine($"Concorrencia — alteracao simulada de terceiro: VALOR={terceiro}.");

            using var dbRollbackD = NewInMemoryDb();
            var rollbackOrchestratorD = BuildRollbackOrchestrator(indexD, writer, profileStoreD, writeAuditD, dbRollbackD, clockD,
                new RecoveryHomologationWriteAdapter(configuration, RowId, targetValue: null));
            var analysisD = await rollbackOrchestratorD.AnalyzeAsync(executionIdD, new RecoveryHomologationWriteAdapter(configuration, RowId, targetValue: null));
            output.WriteLine($"Concorrencia — analise: status={analysisD.Status}, reasons=[{string.Join(", ", analysisD.Reasons)}]");

            Assert.Equal(RollbackAnalysisStatus.BlockedConcurrentChange, analysisD.Status);
            Assert.Contains(RollbackOrchestrator.ConcurrentChangeReason, analysisD.Reasons);
            Assert.NotEmpty(analysisD.ConcurrencyFindings);
            Assert.Null(analysisD.ConfirmationHandle);
            Assert.Equal(terceiro, await ReadValorAsync(connectionString, RowId));
            output.WriteLine("Concorrencia — ROLLBACK_BLOCKED_CONCURRENT_CHANGE confirmado; nenhuma escrita de rollback ocorreu.");

            await AdminSetValorAsync(connectionString, RowId, Antes);
            output.WriteLine("Concorrencia — limpeza administrativa concluida; VALOR confirmado = ANTES.");
        }
        finally
        {
            await DropTableAsync(connectionString);
            output.WriteLine($"Limpeza final: tabela {Table} removida de SOMA_DESENV (se existia).");
        }
    }

    // ------------------------------------------------------------------------------------------------------
    // Wiring helpers
    // ------------------------------------------------------------------------------------------------------

    private static GovernedWriteExecutionRequest Request(string requestId) => new(
        Context(requestId), Routing(), Analysis(),
        new IdentityPermissionContext(RequestedBy, HasEffectivePermission: true),
        "recovery-homologation-update",
        WriteVerificationProfileSeeds.LinxDevelopment,
        "192.168.9.98",
        "SOMA_DESENV",
        [$"ID={RowId}"],
        [ExpectedAfterSet(Depois)],
        "Homologacao generica do framework de Write Verification & Recovery Policy.",
        []);

    private static StructuredActionContext Context(string requestId) => new(
        requestId, RequestedBy, GovernanceEnvironment.Development, "SOMA/Linx",
        ActionResourceType.DatabaseTable, RecoveryHomologationWriteAdapter.TableName, OperationIntent.Update,
        [RecoveryHomologationWriteAdapter.CapabilityId], ["VALOR"], $"ID={RowId}", 1,
        "Homologacao generica do framework de Write Verification & Recovery Policy.",
        DataClassification.Internal, false, false, false, ActionReversibility.Reversible,
        ConnectionProfile: WriteVerificationProfileSeeds.LinxDevelopment);

    private static RoutingEvidence Routing() => new(true, RecoveryHomologationWriteAdapter.OwnerAgentId, [], [], [], []);

    private static AgentWriteAnalysis Analysis() => new(
        RecoveryHomologationWriteAdapter.OwnerAgentId, RecoveryHomologationWriteAdapter.CapabilityId,
        ["VALOR"], $"ID={RowId}", 1, ActionReversibility.Reversible);

    private static RecoveryDataSet ExpectedAfterSet(string valor) => new(RecoveryHomologationWriteAdapter.TableName,
        [new Dictionary<string, string?> { ["ID"] = RowId, ["VALOR"] = valor }]);

    private static ApprovalGrant GrantFor(StructuredActionContext context, DateTimeOffset now)
    {
        var build = new StructuredActionProposalAdapter().Build(context, Routing(), Analysis(), now);
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
        .UseInMemoryDatabase($"recovery-homologation-e2e-{Guid.NewGuid():N}").Options);

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
    // Direct-SQL helpers — table lifecycle and administrative resets. Never the governed write path itself.
    // ------------------------------------------------------------------------------------------------------

    private static (IConfiguration Configuration, string? ConnectionString) LoadConfiguration()
    {
        if (Environment.GetEnvironmentVariable("GOVERNANCE_E2E_TESTS") != "1") return (null!, null);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("ErpConnection");
        return string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal)
            ? (configuration, null)
            : (configuration, connectionString);
    }

    private static async Task CreateTableAsync(string connectionString)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"IF OBJECT_ID('dbo.{Table}', 'U') IS NOT NULL DROP TABLE [dbo].[{Table}]; " +
            $"CREATE TABLE [dbo].[{Table}] ([ID] VARCHAR(20) NOT NULL PRIMARY KEY, [VALOR] VARCHAR(100) NOT NULL);";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ResetRowAsync(string connectionString, string valor)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM [dbo].[{Table}] WHERE [ID] = @id; INSERT INTO [dbo].[{Table}] ([ID], [VALOR]) VALUES (@id, @valor);";
        command.Parameters.AddWithValue("@id", RowId);
        command.Parameters.AddWithValue("@valor", valor);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string?> ReadValorAsync(string connectionString, string id)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT [VALOR] FROM [dbo].[{Table}] WHERE [ID] = @id";
        command.Parameters.AddWithValue("@id", id);
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : Convert.ToString(result);
    }

    private static async Task AdminSetValorAsync(string connectionString, string id, string valor)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE [dbo].[{Table}] SET [VALOR] = @valor WHERE [ID] = @id";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@valor", valor);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropTableAsync(string? connectionString)
    {
        if (connectionString is null) return;
        try
        {
            await using var connection = await OpenAsync(connectionString);
            await using var command = connection.CreateCommand();
            command.CommandText = $"IF OBJECT_ID('dbo.{Table}', 'U') IS NOT NULL DROP TABLE [dbo].[{Table}];";
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best effort — a failed drop here must not mask a real assertion result upstream.
        }
    }

    private static async Task<SqlConnection> OpenAsync(string connectionString)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
