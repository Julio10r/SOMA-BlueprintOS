using System.Net;
using System.Net.Http.Json;
using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Auth;

/// <summary>Teste de pipeline HTTP real dos três endpoints entregues por O1.4.3.1
/// (<c>GET /bootstrap/estado</c>, <c>POST /bootstrap/iniciar</c>, <c>POST /bootstrap/otp/verificar</c>).
/// Cobre os itens 7 (rate limiting), 13 (404 pós-conclusão) e 16 (CSRF) do plano de testes da Work Order
/// O1.4.3 (seção 18), usando fakes para os casos de uso (sem <c>AddInfrastructure</c>/banco real).</summary>
public sealed class BootstrapControllerEndpointsTests : IAsyncDisposable
{
    private WebApplication? _app;
    private FakeIniciarBootstrapUseCase? _iniciar;
    private FakeValidarOtpBootstrapUseCase? _verificar;

    [Fact]
    public async Task GetEstado_Should_Not_Require_Csrf_Header_And_Return_Ok()
    {
        var client = await StartAppAndCreateClientAsync();

        var response = await client.GetAsync("/bootstrap/estado");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_Without_Csrf_Header_Should_Return_403()
    {
        var client = await StartAppAndCreateClientAsync();

        var response = await client.PostAsJsonAsync("/bootstrap/iniciar", new { email = "a@b.com", secret = "x" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerificarOtp_Without_Csrf_Header_Should_Return_403()
    {
        var client = await StartAppAndCreateClientAsync();

        var response = await client.PostAsJsonAsync("/bootstrap/otp/verificar", new { email = "a@b.com", codigo = "123456" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_With_Csrf_Header_Should_Return_Generic_Ok_Message()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");

        var response = await client.PostAsJsonAsync("/bootstrap/iniciar", new { email = "a@b.com", secret = "x" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_Should_Return_404_When_Bootstrap_Already_Concluded()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");
        _iniciar!.BootstrapDisponivel = false;

        var response = await client.PostAsJsonAsync("/bootstrap/iniciar", new { email = "a@b.com", secret = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_Should_Return_429_After_Exceeding_Rate_Limit_By_Ip()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");

        HttpResponseMessage? last = null;
        for (var i = 0; i < 5; i++)
        {
            last = await client.PostAsJsonAsync("/bootstrap/iniciar", new { email = "a@b.com", secret = "x" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [Fact]
    public async Task VerificarOtp_On_Success_Should_Set_BootstrapCookie_And_Return_204()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");
        _verificar!.Sucesso = true;
        _verificar!.SessionRawToken = "token-de-teste";

        var response = await client.PostAsJsonAsync("/bootstrap/otp/verificar", new { email = "a@b.com", codigo = "123456" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith(BootstrapCookie.Name + "="));
    }

    [Fact]
    public async Task VerificarOtp_On_Failure_Should_Return_400_With_Generic_Message()
    {
        var client = await StartAppAndCreateClientAsync();
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");
        _verificar!.Sucesso = false;

        var response = await client.PostAsJsonAsync("/bootstrap/otp/verificar", new { email = "a@b.com", codigo = "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> StartAppAndCreateClientAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _iniciar = new FakeIniciarBootstrapUseCase();
        _verificar = new FakeValidarOtpBootstrapUseCase();

        builder.Services.AddSingleton<IConsultarBootstrapEstadoUseCase>(new FakeConsultarBootstrapEstadoUseCase());
        builder.Services.AddSingleton<IIniciarBootstrapUseCase>(_iniciar);
        builder.Services.AddSingleton<IValidarOtpBootstrapUseCase>(_verificar);
        builder.Services.AddSingleton<IConcluirBootstrapUseCase>(new FakeConcluirBootstrapUseCaseForEstadoTests());
        builder.Services.AddSingleton<IBootstrapSessaoRepository>(new FakeBootstrapSessaoRepositoryForEstadoTests());
        builder.Services.AddSingleton<IBootstrapEstadoRepository>(new FakeBootstrapEstadoRepositoryForEstadoTests());
        builder.Services.AddRateLimiter(RateLimitingPolicies.Configure);

        // /bootstrap/concluir (O1.4.3.2) exige a política BootstrapAuthenticated — precisa do esquema
        // BootstrapSession e do pipeline de autorização registrados, mesmo neste harness focado nos
        // endpoints de O1.4.3.1, para que o app inicie sem erro (RequireAuthorization exige
        // IAuthorizationService disponível independentemente de qual endpoint é chamado no teste).
        builder.Services.AddAuthentication(BootstrapSessionAuthenticationDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, BootstrapSessionAuthenticationHandler>(
                BootstrapSessionAuthenticationDefaults.Scheme, null);
        builder.Services.AddScoped<IAuthorizationHandler, BootstrapNaoConcluidoAuthorizationHandler>();
        builder.Services.AddAuthorization(options =>
        {
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

    private sealed class FakeConsultarBootstrapEstadoUseCase : IConsultarBootstrapEstadoUseCase
    {
        public Task<ConsultarBootstrapEstadoResultado> ExecuteAsync(CancellationToken ct) =>
            Task.FromResult(new ConsultarBootstrapEstadoResultado(Disponivel: true));
    }

    private sealed class FakeIniciarBootstrapUseCase : IIniciarBootstrapUseCase
    {
        public bool BootstrapDisponivel { get; set; } = true;

        public Task<IniciarBootstrapResultado> ExecuteAsync(string email, string secret, CancellationToken ct) =>
            Task.FromResult(new IniciarBootstrapResultado(BootstrapDisponivel));
    }

    private sealed class FakeValidarOtpBootstrapUseCase : IValidarOtpBootstrapUseCase
    {
        public bool Sucesso { get; set; }
        public string? SessionRawToken { get; set; }

        public Task<ValidarOtpBootstrapResultado> ExecuteAsync(string email, string codigo, CancellationToken ct) =>
            Task.FromResult(new ValidarOtpBootstrapResultado(
                Sucesso,
                Sucesso ? null : "Código inválido ou expirado.",
                Sucesso ? SessionRawToken : null,
                Sucesso ? email : null));
    }

    private sealed class FakeConcluirBootstrapUseCaseForEstadoTests : IConcluirBootstrapUseCase
    {
        public Task<ConcluirBootstrapResultado> ExecuteAsync(
            Guid bootstrapSessaoId,
            UnidadeNegocioBootstrapPayload unidadeNegocio,
            AdministradorSeniorBootstrapPayload administrador,
            CancellationToken ct) =>
            Task.FromResult(new ConcluirBootstrapResultado(false, "não usado neste harness", null, null, null, null));
    }

    private sealed class FakeBootstrapSessaoRepositoryForEstadoTests : IBootstrapSessaoRepository
    {
        public Task<BootstrapSessao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct) =>
            Task.FromResult<BootstrapSessao?>(null);

        public Task<BootstrapSessao?> ObterAtivaPorEmailCandidatoAsync(string emailCandidato, CancellationToken ct) =>
            Task.FromResult<BootstrapSessao?>(null);

        public Task<BootstrapSessao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<BootstrapSessao?>(null);

        public Task AdicionarAsync(BootstrapSessao sessao, CancellationToken ct) => Task.CompletedTask;
        public Task AtualizarAsync(BootstrapSessao sessao, CancellationToken ct) => Task.CompletedTask;
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeBootstrapEstadoRepositoryForEstadoTests : IBootstrapEstadoRepository
    {
        public Task<BootstrapEstado?> ObterAsync(CancellationToken ct) =>
            Task.FromResult<BootstrapEstado?>(BootstrapEstado.CriarInicial());

        public Task AtualizarAsync(BootstrapEstado estado, CancellationToken ct) => Task.CompletedTask;
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
