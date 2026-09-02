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

/// <summary>B3 — Bloco 4: prova de ENFORCEMENT REAL nas rotas verdadeiras de
/// <see cref="ItemFiscalReferenciasFornecedorController"/> (mapeadas via
/// <c>MapItemFiscalReferenciasFornecedor</c>), mesmo padrão de <c>ItensFiscaisRbacTests</c>. Decisão do
/// Bloco 4 (evitar aumentar o catálogo RBAC sem necessidade real): as referências REAPROVEITAM as
/// permissões já existentes do Item Fiscal — <c>ItemFiscal.Visualizar</c> autoriza GET,
/// <c>ItemFiscal.Editar</c> autoriza POST/PUT/DELETE. Nenhuma permissão nova é criada por este bloco.</summary>
public sealed class ItemFiscalReferenciasFornecedorRbacTests : IAsyncDisposable
{
    private const string TokenSessao = "sessao-normal-valida";
    private static readonly Guid UnidadeNegocioId = Guid.NewGuid();
    private static readonly Guid ItemFiscalId = Guid.NewGuid();
    private WebApplication? _app;

    [Fact]
    public async Task Should_Return_401_Without_Any_Session()
    {
        var client = await StartAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Rota())).StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_On_Get_Without_Visualizar()
    {
        var client = await StartAsync();
        ComSessao(client);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Rota())).StatusCode);
    }

    [Fact]
    public async Task Should_Return_200_On_Get_With_Visualizar()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalVisualizar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Rota())).StatusCode);
    }

    [Fact]
    public async Task Visualizar_Should_Not_Authorize_Incluir_Atualizar_Or_Remover()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalVisualizar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Rota(), new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(Rota(Guid.NewGuid()), new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync(Rota(Guid.NewGuid()))).StatusCode);
    }

    [Fact]
    public async Task Should_Return_201_On_Post_With_Editar()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalEditar);
        ComSessao(client);
        ComCsrf(client);

        var response = await client.PostAsJsonAsync(Rota(), new { fornecedorId = Guid.NewGuid(), codigoItemFornecedor = "hduahd78" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_200_On_Put_With_Editar()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalEditar);
        ComSessao(client);
        ComCsrf(client);

        var response = await client.PutAsJsonAsync(Rota(Guid.NewGuid()), new { codigoItemFornecedor = "novo-codigo" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_204_On_Delete_With_Editar()
    {
        var client = await StartAsync(PermissaoCatalogo.ItemFiscalEditar);
        ComSessao(client);
        ComCsrf(client);

        var response = await client.DeleteAsync(Rota(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static string Rota(Guid? referenciaId = null) =>
        referenciaId is null
            ? $"/api/administracao/itens-fiscais/{ItemFiscalId}/referencias-fornecedor"
            : $"/api/administracao/itens-fiscais/{ItemFiscalId}/referencias-fornecedor/{referenciaId}";

    private static void ComSessao(HttpClient client) =>
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={TokenSessao}");

    private static void ComCsrf(HttpClient client) =>
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");

    private async Task<HttpClient> StartAsync(params string[] permissoesEfetivas)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton<IObterIdentidadeAtualUseCase>(new FakeIdentidadeComPermissoes { Permissoes = permissoesEfetivas });
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ICurrentIdentity>(_ => new FakeCurrentIdentity());
        builder.Services.AddScoped<IListarReferenciasFornecedorUseCase, FakeListarReferenciasFornecedorUseCase>();
        builder.Services.AddScoped<IIncluirReferenciaFornecedorUseCase, FakeIncluirReferenciaFornecedorUseCase>();
        builder.Services.AddScoped<IAtualizarReferenciaFornecedorUseCase, FakeAtualizarReferenciaFornecedorUseCase>();
        builder.Services.AddScoped<IRemoverReferenciaFornecedorUseCase, FakeRemoverReferenciaFornecedorUseCase>();

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
        _app.MapItemFiscalReferenciasFornecedor();

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

    private static readonly ItemFiscalReferenciaFornecedorDto FakeReferencia = new(
        Guid.NewGuid(), ItemFiscalId, Guid.NewGuid(), "Amazon", "hduahd78", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private sealed class FakeListarReferenciasFornecedorUseCase : IListarReferenciasFornecedorUseCase
    {
        public Task<RbacResultado<IReadOnlyList<ItemFiscalReferenciaFornecedorDto>>> ExecuteAsync(Guid itemFiscalId, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(RbacResultado<IReadOnlyList<ItemFiscalReferenciaFornecedorDto>>.Ok([FakeReferencia]));
    }

    private sealed class FakeIncluirReferenciaFornecedorUseCase : IIncluirReferenciaFornecedorUseCase
    {
        public Task<RbacResultado<ItemFiscalReferenciaFornecedorDto>> ExecuteAsync(Guid itemFiscalId, ItemFiscalReferenciaFornecedorCriarInput input, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(RbacResultado<ItemFiscalReferenciaFornecedorDto>.Ok(FakeReferencia));
    }

    private sealed class FakeAtualizarReferenciaFornecedorUseCase : IAtualizarReferenciaFornecedorUseCase
    {
        public Task<RbacResultado<ItemFiscalReferenciaFornecedorDto>> ExecuteAsync(Guid itemFiscalId, Guid referenciaId, ItemFiscalReferenciaFornecedorAtualizarInput input, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(RbacResultado<ItemFiscalReferenciaFornecedorDto>.Ok(FakeReferencia));
    }

    private sealed class FakeRemoverReferenciaFornecedorUseCase : IRemoverReferenciaFornecedorUseCase
    {
        public Task<RbacResultado<bool>> ExecuteAsync(Guid itemFiscalId, Guid referenciaId, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(RbacResultado<bool>.Ok(true));
    }
}
