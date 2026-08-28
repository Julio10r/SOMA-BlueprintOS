using System.Net;
using System.Text.Encodings.Web;
using BlueprintOS.Api.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Api.Identity;

/// <summary>Hardening de origem (O1.4.2.2 — Security Validation II, Achado E). Cobre exatamente os
/// cenários exigidos: loopback IPv4/IPv6 autentica, origem não-loopback nunca autentica (mesmo com
/// header válido), ambientes fora de Development nunca autenticam, e X-Forwarded-For nunca é honrado
/// como prova de origem (não há UseForwardedHeaders() em Program.cs).</summary>
public sealed class DevelopmentHeaderAuthenticationHandlerTests
{
    private const string UserIdHeader = "X-Development-User-Id";
    private const string ForwardedForHeader = "X-Forwarded-For";

    [Fact]
    public async Task Should_Authenticate_Development_Loopback_IPv4_With_Valid_Header()
    {
        var result = await AuthenticateAsync(Environments.Development, IPAddress.Loopback, Guid.NewGuid().ToString());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
    }

    [Fact]
    public async Task Should_Authenticate_Development_Loopback_IPv6_With_Valid_Header()
    {
        var result = await AuthenticateAsync(Environments.Development, IPAddress.IPv6Loopback, Guid.NewGuid().ToString());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Should_Not_Authenticate_Development_NonLoopback_With_Valid_Header()
    {
        var result = await AuthenticateAsync(Environments.Development, IPAddress.Parse("203.0.113.10"), Guid.NewGuid().ToString());

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task Should_Not_Authenticate_Development_Loopback_With_Invalid_Header()
    {
        var result = await AuthenticateAsync(Environments.Development, IPAddress.Loopback, "nao-e-um-guid");

        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task Should_Not_Authenticate_Development_Loopback_Without_Header()
    {
        var result = await AuthenticateAsync(Environments.Development, IPAddress.Loopback, headerValue: null);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Should_Not_Authenticate_Staging_Even_With_Loopback_And_Valid_Header()
    {
        var result = await AuthenticateAsync(Environments.Staging, IPAddress.Loopback, Guid.NewGuid().ToString());

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task Should_Not_Authenticate_Production_Even_With_Loopback_And_Valid_Header()
    {
        var result = await AuthenticateAsync(Environments.Production, IPAddress.Loopback, Guid.NewGuid().ToString());

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task Should_Not_Authenticate_When_XForwardedFor_Claims_Loopback_But_RemoteIp_Is_External()
    {
        var result = await AuthenticateAsync(
            Environments.Development,
            IPAddress.Parse("203.0.113.10"),
            Guid.NewGuid().ToString(),
            forwardedFor: "127.0.0.1");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Should_Authenticate_When_RemoteIp_Is_Loopback_Even_If_XForwardedFor_Claims_External()
    {
        // Documenta o comportamento atual: nenhum ForwardedHeaders middleware está registrado em
        // Program.cs, então X-Forwarded-For nunca é lido por este handler — RemoteIpAddress é sempre a
        // fonte de verdade. Isto NÃO torna o mecanismo seguro atrás de um proxy reverso local (nesse
        // cenário o próprio RemoteIpAddress passaria a ser loopback para todo tráfego externo) — apenas
        // confirma que o header em si não é confiado. Proxy/túnel permanece uma topologia não suportada.
        var result = await AuthenticateAsync(
            Environments.Development,
            IPAddress.Loopback,
            Guid.NewGuid().ToString(),
            forwardedFor: "203.0.113.10");

        Assert.True(result.Succeeded);
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(
        string environmentName, IPAddress remoteIp, string? headerValue, string? forwardedFor = null)
    {
        var handler = new DevelopmentHeaderAuthenticationHandler(
            new FakeOptionsMonitor(), NullLoggerFactory.Instance, UrlEncoder.Default,
            new FakeHostEnvironment(environmentName));

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteIp;
        if (headerValue is not null) context.Request.Headers[UserIdHeader] = headerValue;
        if (forwardedFor is not null) context.Request.Headers[ForwardedForHeader] = forwardedFor;

        await handler.InitializeAsync(
            new AuthenticationScheme(DevelopmentHeaderAuthenticationDefaults.Scheme, null, typeof(DevelopmentHeaderAuthenticationHandler)),
            context);
        return await handler.AuthenticateAsync();
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "BlueprintOS.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class FakeOptionsMonitor : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue { get; } = new();
        public AuthenticationSchemeOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AuthenticationSchemeOptions, string?> listener) => null;
    }
}
