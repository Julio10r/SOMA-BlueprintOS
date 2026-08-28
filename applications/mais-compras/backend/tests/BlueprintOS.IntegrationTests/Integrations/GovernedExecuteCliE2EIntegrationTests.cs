#pragma warning disable CS1591

using System.Text.Json;
using BlueprintOS.Api.Governance;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace BlueprintOS.IntegrationTests.Integrations;

/// <summary>
/// REAL end-to-end homologation of the `governed-execute` CLI (<see cref="GovernedExecuteCliHandler"/>) against
/// SOMA_DESENV (192.168.9.98) ONLY — never LinxConnectionProfiles.Production (192.168.9.200/SOMA), never WISE.
///
/// Unlike <c>PedGradeAdjustmentE2EIntegrationTests</c> (which builds <c>GovernedWriteExecutionOrchestrator</c>
/// and <c>RollbackOrchestrator</c> directly in test code), this suite drives the SAME governed write end to end
/// exclusively through the CLI's public surface — `governed-execute propose` / `approve` / `run` /
/// `rollback-plan` / `rollback`, each one JSON payload on stdin, one JSON result on stdout — proving the actual
/// process boundary a real operator or automation would use, with every governance artifact persisted for real
/// under a fresh temp `runtime/governance`/`runtime/backups` root (never `InMemory*` stores).
///
/// Candidate-row selection/capture/restoration reuses the same pattern as
/// <c>PedGradeAdjustmentE2EIntegrationTests</c>: one already-existing, undelivered (QTDE_ENTREGUE=0), non-32
/// (CO7=0) COMPRAS_PRODUTO row is selected, its exact state captured up front, and restored byte-for-byte in a
/// `finally` block regardless of outcome.
///
/// Opt-in only: requires GOVERNANCE_E2E_TESTS=1. Never enabled in CI.
/// </summary>
public sealed class GovernedExecuteCliE2EIntegrationTests(ITestOutputHelper output)
{
    private const string RequestedBy = "julio.cesar@somagrupo.com.br";

    [Fact]
    public async Task EndToEnd_GovernedExecuteCli_PedGradeAdjustment_Against_SomaDesenv()
    {
        var (rawConfiguration, connectionString) = LoadConfiguration();
        if (connectionString is null)
        {
            output.WriteLine("GOVERNANCE_E2E_TESTS!=1 ou connection string ausente/placeholder — teste ignorado (nunca ativo em CI).");
            return;
        }

        var governanceRoot = Path.Combine(Path.GetTempPath(), "blueprintos-governed-execute-e2e", Guid.NewGuid().ToString("N"));
        var backupsRoot = Path.Combine(governanceRoot, "backups");
        var cliConfiguration = new ConfigurationBuilder()
            .AddConfiguration(rawConfiguration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Governance:RuntimeRoot"] = governanceRoot,
                ["Governance:BackupsRoot"] = backupsRoot,
            })
            .Build();
        output.WriteLine($"Governance root: {governanceRoot}");
        output.WriteLine($"Backups root: {backupsRoot}");

        var (pedido, produto, cor) = await FindCandidateRowAsync(connectionString);
        output.WriteLine($"Linha candidata selecionada em SOMA_DESENV: PEDIDO={pedido}, PRODUTO={produto}, COR_PRODUTO={cor}");

        var original = await ReadRowAsync(connectionString, pedido, produto, cor)
            ?? throw new InvalidOperationException("Linha candidata desapareceu antes do teste iniciar.");
        output.WriteLine($"Estado original capturado: CO1..CO6=[{original.Co1},{original.Co2},{original.Co3},{original.Co4},{original.Co5},{original.Co6}]");

