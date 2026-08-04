using System.Net;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Infrastructure.Integrations.CnpjConsulta;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Integrations.CnpjConsulta;

public sealed class BrasilApiCnpjProviderTests
{
    [Fact]
    public async Task ConsultarAsync_Should_Map_Success_Response()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """
        {
          "cnpj": "12345678000195",
          "razao_social": "Fornecedor Brasil API Ltda",
          "nome_fantasia": "Fornecedor API",
          "descricao_situacao_cadastral": "ATIVA",
          "data_situacao_cadastral": "2024-01-10",
          "cep": "01311000",
          "logradouro": "Avenida Paulista",
          "numero": "1000",
          "complemento": "10 andar",
          "bairro": "Bela Vista",
          "municipio": "São Paulo",
          "uf": "SP",
          "email": "contato@example.com",
          "ddd_telefone_1": "1133334444",
          "natureza_juridica": "Sociedade Empresária Limitada",
          "porte": "DEMAIS"
        }
        """));

        var result = await provider.ConsultarAsync("12.345.678/0001-95");

        Assert.True(result.Sucesso);
        Assert.Equal("12345678000195", result.Cnpj_Cpf);
        Assert.Equal("Fornecedor Brasil API Ltda", result.RazaoSocial);
        Assert.Equal("Fornecedor API", result.NomeFantasia);
        Assert.Equal("Juridica", result.TipoPessoa);
        Assert.Equal(SituacaoCadastralCnpj.Ativa, result.SituacaoCadastral);
        Assert.Equal(new DateOnly(2024, 1, 10), result.DataSituacaoCadastral);
        Assert.Equal("01311000", result.Cep);
        Assert.Equal("Avenida Paulista", result.Logradouro);
        Assert.Equal("1000", result.Numero);
        Assert.Equal("10 andar", result.Complemento);
        Assert.Equal("Bela Vista", result.Bairro);
        Assert.Equal("São Paulo", result.Cidade);
        Assert.Equal("SP", result.Estado);
        Assert.Equal("contato@example.com", result.Email);
        Assert.Equal("1133334444", result.Telefone);
        Assert.Equal("BrasilAPI", result.FonteConsulta);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_Timeout_Failure()
    {
        var provider = CreateProvider(new DelayedHandler(TimeSpan.FromSeconds(5)), timeoutSeconds: 1);

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Equal(StatusConsultaCnpj.Falha, result.StatusConsulta);
        Assert.Equal("Timeout ao consultar a fonte externa.", result.MensagemErro);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_External_Error_Failure()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.ServiceUnavailable, "{}"));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Equal("Fonte externa indisponível.", result.MensagemErro);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_NotFound_Failure()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.NotFound, "{}"));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Equal("CNPJ não encontrado.", result.MensagemErro);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_Invalid_Format_Failure_Without_Calling_Provider()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, "{}"));

        var result = await provider.ConsultarAsync("123");

        Assert.False(result.Sucesso);
        Assert.Equal("CNPJ inválido para consulta.", result.MensagemErro);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Respect_CancellationToken()
    {
        var provider = CreateProvider(new DelayedHandler(TimeSpan.FromSeconds(5)), timeoutSeconds: 10);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.ConsultarAsync("12345678000195", cancellation.Token));
    }

    [Fact]
    public async Task ConsultarAsync_Should_Map_Baixada_As_Success_With_Warning_State()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """
        {
          "cnpj": "12345678000195",
          "razao_social": "Fornecedor Baixado",
          "descricao_situacao_cadastral": "BAIXADA"
        }
        """));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.True(result.Sucesso);
        Assert.Equal(SituacaoCadastralCnpj.Baixada, result.SituacaoCadastral);
        Assert.Null(result.MensagemErro);
    }

    private static BrasilApiCnpjProvider CreateProvider(HttpMessageHandler handler, int timeoutSeconds = 10)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/api/cnpj/v1/") };
        return new BrasilApiCnpjProvider(httpClient, Options.Create(new CnpjConsultaOptions
        {
            BaseUrl = "https://example.test/api/cnpj/v1/",
            TimeoutSeconds = timeoutSeconds
        }));
    }

    private sealed class JsonHandler(HttpStatusCode statusCode, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json)
            };
            return Task.FromResult(response);
        }
    }

    private sealed class DelayedHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
