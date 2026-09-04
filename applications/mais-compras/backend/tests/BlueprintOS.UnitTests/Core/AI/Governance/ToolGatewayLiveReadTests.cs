using System.Reflection;
using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

/// <summary>
/// B3 — Bloco 5A.9, Gate A: covers the new LiveRead path (a real, non-mutating, pre-registered-dataset bulk
/// read) symmetrically to <see cref="ToolGatewayLiveExecutionTests"/>'s coverage of LiveExecution — including
/// the negative boundary in BOTH directions (a write-only adapter can never execute via LiveRead, and a
/// read-only adapter can never execute via LiveExecution), the dataset catalog's rejection of anything that
/// is not an exact registered dataset name (proving SQL injection through <c>ActionProposal.Resource</c> is
/// structurally impossible), the hash-stability that lets one human approval cover every future identical
/// daily execution without weakening LGPD, and the absence of any <see cref="IAIRuntime"/> dependency
/// anywhere in the LiveRead call chain.
/// </summary>
public sealed class ToolGatewayLiveReadTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LiveRead_Allowed_Executes_Through_The_Read_Adapter_Exactly_Once()
    {
        var fixture = CreateFixture();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest());

        Assert.Equal(ToolGatewayStatus.LiveReadCompleted, result.Status);
        Assert.True(result.LiveReadEnabled);
        Assert.Equal(1, fixture.Adapter.ExecuteCallCount);
        Assert.NotNull(result.ReadExecution);
        Assert.True(result.ReadExecution!.Succeeded);
        Assert.Equal(100, result.ReadExecution.RowsRead);
        Assert.Equal(100, result.ReadExecution.RowsWritten);
    }

    [Fact]
    public async Task LiveRead_RequiresApproval_Without_Grant_Is_Blocked()
    {
        var fixture = CreateFixture();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest() with { ApprovalGrant = null });

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("VALID_APPROVAL_REQUIRED", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task LiveRead_Blocked_By_Policy_Never_Executes()
    {
        var fixture = CreateFixture();
        var blocked = fixture.Decision with { Status = PolicyDecisionStatus.Blocked, RiskClassification = RiskClassification.Red };
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest() with { PolicyDecision = blocked });

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("POLICY_BLOCKED", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task LiveRead_Unknown_Capability_Is_Blocked()
    {
        var fixture = CreateFixture();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest() with { Capability = "does-not-exist" });

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("CAPABILITY_NOT_REGISTERED", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task LiveRead_Adapter_With_Wrong_Owner_Agent_Is_Blocked()
    {
        var fixture = CreateFixture();
        var proposal = fixture.Proposal with { RequestingAgent = "some-other-agent" };
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest() with { Proposal = proposal, RoutedPrimaryAgent = "some-other-agent" });

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("OWNER_MISMATCH", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task LiveRead_Adapter_With_Wrong_Connection_Profile_Is_Blocked()
    {
        var fixture = CreateFixture();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest() with { ConnectionProfile = "linx-erp-governed-write" });

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("CONNECTION_PROFILE_NOT_GOVERNED", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task Write_Adapter_Cannot_Execute_Via_LiveRead()
    {
        IGovernedToolAdapter[] adapters = [new FakeWriteOnlyAdapter()];
        var fixture = CreateFixture(adapters);
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest());

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("LIVE_READ_DISABLED", result.Reasons);
        Assert.Contains("LIVE_READ_ADAPTER_NOT_CAPABLE", result.Reasons);
    }

    [Fact]
    public async Task Read_Adapter_Cannot_Execute_Via_LiveExecution()
    {
        var fixture = CreateFixture();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest() with { ExecutionMode = GovernedExecutionMode.LiveExecution });

        Assert.Equal(ToolGatewayStatus.Blocked, result.Status);
        Assert.Contains("LIVE_EXECUTION_DISABLED", result.Reasons);
        Assert.Contains("LIVE_EXECUTION_ADAPTER_NOT_CAPABLE", result.Reasons);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
    }

    [Fact]
    public async Task LiveRead_Adapter_Failure_Is_Reported_As_LiveReadFailed_Never_As_Success()
    {
        var fixture = CreateFixture();
        fixture.Adapter.FailWith = "connection reset";
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest());

        Assert.Equal(ToolGatewayStatus.LiveReadFailed, result.Status);
        Assert.False(result.ReadExecution!.Succeeded);
    }

    [Fact]
    public async Task LiveRead_Adapter_Exception_Is_Contained_And_Reported_As_LiveReadFailed()
    {
        var fixture = CreateFixture();
        fixture.Adapter.ThrowWith = "socket closed";
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest());

        Assert.Equal(ToolGatewayStatus.LiveReadFailed, result.Status);
        Assert.Contains("LIVE_READ_ADAPTER_FAILED", result.Reasons);
    }

    [Fact]
    public async Task LiveRead_Respects_Cancellation_Without_Throwing_Out_Of_The_Gateway()
    {
        // A cooperative cancellation observed and reported BY THE ADAPTER (exactly how the real
        // SomaLinxDatasetBulkReader catches OperationCanceledException and returns a graceful result — see
        // its doc comment) must surface as LiveReadFailed with reason CANCELLED, never as an unhandled
        // exception out of the Gateway. A token cancelled before InvokeAsync is even called is a different,
        // pre-existing concern (the Gateway's own file-based audit I/O also observes it) — deliberately not
        // what this test is about.
        var fixture = CreateFixture();
        fixture.Adapter.SimulateCancellation = true;
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest());

        Assert.Equal(ToolGatewayStatus.LiveReadFailed, result.Status);
        Assert.Contains("CANCELLED", result.ReadExecution!.Reasons);
    }

    [Fact]
    public async Task LiveRead_Is_Audited_With_Rows_Isolation_And_Duration()
    {
        var fixture = CreateFixture();
        await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest());

        var audit = await fixture.Audit.ListByRequestAsync(fixture.Proposal.Id.ToString("N"));
        Assert.Contains(audit, item => item.EventType == "gateway.live-read.requested");
        Assert.Contains(audit, item => item.EventType == "gateway.live-read.started");
        var completed = Assert.Single(audit, item => item.EventType == "gateway.live-read.completed");
        Assert.Contains(completed.Categories, c => c.StartsWith("ROWS_READ=", StringComparison.Ordinal));
        Assert.Contains(completed.Categories, c => c.StartsWith("ISOLATION=", StringComparison.Ordinal));
        Assert.Contains(completed.Categories, c => c.StartsWith("DURATION_MS=", StringComparison.Ordinal));
        Assert.All(audit, item => Assert.All(item.Categories, category =>
            Assert.DoesNotContain("password", category, StringComparison.OrdinalIgnoreCase)));
        Assert.All(audit, item => Assert.All(item.Categories, category =>
            Assert.DoesNotContain("Server=", category, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task DryRun_Through_The_Same_Read_Capable_Adapter_Still_Performs_No_External_Execution()
    {
        var fixture = CreateFixture();
        var result = await fixture.Gateway.InvokeAsync(fixture.LiveReadRequest() with { ExecutionMode = GovernedExecutionMode.DryRun });

        Assert.Equal(ToolGatewayStatus.DryRunCompleted, result.Status);
        Assert.Equal(0, fixture.Adapter.ExecuteCallCount);
        Assert.False(result.Preview!.ExternalExecutionPerformed);
    }

    [Fact]
    public void Recurring_Daily_Proposal_Content_Is_Hash_Stable_So_One_Approval_Covers_Every_Future_Execution()
    {
        var day1 = Proposal() with { Id = Guid.NewGuid(), CreatedAt = Now };
        var day2 = Proposal() with { Id = Guid.NewGuid(), CreatedAt = Now.AddDays(1) };

        Assert.Equal(day1.ProposalHash, day2.ProposalHash);
    }

    [Fact]
    public void Zero_Llm_Dependency_Anywhere_In_The_LiveRead_Call_Chain()
    {
        Type[] chain =
        [
            typeof(ToolGateway),
            typeof(LinxDatasetSnapshotReadAdapter),
            typeof(SomaLinxDatasetBulkReader),
            typeof(LinxReadDatasetCatalog),
        ];

        foreach (var type in chain)
        {
            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.DoesNotContain(ctor.GetParameters(), p =>
                    p.ParameterType == typeof(IAIRuntime) || p.ParameterType.Namespace == typeof(IAIRuntime).Namespace);
            }
        }
    }

    [Fact]
    public async Task Real_Catalog_Resolves_The_Registered_Dataset_And_Rejects_Everything_Else()
    {
        var catalog = new LinxReadDatasetCatalog();

        Assert.True(catalog.TryGet(LinxReadDatasetCatalog.FornecedoresSnapshot, out var known));
        Assert.NotNull(known);
        Assert.True(known!.CommandTimeoutSeconds > 0);

        Assert.False(catalog.TryGet("'; DROP TABLE FORNECEDORES; --", out var sqlLike));
        Assert.Null(sqlLike);
        Assert.False(catalog.TryGet("linx.fornecedores.snapshot ", out _)); // no trailing-whitespace leniency
        Assert.False(catalog.TryGet(string.Empty, out _));
        Assert.False(catalog.TryGet("select * from FORNECEDORES", out _));

        var bulkReader = new CountingFakeBulkReader();
        var adapter = new LinxDatasetSnapshotReadAdapter(catalog, bulkReader, new FakeDatasetLoadGate(permitido: true, watermark: Now));

        // Built as a fresh proposal (not a mutated copy of an already-decided one): Resource is part of
        // ActionProposal.ComputeHash's payload, so a request whose Proposal.Resource was tampered with AFTER
        // policy/approval were computed would fail on POLICY_DECISION_PROPOSAL_MISMATCH first — a correct,
        // separate tamper-evidence property, not what this test is isolating (dataset-catalog rejection).
        var maliciousProposal = Proposal(LinxDatasetSnapshotReadAdapter.Capability, LinxDatasetSnapshotReadAdapter.OwnerAgent)
            with
        { Resource = "'; DROP TABLE FORNECEDORES; --" };
        var decision = new PolicyDecision(Guid.NewGuid(), maliciousProposal.Id, maliciousProposal.ProposalHash,
            RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval, ["personal data requires approval"], Now, true, false);
        var grant = new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), maliciousProposal.ProposalHash,
            "subject-product-owner-001", Now, Now.AddYears(1), "recurring-dataset-approval", null, null);
        var auditRoot = Path.Combine(Path.GetTempPath(), "blueprintos-governance-tests", Guid.NewGuid().ToString("N"));
        var gateway = new ToolGateway([adapter], new ApprovalPolicy(), new FileGovernanceAuditStore(auditRoot), new FixedTimeProvider(Now));
        var request = new ToolGatewayRequest(
            LinxDatasetSnapshotReadAdapter.Capability, LinxDatasetSnapshotReadAdapter.OwnerAgent, true,
            maliciousProposal, decision, grant, [], "linx-erp-governed-live-read",
            new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true), GovernedExecutionMode.LiveRead);

        var result = await gateway.InvokeAsync(request);

        Assert.Equal(ToolGatewayStatus.LiveReadFailed, result.Status);
        Assert.Contains("DATASET_UNKNOWN", result.ReadExecution!.Reasons);
        Assert.Equal(0, bulkReader.StreamCallCount);
    }

    [Fact]
    public void RawLinxFornecedorSnapshotExecucao_Starts_Incomplete_And_Only_Becomes_Complete_When_Explicitly_Concluded()
    {
        var execucao = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Full, Now);
        Assert.False(execucao.Completa);

        execucao.Concluir(Now.AddMinutes(5), completa: false, linhasLidas: 40000, linhasGravadas: 40000, isolamentoUtilizado: "READ UNCOMMITTED", erro: "cancelado no meio");
        Assert.False(execucao.Completa);

        execucao.Concluir(Now.AddMinutes(10), completa: true, linhasLidas: 78374, linhasGravadas: 78374, isolamentoUtilizado: "READ UNCOMMITTED", erro: null);
        Assert.True(execucao.Completa);
    }

    [Fact]
    public void Full_Execution_Never_Carries_A_Watermark_Even_If_One_Is_Passed_In()
    {
        var execucao = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Full, Now, watermarkInicial: Now);
        Assert.Null(execucao.WatermarkInicial);

        execucao.Concluir(Now.AddHours(1), completa: true, linhasLidas: 78374, linhasGravadas: 78374, isolamentoUtilizado: "READ UNCOMMITTED", erro: null, watermarkFinal: Now.AddHours(1));
        Assert.Null(execucao.WatermarkFinal);
    }

    [Fact]
    public void Incremental_Execution_Carries_Watermarks_Only_When_Completed_Successfully()
    {
        var execucao = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Incremental, Now, watermarkInicial: Now.AddDays(-1));
        Assert.Equal(Now.AddDays(-1), execucao.WatermarkInicial);

        execucao.Concluir(Now.AddMinutes(1), completa: false, linhasLidas: 10, linhasGravadas: 10, isolamentoUtilizado: "READ UNCOMMITTED", erro: "timeout", watermarkFinal: Now);
        Assert.Null(execucao.WatermarkFinal); // never carries a final watermark when incomplete

        var sucesso = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Incremental, Now, watermarkInicial: Now.AddDays(-1));
        sucesso.Concluir(Now.AddMinutes(1), completa: true, linhasLidas: 42, linhasGravadas: 42, isolamentoUtilizado: "READ UNCOMMITTED", erro: null, watermarkFinal: Now);
        Assert.Equal(Now, sucesso.WatermarkFinal);
    }

    [Fact]
    public void Reconciliation_Can_Never_Be_Registered_For_An_Incomplete_Execution()
    {
        var execucao = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Full, Now);

        Assert.Throws<InvalidOperationException>(() => execucao.RegistrarReconciliacao(RawReconciliacaoStatus.Aprovada, Now));
    }

    [Fact]
    public void HomologarBaseline_Requires_A_Completed_Full_Execution_Reconciled_As_Aprovada()
    {
        var incompleta = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Full, Now);
        var estado1 = LinxDatasetLoadState.Novo(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot);
        Assert.Throws<InvalidOperationException>(() => estado1.HomologarBaseline(incompleta, Now, null));

        var semReconciliar = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Full, Now);
        semReconciliar.Concluir(Now.AddHours(1), completa: true, 78374, 78374, "READ UNCOMMITTED", null);
        var estado2 = LinxDatasetLoadState.Novo(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot);
        Assert.Throws<InvalidOperationException>(() => estado2.HomologarBaseline(semReconciliar, Now, null));

        var reprovada = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Full, Now);
        reprovada.Concluir(Now.AddHours(1), completa: true, 78374, 78374, "READ UNCOMMITTED", null);
        reprovada.RegistrarReconciliacao(RawReconciliacaoStatus.Reprovada, Now);
        var estado3 = LinxDatasetLoadState.Novo(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot);
        Assert.Throws<InvalidOperationException>(() => estado3.HomologarBaseline(reprovada, Now, null));

        var incremental = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Incremental, Now, Now.AddDays(-1));
        incremental.Concluir(Now.AddMinutes(5), completa: true, 12, 12, "READ UNCOMMITTED", null, watermarkFinal: Now);
        incremental.RegistrarReconciliacao(RawReconciliacaoStatus.Aprovada, Now);
        var estado4 = LinxDatasetLoadState.Novo(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot);
        Assert.Throws<InvalidOperationException>(() => estado4.HomologarBaseline(incremental, Now, null));
    }

    [Fact]
    public void HomologarBaseline_Liberates_Incremental_Only_After_A_Genuinely_Approved_Full_Reconciliation()
    {
        var execucao = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Full, Now);
        execucao.Concluir(Now.AddHours(1), completa: true, 78374, 78374, "READ UNCOMMITTED", null);
        execucao.RegistrarReconciliacao(RawReconciliacaoStatus.Aprovada, Now.AddHours(2));

        var estado = LinxDatasetLoadState.Novo(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot);
        Assert.False(estado.PodeExecutarIncremental());

        estado.HomologarBaseline(execucao, Now.AddHours(2), Now.AddHours(1));

        Assert.True(estado.CargaFullInicialValidada);
        Assert.True(estado.IncrementalLiberado);
        Assert.True(estado.PodeExecutarIncremental());
        Assert.Equal(execucao.Id, estado.BaselineExecucaoId);
    }

    [Fact]
    public void AvancarWatermark_Rejects_Incomplete_Cancelled_Or_Full_Executions()
    {
        var estado = HomologatedState();

        var incompleta = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Incremental, Now, Now);
        incompleta.Concluir(Now.AddMinutes(1), completa: false, 5, 5, "READ UNCOMMITTED", "timeout");
        Assert.Throws<InvalidOperationException>(() => estado.AvancarWatermark(incompleta));

        var full = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Full, Now);
        full.Concluir(Now.AddMinutes(1), completa: true, 5, 5, "READ UNCOMMITTED", null);
        Assert.Throws<InvalidOperationException>(() => estado.AvancarWatermark(full));
    }

    [Fact]
    public void AvancarWatermark_Never_Regresses_And_Blocks_Until_Bootstrap_Is_Homologated()
    {
        var estadoNaoHomologado = LinxDatasetLoadState.Novo(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot);
        var execucaoValida = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Incremental, Now, Now.AddDays(-1));
        execucaoValida.Concluir(Now.AddMinutes(1), completa: true, 10, 10, "READ UNCOMMITTED", null, watermarkFinal: Now);
        Assert.Throws<InvalidOperationException>(() => estadoNaoHomologado.AvancarWatermark(execucaoValida));

        var estado = HomologatedState(baselineWatermark: Now);
        var maisAntiga = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Incremental, Now, Now.AddDays(-1));
        maisAntiga.Concluir(Now.AddMinutes(1), completa: true, 3, 3, "READ UNCOMMITTED", null, watermarkFinal: Now.AddMinutes(-10));
        estado.AvancarWatermark(maisAntiga);
        Assert.Equal(Now, estado.UltimoWatermarkValido); // regression rejected silently, never advances backwards

        var maisNova = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Incremental, Now, Now);
        maisNova.Concluir(Now.AddMinutes(1), completa: true, 7, 7, "READ UNCOMMITTED", null, watermarkFinal: Now.AddHours(1));
        estado.AvancarWatermark(maisNova);
        Assert.Equal(Now.AddHours(1), estado.UltimoWatermarkValido);
    }

    [Fact]
    public void DatasetLoadModeContext_Round_Trips_And_Rejects_Absent_Context()
    {
        Assert.False(DatasetLoadModeContext.TryDecode(null, out _));
        Assert.False(DatasetLoadModeContext.TryDecode("garbage", out _));

        Assert.True(DatasetLoadModeContext.TryDecode(DatasetLoadModeContext.Encode(DatasetLoadKind.Full), out var full));
        Assert.Equal(DatasetLoadKind.Full, full);

        Assert.True(DatasetLoadModeContext.TryDecode(DatasetLoadModeContext.Encode(DatasetLoadKind.Incremental), out var incremental));
        Assert.Equal(DatasetLoadKind.Incremental, incremental);
    }

    /// <summary>
    /// B3/Bloco 5A, decisão do PO (fail-closed, revisão pós-homologação do Gate FULL→RAW): prova que
    /// <see cref="DatasetLoadModeContext.TryDecode"/> NUNCA produz um <see cref="DatasetLoadKind"/> a partir
    /// de entrada não reconhecida — sempre retorna <c>false</c>, nunca cai silenciosamente em Full. Inclui
    /// especificamente os valores numéricos que <c>Enum.TryParse</c> aceitaria (a superfície aberta que a
    /// implementação anterior tinha; o TryDecode atual usa apenas correspondência exaustiva de string
    /// literal, nunca parsing de enum) e, adicionalmente, "Full"/"Incremental" com o prefixo — que antes da
    /// correção fail-closed caiam silenciosamente em Full, e agora devem ser rejeitados como qualquer outra
    /// entrada não reconhecida.
    /// </summary>
    [Theory]
    [InlineData("loadMode=1")]
    [InlineData("loadMode=2")]
    [InlineData("loadMode=999")]
    [InlineData("loadMode=-1")]
    [InlineData("loadMode=full")] // wrong casing
    [InlineData("loadMode=INCREMENTAL")] // wrong casing
    [InlineData("loadMode=Incremental; DROP TABLE FORNECEDORES;--")]
    [InlineData("loadMode=Incremental ")] // trailing whitespace
    [InlineData(" loadMode=Incremental")] // leading whitespace
    [InlineData("loadMode=")]
    [InlineData("Full")] // missing prefix
    [InlineData("something-else-entirely")]
    [InlineData("")]
    public void DatasetLoadModeContext_TryDecode_FailsClosed_For_Anything_Not_Exactly_Recognized(string arbitraryInput)
    {
        Assert.False(DatasetLoadModeContext.TryDecode(arbitraryInput, out var decoded));
        Assert.Equal(default, decoded);
    }

    [Fact]
    public void DatasetLoadModeContext_Encode_Rejects_Undefined_Enum_Values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DatasetLoadModeContext.Encode((DatasetLoadKind)999));
    }

    [Fact]
    public void Full_And_Incremental_Proposals_For_The_Same_Dataset_Never_Share_A_Hash()
    {
        // Deliberate: an exceptional administrative Full reload must never silently ride on the daily
        // Incremental proposal's standing (hash-stable) approval grant.
        var full = Proposal() with { AdditionalContext = DatasetLoadModeContext.Encode(DatasetLoadKind.Full) };
        var incremental = Proposal() with { AdditionalContext = DatasetLoadModeContext.Encode(DatasetLoadKind.Incremental) };

        Assert.NotEqual(full.ProposalHash, incremental.ProposalHash);
    }

    [Fact]
    public void Catalog_Full_Query_Has_No_Watermark_Filter_While_Incremental_Uses_A_Hybrid_Parameterized_One()
    {
        Assert.True(new LinxReadDatasetCatalog().TryGet(LinxReadDatasetCatalog.FornecedoresSnapshot, out var dataset));

        var fullText = dataset!.ResolveCommandText(DatasetLoadKind.Full);
        Assert.DoesNotContain("@watermark", fullText, StringComparison.Ordinal);
        Assert.DoesNotContain("WHERE", fullText, StringComparison.OrdinalIgnoreCase);

        var incrementalText = dataset.ResolveCommandText(DatasetLoadKind.Incremental);
        Assert.Contains("@watermark", incrementalText, StringComparison.Ordinal);
        // Hybrid: both tables' recency column must be considered independently (confirmed real risk — see
        // dataset catalog doc comment), never just one.
        Assert.Contains("f.[DATA_PARA_TRANSFERENCIA] >= @watermark", incrementalText, StringComparison.Ordinal);
        Assert.Contains("c.[DATA_PARA_TRANSFERENCIA] >= @watermark", incrementalText, StringComparison.Ordinal);

        Assert.NotNull(dataset.Watermark);
        Assert.Equal(2, dataset.Watermark!.QualifiedColumns.Count);
        Assert.True(dataset.Watermark.OverlapWindow > TimeSpan.Zero);
        Assert.True(dataset.BootstrapFullObrigatorio); // dataset is recommended Incremental
    }

    /// <summary>B3/Bloco 5A, decisão do PO ("Aceitar EXCLUSIVAMENTE Full/Incremental... qualquer outro
    /// valor deve rejeitar a execução... NÃO escolher Full implicitamente"): prova ponta a ponta, no adapter
    /// real, que um <c>AdditionalContext</c> ausente ou malformado bloqueia a execução ANTES de qualquer
    /// tentativa de streaming — nunca executa como se fosse Full.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("loadMode=full")] // wrong casing — must not be silently accepted as Full
    public async Task Adapter_Rejects_Invalid_Or_Missing_LoadMode_Without_Ever_Defaulting_To_Full(string? additionalContext)
    {
        var catalog = new LinxReadDatasetCatalog();
        var bulkReader = new CountingFakeBulkReader();
        var adapter = new LinxDatasetSnapshotReadAdapter(catalog, bulkReader, new FakeDatasetLoadGate(permitido: true, watermark: Now));

        var proposal = Proposal(LinxDatasetSnapshotReadAdapter.Capability, LinxDatasetSnapshotReadAdapter.OwnerAgent)
            with
        { AdditionalContext = additionalContext };
        var request = BuildDirectRequest(adapter: null, proposal);

        var result = await adapter.ExecuteAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains("LOAD_MODE_INVALID", result.Reasons);
        Assert.Equal(0, bulkReader.StreamCallCount);
    }

    [Fact]
    public async Task Adapter_Blocks_Incremental_Until_The_Load_Gate_Confirms_Bootstrap_Is_Homologated()
    {
        var catalog = new LinxReadDatasetCatalog();
        var bulkReader = new CountingFakeBulkReader();
        var adapter = new LinxDatasetSnapshotReadAdapter(catalog, bulkReader, new FakeDatasetLoadGate(permitido: false, watermark: null));

        var proposal = Proposal(LinxDatasetSnapshotReadAdapter.Capability, LinxDatasetSnapshotReadAdapter.OwnerAgent)
            with
        { AdditionalContext = DatasetLoadModeContext.Encode(DatasetLoadKind.Incremental) };
        var request = BuildDirectRequest(adapter: null, proposal);

        var result = await adapter.ExecuteAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains("INCREMENTAL_BLOCKED_BOOTSTRAP_PENDING", result.Reasons);
        Assert.Equal(0, bulkReader.StreamCallCount);
    }

    [Fact]
    public async Task Adapter_Runs_Incremental_With_The_Gates_Effective_Watermark_Once_Liberated()
    {
        var catalog = new LinxReadDatasetCatalog();
        var bulkReader = new CountingFakeBulkReader();
        var watermark = Now.AddDays(-1);
        var adapter = new LinxDatasetSnapshotReadAdapter(catalog, bulkReader, new FakeDatasetLoadGate(permitido: true, watermark: watermark));

        var proposal = Proposal(LinxDatasetSnapshotReadAdapter.Capability, LinxDatasetSnapshotReadAdapter.OwnerAgent)
            with
        { AdditionalContext = DatasetLoadModeContext.Encode(DatasetLoadKind.Incremental) };
        var request = BuildDirectRequest(adapter: null, proposal);

        var result = await adapter.ExecuteAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(1, bulkReader.StreamCallCount);
        Assert.Equal(DatasetLoadKind.Incremental, bulkReader.LastModo);
        Assert.Equal(watermark, bulkReader.LastWatermark);
    }

    private static ToolGatewayRequest BuildDirectRequest(IGovernedToolAdapter? adapter, ActionProposal proposal)
    {
        var decision = new PolicyDecision(Guid.NewGuid(), proposal.Id, proposal.ProposalHash,
            RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval, ["personal data requires approval"], Now, true, false);
        var grant = new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), proposal.ProposalHash,
            "subject-product-owner-001", Now, Now.AddYears(1), "recurring-dataset-approval", null, null);
        return new ToolGatewayRequest(
            LinxDatasetSnapshotReadAdapter.Capability, LinxDatasetSnapshotReadAdapter.OwnerAgent, true,
            proposal, decision, grant, [], "linx-erp-governed-live-read",
            new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true), GovernedExecutionMode.LiveRead);
    }

    private static LinxDatasetLoadState HomologatedState(DateTimeOffset? baselineWatermark = null)
    {
        var execucao = RawLinxFornecedorSnapshotExecucao.Iniciar(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot, RawLoadMode.Full, Now);
        execucao.Concluir(Now.AddHours(1), completa: true, 78374, 78374, "READ UNCOMMITTED", null);
        execucao.RegistrarReconciliacao(RawReconciliacaoStatus.Aprovada, Now.AddHours(2));

        var estado = LinxDatasetLoadState.Novo(Guid.NewGuid(), LinxReadDatasetCatalog.FornecedoresSnapshot);
        estado.HomologarBaseline(execucao, Now.AddHours(2), baselineWatermark ?? Now.AddHours(1));
        return estado;
    }

    private static Fixture CreateFixture(IGovernedToolAdapter[]? adapters = null, string? capabilityId = null, string? ownerAgentId = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "blueprintos-governance-tests", Guid.NewGuid().ToString("N"));
        var audit = new FileGovernanceAuditStore(root);
        var readAdapter = new FakeReadExecutionAdapter();
        var resolvedAdapters = adapters ?? [readAdapter];
        var gateway = new ToolGateway(resolvedAdapters, new ApprovalPolicy(), audit, new FixedTimeProvider(Now));

        var effectiveCapability = capabilityId ?? FakeReadExecutionAdapter.CapabilityId;
        var effectiveOwner = ownerAgentId ?? FakeReadExecutionAdapter.OwnerAgentId;
        var proposal = Proposal(effectiveCapability, effectiveOwner);
        var decision = new PolicyDecision(Guid.NewGuid(), proposal.Id, proposal.ProposalHash,
            RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval, ["personal data requires approval"], Now, true, false);
        var grant = new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), proposal.ProposalHash,
            "subject-product-owner-001", Now, Now.AddYears(1), "recurring-dataset-approval", null, null);

        return new(gateway, readAdapter, audit, proposal, decision, grant, effectiveCapability, effectiveOwner);
    }

    private static ActionProposal Proposal(string capability = FakeReadExecutionAdapter.CapabilityId, string owner = FakeReadExecutionAdapter.OwnerAgentId) => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = Now,
        RequestingAgent = owner,
        Environment = GovernanceEnvironment.Development,
        System = "SOMA/Linx",
        ResourceType = ActionResourceType.DatabaseTable,
        Resource = LinxReadDatasetCatalog.FornecedoresSnapshot,
        Operation = ActionOperation.Select,
        Fields = [],
        FilterSummary = null,
        ExpectedAffectedRows = null,
        Purpose = "Carga diaria RAW de Fornecedores Linx (B3, Bloco 5A.9, Gate A).",
        DataClassification = DataClassification.PersonalData,
        ContainsPersonalData = true,
        ContainsSensitivePersonalData = false,
        ContainsSecrets = false,
        Reversibility = ActionReversibility.Reversible,
    };

    private sealed record Fixture(
        ToolGateway Gateway,
        FakeReadExecutionAdapter Adapter,
        FileGovernanceAuditStore Audit,
        ActionProposal Proposal,
        PolicyDecision Decision,
        ApprovalGrant Grant,
        string CapabilityId,
        string OwnerAgentId)
    {
        public ToolGatewayRequest LiveReadRequest() => new(
            CapabilityId,
            OwnerAgentId,
            true,
            Proposal,
            Decision,
            Grant,
            [],
            "linx-erp-governed-live-read",
            new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true),
            GovernedExecutionMode.LiveRead);
    }

    private sealed class FakeReadExecutionAdapter : IReadExecutionAdapter
    {
        public const string CapabilityId = "fake-governed-read";
        public const string OwnerAgentId = "linx-database-specialist-agent";

        public string Capability => CapabilityId;
        public string OwnerAgent => OwnerAgentId;
        public IReadOnlyList<string> AllowedConnectionProfiles => ["linx-erp-governed-live-read"];

        public int ExecuteCallCount { get; private set; }
        public string? FailWith { get; set; }
        public string? ThrowWith { get; set; }
        public bool SimulateCancellation { get; set; }

        public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SomaLinxDryRunPreview(
                request.Proposal.System, request.Proposal.Environment, request.Proposal.Resource, request.Proposal.Operation,
                request.Proposal.Fields, request.Proposal.FilterSummary, request.Proposal.ExpectedAffectedRows,
                request.Proposal.Purpose, request.ConnectionProfile, request.PolicyDecision.RiskClassification,
                request.PolicyDecision.Status, "granted", request.Proposal.Reversibility, request.ExecutionMode,
                true, true, false, false));

        public Task<ReadExecutionResult> ExecuteAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default)
        {
            ExecuteCallCount++;
            if (SimulateCancellation)
                return Task.FromResult(new ReadExecutionResult(false, 0, 0, "READ UNCOMMITTED", TimeSpan.Zero, ["CANCELLED"], "Cancelado."));
            if (ThrowWith is not null) throw new InvalidOperationException(ThrowWith);
            if (FailWith is not null) return Task.FromResult(new ReadExecutionResult(false, 0, 0, "READ UNCOMMITTED", TimeSpan.Zero, ["READ_FAILED"], FailWith));

            return Task.FromResult(new ReadExecutionResult(true, 100, 100, "READ UNCOMMITTED", TimeSpan.FromMilliseconds(42), []));
        }
    }

    private sealed class FakeWriteOnlyAdapter : IGovernedToolAdapter, IWriteExecutionAdapter
    {
        public string Capability => FakeReadExecutionAdapter.CapabilityId;
        public string OwnerAgent => FakeReadExecutionAdapter.OwnerAgentId;
        public IReadOnlyList<string> AllowedConnectionProfiles => ["linx-erp-governed-live-read"];

        public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Should never be reached in these tests.");

        public Task<WriteExecutionResult> ExecuteAsync(ToolGatewayRequest request, RecoveryPackageReceipt? recoveryPackage, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("A write-only adapter must never be invoked through the LiveRead path.");
    }

    private sealed class CountingFakeBulkReader : ISomaLinxDatasetBulkReader
    {
        public int StreamCallCount { get; private set; }
        public DatasetLoadKind? LastModo { get; private set; }
        public DateTimeOffset? LastWatermark { get; private set; }

        public Task<ReadExecutionResult> StreamAsync(ReadDatasetDefinition dataset, Guid executionId, DatasetLoadKind modo, DateTimeOffset? watermark, CancellationToken cancellationToken = default)
        {
            StreamCallCount++;
            LastModo = modo;
            LastWatermark = watermark;
            return Task.FromResult(new ReadExecutionResult(true, 1, 1, "READ UNCOMMITTED", TimeSpan.FromMilliseconds(1), []));
        }
    }

    private sealed class FakeDatasetLoadGate(bool permitido, DateTimeOffset? watermark) : IDatasetLoadGate
    {
        public Task<IncrementalAuthorization> AuthorizeIncrementalAsync(string dataset, TimeSpan overlapWindow, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IncrementalAuthorization(permitido, permitido ? watermark : null));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
