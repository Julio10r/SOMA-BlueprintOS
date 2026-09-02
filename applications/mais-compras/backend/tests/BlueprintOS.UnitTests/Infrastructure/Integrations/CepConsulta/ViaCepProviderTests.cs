using System.Net;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Infrastructure.Integrations.CepConsulta;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Integrations.CepConsulta;

public sealed class ViaCepProviderTests
{
    [Fact]
    public async Task ConsultarAsync_Should_Map_Success_Response()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """
        {
          "cep": "01311-000",
          "logradouro": "Avenida Paulista",
          "complemento": "de 612 a 1510 - lado par",
          "bairro": "Bela Vista",
          "localidade": "São Paulo",
          "uf": "SP"
        }
        """));

        var result = await provider.ConsultarAsync("01311-000");

        Assert.True(result.Sucesso);
        Assert.Equal("01311000", result.Cep);
        Assert.Equal("Avenida Paulista", result.Logradouro);
        Assert.Equal("Bela Vista", result.Bairro);
        Assert.Equal("São Paulo", result.Cidade);
        Assert.Equal("SP", result.Estado);
        Assert.Equal("ViaCEP", result.FonteConsulta);
        Assert.Null(result.TipoErro);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_NaoEncontrado_When_Provider_Responds_Erro_True()
    {
        // ViaCEP responde 200 OK com {"erro": true} para CEP inexistente — nunca 404 (achado 2,
        // docs/audits/Discovery-Fornecedor-Tela-001016G1.md).
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """{"erro": true}"""));

        var result = await provider.ConsultarAsync("00000000");

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCep.NaoEncontrado, result.TipoErro);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    [InlineData("abcdefgh")]
    public async Task ConsultarAsync_Should_Return_CepInvalido_Without_Calling_Http_When_Not_8_Digits(string cepInvalido)
    {
        var handler = new JsonHandler(HttpStatusCode.OK, """{"erro": true}""");
        var provider = CreateProvider(handler);

        var result = await provider.ConsultarAsync(cepInvalido);

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCep.CepInvalido, result.TipoErro);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_FonteIndisponivel_On_Non_Success_Status()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.InternalServerError, "{}"));

        var result = await provider.ConsultarAsync("01311000");

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCep.FonteIndisponivel, result.TipoErro);
    }

    private static ViaCepProvider CreateProvider(HttpMessageHandler handler, int timeoutSeconds = 10)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/ws/") };
        return new ViaCepProvider(httpClient, Options.Create(new CepConsultaOptions
        {
            BaseUrl = "https://example.test/ws/",
            TimeoutSeconds = timeoutSeconds
        }));
    }

    private sealed class JsonHandler(HttpStatusCode statusCode, string json) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
