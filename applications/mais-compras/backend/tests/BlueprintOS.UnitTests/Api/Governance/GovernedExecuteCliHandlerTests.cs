#pragma warning disable CS1591

using System.Text.Json;
using BlueprintOS.Api.Governance;
using BlueprintOS.Core.AI.Governance.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BlueprintOS.UnitTests.Api.Governance;

/// <summary>
/// Unit coverage for the `governed-execute` CLI (<see cref="GovernedExecuteCliHandler"/>) that does NOT require
/// a real SQL Server: propose/approve persistence round-trip against a real, file-based store rooted at a
/// fresh temp directory per test, plus every hard-abort guard `run` enforces BEFORE ever touching the adapter's
/// database connection (missing/invalid connection profile, server/database mismatch, missing/expired/revoked
/// approval grant, unrecognized capability). The real live-write happy path (a genuine UPDATE against
/// COMPRAS_PRODUTO landing, backup captured, post-write validation passing) is proven separately by
/// <c>GovernedExecuteE2EIntegrationTests</c> against SOMA_DESENV — this class intentionally never opens a
/// SqlConnection, matching the same boundary <c>PedGradeAdjustmentGovernanceTests</c> already draws.
/// </summary>
public sealed class GovernedExecuteCliHandlerTests : IDisposable
{
    private readonly string _governanceRoot = Path.Combine(Path.GetTempPath(), "blueprintos-governed-execute-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_governanceRoot)) Directory.Delete(_governanceRoot, recursive: true);
    }

