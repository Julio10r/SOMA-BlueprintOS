using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

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
        var result = ConsultaCnpjResultado.CriarFalha("12345678000195", "ConsultaTeste", DateTimeOffset.UtcNow, "Documento não encontrado.");

        var response = await Create(context, new FakeProvider(result)).ExecuteAsync(new("12345678000195", "BU-A", null, "corr-cnpj-erro"));

        var history = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.False(response.Sucesso);
        Assert.Equal(StatusConsultaCnpj.Falha, response.StatusConsulta);
        Assert.Equal("Falha", history.Status);
        Assert.Equal("Documento não encontrado.", history.MensagemErro);
        Assert.Equal("corr-cnpj-erro", history.CorrelationId);
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
    public void Result_Should_Require_Document_Source_And_Error_Message()
    {
        Assert.Throws<ArgumentException>(() => ConsultaCnpjResultado.CriarSucesso("", "ConsultaTeste", SituacaoCadastralCnpj.Ativa, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => ConsultaCnpjResultado.CriarSucesso("12345678000195", "", SituacaoCadastralCnpj.Baixada, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => ConsultaCnpjResultado.CriarFalha("12345678000195", "ConsultaTeste", DateTimeOffset.UtcNow, ""));
    }

    private static ConsultarCnpjFornecedorUseCase Create(BlueprintOSDbContext context, ICnpjConsultaProvider provider) =>
        new(provider, new FornecedorCnpjConsultaHistoricoRepository(context), new FakeIdentity());

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
}
