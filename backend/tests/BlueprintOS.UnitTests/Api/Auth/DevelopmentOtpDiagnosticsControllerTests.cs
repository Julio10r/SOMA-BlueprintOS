using System.Net;
using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Auth;

/// <summary>Defesa redundante interna do /dev/otp (O1.4.2.1, Etapa 4/Achado D) — o handler nega mesmo se
/// mapeado incorretamente fora de Development, e mesmo em Development nega origem não-loopback.</summary>
public sealed class DevelopmentOtpDiagnosticsControllerTests
{
    [Fact]
    public async Task Should_Deny_When_Environment_Is_Not_Development_Even_If_Mapped()
    {
        var statusCode = await ExecuteAsync(IPAddress.Loopback, Environments.Production, "ana@somagrupo.com.br");
        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
    }

    [Fact]
    public async Task Should_Deny_When_Remote_Address_Is_Not_Loopback()
    {
        var statusCode = await ExecuteAsync(IPAddress.Parse("203.0.113.10"), Environments.Development, "ana@somagrupo.com.br");
        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
    }

    [Fact]
    public async Task Should_Allow_When_Development_And_Loopback()
    {
        var statusCode = await ExecuteAsync(IPAddress.Loopback, Environments.Development, "ana@somagrupo.com.br");
        Assert.Equal(StatusCodes.Status200OK, statusCode);
    }

    [Fact]
    public async Task Should_Allow_When_Development_And_IPv6_Loopback()
    {
        var statusCode = await ExecuteAsync(IPAddress.IPv6Loopback, Environments.Development, "ana@somagrupo.com.br");
        Assert.Equal(StatusCodes.Status200OK, statusCode);
    }

    private static async Task<int> ExecuteAsync(IPAddress remoteIp, string environmentName, string email)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };
        context.Connection.RemoteIpAddress = remoteIp;
        context.Response.Body = new MemoryStream();

        var store = new DevelopmentOtpInspectionStore(TimeProvider.System);
        store.Store(email, "123456");

        var result = DevelopmentOtpDiagnosticsController.GetLastOtp(email, context, store, new FakeHostEnvironment(environmentName));
        await result.ExecuteAsync(context);
        return context.Response.StatusCode;
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "BlueprintOS.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
