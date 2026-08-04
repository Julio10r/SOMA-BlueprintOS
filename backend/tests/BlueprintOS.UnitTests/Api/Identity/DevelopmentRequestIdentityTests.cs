using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Identity;

public sealed class DevelopmentRequestIdentityTests
{
    [Fact]
    public void GetRequired_Should_Return_Identity_In_Development()
    {
        var context = new DefaultHttpContext();
        var userId = Guid.NewGuid();
        context.Request.Headers["X-Development-User-Id"] = userId.ToString();
        var adapter = CreateAdapter(context, Environments.Development);

        var identity = adapter.GetRequired();

        Assert.Equal(userId, identity.UserId);
        Assert.Equal("Buyer", identity.Role);
    }

    [Fact]
    public void GetRequired_Should_Fail_Safely_Outside_Development()
    {
        var adapter = CreateAdapter(new DefaultHttpContext(), Environments.Production);

        var exception = Assert.Throws<IdentityUnavailableException>(adapter.GetRequired);

        Assert.True(exception.IsEnvironmentFailure);
    }

    private static DevelopmentRequestIdentity CreateAdapter(HttpContext context, string environmentName)
    {
        var accessor = new HttpContextAccessor { HttpContext = context };
        var environment = new FakeHostEnvironment(environmentName);
        return new DevelopmentRequestIdentity(accessor, environment);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "BlueprintOS.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
