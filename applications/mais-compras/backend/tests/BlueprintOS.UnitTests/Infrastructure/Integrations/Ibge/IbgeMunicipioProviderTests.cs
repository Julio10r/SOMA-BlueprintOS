using System.Net;
using BlueprintOS.Infrastructure.Integrations.Ibge;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Integrations.Ibge;

public sealed class IbgeMunicipioProviderTests
{
    [Fact]
    public async Task ListarPorUfAsync_Should_Return_Sorted_Municipio_Names()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """
        [{"id":1,"nome":"São Paulo"},{"id":2,"nome":"Adamantina"},{"id":3,"nome":"Campinas"}]
        """));

        var municipios = await provider.ListarPorUfAsync("SP");

        Assert.Equal(["Adamantina", "Campinas", "São Paulo"], municipios);
    }

    [Fact]
    public async Task ListarPorUfAsync_Should_Return_Empty_On_Non_Success_Status()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.NotFound, "[]"));

        var municipios = await provider.ListarPorUfAsync("XX");

        Assert.Empty(municipios);
    }

    [Fact]
    public async Task ListarPorUfAsync_Should_Return_Empty_On_Malformed_Json()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, "not json"));

        var municipios = await provider.ListarPorUfAsync("SP");

        Assert.Empty(municipios);
    }

    private static IbgeMunicipioProvider CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/estados/") };
        return new IbgeMunicipioProvider(httpClient, Options.Create(new IbgeMunicipioOptions
        {
            BaseUrl = "https://example.test/estados/",
            TimeoutSeconds = 10
        }));
    }

    private sealed class JsonHandler(HttpStatusCode statusCode, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
