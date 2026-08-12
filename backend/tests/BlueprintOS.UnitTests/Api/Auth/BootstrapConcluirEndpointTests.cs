using System.Net;
using System.Net.Http.Json;
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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Auth;

/// <summary>Teste de pipeline HTTP real de <c>POST /bootstrap/concluir</c> (O1.4.3.2; Work Order O1.4.3,
/// seção 18, itens 8/9/10/16): exige <c>BootstrapSessao</c> válida via a política <c>BootstrapAuthenticated</c>
/// (nunca <c>FallbackPolicy</c>/<c>DevelopmentHeader</c>/sessão normal) e o header CSRF — usa fakes para o
/// caso de uso (sem <c>AddInfrastructure</c>/banco real, mesmo padrão de
/// <c>BootstrapAuthorizationPipelineTests</c>).</summary>
public sealed class BootstrapConcluirEndpointTests : IAsyncDisposable
{
    private const string RawToken = "sessao-bootstrap-para-conclusao";
    private WebApplication? _app;
    private FakeConcluirBootstrapUseCase? _concluir;

    [Fact]
    public async Task Concluir_Without_BootstrapSessao_Should_Return_401()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");

        var response = await client.PostAsJsonAsync("/bootstrap/concluir", NovoPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Concluir_With_Normal_SessionCookie_Instead_Of_BootstrapSessao_Should_Return_401()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}=sessao-normal-valida");

