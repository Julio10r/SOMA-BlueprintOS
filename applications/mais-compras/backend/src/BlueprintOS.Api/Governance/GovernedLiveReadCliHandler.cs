using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using BlueprintOS.Infrastructure.DependencyInjection;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Api.Governance;

/// <summary>
/// B3 — Bloco 5A.9, Gate B4: the CLI entry point for a REAL LiveRead execution — the same architectural
/// isolation as <see cref="GovernedExecuteCliHandler"/>'s live-write path: this capability is deliberately
/// NOT registered in the general web/API DI composition (<c>AddGovernedWriteStack</c>), so a real, governed,
/// non-mutating dataset read is reachable only from this process, never from a web request.
///
/// A single verb ("run") is offered rather than the write side's propose/approve/run split: unlike a write,
/// a LiveRead has nothing to roll back and no recovery package to prepare ahead of time, and every execution
/// this handler performs requires the operator to have already obtained explicit, out-of-band Product Owner
/// authorization for that specific dataset/mode (recorded in <c>ApprovalGrant.Notes</c>) — collapsing
/// propose+approve+run into one command here is honest about that, not a shortcut around governance.
/// </summary>
public static class GovernedLiveReadCliHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<int> RunAsync(string[] args, TextWriter output, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (args.Length < 2 || args[1] != "run")
        {
            return await WriteErrorAsync(output, "UNKNOWN_VERB", "Uso: linx-liveread run --dataset <nome> --mode Full|Incremental --approved-by <nome> --reason <texto>");
        }

        var datasetName = ReadOption(args, "--dataset") ?? LinxReadDatasetCatalog.FornecedoresSnapshot;
        var modeText = ReadOption(args, "--mode") ?? nameof(DatasetLoadKind.Full);
        var approvedBy = ReadOption(args, "--approved-by");
        var reason = ReadOption(args, "--reason");

        if (!Enum.TryParse<DatasetLoadKind>(modeText, ignoreCase: false, out var modo) || !Enum.IsDefined(modo))
        {
            return await WriteErrorAsync(output, "INVALID_MODE", $"--mode deve ser exatamente 'Full' ou 'Incremental' (recebido: '{modeText}').");
        }

        if (string.IsNullOrWhiteSpace(approvedBy) || string.IsNullOrWhiteSpace(reason))
        {
            return await WriteErrorAsync(output, "APPROVAL_CONTEXT_REQUIRED", "--approved-by e --reason sao obrigatorios: toda execucao real fica registrada, com nome e justificativa, no ApprovalGrant persistido.");
        }

        var catalog = new LinxReadDatasetCatalog();
        if (!catalog.TryGet(datasetName, out var dataset) || dataset is null)
        {
            return await WriteErrorAsync(output, "DATASET_UNKNOWN", $"Dataset '{datasetName}' nao esta registrado no catalogo governado.");
        }

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
#pragma warning disable ASP0000 // Isolated CLI composition root; no ASP.NET host is created.
        await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BlueprintOSDbContext>();

        var now = TimeProvider.System.GetUtcNow();
        var proposal = BuildProposal(dataset.Name, modo, reason);
        var decision = new AIGovernancePolicyEngine().Evaluate(proposal, now);

        var governanceRoot = Path.Combine(RuntimeRootLocator.ResolveRuntimeRoot(), "governance");
        IApprovalStore approvals = new FileApprovalStore(governanceRoot);
        IGovernanceAuditStore audit = new FileGovernanceAuditStore(governanceRoot);

        ApprovalGrant? grant = null;
        if (decision.Status == PolicyDecisionStatus.RequiresApproval)
        {
            grant = new ApprovalGrant(
                Id: Guid.NewGuid(),
                ApprovalRequestId: Guid.NewGuid(),
                ProposalHash: proposal.ProposalHash,
                ApprovedBy: approvedBy,
                ApprovedAt: now,
                // Deliberadamente curto: uma execucao Full e uma acao excepcional/administrativa (nunca a
                // recorrente diaria), nao deve carregar uma concessao de longa duracao.
                ExpiresAt: now.AddHours(6),
                Scope: $"{dataset.Name}:{modo}",
                Notes: reason,
                RevokedAt: null);
            await approvals.SaveGrantAsync(grant, cancellationToken);
        }
        else if (decision.Status == PolicyDecisionStatus.Blocked)
        {
            return await WriteErrorAsync(output, "POLICY_BLOCKED", string.Join("; ", decision.Reasons));
        }

        var stateRepository = new LinxDatasetLoadStateRepository(db);
        var loadGate = new LinxDatasetLoadStateGate(stateRepository);
        var bulkReader = new SomaLinxDatasetBulkReader(configuration, LoggerFactory.Create(b => b.AddConsole()).CreateLogger<SomaLinxDatasetBulkReader>());
        var readAdapter = new LinxDatasetSnapshotReadAdapter(catalog, bulkReader, loadGate);
        var gateway = new ToolGateway([readAdapter], new ApprovalPolicy(), audit, TimeProvider.System);

        var request = new ToolGatewayRequest(
            LinxDatasetSnapshotReadAdapter.Capability,
            LinxDatasetSnapshotReadAdapter.OwnerAgent,
            RoutingResolved: true,
            proposal,
            decision,
            grant,
            CrossCuttingAgents: [],
            ConnectionProfile: "linx-erp-governed-live-read",
            Identity: new IdentityPermissionContext($"cli:linx-liveread:{approvedBy}", HasEffectivePermission: true),
            ExecutionMode: GovernedExecutionMode.LiveRead);

        var rawLoadMode = modo == DatasetLoadKind.Full ? RawLoadMode.Full : RawLoadMode.Incremental;
        var execucao = RawLinxFornecedorSnapshotExecucao.Iniciar(proposal.Id, dataset.Name, rawLoadMode, now);
        db.RawLinxFornecedoresSnapshotExecucoes.Add(execucao);
        await db.SaveChangesAsync(cancellationToken);

        var result = await gateway.InvokeAsync(request, cancellationToken);
        var concluidoEm = TimeProvider.System.GetUtcNow();
        var execucaoResult = result.ReadExecution;

        // Decisão do PO (B3/Bloco 5A, regra definitiva de watermark): o candidato a próximo watermark é o
        // instante de INÍCIO desta execução (execucao.IniciadoEm), nunca o de conclusão — protege contra uma
        // alteração feita no Linx durante a própria janela de leitura. Concluir() só efetivamente grava este
        // valor em WatermarkFinal quando Modo==Incremental && completa (Full nunca tem WatermarkFinal).
        execucao.Concluir(
            concluidoEm,
            completa: result.Status == ToolGatewayStatus.LiveReadCompleted,
            linhasLidas: execucaoResult?.RowsRead ?? 0,
            linhasGravadas: execucaoResult?.RowsWritten ?? 0,
            isolamentoUtilizado: execucaoResult?.IsolationLevelUsed ?? "N/A",
            erro: execucaoResult?.ErrorMessage,
            watermarkFinal: execucao.IniciadoEm);
        await db.SaveChangesAsync(cancellationToken);

        await WriteAsync(output, new
        {
            executionId = proposal.Id,
            dataset = dataset.Name,
            modo,
            status = result.Status.ToString(),
            reasons = result.Reasons,
            rowsRead = execucaoResult?.RowsRead,
            rowsWritten = execucaoResult?.RowsWritten,
            isolationLevelUsed = execucaoResult?.IsolationLevelUsed,
            durationMs = execucaoResult?.Duration.TotalMilliseconds,
            completa = execucao.Completa,
            errorMessage = execucaoResult?.ErrorMessage,
            approvalGrantId = grant?.Id,
            governanceRoot,
        });

        return result.Status == ToolGatewayStatus.LiveReadCompleted ? 0 : 1;
    }

    private static ActionProposal BuildProposal(string dataset, DatasetLoadKind modo, string reason) => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = TimeProvider.System.GetUtcNow(),
        RequestingAgent = LinxDatasetSnapshotReadAdapter.OwnerAgent,
        Environment = GovernanceEnvironment.Development,
        System = "SOMA/Linx",
        ResourceType = ActionResourceType.DatabaseTable,
        Resource = dataset,
        Operation = ActionOperation.Select,
        Fields = [],
        FilterSummary = null,
        ExpectedAffectedRows = null,
        Purpose = reason,
        DataClassification = DataClassification.PersonalData,
        ContainsPersonalData = true,
        ContainsSensitivePersonalData = false,
        ContainsSecrets = false,
        Reversibility = ActionReversibility.Reversible,
        AdditionalContext = DatasetLoadModeContext.Encode(modo),
    };

    private static string? ReadOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal)) return args[i + 1];
        }

        return null;
    }

    private static Task WriteAsync(TextWriter output, object value) =>
        output.WriteLineAsync(JsonSerializer.Serialize(value, JsonOptions));

    private static async Task<int> WriteErrorAsync(TextWriter output, string error, string? message = null)
    {
        await WriteAsync(output, new { error, message });
        return 1;
    }
}
