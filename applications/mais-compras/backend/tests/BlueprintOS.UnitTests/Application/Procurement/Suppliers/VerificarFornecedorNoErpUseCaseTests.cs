using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Recovery;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

/// <summary>Gate de homologação de Fornecedores (2026-09-01): os 3 estados reais de um CNPJ/CPF no
/// Linx, exatamente como documentado em
/// agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md (unidade
/// linx-idempotencia-convergencia-create-update-fornecedor).</summary>
public sealed class VerificarFornecedorNoErpUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Return_NaoExiste_When_Snapshot_Has_No_Cadastro()
    {
        var useCase = CreateUseCase(new FakeSnapshotAdapter([
            new RecoveryDataSet("CADASTRO_CLI_FOR", []),
            new RecoveryDataSet("FORNECEDORES", [])
        ]));

        var resultado = await useCase.ExecuteAsync("SOMA", "12345678000195");

        Assert.Equal(EstadoFornecedorErp.NaoExiste, resultado.Estado);
        Assert.Null(resultado.CodigoClifor);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_ExisteSemPapelFornecedor_When_Cadastro_Existe_Mas_Fornecedores_Vazio()
    {
        var useCase = CreateUseCase(new FakeSnapshotAdapter([
            new RecoveryDataSet("CADASTRO_CLI_FOR", [new Dictionary<string, string?> { ["COD_CLIFOR"] = "001234" }]),
            new RecoveryDataSet("FORNECEDORES", [])
        ]));

        var resultado = await useCase.ExecuteAsync("SOMA", "12345678000195");

        Assert.Equal(EstadoFornecedorErp.ExisteSemPapelFornecedor, resultado.Estado);
        Assert.Equal("001234", resultado.CodigoClifor);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_ExisteComPapelFornecedor_When_Ambos_Existem()
    {
        var useCase = CreateUseCase(new FakeSnapshotAdapter([
            new RecoveryDataSet("CADASTRO_CLI_FOR", [new Dictionary<string, string?> { ["COD_CLIFOR"] = "001234" }]),
            new RecoveryDataSet("FORNECEDORES", [new Dictionary<string, string?> { ["COD_FORNECEDOR"] = "001234" }])
        ]));

        var resultado = await useCase.ExecuteAsync("SOMA", "12345678000195");

        Assert.Equal(EstadoFornecedorErp.ExisteComPapelFornecedor, resultado.Estado);
        Assert.Equal("001234", resultado.CodigoClifor);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_NaoExiste_Without_Blocking_When_Adapter_Nao_Suporta_Snapshot()
    {
        // Capability gap (adapter sem ISnapshotCapableAdapter): nunca bloqueia o cadastro por causa
        // disso — apenas não detecta duplicidade prévia no Linx (mesmo comportamento de antes desta
        // verificação existir).
        var useCase = CreateUseCase(new FakeNonSnapshotAdapter());

        var resultado = await useCase.ExecuteAsync("SOMA", "12345678000195");

        Assert.Equal(EstadoFornecedorErp.NaoExiste, resultado.Estado);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Keep_Letters_In_Business_Key_For_Cnpj_Alfanumerico()
    {
        // Gate de homologação (2026-09-01): CNPJ alfanumérico (Instrução Normativa RFB nº
        // 2.229/2024, vigente a partir de julho/2026) — CGC_CPF no Linx é varchar(19), sem
        // constraint numérica (docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md:244), então a
        // chave de negócio enviada ao adapter precisa preservar as letras, nunca descartá-las.
        var adapter = new FakeSnapshotAdapter([
            new RecoveryDataSet("CADASTRO_CLI_FOR", []),
            new RecoveryDataSet("FORNECEDORES", [])
        ]);
        var useCase = CreateUseCase(adapter);

        await useCase.ExecuteAsync("SOMA", "12.ABC.345/01DE-35");

        Assert.Equal(["CGC_CPF=12ABC34501DE35"], adapter.ChavesRecebidas);
    }

    private static VerificarFornecedorNoErpUseCase CreateUseCase(IGarantirFornecedorErpAdapter adapter) =>
        new(new FakeResolver(adapter), NullLogger<VerificarFornecedorNoErpUseCase>.Instance);

    private sealed class FakeResolver(IGarantirFornecedorErpAdapter adapter) : IGarantirFornecedorErpAdapterResolver
    {
        public IGarantirFornecedorErpAdapter Resolver(string businessUnit) => adapter;
    }

    private sealed class FakeSnapshotAdapter(IReadOnlyList<RecoveryDataSet> snapshot) : IGarantirFornecedorErpAdapter, ISnapshotCapableAdapter
    {
        public string ErpSistema => "SOMA_DESENV";
        public IReadOnlyList<string>? ChavesRecebidas { get; private set; }
        public Task<GarantirFornecedorErpResultado> GarantirAsync(GarantirFornecedorErpRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException("Não deve ser chamado por esta verificação somente-leitura.");
        public Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken ct = default)
        {
            ChavesRecebidas = businessKeys;
            return Task.FromResult(snapshot);
        }
    }

    /// <summary>Adapter que não implementa ISnapshotCapableAdapter — simula o capability gap.</summary>
    private sealed class FakeNonSnapshotAdapter : IGarantirFornecedorErpAdapter
    {
        public string ErpSistema => "SOMA_DESENV";
        public Task<GarantirFornecedorErpResultado> GarantirAsync(GarantirFornecedorErpRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