    private IConfiguration BuildConfiguration(string? developmentConnectionString = null) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Governance:RuntimeRoot"] = _governanceRoot,
            ["Governance:BackupsRoot"] = Path.Combine(_governanceRoot, "backups"),
            ["ConnectionStrings:LinxDevelopmentConnection"] = developmentConnectionString,
        })
        .Build();

    private static async Task<(int ExitCode, JsonElement Json)> InvokeAsync(string[] args, string payload, IConfiguration configuration)
    {
        using var input = new StringReader(payload);
        using var outputWriter = new StringWriter();
        var exitCode = await GovernedExecuteCliHandler.RunAsync(args, input, outputWriter, configuration);
        var json = JsonDocument.Parse(outputWriter.ToString()).RootElement;
        return (exitCode, json);
    }

    private static string ProposePayloadJson(string requestId = "REQ-TEST-1") => JsonSerializer.Serialize(new
    {
        RequestId = requestId,
        RequestedBy = "julio.cesar@somagrupo.com.br",
        AgentId = "linx-erp-specialist-agent",
        Capability = "ped-grade-adjustment-write",
        Environment = "Development",
        System = "SOMA/Linx",
        ResourceType = "DatabaseTable",
        Resource = "COMPRAS_PRODUTO",
        OperationIntent = "Update",
        Fields = new[] { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" },
        FilterSummary = "PEDIDO=000001|PRODUTO=PROD001|COR_PRODUTO=01",
        ExpectedAffectedRows = 1,
        Purpose = "Teste unitario do governed-execute CLI.",
        DataClassification = "Internal",
        ContainsPersonalData = false,
        ContainsSensitivePersonalData = false,
        ContainsSecrets = false,
        Reversibility = "Reversible",
        RunbookReference = (string?)null,
        ConnectionProfile = WriteVerificationProfileSeeds.LinxDevelopment,
        AdditionalContext = (string?)null,
    });

    // ---------------------------------------------------------------------------------------------------
    // propose / approve — real persistence round-trip against a temp file-based store.
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Propose_Persists_A_Real_ApprovalRequest_For_An_Update_Proposal()
    {
        var configuration = BuildConfiguration();
        var (exitCode, json) = await InvokeAsync(["governed-execute", "propose"], ProposePayloadJson(), configuration);

        Assert.Equal(0, exitCode);
        Assert.True(json.GetProperty("proposalBuild").GetProperty("succeeded").GetBoolean());
        Assert.Equal("RequiresApproval", json.GetProperty("policyDecision").GetProperty("status").GetString());
        var approvalRequestId = json.GetProperty("approvalRequest").GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, approvalRequestId);

        // Persisted for real — a second, brand-new process reading the same root can see it.
        var requestPath = Path.Combine(_governanceRoot, "approvals", "requests", $"{approvalRequestId:N}.json");
        Assert.True(File.Exists(requestPath));
    }

    [Fact]
    public async Task Approve_Persists_A_Real_ApprovalGrant_Fetchable_By_A_Later_Run()
    {
        var configuration = BuildConfiguration();
        var (_, proposeJson) = await InvokeAsync(["governed-execute", "propose"], ProposePayloadJson(), configuration);
        var approvalRequestId = proposeJson.GetProperty("approvalRequest").GetProperty("id").GetGuid();

        var approvePayload = JsonSerializer.Serialize(new
        {
            ApprovalRequestId = approvalRequestId,
            ApprovedBy = "authorized-product-owner",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            Scope = "teste unitario",
            Notes = (string?)null,
        });

        var (exitCode, approveJson) = await InvokeAsync(["governed-execute", "approve"], approvePayload, configuration);

        Assert.Equal(0, exitCode);
        var grantId = approveJson.GetProperty("approvalGrant").GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, grantId);

        var grantPath = Path.Combine(_governanceRoot, "approvals", "grants", $"{grantId:N}.json");
        Assert.True(File.Exists(grantPath));
    }

    [Fact]
    public async Task Approve_Rejects_An_ApprovalRequestId_That_Was_Never_Proposed()
    {
        var configuration = BuildConfiguration();
        var approvePayload = JsonSerializer.Serialize(new
        {
            ApprovalRequestId = Guid.NewGuid(),
            ApprovedBy = "authorized-product-owner",
            ExpiresAt = (DateTimeOffset?)null,
            Scope = "teste",
            Notes = (string?)null,
        });

        var (exitCode, json) = await InvokeAsync(["governed-execute", "approve"], approvePayload, configuration);

        Assert.Equal(1, exitCode);
        Assert.Equal("APPROVAL_REQUEST_NOT_FOUND", json.GetProperty("error").GetString());
    }

    // ---------------------------------------------------------------------------------------------------
    // run — every hard-abort guard, all reachable WITHOUT a real database connection.
    // ---------------------------------------------------------------------------------------------------

    private static string RunPayloadJson(
        string connectionProfile = "linx-development", string server = "192.168.9.98", string database = "SOMA_DESENV",
        Guid? approvalGrantId = null, string capability = "ped-grade-adjustment-write") => JsonSerializer.Serialize(new
        {
            Context = new
            {
                RequestId = "REQ-TEST-RUN",
                RequestedBy = "julio.cesar@somagrupo.com.br",
                AgentId = "linx-erp-specialist-agent",
                Capability = capability,
                Environment = "Development",
                System = "SOMA/Linx",
                ResourceType = "DatabaseTable",
                Resource = "COMPRAS_PRODUTO",
                OperationIntent = "Update",
                Fields = new[] { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" },
                FilterSummary = "PEDIDO=000001|PRODUTO=PROD001|COR_PRODUTO=01",
                ExpectedAffectedRows = 1,
                Purpose = "Teste unitario do governed-execute run.",
                DataClassification = "Internal",
                ContainsPersonalData = false,
                ContainsSensitivePersonalData = false,
                ContainsSecrets = false,
                Reversibility = "Reversible",
                RunbookReference = (string?)null,
                ConnectionProfile = connectionProfile,
                AdditionalContext = (string?)null,
            },
            ExecutionName = "ped-grade-adjustment-unit-test",
            ConnectionProfile = connectionProfile,
            Server = server,
            Database = database,
            BusinessKeys = new[] { "PEDIDO=000001|PRODUTO=PROD001|COR_PRODUTO=01" },
            ProceduresInvoked = new[] { "LX_MOVIMENTA_COMPRAS_PA", "LX_RECALCULO_RESERVA_MATERIAIS" },
            OriginalRequestSummary = "Teste unitario.",
            AllowsMissingBeforeState = false,
            ApprovalGrantId = approvalGrantId,
            PedGradeAdjustment = new { Pedido = "000001", Produto = "PROD001", CorProduto = "01", Tam1 = 1, Tam2 = 2, Tam3 = 3, Tam4 = 4, Tam5 = 5, Tam6 = 6 },
        });

    [Fact]
    public async Task Run_Rejects_A_Capability_This_Host_Does_Not_Execute()
    {
        var configuration = BuildConfiguration();
        var (exitCode, json) = await InvokeAsync(["governed-execute", "run"], RunPayloadJson(capability: "some-other-capability"), configuration);

        Assert.Equal(1, exitCode);
        Assert.Equal("CAPABILITY_NOT_SUPPORTED_BY_THIS_HOST", json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Run_Rejects_An_Ungoverned_Connection_Profile()
    {
        var configuration = BuildConfiguration();
        var (exitCode, json) = await InvokeAsync(["governed-execute", "run"], RunPayloadJson(connectionProfile: "wise"), configuration);

        Assert.Equal(1, exitCode);
        Assert.Equal("CONNECTION_PROFILE_NOT_GOVERNED", json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Run_Rejects_A_Server_Or_Database_That_Does_Not_Match_The_Declared_Profile()
    {
        var configuration = BuildConfiguration();
        var (exitCode, json) = await InvokeAsync(["governed-execute", "run"], RunPayloadJson(server: "10.0.0.1", database: "SOMA_DESENV"), configuration);

        Assert.Equal(1, exitCode);
        Assert.Equal("SERVER_OR_DATABASE_MISMATCH", json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Run_Rejects_When_The_Configured_Connection_String_Does_Not_Match_The_Expected_Profile()
    {
        // A connection string pointed at PRODUCTION's server/database while the payload declares the
        // Development profile — LinxConnectionStringResolver must block this BEFORE any write attempt.
        var configuration = BuildConfiguration(developmentConnectionString: "Server=192.168.9.200;Database=SOMA;User Id=x;Password=y;TrustServerCertificate=True;");
        var (exitCode, json) = await InvokeAsync(["governed-execute", "run"], RunPayloadJson(), configuration);

        Assert.Equal(1, exitCode);
        Assert.Equal("CONNECTION_STRING_VALIDATION_FAILED", json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Run_Rejects_When_No_Connection_String_Is_Configured_At_All()
    {
        var configuration = BuildConfiguration(developmentConnectionString: null);
        var (exitCode, json) = await InvokeAsync(["governed-execute", "run"], RunPayloadJson(), configuration);

        Assert.Equal(1, exitCode);
        Assert.Equal("CONNECTION_STRING_VALIDATION_FAILED", json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Run_Rejects_An_ApprovalGrantId_That_Was_Never_Persisted()
    {
        var configuration = BuildConfiguration(developmentConnectionString: "Server=192.168.9.98;Database=SOMA_DESENV;User Id=x;Password=y;TrustServerCertificate=True;");
        var (exitCode, json) = await InvokeAsync(["governed-execute", "run"], RunPayloadJson(approvalGrantId: Guid.NewGuid()), configuration);

        Assert.Equal(1, exitCode);
        Assert.Equal("APPROVAL_GRANT_NOT_FOUND", json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Run_Rejects_A_Revoked_ApprovalGrant()
    {
        var configuration = BuildConfiguration(developmentConnectionString: "Server=192.168.9.98;Database=SOMA_DESENV;User Id=x;Password=y;TrustServerCertificate=True;");

        var (_, proposeJson) = await InvokeAsync(["governed-execute", "propose"], ProposePayloadJson(), configuration);
        var approvalRequestId = proposeJson.GetProperty("approvalRequest").GetProperty("id").GetGuid();
        var approvePayload = JsonSerializer.Serialize(new { ApprovalRequestId = approvalRequestId, ApprovedBy = "authorized-product-owner", ExpiresAt = (DateTimeOffset?)null, Scope = "teste", Notes = (string?)null });
        var (_, approveJson) = await InvokeAsync(["governed-execute", "approve"], approvePayload, configuration);
        var grantId = approveJson.GetProperty("approvalGrant").GetProperty("id").GetGuid();

        var grantPath = Path.Combine(_governanceRoot, "approvals", "grants", $"{grantId:N}.json");
        var grant = JsonSerializer.Deserialize<ApprovalGrant>(await File.ReadAllTextAsync(grantPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        await File.WriteAllTextAsync(grantPath, JsonSerializer.Serialize(grant with { RevokedAt = DateTimeOffset.UtcNow }));

        var (exitCode, json) = await InvokeAsync(["governed-execute", "run"], RunPayloadJson(approvalGrantId: grantId), configuration);

        Assert.Equal(1, exitCode);
        Assert.Equal("APPROVAL_GRANT_REVOKED", json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Run_Rejects_An_Expired_ApprovalGrant()
    {
        var configuration = BuildConfiguration(developmentConnectionString: "Server=192.168.9.98;Database=SOMA_DESENV;User Id=x;Password=y;TrustServerCertificate=True;");

        var (_, proposeJson) = await InvokeAsync(["governed-execute", "propose"], ProposePayloadJson(), configuration);
        var approvalRequestId = proposeJson.GetProperty("approvalRequest").GetProperty("id").GetGuid();
        // ExpiresAt in the past deliberately — GovernedWriteStack.GrantAsync would itself reject a past
        // expiration, so we approve normally then rewrite the persisted grant file directly, exactly like the
        // revoked-grant test does, to exercise the CLI's own expiry check in isolation.
        var approvePayload = JsonSerializer.Serialize(new { ApprovalRequestId = approvalRequestId, ApprovedBy = "authorized-product-owner", ExpiresAt = (DateTimeOffset?)null, Scope = "teste", Notes = (string?)null });
        var (_, approveJson) = await InvokeAsync(["governed-execute", "approve"], approvePayload, configuration);
        var grantId = approveJson.GetProperty("approvalGrant").GetProperty("id").GetGuid();

        var grantPath = Path.Combine(_governanceRoot, "approvals", "grants", $"{grantId:N}.json");
        var grant = JsonSerializer.Deserialize<ApprovalGrant>(await File.ReadAllTextAsync(grantPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        await File.WriteAllTextAsync(grantPath, JsonSerializer.Serialize(grant with { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) }));

        var (exitCode, json) = await InvokeAsync(["governed-execute", "run"], RunPayloadJson(approvalGrantId: grantId), configuration);

        Assert.Equal(1, exitCode);
        Assert.Equal("APPROVAL_GRANT_EXPIRED", json.GetProperty("error").GetString());
    }

    // ---------------------------------------------------------------------------------------------------
    // Unknown mode
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Unknown_Mode_Is_Rejected_Explicitly()
    {
        var configuration = BuildConfiguration();
        var (exitCode, json) = await InvokeAsync(["governed-execute", "explode"], "{}", configuration);

        Assert.Equal(1, exitCode);
        Assert.Equal("UNKNOWN_MODE", json.GetProperty("error").GetString());
    }
}
