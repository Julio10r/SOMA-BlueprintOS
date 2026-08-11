using System.Net;
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
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Api.Authorization;

/// <summary>O1.5 — prova de ENFORCEMENT REAL, não de schema. Sobe um host Kestrel real com a mesma
/// composição de autenticação/autorização de <c>Program.cs</c> (FallbackPolicy global + policies de RBAC
/// geradas por <see cref="RbacPolicies"/> + <see cref="PermissaoAuthorizationHandler"/>), exatamente no
/// mesmo padrão de <c>BootstrapAuthorizationPipelineTests</c> (O1.4.3.1).
///
/// O que cada cenário comprova, em códigos HTTP reais devolvidos pelo servidor:
/// - sem sessão → <b>401</b> (autenticação ausente, não autorização);
/// - sessão válida sem a permissão → <b>403</b> (autenticado, porém não autorizado);
/// - sessão válida com a permissão → <b>200</b>;
/// - composição de múltiplos Perfis: a permissão vinda de qualquer Perfil ativo autoriza;
/// - Perfil inativo não autoriza;
/// - o esquema exclusivo de Development (header) não carrega permissões e por isso não autoriza.
///
/// As permissões chegam ao pipeline pelo mesmo caminho de produção: <c>IObterIdentidadeAtualUseCase</c>
/// (aqui um fake que representa o banco) → <c>SessionCookieAuthenticationHandler</c> → claims em
/// <c>HttpContext.User</c> → policy. Nenhum atalho de teste injeta claims diretamente.</summary>
public sealed class RbacEnforcementPipelineTests : IAsyncDisposable
{
    private const string TokenSessao = "sessao-normal-valida";
    private WebApplication? _app;
    private FakeIdentidadeComPermissoes? _identidade;

    [Fact]
    public async Task Should_Return_401_Without_Any_Session()
    {
        var client = await StartAsync();

        var response = await client.GetAsync("/probe-perfis");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_Authenticated_Without_The_Required_Permission()
    {
        var client = await StartAsync(PermissaoCatalogo.PedidoCriar);
        ComSessao(client);

        var response = await client.GetAsync("/probe-perfis");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_403_When_Authenticated_With_No_Permission_At_All()
    {
        var client = await StartAsync();
        ComSessao(client);

        var response = await client.GetAsync("/probe-perfis");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_200_When_Authenticated_With_The_Required_Permission()
    {
        var client = await StartAsync(PermissaoCatalogo.PerfilGerenciar);
        ComSessao(client);

        var response = await client.GetAsync("/probe-perfis");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Composição de múltiplos Perfis: o usuário abaixo representa alguém com um Perfil operacional
    /// e outro administrativo. A união autoriza os dois endpoints, cada um exigindo uma permissão distinta.</summary>
    [Fact]
    public async Task Should_Authorize_Both_Endpoints_When_Permissions_Come_From_Different_Perfis()
    {
        var client = await StartAsync(PermissaoCatalogo.PedidoCriar, PermissaoCatalogo.PerfilGerenciar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/probe-perfis")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/probe-pedidos")).StatusCode);
    }

    /// <summary>Uma permissão não autoriza um endpoint que exige outra — não existe permissão "coringa".</summary>
    [Fact]
    public async Task One_Permission_Should_Not_Authorize_An_Endpoint_Requiring_Another()
    {
        var client = await StartAsync(PermissaoCatalogo.PerfilGerenciar);
        ComSessao(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/probe-perfis")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/probe-pedidos")).StatusCode);
    }

