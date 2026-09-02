using System.Net;
using BlueprintOS.Api.Administration;
using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Administration;

/// <summary>B3 — Bloco 1: prova de ENFORCEMENT REAL nas rotas verdadeiras de
/// <see cref="ContasContabeisController"/> (mapeadas via <c>MapContasContabeis</c>), mesmo padrão de
/// <c>FornecedorDiscoveryRbacTests</c>/<c>RbacEnforcementPipelineTests</c>: sem sessão → 401; autenticado
/// sem <c>ContaContabil.Gerenciar</c> → 403; autenticado com <c>ContaContabil.Gerenciar</c> e Unidade de
/// Negócio resolvida → 200.</summary>
public sealed class ContasContabeisRbacTests : IAsyncDisposable
{
    private const string TokenSessao = "sessao-normal-valida";
    private static readonly Guid UnidadeNegocioId = Guid.NewGuid();
    private WebApplication? _app;

    [Fact]
    public async Task Should_Return_401_Without_Any_Session()
    {
        var client = await StartAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/administracao/contas-contabeis")).StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_Authenticated_Without_ContaContabilGerenciar()
    {
        var client = await StartAsync();
        ComSessao(client);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/administracao/contas-contabeis")).StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_Authenticated_With_A_Different_Permission()
    {
        var client = await StartAsync(PermissaoCatalogo.CentroCustoGerenciar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/administracao/contas-contabeis")).StatusCode);
    }

    [Fact]
    public async Task Should_Return_200_When_Authenticated_With_ContaContabilGerenciar()
    {
        var client = await StartAsync(PermissaoCatalogo.ContaContabilGerenciar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/administracao/contas-contabeis")).StatusCode);
    }

    private static void ComSessao(HttpClient client) =>
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={TokenSessao}");

    private async Task<HttpClient> StartAsync(params string[] permissoesEfetivas)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton<IObterIdentidadeAtualUseCase>(new FakeIdentidadeComPermissoes { Permissoes = permissoesEfetivas });
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ICurrentIdentity>(_ => new FakeCurrentIdentity());
        builder.Services.AddScoped<IListarContasContabeisUseCase, FakeListarContasContabeisUseCase>();
        builder.Services.AddScoped<IAtualizarMetadadoContaContabilUseCase, FakeAtualizarMetadadoContaContabilUseCase>();

        builder.Services.AddAuthentication(DevelopmentHeaderAuthenticationDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentHeaderAuthenticationHandler>(DevelopmentHeaderAuthenticationDefaults.Scheme, null)
            .AddScheme<AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>(SessionCookieAuthenticationDefaults.Scheme, null);

        builder.Services.AddScoped<IAuthorizationHandler, PermissaoAuthorizationHandler>();
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddRbacPolicies();
        });

        _app = builder.Build();
        _app.Use(async (context, next) =>
        {
            if (context.Request.Cookies.ContainsKey(AuthCookie.Name))
            {
                var resultado = await context.AuthenticateAsync(SessionCookieAuthenticationDefaults.Scheme);
                if (!resultado.Succeeded) { context.Response.StatusCode = (int)HttpStatusCode.Unauthorized; return; }
                context.User = resultado.Principal!;
            }
            await next();
        });
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapContasContabeis();

        await _app.StartAsync();
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null) await _app.DisposeAsync();
    }

    private sealed class FakeIdentidadeComPermissoes : IObterIdentidadeAtualUseCase
    {
        public IReadOnlyList<string> Permissoes { get; set; } = [];

        public Task<IdentidadeAtualDto?> ExecuteAsync(string sessionRawToken, CancellationToken ct) =>
            sessionRawToken == TokenSessao
                ? Task.FromResult<IdentidadeAtualDto?>(new IdentidadeAtualDto(
                    Guid.NewGuid(), "ana@example.invalid", "Ana", UnidadeNegocioId, Permissoes, EscopoAdministrativo.Negocio))
                : Task.FromResult<IdentidadeAtualDto?>(null);
    }

    private sealed class FakeCurrentIdentity : ICurrentIdentity
    {
        public RequestIdentity GetRequired() => new(Guid.NewGuid(), "Buyer", UnidadeNegocioId);
    }

    private sealed class FakeListarContasContabeisUseCase : IListarContasContabeisUseCase
    {
        public Task<IReadOnlyList<ContaContabilDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ContaContabilDto>>([]);
    }

    private sealed class FakeAtualizarMetadadoContaContabilUseCase : IAtualizarMetadadoContaContabilUseCase
    {
        public Task<ErpMetadadoResultado<ContaContabilDto>> ExecuteAsync(string codigoErp, ContaContabilMetadadoInput input, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(ErpMetadadoResultado<ContaContabilDto>.Erro(ErpMetadadoFalha.CodigoErpNaoEncontrado, "não usado neste teste"));
    }
}
