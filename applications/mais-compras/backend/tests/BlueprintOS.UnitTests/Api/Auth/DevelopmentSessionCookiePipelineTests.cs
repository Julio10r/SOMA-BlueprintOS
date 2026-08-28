using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Auth;

/// <summary>Regressão do login normal pós-O1.4.3: em Development, o esquema de autenticação default era
/// exclusivamente <see cref="DevelopmentHeaderAuthenticationHandler"/>, que nunca examina o cookie
/// <c>mc_sid</c>. Uma sessão real criada por <c>POST /auth/otp/verify</c> (que emite o cookie
/// independentemente do ambiente) nunca autenticava em <c>GET /auth/me</c> localmente — o usuário validava
/// o OTP e caía de volta na tela de login. Este teste sobe a MESMA composição de <c>Program.cs</c>
/// (PolicyScheme por cookie) e prova as duas pontas: sessão de cookie autentica, e o comportamento anterior
/// via header (usado por outros testes/fluxos de Development) continua intacto.</summary>
public sealed class DevelopmentSessionCookiePipelineTests : IAsyncDisposable
{
    private WebApplication? _app;
    private const string ValidToken = "token-valido";

    [Fact]
    public async Task Protected_Endpoint_Should_Return_401_Without_Cookie_Or_Header()
    {
        var client = await StartAppAndCreateClientAsync();

        var response = await client.GetAsync("/probe");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Should_Authenticate_With_Development_Header_When_No_Session_Cookie()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("X-Development-User-Id", Guid.NewGuid().ToString());

        var response = await client.GetAsync("/probe");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Should_Authenticate_With_Real_Session_Cookie_Even_In_Development()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={ValidToken}");

        var response = await client.GetAsync("/probe");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Should_Return_401_With_Invalid_Session_Cookie()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}=token-invalido");

        var response = await client.GetAsync("/probe");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> StartAppAndCreateClientAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton<IObterIdentidadeAtualUseCase, FakeObterIdentidadeAtualUseCase>();

        const string DevelopmentDefaultScheme = "DevelopmentOrSessionCookie";
        builder.Services.AddAuthentication(DevelopmentDefaultScheme)
            .AddPolicyScheme(DevelopmentDefaultScheme, DevelopmentDefaultScheme, policyOptions =>
            {
                policyOptions.ForwardDefaultSelector = context =>
                    context.Request.Cookies.ContainsKey(AuthCookie.Name)
                        ? SessionCookieAuthenticationDefaults.Scheme
                        : DevelopmentHeaderAuthenticationDefaults.Scheme;
            })
            .AddScheme<AuthenticationSchemeOptions, DevelopmentHeaderAuthenticationHandler>(
                DevelopmentHeaderAuthenticationDefaults.Scheme, null)
            .AddScheme<AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>(
                SessionCookieAuthenticationDefaults.Scheme, null);
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

    private sealed class FakeObterIdentidadeAtualUseCase : IObterIdentidadeAtualUseCase
    {
        public Task<IdentidadeAtualDto?> ExecuteAsync(string sessionRawToken, CancellationToken ct)
        {
            IdentidadeAtualDto? resultado = sessionRawToken == ValidToken
                ? new IdentidadeAtualDto(Guid.NewGuid(), "julio.cesar@somagrupo.com.br", "Julio Cesar", Guid.NewGuid(), [], EscopoAdministrativo.Negocio)
                : null;
            return Task.FromResult(resultado);
        }
    }
}
