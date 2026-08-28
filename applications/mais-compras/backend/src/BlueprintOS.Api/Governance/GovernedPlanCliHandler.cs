using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Application.Governance;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Api.Governance;

/// <summary>
/// The `governed-plan` CLI command — the real process boundary between the
/// Node Governed Orchestrator (tools/agents/governed-orchestrator.js,
/// GovernedOrchestrator.buildActionProposalPayload) and the .NET governance
/// pipeline (GovernedPlanBridge → GovernedWriteStack → Policy → Approval →
/// ToolGateway → Adapter → DryRun/Blocked). Deterministic, stdin/stdout JSON,
/// no HTTP surface — this is the "smallest technical bridge" the task asked
/// for, not a new API.
///
/// Invocation: `dotnet run --project backend/src/BlueprintOS.Api -- governed-plan`
/// with the GovernedPlanPayload JSON on stdin; the result (proposal build,
/// policy decision, approval request, or a parse/validation error) is written
/// as JSON to stdout. Exit code 0 for a structurally valid run (even if the
/// governed outcome is Blocked/RequiresApproval — that is a correct governed
/// result, not a CLI failure); exit code 1 only for malformed/unreadable input.
///
/// This CLI mode intentionally does NOT use the application's real
/// SqlServer-backed BlueprintOSDbContext/EfApprovalStore/EfGovernanceAuditStore
/// (AddInfrastructure/AddGovernedWriteStack) — it constructs the governance
/// object graph directly with process-lifetime, non-persisted stores
/// (<see cref="InMemoryApprovalStore"/>, <see cref="InMemoryPlanAuditStore"/>),
/// because this bridge must work fully offline with no external connection.
/// Wiring this command to the real persisted store (so approvals/audit survive
/// across invocations) is a follow-up for when this command is invoked as part
/// of a long-lived, already-configured host — see
/// docs/audits/AgentsV1-FinalCertification.md.
/// </summary>
public static class GovernedPlanCliHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task<int> RunAsync(TextReader input, TextWriter output, CancellationToken cancellationToken = default)
    {
        string raw;
        try
        {
            raw = await input.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await WriteAsync(output, new { error = "STDIN_READ_FAILED", message = ex.Message });
            return 1;
        }

        GovernedPlanPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<GovernedPlanPayload>(raw, JsonOptions);
        }
        catch (JsonException ex)
        {
            await WriteAsync(output, new { error = "INVALID_JSON_PAYLOAD", message = ex.Message });
            return 1;
        }

        if (payload is null)
        {
            await WriteAsync(output, new { error = "EMPTY_PAYLOAD" });
            return 1;
        }

        var audit = new InMemoryPlanAuditStore();
        var approvals = new InMemoryApprovalStore();
        var gateway = new ToolGateway(
            [new SomaLinxDryRunAdapter(), new SomaLinxReadOnlyAdapter(), new WiseGovernedAdapter(), new LinxKnowledgeStoreReadOnlyAdapter()],
            new ApprovalPolicy(), audit, TimeProvider.System);
        var writeStack = new GovernedWriteStack(
            new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, audit, gateway, TimeProvider.System);
        var bridge = new GovernedPlanBridge(writeStack);

        GovernedWritePreparation preparation;
        try
        {
            preparation = await bridge.PrepareAsync(payload, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            await WriteAsync(output, new { error = "INVALID_ENUM_VALUE", message = ex.Message });
            return 1;
        }

        await WriteAsync(output, new
        {
            requestId = payload.RequestId,
            proposalBuild = new
            {
                succeeded = preparation.ProposalBuild.Succeeded,
                contextGaps = preparation.ProposalBuild.ContextGaps,
                proposalId = preparation.ProposalBuild.Proposal?.Id,
                proposalHash = preparation.ProposalBuild.Proposal?.ProposalHash,
            },
            policyDecision = preparation.PolicyDecision is null ? null : new
            {
                status = preparation.PolicyDecision.Status.ToString(),
                riskClassification = preparation.PolicyDecision.RiskClassification.ToString(),
                reasons = preparation.PolicyDecision.Reasons,
            },
            approvalRequest = preparation.ApprovalRequest is null ? null : new
            {
                id = preparation.ApprovalRequest.Id,
                status = preparation.ApprovalRequest.Status.ToString(),
                expiresAt = preparation.ApprovalRequest.ExpiresAt,
            },
            liveExecution = "BLOCKED",
            nextStep = preparation.ApprovalRequest is not null
                ? "Await a real ApprovalPolicy grant, then invoke the Tool Gateway for DryRun."
                : preparation.ProposalBuild.Succeeded
                    ? "Invoke the Tool Gateway for DryRun."
                    : "Resolve context gaps before a proposal can be built.",
        });
        return 0;
    }

    private static Task WriteAsync(TextWriter output, object value) =>
        output.WriteLineAsync(JsonSerializer.Serialize(value, JsonOptions));
}