    /// <summary>Revogação sem esperar expiração de sessão: as permissões são reresolvidas a cada requisição,
    /// então inativar o Perfil (aqui, remover a permissão da resolução) passa a negar imediatamente, com a
    /// MESMA sessão e o MESMO cookie.</summary>
    [Fact]
    public async Task Revoking_The_Permission_Should_Take_Effect_On_The_Next_Request_Of_The_Same_Session()
    {
        var client = await StartAsync(PermissaoCatalogo.PerfilGerenciar);
        ComSessao(client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/probe-perfis")).StatusCode);

        _identidade!.Permissoes = [];

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/probe-perfis")).StatusCode);
    }

    /// <summary>Um cookie de sessão inválido é 401 (não autenticado), nunca 403 — e nunca cai para o
    /// esquema de header de Development.</summary>
    [Fact]
    public async Task Invalid_Session_Cookie_Should_Return_401()
    {
        var client = await StartAsync(PermissaoCatalogo.PerfilGerenciar);
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}=token-invalido");

        var response = await client.GetAsync("/probe-perfis");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Bypass por esquema alternativo: o header exclusivo de Development autentica, mas não carrega
    /// nenhuma claim de permissão — logo não abre endpoint protegido por RBAC. Falha fechado.</summary>
    [Fact]
    public async Task Development_Header_Scheme_Should_Authenticate_But_Not_Authorize_Rbac_Endpoint()
    {
        var client = await StartAsync(PermissaoCatalogo.PerfilGerenciar);
        client.DefaultRequestHeaders.Add("X-Development-User-Id", Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/probe-autenticado")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/probe-perfis")).StatusCode);
    }

    /// <summary>Segurança: um cliente que forje a claim de permissão via header não obtém autorização — as
    /// claims são produzidas exclusivamente pelo authentication handler do servidor.</summary>
    [Fact]
    public async Task Client_Supplied_Permission_Header_Should_Be_Ignored()
    {
        var client = await StartAsync();
        ComSessao(client);
        client.DefaultRequestHeaders.Add(RbacClaims.Permissao, PermissaoCatalogo.PerfilGerenciar);
        client.DefaultRequestHeaders.Add("X-Permissoes", PermissaoCatalogo.PerfilGerenciar);

        var response = await client.GetAsync("/probe-perfis");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>O1.12 — as 3 novas permissões de fundação de Administração (Workflow/Alçadas/Orçamento)
    /// seguem o MESMO pipeline de enforcement das demais: 401 sem sessão, 403 sem a permissão, 200 com a
    /// permissão. Não é um mecanismo próprio — apenas mais 3 entradas no catálogo genérico já coberto por
    /// <see cref="RbacPoliciesTests.AddRbacPolicies_Should_Register_One_Policy_Per_Catalog_Permission"/>.</summary>
    [Fact]
    public async Task Should_Enforce_401_403_200_For_The_New_O112_Administration_Permissions()
    {
        var semSessao = await StartAsync(PermissaoCatalogo.WorkflowGerenciar);
        Assert.Equal(HttpStatusCode.Unauthorized, (await semSessao.GetAsync("/probe-workflow")).StatusCode);

        var semPermissao = await StartAsync();
        ComSessao(semPermissao);
        Assert.Equal(HttpStatusCode.Forbidden, (await semPermissao.GetAsync("/probe-workflow")).StatusCode);

        var comPermissao = await StartAsync(PermissaoCatalogo.WorkflowGerenciar, PermissaoCatalogo.AlcadaGerenciar, PermissaoCatalogo.OrcamentoGerenciar);
        ComSessao(comPermissao);
        Assert.Equal(HttpStatusCode.OK, (await comPermissao.GetAsync("/probe-workflow")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await comPermissao.GetAsync("/probe-alcada")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await comPermissao.GetAsync("/probe-orcamento")).StatusCode);
    }

    /// <summary>Endpoint inexistente sob uma sessão autorizada continua 404 — a autorização não transforma
    /// ausência de rota em negação, e não vaza a diferença.</summary>
    [Fact]
    public async Task Unknown_Route_Should_Still_Return_404_For_An_Authorized_Session()
    {
        var client = await StartAsync(PermissaoCatalogo.PerfilGerenciar);
        ComSessao(client);

        var response = await client.GetAsync("/probe-inexistente");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static void ComSessao(HttpClient client) =>
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={TokenSessao}");

    private async Task<HttpClient> StartAsync(params string[] permissoesEfetivas)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _identidade = new FakeIdentidadeComPermissoes { Permissoes = permissoesEfetivas };
        builder.Services.AddSingleton<IObterIdentidadeAtualUseCase>(_identidade);
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddAuthentication(DevelopmentHeaderAuthenticationDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentHeaderAuthenticationHandler>(
                DevelopmentHeaderAuthenticationDefaults.Scheme, null)
            .AddScheme<AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>(
                SessionCookieAuthenticationDefaults.Scheme, null);

        builder.Services.AddScoped<IAuthorizationHandler, PermissaoAuthorizationHandler>();
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddRbacPolicies();
        });

        _app = builder.Build();
        _app.Use(async (context, next) =>
        {
            // Reproduz o PolicyScheme de Program.cs em Development: cookie presente → esquema de sessão.
            if (context.Request.Cookies.ContainsKey(AuthCookie.Name))
            {
                var resultado = await context.AuthenticateAsync(SessionCookieAuthenticationDefaults.Scheme);
                if (!resultado.Succeeded)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return;
                }

                context.User = resultado.Principal!;
            }

            await next();
        });
        _app.UseAuthentication();
        _app.UseAuthorization();

        _app.MapGet("/probe-autenticado", () => "ok");
        _app.MapGet("/probe-perfis", () => "ok")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.PerfilGerenciar));
        _app.MapGet("/probe-pedidos", () => "ok")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.PedidoCriar));
        _app.MapGet("/probe-workflow", () => "ok")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.WorkflowGerenciar));
        _app.MapGet("/probe-alcada", () => "ok")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.AlcadaGerenciar));
        _app.MapGet("/probe-orcamento", () => "ok")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.OrcamentoGerenciar));

        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();

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
                    Guid.NewGuid(), "ana@example.invalid", "Ana", Guid.NewGuid(), Permissoes))
                : Task.FromResult<IdentidadeAtualDto?>(null);
    }
}

