using System.Security.Claims;
using System.Text.Encodings.Web;
using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Security;
using BlueprintOS.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Api.Auth;

/// <summary>Testa <see cref="BootstrapSessionAuthenticationHandler"/> diretamente, sem exigir host HTTP
/// completo — mesmo padrão de <c>SessionCookieAuthenticationHandlerTests</c>.</summary>
public sealed class BootstrapSessionAuthenticationHandlerTests
{
    [Fact]
    public async Task Should_Return_NoResult_When_No_Cookie_Present()
    {
        var handler = await CreateHandlerAsync(new FakeBootstrapSessaoRepository(), cookieValue: null);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task Should_Fail_When_Cookie_Present_But_Session_Not_Found()
    {
        var handler = await CreateHandlerAsync(new FakeBootstrapSessaoRepository(), cookieValue: "token-desconhecido");

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task Should_Fail_When_Session_Already_Used()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", OpaqueSessionToken.Hash("token-valido"), agora);
        sessao.MarcarUsada(agora);
        var repo = new FakeBootstrapSessaoRepository(sessao);

        var handler = await CreateHandlerAsync(repo, cookieValue: "token-valido");
        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Should_Fail_When_Session_Revoked()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", OpaqueSessionToken.Hash("token-valido"), agora);
        sessao.Revogar(agora);
        var repo = new FakeBootstrapSessaoRepository(sessao);

        var handler = await CreateHandlerAsync(repo, cookieValue: "token-valido");
        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Should_Fail_When_Session_Expired()
    {
        var criadoEm = DateTimeOffset.UtcNow - BootstrapSessao.Validade - TimeSpan.FromMinutes(1);
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", OpaqueSessionToken.Hash("token-valido"), criadoEm);
        var repo = new FakeBootstrapSessaoRepository(sessao);

        var handler = await CreateHandlerAsync(repo, cookieValue: "token-valido");
        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Should_Succeed_And_Publish_Only_Email_Claim_Never_A_Role_Claim()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", OpaqueSessionToken.Hash("token-valido"), agora);
        var repo = new FakeBootstrapSessaoRepository(sessao);

        var handler = await CreateHandlerAsync(repo, cookieValue: "token-valido");
        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("candidato@somagrupo.com.br", result.Principal!.FindFirst(ClaimTypes.Email)!.Value);
        // Work Order O1.4.3, seção 8: "nunca uma claim de papel/perfil" — reforço explícito.
        Assert.Null(result.Principal!.FindFirst(ClaimTypes.Role));
        Assert.Null(result.Principal!.FindFirst(ClaimTypes.NameIdentifier));
    }

    private static async Task<BootstrapSessionAuthenticationHandler> CreateHandlerAsync(
        IBootstrapSessaoRepository repo, string? cookieValue)
    {
        var handler = new BootstrapSessionAuthenticationHandler(
            new FakeOptionsMonitor(),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            repo,
            TimeProvider.System);

        var context = new DefaultHttpContext();
        if (cookieValue is not null)
        {
            context.Request.Headers.Append("Cookie", $"{BootstrapCookie.Name}={cookieValue}");
        }

        await handler.InitializeAsync(
            new AuthenticationScheme(BootstrapSessionAuthenticationDefaults.Scheme, null, typeof(BootstrapSessionAuthenticationHandler)),
            context);
        return handler;
    }

    private sealed class FakeBootstrapSessaoRepository : IBootstrapSessaoRepository
    {
        private readonly BootstrapSessao? _sessao;

        public FakeBootstrapSessaoRepository(BootstrapSessao? sessao = null) => _sessao = sessao;

        public Task<BootstrapSessao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct) =>
            Task.FromResult(_sessao is not null && _sessao.IdentificadorHash == identificadorHash ? _sessao : null);

        public Task<BootstrapSessao?> ObterAtivaPorEmailCandidatoAsync(string emailCandidato, CancellationToken ct) =>
            Task.FromResult<BootstrapSessao?>(null);

        public Task<BootstrapSessao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_sessao is not null && _sessao.Id == id ? _sessao : null);

        public Task AdicionarAsync(BootstrapSessao sessao, CancellationToken ct) => Task.CompletedTask;
        public Task AtualizarAsync(BootstrapSessao sessao, CancellationToken ct) => Task.CompletedTask;
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeOptionsMonitor : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue { get; } = new();
        public AuthenticationSchemeOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AuthenticationSchemeOptions, string?> listener) => null;
    }
}