        var response = await client.PostAsJsonAsync("/bootstrap/concluir", NovoPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Concluir_Without_Csrf_Header_Should_Return_403()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add("Cookie", $"{BootstrapCookie.Name}={RawToken}");

        var response = await client.PostAsJsonAsync("/bootstrap/concluir", NovoPayload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Concluir_With_Valid_BootstrapSessao_And_Csrf_Should_Return_200_And_Delete_Cookie()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");
        client.DefaultRequestHeaders.Add("Cookie", $"{BootstrapCookie.Name}={RawToken}");
        _concluir!.Resultado = new ConcluirBootstrapResultado(true, null, Guid.NewGuid(), "admin@example.invalid", "Admin", Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/bootstrap/concluir", NovoPayload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith(BootstrapCookie.Name + "=") && c.Contains("expires="));
    }

    [Fact]
    public async Task Concluir_On_Business_Failure_Should_Return_400()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");
        client.DefaultRequestHeaders.Add("Cookie", $"{BootstrapCookie.Name}={RawToken}");
        _concluir!.Resultado = new ConcluirBootstrapResultado(false, "Bootstrap indisponível.", null, null, null, null);

        var response = await client.PostAsJsonAsync("/bootstrap/concluir", NovoPayload());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static object NovoPayload() => new
    {
        unidadeNegocio = new { nome = "SOMA Matriz", slug = "soma-matriz" },
        administrador = new { nome = "Administradora Sênior" },
    };

    private async Task<HttpClient> StartAppAndCreateClientAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var bootstrapSessoes = new FakeBootstrapSessaoRepository();
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", OpaqueSessionToken.Hash(RawToken), DateTimeOffset.UtcNow);
        bootstrapSessoes.All.Add(sessao);

        var estados = new FakeBootstrapEstadoRepository();
        _concluir = new FakeConcluirBootstrapUseCase();

        builder.Services.AddSingleton<IBootstrapSessaoRepository>(bootstrapSessoes);
        builder.Services.AddSingleton<IBootstrapEstadoRepository>(estados);
        // MapBootstrap() mapeia os quatro endpoints de Bootstrap (não só /concluir) — todos precisam de
        // seus casos de uso resolvíveis no container para que a inferência de metadata dos Minimal APIs
        // não trate o parâmetro "useCase" como corpo da requisição por engano.
        builder.Services.AddSingleton<IConsultarBootstrapEstadoUseCase>(new NotUsedConsultarBootstrapEstadoUseCase());
        builder.Services.AddSingleton<IIniciarBootstrapUseCase>(new NotUsedIniciarBootstrapUseCase());
        builder.Services.AddSingleton<IValidarOtpBootstrapUseCase>(new NotUsedValidarOtpBootstrapUseCase());
        builder.Services.AddSingleton<IConcluirBootstrapUseCase>(_concluir);
        builder.Services.AddSingleton<IObterIdentidadeAtualUseCase>(new FakeObterIdentidadeAtualUseCase());
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddRateLimiter(RateLimitingPolicies.Configure);

        builder.Services.AddAuthentication(SessionCookieAuthenticationDefaults.Scheme)
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
        _app.UseRateLimiter();
        _app.MapBootstrap();

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

    private sealed class FakeBootstrapSessaoRepository : IBootstrapSessaoRepository
    {
        public List<BootstrapSessao> All { get; } = [];

        public Task<BootstrapSessao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.IdentificadorHash == identificadorHash));

        public Task<BootstrapSessao?> ObterAtivaPorEmailCandidatoAsync(string emailCandidato, CancellationToken ct) =>
            Task.FromResult(All.Where(x => x.EmailCandidato == emailCandidato && x.UsadaEm == null && x.RevokedAt == null)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefault());

        public Task<BootstrapSessao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.Id == id));

        public Task AdicionarAsync(BootstrapSessao sessao, CancellationToken ct) { All.Add(sessao); return Task.CompletedTask; }
        public Task AtualizarAsync(BootstrapSessao sessao, CancellationToken ct) => Task.CompletedTask;
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeBootstrapEstadoRepository : IBootstrapEstadoRepository
    {
        public Task<BootstrapEstado?> ObterAsync(CancellationToken ct) =>
            Task.FromResult<BootstrapEstado?>(BootstrapEstado.CriarInicial());

        public Task AtualizarAsync(BootstrapEstado estado, CancellationToken ct) => Task.CompletedTask;
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NotUsedConsultarBootstrapEstadoUseCase : IConsultarBootstrapEstadoUseCase
    {
        public Task<ConsultarBootstrapEstadoResultado> ExecuteAsync(CancellationToken ct) =>
            throw new InvalidOperationException("Não deveria ser chamado pelos testes de /bootstrap/concluir.");
    }

    private sealed class NotUsedIniciarBootstrapUseCase : IIniciarBootstrapUseCase
    {
        public Task<IniciarBootstrapResultado> ExecuteAsync(string email, string secret, CancellationToken ct) =>
            throw new InvalidOperationException("Não deveria ser chamado pelos testes de /bootstrap/concluir.");
    }

    private sealed class NotUsedValidarOtpBootstrapUseCase : IValidarOtpBootstrapUseCase
    {
        public Task<ValidarOtpBootstrapResultado> ExecuteAsync(string email, string codigo, CancellationToken ct) =>
            throw new InvalidOperationException("Não deveria ser chamado pelos testes de /bootstrap/concluir.");
    }

    private sealed class FakeObterIdentidadeAtualUseCase : IObterIdentidadeAtualUseCase
    {
        public Task<IdentidadeAtualDto?> ExecuteAsync(string sessionRawToken, CancellationToken ct) =>
            sessionRawToken == "sessao-normal-valida"
                ? Task.FromResult<IdentidadeAtualDto?>(new IdentidadeAtualDto(Guid.NewGuid(), "ana@somagrupo.com.br", "Ana", Guid.NewGuid(), [], EscopoAdministrativo.Negocio))
                : Task.FromResult<IdentidadeAtualDto?>(null);
    }

    private sealed class FakeConcluirBootstrapUseCase : IConcluirBootstrapUseCase
    {
        public ConcluirBootstrapResultado Resultado { get; set; } =
            new(true, null, Guid.NewGuid(), "admin@example.invalid", "Admin", Guid.NewGuid());

        public Task<ConcluirBootstrapResultado> ExecuteAsync(
            Guid bootstrapSessaoId,
            UnidadeNegocioBootstrapPayload unidadeNegocio,
            AdministradorSeniorBootstrapPayload administrador,
            CancellationToken ct) => Task.FromResult(Resultado);
    }
}