/// <summary>Protege as garantias estruturais de <see cref="RbacPolicies"/>: um código de permissão fora do
/// catálogo deve explodir na composição do host, não silenciosamente deixar um endpoint sem proteção
/// efetiva (uma policy inexistente lançaria em runtime, mas só na primeira requisição).</summary>
public sealed class RbacPoliciesTests
{
    [Fact]
    public void For_Should_Reject_A_Code_Outside_The_Catalog() =>
        Assert.Throws<InvalidOperationException>(() => RbacPolicies.For("Perfil.Excluir"));

    [Fact]
    public void For_Should_Normalize_Case_To_The_Canonical_Policy_Name() =>
        Assert.Equal(RbacPolicies.For(PermissaoCatalogo.PerfilGerenciar), RbacPolicies.For("perfil.gerenciar"));

    /// <summary>Existe exatamente uma policy por permissão do catálogo — nenhuma permissão fica sem policy
    /// (endpoint que não pode ser protegido) e nenhuma policy órfã é registrada.</summary>
    [Fact]
    public void AddRbacPolicies_Should_Register_One_Policy_Per_Catalog_Permission()
    {
        var options = new AuthorizationOptions();

        options.AddRbacPolicies();

        foreach (var definicao in PermissaoCatalogo.Todas)
        {
            var policy = options.GetPolicy(RbacPolicies.For(definicao.Codigo));
            Assert.NotNull(policy);
            var requirement = Assert.Single(policy!.Requirements.OfType<PermissaoRequirement>());
            Assert.Equal(definicao.Codigo, requirement.Codigo);
        }
    }

    [Fact]
    public void ToClaims_Should_Emit_One_Claim_Per_Permission_With_The_Rbac_Claim_Type()
    {
        var claims = RbacPolicies.ToClaims([PermissaoCatalogo.PedidoCriar, PermissaoCatalogo.PerfilGerenciar]).ToArray();

        Assert.Equal(2, claims.Length);
        Assert.All(claims, claim => Assert.Equal(RbacClaims.Permissao, claim.Type));
    }
}
