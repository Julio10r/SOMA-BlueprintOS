using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Identity.Security;
using BlueprintOS.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Auth;

/// <summary>Teste de pipeline HTTP real (mesmo padrão de <c>DevelopmentHeaderPipelineTests</c>, O1.4.2.2) —
/// sobe um host Kestrel real com a mesma composição de autenticação/autorização de <c>Program.cs</c> relevante
/// ao Bootstrap (sem <c>AddInfrastructure</c>, que exigiria banco real). Cobre os itens 8, 9, 10 e 19 do
/// plano de testes da Work Order O1.4.3 (seção 18):
/// - item 8: uma sessão de Bootstrap válida não autentica endpoints de negócio (<c>FallbackPolicy</c>).
/// - item 9: a política <c>BootstrapAuthenticated</c> rejeita uma sessão normal (<c>SessionCookie</c>).
/// - item 10: sessão normal não autentica em endpoints protegidos por <c>BootstrapAuthenticated</c>.
/// - item 19: <c>DevelopmentHeaderAuthenticationHandler</c> não autentica endpoints de Bootstrap.
///
/// O endpoint <c>/probe-bootstrap</c> abaixo representa, para fins de teste desta etapa (O1.4.3.1), a
/// classificação de autorização que <c>POST /bootstrap/concluir</c> usará em O1.4.3.2 (ainda não
/// implementado) — a política <c>BootstrapAuthenticated</c> em si já é entregue nesta etapa (Work Order
/// O1.4.3, seção 21).</summary>
public sealed class BootstrapAuthorizationPipelineTests : IAsyncDisposable
{
    private WebApplication? _app;
    private FakeBootstrapEstadoRepositoryMutable? _estados;

    [Fact]
    public async Task Probe_Negocio_Should_Return_401_With_Only_Bootstrap_Cookie()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{BootstrapCookie.Name}=sessao-bootstrap-valida");

        var response = await client.GetAsync("/probe-negocio");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Probe_Bootstrap_Should_Return_401_With_Only_SessionCookie_Normal_Session()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}=sessao-normal-valida");

        var response = await client.GetAsync("/probe-bootstrap");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Probe_Bootstrap_Should_Return_401_With_DevelopmentHeader_Even_From_Loopback()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("X-Development-User-Id", Guid.NewGuid().ToString());

        var response = await client.GetAsync("/probe-bootstrap");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Probe_Bootstrap_Should_Return_200_With_Valid_Bootstrap_Session_When_Not_Concluded()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{BootstrapCookie.Name}=sessao-bootstrap-valida");
        _estados!.Concluido = false;

        var response = await client.GetAsync("/probe-bootstrap");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Probe_Bootstrap_Should_Return_403_With_Valid_Bootstrap_Session_When_Already_Concluded()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{BootstrapCookie.Name}=sessao-bootstrap-valida");
        _estados!.Concluido = true;

        var response = await client.GetAsync("/probe-bootstrap");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Probe_Bootstrap_Should_Return_401_Without_Any_Cookie()
    {
        var client = await StartAppAndCreateClientAsync();

        var response = await client.GetAsync("/probe-bootstrap");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> StartAppAndCreateClientAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var bootstrapSessoes = new FakeBootstrapSessaoRepositoryForPipeline();
        var agora = DateTimeOffset.UtcNow;
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", OpaqueSessionToken.Hash("sessao-bootstrap-valida"), agora);
        bootstrapSessoes.All.Add(sessao);

        _estados = new FakeBootstrapEstadoRepositoryMutable { Concluido = false };

        builder.Services.AddSingleton<IBootstrapSessaoRepository>(bootstrapSessoes);
        builder.Services.AddSingleton<IBootstrapEstadoRepository>(_estados);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IObterIdentidadeAtualUseCase>(new FakeObterIdentidadeAtualUseCaseForPipeline());

        builder.Services.AddAuthentication(DevelopmentHeaderAuthenticationDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentHeaderAuthenticationHandler>(
                DevelopmentHeaderAuthenticationDefaults.Scheme, null)
            .AddScheme<AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>(
                SessionCookieAuthenticationDefaults.Scheme, null)
            .AddScheme<AuthenticationSchemeOptions, BootstrapSessionAuthenticationHandler>(
                BootstrapSessionAuthenticationDefaults.Scheme, null);

        builder.Services.AddScoped<IAuthorizationHandler, BootstrapNaoConcluidoAuthorizationHandler>();
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy(BootstrapAuthorizationPolicies.BootstrapAuthenticated, policy => policy
                .AddAuthenticationSchemes(BootstrapSessionAuthenticationDefaults.Scheme)
                .RequireAuthenticatedUser()
                .AddRequirements(new BootstrapNaoConcluidoRequirement()));
        });

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGet("/probe-negocio", () => "ok");
        _app.MapGet("/probe-bootstrap", () => "ok").RequireAuthorization(BootstrapAuthorizationPolicies.BootstrapAuthenticated);

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

    private sealed class FakeBootstrapSessaoRepositoryForPipeline : IBootstrapSessaoRepository
    {
        public List<BootstrapSessao> All { get; } = [];

        public Task<BootstrapSessao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.IdentificadorHash == identificadorHash));

        public Task<BootstrapSessao?> ObterAtivaPorEmailCandidatoAsync(string emailCandidato, CancellationToken ct) =>
            Task.FromResult(All.Where(x => x.EmailCandidato == emailCandidato && x.UsadaEm == null && x.RevokedAt == null)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefault());

        public Task AdicionarAsync(BootstrapSessao sessao, CancellationToken ct) { All.Add(sessao); return Task.CompletedTask; }
        public Task AtualizarAsync(BootstrapSessao sessao, CancellationToken ct) => Task.CompletedTask;
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<BootstrapSessao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.Id == id));
    }

    private sealed class FakeBootstrapEstadoRepositoryMutable : IBootstrapEstadoRepository
    {
        public bool Concluido { get; set; }

        public Task<BootstrapEstado?> ObterAsync(CancellationToken ct)
        {
            var estado = BootstrapEstado.CriarInicial();
            if (Concluido)
            {
                typeof(BootstrapEstado).GetProperty(nameof(BootstrapEstado.Concluido))!.SetValue(estado, true);
            }

            return Task.FromResult<BootstrapEstado?>(estado);
        }

        public Task AtualizarAsync(BootstrapEstado estado, CancellationToken ct) => Task.CompletedTask;
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeObterIdentidadeAtualUseCaseForPipeline : IObterIdentidadeAtualUseCase
    {
        public Task<IdentidadeAtualDto?> ExecuteAsync(string sessionRawToken, CancellationToken ct) =>
            sessionRawToken == "sessao-normal-valida"
                ? Task.FromResult<IdentidadeAtualDto?>(new IdentidadeAtualDto(Guid.NewGuid(), "ana@somagrupo.com.br", "Ana", Guid.NewGuid()))
                : Task.FromResult<IdentidadeAtualDto?>(null);
    }
}