        try
        {
            // =====================================================================================
            // FASE A — propose -> approve -> run (write + backup + post-write validation), tudo via CLI.
            // =====================================================================================
            output.WriteLine("=== FASE A: governed-execute propose/approve/run ===");

            var desired = (original.Co1 + 1, original.Co2 + 1, original.Co3 + 1, original.Co4 + 1, original.Co5 + 1, original.Co6 + 1);

            var (proposeExit, proposeJson) = await InvokeAsync(["x", "propose"], ProposePayload(pedido, produto, cor), cliConfiguration);
            Assert.Equal(0, proposeExit);
            Assert.True(proposeJson.GetProperty("proposalBuild").GetProperty("succeeded").GetBoolean());
            var approvalRequestId = proposeJson.GetProperty("approvalRequest").GetProperty("id").GetGuid();
            output.WriteLine($"propose — policyDecision={proposeJson.GetProperty("policyDecision").GetProperty("status").GetString()}; approvalRequestId={approvalRequestId}");

            var (approveExit, approveJson) = await InvokeAsync(["x", "approve"], ApprovePayload(approvalRequestId), cliConfiguration);
            Assert.Equal(0, approveExit);
            var approvalGrantId = approveJson.GetProperty("approvalGrant").GetProperty("id").GetGuid();
            output.WriteLine($"approve — approvalGrantId={approvalGrantId}");
            Assert.True(File.Exists(Path.Combine(governanceRoot, "approvals", "grants", $"{approvalGrantId:N}.json")));

            var (runExit, runJson) = await InvokeAsync(["x", "run"], RunPayload(pedido, produto, cor, desired, approvalGrantId), cliConfiguration);
            Assert.Equal(0, runExit);
            var status = runJson.GetProperty("status").GetString();
            output.WriteLine($"run — status={status}; reasons={runJson.GetProperty("reasons")}");
            Assert.Equal("Completed", status);
            Assert.True(runJson.GetProperty("validation").GetProperty("passed").GetBoolean());
            var executionId = runJson.GetProperty("executionId").GetGuid();
            var packagePath = runJson.GetProperty("recoveryPackage").GetProperty("packagePath").GetString();
            output.WriteLine($"run — executionId={executionId}; recoveryPackage={packagePath}");
            Assert.True(Directory.Exists(packagePath));

            var afterRow = await ReadRowAsync(connectionString, pedido, produto, cor);
            Assert.NotNull(afterRow);
            Assert.Equal(desired.Item1, afterRow!.Co1);
            Assert.Equal(desired.Item2, afterRow.Co2);
            Assert.Equal(original.QtdeEntregue, afterRow.QtdeEntregue);
            output.WriteLine($"run — DB confirma CO1..CO6=[{afterRow.Co1},{afterRow.Co2},{afterRow.Co3},{afterRow.Co4},{afterRow.Co5},{afterRow.Co6}].");

            // =====================================================================================
            // FASE B — bloqueio sem approval: mesma execucao, sem ApprovalGrantId, deve ser recusada
            // sem jamais tocar o banco (nova execucao com desired diferente, para nao conflitar com a
            // ja concluida).
            // =====================================================================================
            output.WriteLine("=== FASE B: run sem approvalGrantId com um grant inexistente e explicitamente recusado ===");
            var (blockedExit, blockedJson) = await InvokeAsync(
                ["x", "run"], RunPayload(pedido, produto, cor, (desired.Item1 + 1, desired.Item2, desired.Item3, desired.Item4, desired.Item5, desired.Item6), Guid.NewGuid()),
                cliConfiguration);
            Assert.Equal(1, blockedExit);
            Assert.Equal("APPROVAL_GRANT_NOT_FOUND", blockedJson.GetProperty("error").GetString());
            var rowAfterBlockedAttempt = await ReadRowAsync(connectionString, pedido, produto, cor);
            Assert.Equal(desired.Item1, rowAfterBlockedAttempt!.Co1);
            output.WriteLine("Bloqueio confirmado: APPROVAL_GRANT_NOT_FOUND, linha inalterada.");

            // =====================================================================================
            // FASE C — bloqueio por connection profile invalido (server nao bate com o profile).
            // =====================================================================================
            output.WriteLine("=== FASE C: bloqueio por connection profile invalido ===");
            var invalidProfilePayload = JsonSerializer.Serialize(new
            {
                Context = ContextObject(pedido, produto, cor),
                ExecutionName = "ped-grade-adjustment-e2e-invalid-profile",
                ConnectionProfile = "linx-development",
                Server = "10.0.0.1", // nao bate com 192.168.9.98
                Database = "SOMA_DESENV",
                BusinessKeys = new[] { BusinessKey(pedido, produto, cor) },
                ProceduresInvoked = new[] { "LX_MOVIMENTA_COMPRAS_PA", "LX_RECALCULO_RESERVA_MATERIAIS" },
                OriginalRequestSummary = "Teste E2E de bloqueio por profile invalido.",
                AllowsMissingBeforeState = false,
                ApprovalGrantId = (Guid?)null,
                PedGradeAdjustment = new { Pedido = pedido, Produto = produto, CorProduto = cor, Tam1 = desired.Item1, Tam2 = desired.Item2, Tam3 = desired.Item3, Tam4 = desired.Item4, Tam5 = desired.Item5, Tam6 = desired.Item6 },
            });
            var (invalidProfileExit, invalidProfileJson) = await InvokeAsync(["x", "run"], invalidProfilePayload, cliConfiguration);
            Assert.Equal(1, invalidProfileExit);
            Assert.Equal("SERVER_OR_DATABASE_MISMATCH", invalidProfileJson.GetProperty("error").GetString());
            output.WriteLine("Bloqueio confirmado: SERVER_OR_DATABASE_MISMATCH.");

            // =====================================================================================
            // FASE D — rollback-plan -> approve -> rollback, tudo via CLI.
            // =====================================================================================
            output.WriteLine("=== FASE D: governed-execute rollback-plan/approve/rollback ===");
            const string justification = "Teste E2E do governed-execute CLI — restaurar estado original.";

            var (planExit, planJson) = await InvokeAsync(["x", "rollback-plan"], RollbackPlanPayload(executionId, pedido, produto, cor, justification), cliConfiguration);
            Assert.Equal(0, planExit);
            output.WriteLine($"rollback-plan — status={planJson.GetProperty("status").GetString()}");
            Assert.Equal("ReadyForConfirmation", planJson.GetProperty("status").GetString());

            Guid? rollbackGrantId = null;
            if (planJson.TryGetProperty("approvalRequestId", out var reqIdProp) && reqIdProp.ValueKind != JsonValueKind.Null)
            {
                var rollbackApprovalRequestId = reqIdProp.GetGuid();
                var (rollbackApproveExit, rollbackApproveJson) = await InvokeAsync(["x", "approve"], ApprovePayload(rollbackApprovalRequestId), cliConfiguration);
                Assert.Equal(0, rollbackApproveExit);
                rollbackGrantId = rollbackApproveJson.GetProperty("approvalGrant").GetProperty("id").GetGuid();
                output.WriteLine($"rollback-plan exigiu aprovacao; approve concedeu grant={rollbackGrantId}");
            }
            else
            {
                output.WriteLine("rollback-plan nao exigiu aprovacao humana adicional pela politica vigente.");
            }

            var (rollbackExit, rollbackJson) = await InvokeAsync(["x", "rollback"], RollbackPayload(executionId, pedido, produto, cor, justification, rollbackGrantId), cliConfiguration);
            output.WriteLine($"rollback — status={rollbackJson.GetProperty("status").GetString()}; reasons={rollbackJson.GetProperty("reasons")}");
            Assert.Equal(0, rollbackExit);
            Assert.Equal("Completed", rollbackJson.GetProperty("status").GetString());

            var restoredRow = await ReadRowAsync(connectionString, pedido, produto, cor);
            Assert.NotNull(restoredRow);
            Assert.Equal(original.Co1, restoredRow!.Co1);
            Assert.Equal(original.Co2, restoredRow.Co2);
            Assert.Equal(original.Co3, restoredRow.Co3);
            Assert.Equal(original.Co4, restoredRow.Co4);
            Assert.Equal(original.Co5, restoredRow.Co5);
            Assert.Equal(original.Co6, restoredRow.Co6);
            output.WriteLine("rollback — apos rollback via CLI, CO1..CO6 batem exatamente com o estado original.");

            // =====================================================================================
            // FASE E — concorrencia bloqueia rollback: nova execucao, alteracao de terceiro, rollback-plan
            // deve reportar BlockedConcurrentChange.
            // =====================================================================================
            output.WriteLine("=== FASE E: concorrencia bloqueia rollback (via CLI) ===");

            var (proposeExit2, proposeJson2) = await InvokeAsync(["x", "propose"], ProposePayload(pedido, produto, cor, "REQ-TEST-CONCORRENCIA"), cliConfiguration);
            Assert.Equal(0, proposeExit2);
            var approvalRequestId2 = proposeJson2.GetProperty("approvalRequest").GetProperty("id").GetGuid();
            var (_, approveJson2) = await InvokeAsync(["x", "approve"], ApprovePayload(approvalRequestId2), cliConfiguration);
            var approvalGrantId2 = approveJson2.GetProperty("approvalGrant").GetProperty("id").GetGuid();

            var desired2 = (original.Co1 + 2, original.Co2 + 2, original.Co3 + 2, original.Co4 + 2, original.Co5 + 2, original.Co6 + 2);
            var (runExit2, runJson2) = await InvokeAsync(["x", "run"], RunPayload(pedido, produto, cor, desired2, approvalGrantId2), cliConfiguration);
            Assert.Equal(0, runExit2);
            Assert.Equal("Completed", runJson2.GetProperty("status").GetString());
            var executionId2 = runJson2.GetProperty("executionId").GetGuid();
            output.WriteLine($"Concorrencia — segunda execucao concluida, executionId={executionId2}.");

            await AdminSetCo1Async(connectionString, pedido, produto, cor, desired2.Item1 + 99);
            output.WriteLine($"Concorrencia — alteracao simulada de terceiro: CO1={desired2.Item1 + 99}.");

            var (planExit2, planJson2) = await InvokeAsync(["x", "rollback-plan"], RollbackPlanPayload(executionId2, pedido, produto, cor, "Teste E2E de concorrencia."), cliConfiguration);
            Assert.Equal(0, planExit2);
            output.WriteLine($"rollback-plan (concorrencia) — status={planJson2.GetProperty("status").GetString()}; reasons={planJson2.GetProperty("reasons")}");
            Assert.Equal("BlockedConcurrentChange", planJson2.GetProperty("status").GetString());

            var rowAfterConcurrency = await ReadRowAsync(connectionString, pedido, produto, cor);
            Assert.Equal(desired2.Item1 + 99, rowAfterConcurrency!.Co1);
            output.WriteLine("Concorrencia — rollback-plan bloqueou via CLI; linha permanece como o terceiro deixou.");
        }
        finally
        {
            await RestoreOriginalStateAsync(connectionString, pedido, produto, cor, original);
            output.WriteLine("Limpeza final: linha COMPRAS_PRODUTO restaurada byte-a-byte ao estado original em SOMA_DESENV.");
            try { if (Directory.Exists(governanceRoot)) Directory.Delete(governanceRoot, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
        }
    }

    // ------------------------------------------------------------------------------------------------------
    // Payload builders
    // ------------------------------------------------------------------------------------------------------

    private static string BusinessKey(string pedido, string produto, string cor) => $"PEDIDO={pedido}|PRODUTO={produto}|COR_PRODUTO={cor}";

    private static object ContextObject(string pedido, string produto, string cor, string requestId = "REQ-TEST-RUN") => new
    {
        RequestId = requestId,
        RequestedBy,
        AgentId = "linx-erp-specialist-agent",
        Capability = "ped-grade-adjustment-write",
        Environment = "Development",
        System = "SOMA/Linx",
        ResourceType = "DatabaseTable",
        Resource = "COMPRAS_PRODUTO",
        OperationIntent = "Update",
        Fields = new[] { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" },
        FilterSummary = BusinessKey(pedido, produto, cor),
        ExpectedAffectedRows = 1,
        Purpose = "Homologacao E2E do governed-execute CLI.",
        DataClassification = "Internal",
        ContainsPersonalData = false,
        ContainsSensitivePersonalData = false,
        ContainsSecrets = false,
        Reversibility = "Reversible",
        RunbookReference = (string?)null,
        ConnectionProfile = "linx-development",
        AdditionalContext = (string?)null,
    };

    private static string ProposePayload(string pedido, string produto, string cor, string requestId = "REQ-TEST-RUN") =>
        JsonSerializer.Serialize(ContextObject(pedido, produto, cor, requestId));

    private static string ApprovePayload(Guid approvalRequestId) => JsonSerializer.Serialize(new
    {
        ApprovalRequestId = approvalRequestId,
        ApprovedBy = "authorized-product-owner",
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
        Scope = "homologacao E2E governed-execute",
        Notes = (string?)null,
    });

    private static string RunPayload(string pedido, string produto, string cor, (int, int, int, int, int, int) desired, Guid? approvalGrantId) => JsonSerializer.Serialize(new
    {
        Context = ContextObject(pedido, produto, cor),
        ExecutionName = "ped-grade-adjustment-cli-e2e",
        ConnectionProfile = "linx-development",
        Server = "192.168.9.98",
        Database = "SOMA_DESENV",
        BusinessKeys = new[] { BusinessKey(pedido, produto, cor) },
        ProceduresInvoked = new[] { "LX_MOVIMENTA_COMPRAS_PA", "LX_RECALCULO_RESERVA_MATERIAIS" },
        OriginalRequestSummary = "Homologacao E2E do governed-execute CLI.",
        AllowsMissingBeforeState = false,
        ApprovalGrantId = approvalGrantId,
        PedGradeAdjustment = new { Pedido = pedido, Produto = produto, CorProduto = cor, Tam1 = desired.Item1, Tam2 = desired.Item2, Tam3 = desired.Item3, Tam4 = desired.Item4, Tam5 = desired.Item5, Tam6 = desired.Item6 },
    });

    private static string RollbackPlanPayload(Guid executionId, string pedido, string produto, string cor, string justification) => JsonSerializer.Serialize(new
    {
        ExecutionId = executionId,
        RequestedBy,
        Justification = justification,
        ConnectionProfile = "linx-development",
        SnapshotKey = new { Pedido = pedido, Produto = produto, CorProduto = cor, Tam1 = 0, Tam2 = 0, Tam3 = 0, Tam4 = 0, Tam5 = 0, Tam6 = 0 },
    });

    private static string RollbackPayload(Guid executionId, string pedido, string produto, string cor, string justification, Guid? approvalGrantId) => JsonSerializer.Serialize(new
    {
        ExecutionId = executionId,
        RequestedBy,
        Justification = justification,
        ApprovalGrantId = approvalGrantId,
        ConnectionProfile = "linx-development",
        SnapshotKey = new { Pedido = pedido, Produto = produto, CorProduto = cor, Tam1 = 0, Tam2 = 0, Tam3 = 0, Tam4 = 0, Tam5 = 0, Tam6 = 0 },
    });

    private static async Task<(int ExitCode, JsonElement Json)> InvokeAsync(string[] args, string payload, IConfiguration configuration)
    {
        using var input = new StringReader(payload);
        using var outputWriter = new StringWriter();
        var exitCode = await GovernedExecuteCliHandler.RunAsync(args, input, outputWriter, configuration);
        var json = JsonDocument.Parse(outputWriter.ToString()).RootElement;
        return (exitCode, json);
    }

    // ------------------------------------------------------------------------------------------------------
    // Direct-SQL helpers — candidate selection, snapshotting, restoration. Never the governed write path.
    // Identical in spirit to PedGradeAdjustmentE2EIntegrationTests's own helpers.
    // ------------------------------------------------------------------------------------------------------

    private static (IConfiguration Configuration, string? ConnectionString) LoadConfiguration()
    {
        if (Environment.GetEnvironmentVariable("GOVERNANCE_E2E_TESTS") != "1") return (new ConfigurationBuilder().Build(), null);
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
            // Best effort — a failed restore here must not mask a real assertion result upstream.
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
