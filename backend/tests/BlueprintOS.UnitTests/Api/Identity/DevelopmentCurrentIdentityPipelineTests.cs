using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Identity;

/// <summary>Gap identificado após a correção do login normal (Work Order de correção da identidade de
/// negócio em Development): <c>/auth/me</c> já autenticava a sessão OTP real, mas endpoints de negócio que
/// dependem de <see cref="ICurrentIdentity"/> continuavam exigindo <c>X-Development-User-Id</c> porque
/// <c>DevelopmentRequestIdentity</c> reparsava o header por conta própria, em vez de ler a identidade já
/// resolvida pelos authentication handlers. A correção substitui essa implementação, em Development, por
/// <see cref="SessionCurrentIdentity"/> — a MESMA classe usada fora de Development — que lê exclusivamente
/// <c>HttpContext.User</c>. A prioridade sessão-real-sobre-header não é decidida aqui: é uma consequência
/// direta de qual authentication handler roda (ver <c>ForwardDefaultSelector</c> em Program.cs) — com
/// cookie presente, somente <see cref="SessionCookieAuthenticationHandler"/> autentica; o header é
/// completamente ignorado nesse caso, mesmo se enviado. Este teste sobe a mesma composição de
/// autenticação/DI de <c>Program.cs</c> e prova essa cadeia ponta a ponta contra um endpoint de negócio
/// real (dependente de <see cref="ICurrentIdentity"/>), não apenas contra a autenticação HTTP.</summary>
public sealed class DevelopmentCurrentIdentityPipelineTests : IAsyncDisposable
{
    private const string ValidToken = "token-sessao-valido";
    private static readonly Guid SessionUserId = Guid.NewGuid();
    private WebApplication? _app;

    [Fact]
    public async Task Business_Endpoint_Should_Resolve_Real_Identity_From_Valid_Otp_Session()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={ValidToken}");

        var response = await client.GetAsync("/negocio/identidade");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SessionUserId.ToString(), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Business_Endpoint_Should_Resolve_Development_Identity_From_Header_When_No_Session()
    {
        var client = await StartAppAndCreateClientAsync();
        var headerUserId = Guid.NewGuid();
        client.DefaultRequestHeaders.Add("X-Development-User-Id", headerUserId.ToString());

        var response = await client.GetAsync("/negocio/identidade");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(headerUserId.ToString(), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Business_Endpoint_Should_Prefer_Real_Session_Over_Conflicting_Development_Header()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={ValidToken}");
        client.DefaultRequestHeaders.Add("X-Development-User-Id", Guid.NewGuid().ToString());

        var response = await client.GetAsync("/negocio/identidade");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        // A sessão real prevalece — o header é ignorado mesmo estando presente e sintaticamente válido.
        Assert.Equal(SessionUserId.ToString(), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Business_Endpoint_Should_Return_401_Without_Cookie_Or_Header()
    {
        var client = await StartAppAndCreateClientAsync();

        var response = await client.GetAsync("/negocio/identidade");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Business_Endpoint_Should_Return_401_With_Invalid_Session_Cookie_And_No_Header()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}=token-invalido");

        var response = await client.GetAsync("/negocio/identidade");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Comportamento explicitamente decidido (fail-closed): um cookie mc_sid inválido/expirado
    /// NUNCA cai silenciosamente para a identidade de Development via header, mesmo que o header seja
    /// sintaticamente válido. O ForwardDefaultSelector escolhe o esquema pela PRESENÇA do cookie, não pela
    /// validade — cookie presente sempre força a validação real de sessão; se ela falhar, a requisição
    /// falha, sem segunda tentativa via header. Alternativa (permitir fallback ao header quando o cookie é
    /// inválido) foi descartada: abriria uma forma de uma sessão expirada/revogada continuar autenticando
    /// silenciosamente em Development, mascarando exatamente o tipo de bug que este Work Order corrigiu.</summary>
    [Fact]
    public async Task Business_Endpoint_Should_Return_401_With_Invalid_Session_Cookie_Even_With_Valid_Header()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}=token-invalido");
        client.DefaultRequestHeaders.Add("X-Development-User-Id", Guid.NewGuid().ToString());

        var response = await client.GetAsync("/negocio/identidade");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> StartAppAndCreateClientAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IObterIdentidadeAtualUseCase, FakeObterIdentidadeAtualUseCase>();
        builder.Services.AddScoped<ICurrentIdentity, SessionCurrentIdentity>();

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
        _app.MapGet("/negocio/identidade", (ICurrentIdentity identity) => identity.GetRequired().UserId.ToString());

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
                ? new IdentidadeAtualDto(SessionUserId, "julio.cesar@somagrupo.com.br", "Julio Cesar", Guid.NewGuid())
                : null;
            return Task.FromResult(resultado);
        }
    }
}
