using BlueprintOS.Api.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Identity;

/// <summary>Teste de pipeline HTTP real (O1.4.2.2, item 7) — sobe um host Kestrel real com exatamente a
/// mesma composição de autenticação/autorização de <c>Program.cs</c> (sem <c>AddInfrastructure</c>, que
/// exigiria uma connection string real) e faz requisições HTTP reais via <see cref="HttpClient"/> contra
/// um endpoint protegido apenas pela <c>AuthorizationOptions.FallbackPolicy</c> global.
///
/// Limitação documentada: uma requisição de <see cref="HttpClient"/> para <c>127.0.0.1</c> é sempre
/// loopback por construção — não é possível, sem infraestrutura de spoofing de rede, simular aqui uma
/// requisição HTTP real com <c>RemoteIpAddress</c> externo. O caminho negativo (origem não-loopback) já
/// está coberto de forma determinística e direta em
/// <see cref="DevelopmentHeaderAuthenticationHandlerTests.Should_Not_Authenticate_Development_NonLoopback_With_Valid_Header"/>,
/// que testa exatamente a mesma lógica do handler.</summary>
public sealed class DevelopmentHeaderPipelineTests : IAsyncDisposable
{
    private WebApplication? _app;

    [Fact]
    public async Task Protected_Endpoint_Should_Return_401_Without_Header()
    {
        var client = await StartAppAndCreateClientAsync();

        var response = await client.GetAsync("/probe");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Should_Authenticate_Loopback_Request_With_Valid_Header()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("X-Development-User-Id", Guid.NewGuid().ToString());

        var response = await client.GetAsync("/probe");

        // Todo HttpClient conectando a 127.0.0.1 é, por construção, uma requisição loopback real — o
        // handler avalia Connection.RemoteIpAddress do socket TCP real, não um valor simulado.
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Should_Return_401_With_Malformed_Header()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("X-Development-User-Id", "nao-e-um-guid");

        var response = await client.GetAsync("/probe");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> StartAppAndCreateClientAsync()
    {
        // EnvironmentName precisa ser passado via WebApplicationOptions no CreateBuilder — mutar
        // builder.Environment.EnvironmentName depois de CreateBuilder() não é honrado de forma
        // confiável pelo host (o valor observado por IHostEnvironment em tempo de execução permanece o
        // do processo de teste, não o mutado).
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddAuthentication(DevelopmentHeaderAuthenticationDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentHeaderAuthenticationHandler>(
                DevelopmentHeaderAuthenticationDefaults.Scheme, null);
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        });

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGet("/probe", () => "ok");

        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!
            .Addresses.First();

        return new HttpClient { BaseAddress = new Uri(address) };
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null) await _app.DisposeAsync();
    }
}
