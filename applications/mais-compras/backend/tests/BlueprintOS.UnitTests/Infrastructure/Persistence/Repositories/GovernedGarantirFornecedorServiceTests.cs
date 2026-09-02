using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Governance;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Infrastructure.Persistence.Repositories;

/// <summary>
/// Exercises the governed "garantir fornecedor" entry point in isolation, with a fake ERP adapter. No SQL
/// Server, no SOMA_DESENV, no production, no WISE: the ERP adapter is replaced entirely.
/// </summary>
public sealed class GovernedGarantirFornecedorServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 16, 0, 0, TimeSpan.Zero);
    private const string Cnpj = "00000000000191";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blueprintos-garantir-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Governed_Path_Backs_Up_Executes_The_Erp_Adapter_And_Validates()
    {
        var fixture = CreateFixture();
        var result = await fixture.Service.GarantirAsync(Request(), GrantFor(fixture));

        Assert.Equal(GovernedWriteExecutionStatus.Completed, result.Execution.Status);
        Assert.NotNull(result.Erp);
        Assert.Equal(OperacaoGarantirFornecedorErp.Atualizado, result.Erp!.Operacao);
        Assert.Equal(1, fixture.Erp.CallCount);
        Assert.True(result.Execution.Validation!.Passed);
        Assert.NotNull(result.Execution.RecoveryPackage);
    }

    [Fact]
    public async Task Without_An_Approval_Nothing_Reaches_The_Erp_Adapter()
    {
        var fixture = CreateFixture();
        var result = await fixture.Service.GarantirAsync(Request(), approvalGrant: null);

        Assert.Equal(GovernedWriteExecutionStatus.AwaitingApproval, result.Execution.Status);
        Assert.Equal(0, fixture.Erp.CallCount);
        Assert.Null(result.Erp);
    }

    [Fact]
    public async Task Erp_Failure_Is_Reported_As_ExecutionFailed_With_The_Backup_Intact()
    {
        var fixture = CreateFixture();
        fixture.Erp.Throw = new ErpFornecedorEscritaException(ErpFornecedorErro.Persistencia, "O ERP nao confirmou a criacao.");
        var result = await fixture.Service.GarantirAsync(Request(), GrantFor(fixture));

        Assert.Equal(GovernedWriteExecutionStatus.ExecutionFailed, result.Execution.Status);
        Assert.NotNull(result.Execution.RecoveryPackage);
        Assert.True(File.Exists(Path.Combine(result.Execution.RecoveryPackage!.PackagePath, RecoveryPackageWriter.BeforeDataFileName)));
    }

    [Fact]
    public async Task Execution_Is_Discoverable_By_The_Supplier_Business_Key()
    {
        var fixture = CreateFixture();
        var result = await fixture.Service.GarantirAsync(Request(), GrantFor(fixture));

        var entry = Assert.Single(await fixture.RecoveryIndex.FindAsync(new RecoveryIndexQuery { BusinessKey = Cnpj }));
        Assert.Equal(result.Execution.ExecutionId, entry.ExecutionId);
        Assert.Equal(GovernedGarantirFornecedorService.ExecutionName, entry.ExecutionName);
    }

    [Theory]
    [InlineData("CGC_CPF=00000000000191", "00000000000191")]
    [InlineData("00.000.000/0001-91", "00000000000191")]
    [InlineData("", "")]
    // CNPJ alfanumérico (Instrução Normativa RFB nº 2.229/2024, vigente a partir de julho/2026):
    // CGC_CPF no Linx é varchar(19), sem constraint numérica — letras nas 12 primeiras posições
    // precisam ser preservadas, nunca descartadas como se fossem ruído de máscara.
    [InlineData("CGC_CPF=12.ABC.345/01DE-35", "12ABC34501DE35")]
    public void Business_Key_Parsing_Keeps_Digits_And_Letters(string businessKey, string expected) =>
        Assert.Equal(expected, SomaGarantirFornecedorErpAdapter.ExtrairCnpjDaChaveDeNegocio(businessKey));

    private static GovernedGarantirFornecedorRequest Request() => new(
        "REQ-GARANTIR-001",
        "subject-requester-001",
        new GarantirFornecedorErpRequest("BU-SOMA", Cnpj, "ACME", "ACME LTDA", "Sao Paulo", "SP", "BRASIL", true, "CORR-001"),
        new IdentityPermissionContext("subject-executor-001", HasEffectivePermission: true),
        WriteVerificationProfileSeeds.LinxDevelopment,
        "192.168.9.98",
        "SOMA_DESENV");

    /// <summary>Builds a grant bound to the exact proposal the service's context will produce.</summary>
    private static ApprovalGrant GrantFor(Fixture fixture)
    {
        var probe = new ProbeOrchestrator();
        var hash = probe.BuildProposalHash();
        return new ApprovalGrant(Guid.NewGuid(), Guid.NewGuid(), hash, "subject-product-owner-001", Now,
            Now.AddMinutes(30), "specific proposal", null, null);
    }

    private Fixture CreateFixture()
    {
                var governanceRoot = Path.Combine(_root, "governance");
        var clock = new FixedTimeProvider(Now);
        var audit = new FileGovernanceAuditStore(governanceRoot);
        var approvals = new FileApprovalStore(governanceRoot);
        var index = new InMemoryRecoveryIndexStore();
        var erp = new FakeErpAdapter();
        var snapshots = new FakeSnapshotSource();
        var recoveryWriter = new RecoveryPackageWriter(_root);
        var writeAudit = new InMemoryWriteExecutionAuditStore();

        // GarantirFornecedorGovernedWriteAdapter declares RollbackStrategy.NotSupported (a real business rule:
        // "garantir" never deletes) — a profile with RollbackSupported=true would now correctly be blocked
        // BEFORE the write by the rollback capability gate (see RollbackCapabilityGapTests for that scenario).
        // These tests exercise the successful backed-up write path, so they use the realistic combination for
        // THIS capability: backup required, rollback not required.
        var profile = WriteVerificationProfileSeeds.LinxDevelopmentPhaseA with { RollbackSupported = false, PolicyVersion = "test-backup-only-no-rollback" };
        var profileStore = new InMemoryWriteVerificationProfileStore([profile]);

        GovernedWriteExecutionOrchestrator Factory(IWriteExecutionAdapter adapter)
        {
            var gateway = new ToolGateway([adapter], new ApprovalPolicy(), audit, clock);
            var stack = new GovernedWriteStack(new StructuredActionProposalAdapter(), new AIGovernancePolicyEngine(), approvals, audit, gateway, clock);
            return new GovernedWriteExecutionOrchestrator(
                stack, profileStore, new PostWriteValidationRuleCatalog(),
                new InMemoryWriteValidationKnowledgeGapStore(), recoveryWriter, index, gateway, writeAudit, clock);
        }

        return new(new GovernedGarantirFornecedorService(erp, snapshots, Factory), erp, index);
    }

    private sealed record Fixture(
        GovernedGarantirFornecedorService Service,
        FakeErpAdapter Erp,
        InMemoryRecoveryIndexStore RecoveryIndex);

    /// <summary>Rebuilds the same proposal the service builds, only to obtain its hash for the approval grant.</summary>
    private sealed class ProbeOrchestrator
    {
        public string BuildProposalHash()
        {
            var context = new StructuredActionContext(
                "REQ-GARANTIR-001", "subject-requester-001", GovernanceEnvironment.Development, "SOMA/Linx",
                ActionResourceType.DatabaseTable, PostWriteValidationRuleCatalog.FornecedoresResource,
                OperationIntent.Update, [SomaGarantirFornecedorErpAdapter.CapabilityId],
                ["FORNECEDOR", "CGC_CPF", "INATIVO"], $"CGC_CPF={Cnpj}", 1,
                "Garantir cadastro de fornecedor no ERP a partir do CNPJ.", DataClassification.Internal,
                false, false, false, ActionReversibility.Reversible,
                ConnectionProfile: WriteVerificationProfileSeeds.LinxDevelopment);
            var routing = new RoutingEvidence(true, SomaGarantirFornecedorErpAdapter.OwnerAgentId, [], [], [], []);
            var analysis = new AgentWriteAnalysis(
                SomaGarantirFornecedorErpAdapter.OwnerAgentId, SomaGarantirFornecedorErpAdapter.CapabilityId,
                ["FORNECEDOR", "CGC_CPF", "INATIVO"], $"CGC_CPF={Cnpj}", 1, ActionReversibility.Reversible);
            return new StructuredActionProposalAdapter().Build(context, routing, analysis, Now).Proposal!.ProposalHash;
        }
    }

    private sealed class FakeErpAdapter : IGarantirFornecedorErpAdapter
    {
        public string ErpSistema => "SOMA_DESENV_FAKE";
        public int CallCount { get; private set; }
        public Exception? Throw { get; set; }

        public Task<GarantirFornecedorErpResultado> GarantirAsync(GarantirFornecedorErpRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (Throw is not null) throw Throw;
            return Task.FromResult(new GarantirFornecedorErpResultado(
                OperacaoGarantirFornecedorErp.Atualizado, "000123", request.BusinessUnit, ErpSistema, Now, request.CorrelationId));
        }
    }

    private sealed class FakeSnapshotSource : ISnapshotCapableAdapter
    {
        private bool _afterWrite;

        public Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default)
        {
            var inativo = _afterWrite ? "0" : "1";
            _afterWrite = true;
            return Task.FromResult<IReadOnlyList<RecoveryDataSet>>(
            [
                new RecoveryDataSet("FORNECEDORES",
                [
                    new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "000123", ["CGC_CPF"] = Cnpj, ["INATIVO"] = inativo },
                ]),
            ]);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
