using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace BlueprintOS.IntegrationTests.Integrations;

/// <summary>
/// Knowledge Gap: forces GovernedWriteExecutionOrchestrator directly at an (operation, resource) pair the
/// PostWriteValidationRuleCatalog does not cover. No SOMA_DESENV connection needed — a fake adapter proves the
/// policy block in isolation. Split out of the write/rollback E2E suite because this scenario is deliberately
/// generic and does not depend on which real capability (Fornecedor, the recovery homologation table, or
/// anything else) is under test.
/// </summary>
public sealed class GovernedWriteKnowledgeGapE2EIntegrationTests(ITestOutputHelper output)
{
    private const string RequestedBy = "julio.cesar@somagrupo.com.br";

    [Fact]
    public async Task KnowledgeGap_Blocks_Write_When_No_PostWriteValidationRule_Covers_The_Resource()
    {
        const string unmappedResource = "TABELA_SEM_REGRA_DE_VALIDACAO_E2E";
        var clock = new FixedTimeProvider(DateTimeOffset.UtcNow);
        var profileStore = new InMemoryWriteVerificationProfileStore(); // phase A: PostWriteValidationRequired=true
        var index = new InMemoryRecoveryIndexStore();
        var writeAudit = new InMemoryWriteExecutionAuditStore();
        var gapStore = new InMemoryWriteValidationKnowledgeGapStore();
        var writer = new RecoveryPackageWriter(Path.Combine(Path.GetTempPath(), $"blueprintos-e2e-gap-{Guid.NewGuid():N}"));
        using var db = NewInMemoryDb();
        var approvals = new EfApprovalStore(db);
        var governanceAudit = new EfGovernanceAuditStore(db);
        var writeAdapter = new NoOpWriteAdapter(unmappedResource);

        try
        {
            var gateway = new ToolGateway([writeAdapter], new ApprovalPolicy(), governanceAudit, clock);
            var stack = new GovernedWriteStack(new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, governanceAudit, gateway, clock);
            var orchestrator = new GovernedWriteExecutionOrchestrator(
                stack, profileStore, new PostWriteValidationRuleCatalog(), gapStore, writer, index, gateway, writeAudit, clock);

            var context = new StructuredActionContext(
                "REQ-E2E-KNOWLEDGE-GAP", RequestedBy, GovernanceEnvironment.Development, "SOMA/Linx",
                ActionResourceType.DatabaseTable, unmappedResource, OperationIntent.Update,
                [writeAdapter.Capability], ["ALGUM_CAMPO"], "CHAVE=1", 1,
                "Forcar um recurso sem regra de post-write validation para provar bloqueio de politica.",
                DataClassification.Internal, false, false, false, ActionReversibility.Reversible,
                ConnectionProfile: WriteVerificationProfileSeeds.LinxDevelopment);
            var routing = new RoutingEvidence(true, writeAdapter.OwnerAgent, [], [], [], []);
            var analysis = new AgentWriteAnalysis(writeAdapter.OwnerAgent, writeAdapter.Capability, ["ALGUM_CAMPO"], "CHAVE=1", 1, ActionReversibility.Reversible);

            var probe = new StructuredActionProposalAdapter().Build(context, routing, analysis, clock.GetUtcNow());
            var grant = new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), probe.Proposal!.ProposalHash, "authorized-product-owner",
                clock.GetUtcNow(), clock.GetUtcNow().AddMinutes(30), "grant especifico para o teste de knowledge gap", null, null);

            var request = new GovernedWriteExecutionRequest(
                context, routing, analysis, new IdentityPermissionContext(RequestedBy, HasEffectivePermission: true),
                "e2e-knowledge-gap", WriteVerificationProfileSeeds.LinxDevelopment, "192.168.9.98", "SOMA_DESENV",
                ["CHAVE=1"], [new RecoveryDataSet(unmappedResource, [new Dictionary<string, string?> { ["CHAVE"] = "1" }])],
                "Teste E2E de knowledge gap.", []);

            var result = await orchestrator.ExecuteAsync(request, grant, writeAdapter);

            Assert.Equal(GovernedWriteExecutionStatus.Blocked, result.Status);
            Assert.Contains(WriteValidationKnowledgeGap.ReasonCode, result.Reasons);
            Assert.NotNull(result.KnowledgeGap);
            Assert.Equal(unmappedResource, result.KnowledgeGap!.Resource);
            Assert.Equal(0, writeAdapter.ExecuteCallCount); // nenhuma tentativa de escrita

            var gaps = await gapStore.ListAsync();
            var gap = Assert.Single(gaps);
            Assert.Equal(WriteValidationKnowledgeGap.ReasonCode, gap.Reason);
            Assert.Equal(unmappedResource, gap.Resource);

            output.WriteLine($"Knowledge Gap confirmado: reason={result.Reasons[0]}, gap.Id={gap.Id}, ExecuteCallCount={writeAdapter.ExecuteCallCount}");
        }
        finally
        {
            if (Directory.Exists(writer.RootDirectory)) Directory.Delete(writer.RootDirectory, recursive: true);
        }
    }

    private static BlueprintOSDbContext NewInMemoryDb() => new(new DbContextOptionsBuilder<BlueprintOSDbContext>()
        .UseInMemoryDatabase($"knowledge-gap-e2e-{Guid.NewGuid():N}").Options);

    private sealed class NoOpWriteAdapter(string resource) : IWriteExecutionAdapter, ISnapshotCapableAdapter
    {
        public string Capability => "e2e-noop-governed-write";
        public string OwnerAgent => "linx-database-specialist-agent";
        public IReadOnlyList<string> AllowedConnectionProfiles => [WriteVerificationProfileSeeds.LinxDevelopment];
        public int ExecuteCallCount { get; private set; }

        // This test is about the VALIDATION-RULE gap, not the rollback-CAPABILITY gap (see
        // RollbackCapabilityGapTests for that one) — declare rollback support so the capability gate never
        // fires here and the validation-rule check is what actually blocks the write.
        public RollbackStrategy RollbackStrategy => RollbackStrategy.RestoreBeforeState;

        public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SomaLinxDryRunPreview(
                request.Proposal.System, request.Proposal.Environment, request.Proposal.Resource, request.Proposal.Operation,
                request.Proposal.Fields, request.Proposal.FilterSummary, request.Proposal.ExpectedAffectedRows,
                request.Proposal.Purpose, request.ConnectionProfile, request.PolicyDecision.RiskClassification,
                request.PolicyDecision.Status, "granted", request.Proposal.Reversibility, request.ExecutionMode,
                true, true, false, false));

        public Task<WriteExecutionResult> ExecuteAsync(ToolGatewayRequest request, RecoveryPackageReceipt? recoveryPackage, CancellationToken cancellationToken = default)
        {
            ExecuteCallCount++;
            return Task.FromResult(new WriteExecutionResult(true, 1, [], ["LIVE_EXECUTION_COMPLETED"]));
        }

        public Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecoveryDataSet>>([new RecoveryDataSet(resource, [new Dictionary<string, string?> { ["CHAVE"] = "1" }])]);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
