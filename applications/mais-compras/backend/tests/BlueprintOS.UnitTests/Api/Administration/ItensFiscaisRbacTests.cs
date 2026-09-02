using System.Net;
using System.Net.Http.Json;
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

/// <summary>B3 — Bloco 3: prova de ENFORCEMENT REAL nas rotas verdadeiras de
/// <see cref="ItensFiscaisController"/> (mapeadas via <c>MapItensFiscais</c>), mesmo padrão de
/// <c>ContasContabeisRbacTests</c>/<c>FornecedorDiscoveryRbacTests</c>. Diferente dos cadastros de apoio
/// (uma única permissão "Gerenciar"): Item Fiscal separa RBAC por ação — <c>ItemFiscal.Visualizar</c>
/// (GET) NÃO autoriza <c>POST</c>/<c>PUT</c>/<c>PATCH</c>, e vice-versa (Discovery homologado §7).</summary>
public sealed class ItensFiscaisRbacTests : IAsyncDisposable
{
    private const string TokenSessao = "sessao-normal-valida";
    private static readonly Guid UnidadeNegocioId = Guid.NewGuid();
    private WebApplication? _app;

    [Fact]
    public async Task Should_Return_401_Without_Any_Session()
    {
        var client = await StartAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/administracao/itens-fiscais")).StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_On_Get_Without_Visualizar()
    {
        var client = await StartAsync();
        ComSessao(client);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/administracao/itens-fiscais")).StatusCode);
    }

    [Fact]
    public async Task Should_Return_200_On_Get_With_Visualizar()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalVisualizar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/administracao/itens-fiscais")).StatusCode);
    }

    [Fact]
    public async Task Visualizar_Should_Not_Authorize_Criar_Editar_Or_Inativar()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalVisualizar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/administracao/itens-fiscais", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/administracao/itens-fiscais/{Guid.NewGuid()}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/administracao/itens-fiscais/{Guid.NewGuid()}/status", new { ativo = true })).StatusCode);
    }

    [Fact]
    public async Task Should_Return_200_On_Post_With_Criar()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalCriar);
        ComSessao(client);

        ComCsrf(client);

        var response = await client.PostAsJsonAsync("/api/administracao/itens-fiscais", new
        {
            codigo = "001", descricao = "Notebook", unidadeMedidaCodigoErp = "UN", contaContabilCodigoErp = "1.1.01"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Criar_Should_Not_Authorize_Editar_Or_Inativar()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalCriar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/administracao/itens-fiscais/{Guid.NewGuid()}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/administracao/itens-fiscais/{Guid.NewGuid()}/status", new { ativo = true })).StatusCode);
    }

    [Fact]
    public async Task Should_Return_200_On_Put_With_Editar()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalEditar);
        ComSessao(client);

        ComCsrf(client);

        var response = await client.PutAsJsonAsync($"/api/administracao/itens-fiscais/{Guid.NewGuid()}", new
        {
            descricao = "Notebook", unidadeMedidaCodigoErp = "UN", contaContabilCodigoErp = "1.1.01"
        });

        // Fake use case sempre retorna "não encontrado" (404) — o que importa é que passou da barreira 403.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_200_On_Patch_Status_With_Inativar()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalInativar);
        ComSessao(client);

        ComCsrf(client);

        var response = await client.PatchAsJsonAsync($"/api/administracao/itens-fiscais/{Guid.NewGuid()}/status", new { ativo = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static void ComSessao(HttpClient client) =>
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={TokenSessao}");

    /// <summary>Necessário para POST/PUT/PATCH passarem de <see cref="CsrfHeaderFilter"/> — defesa em
    /// profundidade real do backend, não simulada neste teste (mesmo cabeçalho que
    /// `contasContabeisApi.ts`/`unidadesMedidaApi.ts` já enviam em produção).</summary>
    private static void ComCsrf(HttpClient client) =>
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");

    private async Task<HttpClient> StartAsync(params string[] permissoesEfetivas)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton<IObterIdentidadeAtualUseCase>(new FakeIdentidadeComPermissoes { Permissoes = permissoesEfetivas });
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ICurrentIdentity>(_ => new FakeCurrentIdentity());
        builder.Services.AddScoped<IListarItensFiscaisUseCase, FakeListarItensFiscaisUseCase>();
        builder.Services.AddScoped<IObterItemFiscalUseCase, FakeObterItemFiscalUseCase>();
        builder.Services.AddScoped<ICriarItemFiscalUseCase, FakeCriarItemFiscalUseCase>();
        builder.Services.AddScoped<IAtualizarItemFiscalUseCase, FakeAtualizarItemFiscalUseCase>();
        builder.Services.AddScoped<IAlterarStatusItemFiscalUseCase, FakeAlterarStatusItemFiscalUseCase>();

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
        _app.MapItensFiscais();

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

    private static readonly ItemFiscalDto FakeItem = new(
        Guid.NewGuid(), "001", "Notebook", "UN", "Unidade", "1.1.01", "Conta", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private sealed class FakeListarItensFiscaisUseCase : IListarItensFiscaisUseCase
    {
        public Task<IReadOnlyList<ItemFiscalDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ItemFiscalDto>>([FakeItem]);
    }

    private sealed class FakeObterItemFiscalUseCase : IObterItemFiscalUseCase
    {
        public Task<ItemFiscalDto?> ExecuteAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult<ItemFiscalDto?>(null);
    }

    private sealed class FakeCriarItemFiscalUseCase : ICriarItemFiscalUseCase
    {
        public Task<RbacResultado<ItemFiscalDto>> ExecuteAsync(ItemFiscalCriarInput input, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(RbacResultado<ItemFiscalDto>.Ok(FakeItem));
    }

    private sealed class FakeAtualizarItemFiscalUseCase : IAtualizarItemFiscalUseCase
    {
        public Task<RbacResultado<ItemFiscalDto>> ExecuteAsync(Guid id, ItemFiscalAtualizarInput input, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(RbacResultado<ItemFiscalDto>.Erro(RbacFalha.ItemFiscalNaoEncontrado, "não usado neste teste"));
    }

    private sealed class FakeAlterarStatusItemFiscalUseCase : IAlterarStatusItemFiscalUseCase
    {
        public Task<RbacResultado<ItemFiscalDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(RbacResultado<ItemFiscalDto>.Erro(RbacFalha.ItemFiscalNaoEncontrado, "não usado neste teste"));
    }
}
