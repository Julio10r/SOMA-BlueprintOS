using System.Net;
using System.Net.Http.Json;
using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Api.Identity;
using BlueprintOS.Api.Suppliers;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Suppliers;

/// <summary>Retest do Gate de Fornecedores (2026-09-01), item 3 — prova de ENFORCEMENT REAL nas rotas
/// verdadeiras de <see cref="FornecedorDiscoveryController"/> (mapeadas via <c>MapFornecedorDiscovery</c>,
/// nunca uma rota de prova avulsa), no mesmo padrão de <c>RbacEnforcementPipelineTests</c> (O1.5): sem
/// sessão → 401; autenticado sem <c>Fornecedor.Criar</c> → 403; autenticado com <c>Fornecedor.Criar</c> →
/// 200. Antes desta correção os três endpoints (`POST /descobrir`, `GET /descobertas`, `GET /descobertas/
/// {id}`) não tinham nenhuma policy — só autenticação — e um usuário sem qualquer permissão conseguia
/// acessá-los.</summary>
public sealed class FornecedorDiscoveryRbacTests : IAsyncDisposable
{
    private const string TokenSessao = "sessao-normal-valida";
    private WebApplication? _app;

    [Fact]
    public async Task Should_Return_401_Without_Any_Session()
    {
        var client = await StartAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/fornecedores/descobertas")).StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_Authenticated_Without_FornecedorCriar()
    {
        var client = await StartAsync();
        ComSessao(client);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/fornecedores/descobertas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/fornecedores/descobrir", new { codigoItem = "ITEM-1" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/fornecedores/descobertas/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Should_Return_200_When_Authenticated_With_FornecedorCriar()
    {
        var client = await StartAsync(PermissaoCatalogo.FornecedorCriar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/fornecedores/descobertas")).StatusCode);
    }

    /// <summary>Ter outra permissão qualquer (ex.: Fornecedor.Editar) não é suficiente — não existe
    /// permissão coringa para os endpoints de descoberta.</summary>
    [Fact]
    public async Task Should_Return_403_When_Authenticated_With_A_Different_Permission()
    {
        var client = await StartAsync(PermissaoCatalogo.FornecedorEditar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/fornecedores/descobertas")).StatusCode);
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
        builder.Services.AddScoped<IDescobrirFornecedoresUseCase, FakeDescobrirUseCase>();
        builder.Services.AddScoped<IListarDescobertasUseCase, FakeListarDescobertasUseCase>();

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
        _app.MapFornecedorDiscovery();

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
                    Guid.NewGuid(), "ana@example.invalid", "Ana", Guid.NewGuid(), Permissoes, EscopoAdministrativo.Negocio))
                : Task.FromResult<IdentidadeAtualDto?>(null);
    }

    private sealed class FakeCurrentIdentity : ICurrentIdentity
    {
        public RequestIdentity GetRequired() => new(Guid.NewGuid(), "Buyer");
    }

    private sealed class FakeDescobrirUseCase : IDescobrirFornecedoresUseCase
    {
        public Task<IReadOnlyList<FornecedorDescobertoDto>> ExecuteAsync(DescobrirFornecedoresDto dto, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FornecedorDescobertoDto>>([]);
    }

    private sealed class FakeListarDescobertasUseCase : IListarDescobertasUseCase
    {
        public Task<IReadOnlyList<FornecedorDescobertoDto>> ExecuteAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FornecedorDescobertoDto>>([]);

        public Task<FornecedorDescobertoDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<FornecedorDescobertoDto?>(null);
    }
}
