using System.Text.Encodings.Web;
using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Api.Auth;

/// <summary>Cobre a primeira barreira do secure-by-default (O1.4.2.1, Etapa 3) diretamente no
/// authentication handler — sem exigir um host HTTP completo (que exigiria banco configurado via
/// <c>AddInfrastructure</c>).</summary>
public sealed class SessionCookieAuthenticationHandlerTests
{
    [Fact]
    public async Task Should_Return_NoResult_When_No_Cookie_Present()
    {
        var handler = await CreateHandlerAsync(new FakeObterIdentidadeAtualUseCase(sempreNulo: true), cookieValue: null);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task Should_Fail_When_Cookie_Present_But_Session_Invalid()
    {
        var handler = await CreateHandlerAsync(new FakeObterIdentidadeAtualUseCase(sempreNulo: true), cookieValue: "algum-token");

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task Should_Succeed_And_Set_Claims_When_Session_Valid()
    {
        var identidade = new IdentidadeAtualDto(Guid.NewGuid(), "ana@somagrupo.com.br", "Ana", Guid.NewGuid(), [], EscopoAdministrativo.Negocio);
        var handler = await CreateHandlerAsync(new FakeObterIdentidadeAtualUseCase(identidade), cookieValue: "token-valido");

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(identidade.UsuarioId.ToString(), result.Principal!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        Assert.Equal(identidade.Email, result.Principal!.FindFirst(System.Security.Claims.ClaimTypes.Email)!.Value);
    }

    private static async Task<SessionCookieAuthenticationHandler> CreateHandlerAsync(
        IObterIdentidadeAtualUseCase useCase, string? cookieValue)
    {
        var handler = new SessionCookieAuthenticationHandler(
            new FakeOptionsMonitor(),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            useCase);

        var context = new DefaultHttpContext();
        if (cookieValue is not null)
        {
            context.Request.Headers.Append("Cookie", $"{AuthCookie.Name}={cookieValue}");
        }

        await handler.InitializeAsync(new AuthenticationScheme(SessionCookieAuthenticationDefaults.Scheme, null, typeof(SessionCookieAuthenticationHandler)), context);
        return handler;
    }

    private sealed class FakeObterIdentidadeAtualUseCase : IObterIdentidadeAtualUseCase
    {
        private readonly IdentidadeAtualDto? _resultado;

        public FakeObterIdentidadeAtualUseCase(IdentidadeAtualDto? resultado = null, bool sempreNulo = false)
        {
            _resultado = sempreNulo ? null : resultado;
        }

        public Task<IdentidadeAtualDto?> ExecuteAsync(string sessionRawToken, CancellationToken ct) =>
            Task.FromResult(_resultado);
    }

    private sealed class FakeOptionsMonitor : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue { get; } = new();
        public AuthenticationSchemeOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AuthenticationSchemeOptions, string?> listener) => null;
    }
}
