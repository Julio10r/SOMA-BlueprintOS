using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class ConsultarCnpjFornecedorUseCaseTests
{
    [Fact]
    public async Task Execute_Should_Return_Success_And_Persist_History_With_CorrelationId()
    {
        await using var context = NewContext();
        var result = ConsultaCnpjResultado.CriarSucesso("12345678000195", "ConsultaTeste", SituacaoCadastralCnpj.Ativa,
            DateTimeOffset.UtcNow, razaoSocial: "Fornecedor Teste", cidade: "São Paulo", estado: "SP");

        var response = await Create(context, new FakeProvider(result)).ExecuteAsync(new("12345678000195", "BU-A", "SOMA_DESENV", "corr-cnpj-1"));

        var history = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.True(response.Sucesso);
        Assert.Equal("Fornecedor Teste", response.RazaoSocial);
        Assert.Equal("corr-cnpj-1", history.CorrelationId);
        Assert.Equal("Sucesso", history.Status);
        Assert.Equal("Ativa", history.Resultado);
        Assert.Equal("ConsultaTeste", history.FonteConsulta);
    }

    [Fact]
    public async Task Execute_Should_Return_Error_And_Persist_Failure_History()
    {
        await using var context = NewContext();
        var result = ConsultaCnpjResultado.CriarFalha("12345678000195", "ConsultaTeste", DateTimeOffset.UtcNow,
            TipoErroConsultaCnpj.NaoEncontrado, "Documento não encontrado.");

        var response = await Create(context, new FakeProvider(result)).ExecuteAsync(new("12345678000195", "BU-A", null, "corr-cnpj-erro"));

        var history = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.False(response.Sucesso);
        Assert.Equal(StatusConsultaCnpj.Falha, response.StatusConsulta);
        Assert.Equal("Falha", history.Status);
        Assert.Equal("Documento não encontrado.", history.MensagemErro);
        Assert.Equal("corr-cnpj-erro", history.CorrelationId);
    }

    [Fact]
    public async Task Execute_Should_Never_Persist_A_Fornecedor_As_Side_Effect_Of_A_Query()
    {
        // Regressao B2.6 / BUG-1: CONSULTAR nunca pode significar CADASTRAR.
        // ConsultarCnpjFornecedorUseCase deve permanecer estritamente somente
        // leitura em relacao a tabela Fornecedores: a unica escrita permitida
        // e o historico de auditoria da consulta (FornecedoresCnpjConsultas).
        await using var context = NewContext();
        var result = ConsultaCnpjResultado.CriarSucesso("12345678000195", "ConsultaTeste", SituacaoCadastralCnpj.Ativa,
            DateTimeOffset.UtcNow, razaoSocial: "Fornecedor Teste", cidade: "São Paulo", estado: "SP");

        var response = await Create(context, new FakeProvider(result)).ExecuteAsync(new("12345678000195", "BU-A", "SOMA_DESENV", "corr-cnpj-bug1"));

        Assert.True(response.Sucesso);
        Assert.Empty(context.Fornecedores);
        Assert.Single(context.FornecedoresCnpjConsultas);
    }

    [Fact]
    public async Task Execute_Should_Respect_CancellationToken()
    {
        await using var context = NewContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => Create(context, new CancelledProvider()).ExecuteAsync(
            new("12345678000195", "BU-A", null, "corr-cnpj-cancel"), cancellation.Token));
        Assert.Empty(context.FornecedoresCnpjConsultas);
    }

    [Fact]
    public void Result_Should_Require_Document_And_Source()
    {
        Assert.Throws<ArgumentException>(() => ConsultaCnpjResultado.CriarSucesso("", "ConsultaTeste", SituacaoCadastralCnpj.Ativa, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => ConsultaCnpjResultado.CriarSucesso("12345678000195", "", SituacaoCadastralCnpj.Baixada, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => ConsultaCnpjResultado.CriarFalha("12345678000195", "", DateTimeOffset.UtcNow, TipoErroConsultaCnpj.NaoEncontrado));
    }

    [Fact]
    public async Task Execute_Should_Classify_Unexpected_Provider_Exception_As_ErroInterno()
    {
        await using var context = NewContext();

        var response = await Create(context, new ThrowingProvider()).ExecuteAsync(new("12345678000195", "BU-A", null, "corr-cnpj-throw"));

        Assert.False(response.Sucesso);
        Assert.Equal(TipoErroConsultaCnpj.ErroInterno, response.TipoErro);
    }

    [Fact]
    public async Task Execute_Should_Return_Success_When_History_Persistence_Fails()
    {
        // Regressao: se o registro de auditoria do historico falhar (ex: banco
        // corporativo indisponivel), a consulta em si (ja obtida com sucesso do
        // provider) deve continuar sendo retornada ao chamador em vez de estourar
        // uma excecao nao tratada (HTTP 500).
        var result = ConsultaCnpjResultado.CriarSucesso("12345678000195", "ConsultaTeste", SituacaoCadastralCnpj.Ativa,
            DateTimeOffset.UtcNow, razaoSocial: "Fornecedor Teste");

        var useCase = new ConsultarCnpjFornecedorUseCase(new FakeProvider(result), new ThrowingHistoricoRepository(),
            new FakeIdentity(), NullLogger<ConsultarCnpjFornecedorUseCase>.Instance);

        var response = await useCase.ExecuteAsync(new("12345678000195", "BU-A", "SOMA_DESENV", "corr-cnpj-db-down"));

        Assert.True(response.Sucesso);
        Assert.Equal("Fornecedor Teste", response.RazaoSocial);
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_Result_When_History_Persistence_Fails_And_Identity_Is_Unavailable()
    {
        // Regressao: falha ao obter a identidade atual (ex: header de
        // desenvolvimento ausente) ao registrar o historico tambem nao pode
        // derrubar a resposta da consulta.
        var result = ConsultaCnpjResultado.CriarFalha("12345678000195", "ConsultaTeste", DateTimeOffset.UtcNow,
            TipoErroConsultaCnpj.NaoEncontrado, "CNPJ não encontrado.");

        var useCase = new ConsultarCnpjFornecedorUseCase(new FakeProvider(result), new ThrowingHistoricoRepository(),
            new ThrowingIdentity(), NullLogger<ConsultarCnpjFornecedorUseCase>.Instance);

        var response = await useCase.ExecuteAsync(new("12345678000195", "BU-A", null, "corr-cnpj-no-identity"));

        Assert.False(response.Sucesso);
        Assert.Equal("CNPJ não encontrado.", response.MensagemErro);
    }

    [Fact]
    public async Task Execute_Should_Persist_Sanitized_Snapshot_When_Provider_Supports_It()
    {
        await using var context = NewContext();
        var result = ConsultaCnpjResultado.CriarSucesso("12345678000195", "ConsultaComSnapshot", SituacaoCadastralCnpj.Ativa,
            DateTimeOffset.UtcNow, razaoSocial: "Fornecedor Teste");
        var provider = new FakeProviderComSnapshot(result, snapshot: "{\"razao_social\":\"Fornecedor Teste\"}", descartadoPorTamanho: false);

        await Create(context, provider).ExecuteAsync(new("12345678000195", "BU-A", null, "corr-snapshot-1"));

        var history = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.Equal("{\"razao_social\":\"Fornecedor Teste\"}", history.PayloadBrutoJson);
        Assert.False(history.PayloadBrutoDescartadoPorTamanho);
    }

    [Fact]
    public async Task Execute_Should_Never_Contain_Qsa_Fixture_Text_In_Persisted_Snapshot()
    {
        // Fixture sintetica: mesmo que um Provider hipotetico tentasse repassar um snapshot ja
        // sanitizado que ainda contivesse QSA por erro de implementacao, o teste documenta a
        // expectativa de que o historico persistido nunca deve carregar esse conteudo — a
        // responsabilidade de remover QSA e do sanitizador do Provider (verificado em
        // BrasilApiSnapshotSanitizerTests), este teste apenas garante que o use case repassa o
        // snapshot do Provider sem reintroduzir QSA por conta propria.
        await using var context = NewContext();
        var result = ConsultaCnpjResultado.CriarSucesso("12345678000195", "ConsultaComSnapshot", SituacaoCadastralCnpj.Ativa,
            DateTimeOffset.UtcNow, razaoSocial: "Fornecedor Teste");
        var provider = new FakeProviderComSnapshot(result, snapshot: "{\"razao_social\":\"Fornecedor Teste\"}", descartadoPorTamanho: false);

        await Create(context, provider).ExecuteAsync(new("12345678000195", "BU-A", null, "corr-snapshot-qsa"));

        var history = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.DoesNotContain("qsa", history.PayloadBrutoJson ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Execute_Should_Persist_Null_Snapshot_When_Provider_Does_Not_Support_It()
    {
        // Provider futuro/legado que so implementa ICnpjConsultaProvider (sem snapshot) continua
        // funcionando sem qualquer alteracao de dominio — apenas nao contribui com PayloadBrutoJson.
        await using var context = NewContext();
        var result = ConsultaCnpjResultado.CriarSucesso("12345678000195", "ConsultaTeste", SituacaoCadastralCnpj.Ativa,
            DateTimeOffset.UtcNow, razaoSocial: "Fornecedor Teste");

        await Create(context, new FakeProvider(result)).ExecuteAsync(new("12345678000195", "BU-A", null, "corr-sem-snapshot"));

        var history = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.Null(history.PayloadBrutoJson);
        Assert.False(history.PayloadBrutoDescartadoPorTamanho);
    }

    [Fact]
    public async Task Execute_Should_Persist_Null_Snapshot_On_Timeout_Even_With_Snapshot_Capable_Provider()
    {
        await using var context = NewContext();
        var falha = ConsultaCnpjResultado.CriarFalha("12345678000195", "ConsultaComSnapshot", DateTimeOffset.UtcNow,
            TipoErroConsultaCnpj.Timeout);
        var provider = new FakeProviderComSnapshot(falha, snapshot: null, descartadoPorTamanho: false);

        await Create(context, provider).ExecuteAsync(new("12345678000195", "BU-A", null, "corr-timeout"));

        var history = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.Null(history.PayloadBrutoJson);
        Assert.Equal(TipoErroConsultaCnpjHistorico.Timeout, history.TipoErro);
    }

    [Fact]
    public async Task Execute_Should_Persist_TipoErro_On_Typed_Failure()
    {
        await using var context = NewContext();
        var result = ConsultaCnpjResultado.CriarFalha("12345678000195", "ConsultaTeste", DateTimeOffset.UtcNow,
            TipoErroConsultaCnpj.LimiteDeConsultas);

        await Create(context, new FakeProvider(result)).ExecuteAsync(new("12345678000195", "BU-A", null, "corr-tipo-erro"));

        var history = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.Equal(TipoErroConsultaCnpjHistorico.LimiteDeConsultas, history.TipoErro);
    }

    [Fact]
    public async Task Execute_Should_Persist_Null_TipoErro_On_Success()
    {
        await using var context = NewContext();
        var result = ConsultaCnpjResultado.CriarSucesso("12345678000195", "ConsultaTeste", SituacaoCadastralCnpj.Ativa,
            DateTimeOffset.UtcNow);

        await Create(context, new FakeProvider(result)).ExecuteAsync(new("12345678000195", "BU-A", null, "corr-sem-erro"));

        var history = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.Null(history.TipoErro);
    }

    [Fact]
    public async Task Execute_Should_Persist_Truncation_Flag_When_Provider_Reports_Oversized_Snapshot()
    {
        await using var context = NewContext();
        var result = ConsultaCnpjResultado.CriarSucesso("12345678000195", "ConsultaComSnapshot", SituacaoCadastralCnpj.Ativa,
            DateTimeOffset.UtcNow, razaoSocial: "Fornecedor Teste");
        var provider = new FakeProviderComSnapshot(result, snapshot: null, descartadoPorTamanho: true);

        await Create(context, provider).ExecuteAsync(new("12345678000195", "BU-A", null, "corr-tamanho"));

        var history = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.Null(history.PayloadBrutoJson);
        Assert.True(history.PayloadBrutoDescartadoPorTamanho);
    }

    private sealed class FakeProviderComSnapshot(ConsultaCnpjResultado resultado, string? snapshot, bool descartadoPorTamanho)
        : ICnpjConsultaProviderComSnapshot
    {
        public string FonteConsulta => resultado.FonteConsulta;

        public Task<ConsultaCnpjResultado> ConsultarAsync(string _, CancellationToken cancellationToken = default) =>
            Task.FromResult(resultado);

        public Task<CnpjConsultaProviderResposta> ConsultarComSnapshotAsync(string _, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CnpjConsultaProviderResposta(resultado, snapshot, descartadoPorTamanho));
    }

    private static ConsultarCnpjFornecedorUseCase Create(BlueprintOSDbContext context, ICnpjConsultaProvider provider) =>
        new(provider, new FornecedorCnpjConsultaHistoricoRepository(context), new FakeIdentity(),
            NullLogger<ConsultarCnpjFornecedorUseCase>.Instance);

    private static BlueprintOSDbContext NewContext() => new(new DbContextOptionsBuilder<BlueprintOSDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeIdentity : ICurrentIdentity
    {
        public RequestIdentity GetRequired() => new(Guid.NewGuid(), "Buyer");
    }

    private sealed class FakeProvider(ConsultaCnpjResultado result) : ICnpjConsultaProvider
    {
        public string FonteConsulta => "ConsultaTeste";
        public Task<ConsultaCnpjResultado> ConsultarAsync(string _, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }

    private sealed class CancelledProvider : ICnpjConsultaProvider
    {
        public string FonteConsulta => "ConsultaTeste";
        public Task<ConsultaCnpjResultado> ConsultarAsync(string _, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("CancellationToken was not propagated.");
        }
    }

    private sealed class ThrowingProvider : ICnpjConsultaProvider
    {
        public string FonteConsulta => "ConsultaTeste";
        public Task<ConsultaCnpjResultado> ConsultarAsync(string _, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated unexpected provider failure.");
    }

    private sealed class ThrowingHistoricoRepository : IFornecedorCnpjConsultaHistoricoRepository
    {
        public Task AdicionarAsync(FornecedorCnpjConsultaHistorico consulta, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated database connectivity failure while persisting audit history.");

        public Task<int> ExpurgarPayloadBrutoExpiradoAsync(DateTimeOffset referenciaUtc, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated database connectivity failure while purging retention.");
    }

    private sealed class ThrowingIdentity : ICurrentIdentity
    {
        public RequestIdentity GetRequired() => throw new IdentityUnavailableException("A valid development identity is required.", false);
    }
}
