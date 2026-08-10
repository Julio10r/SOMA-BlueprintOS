using BlueprintOS.Api.Identity;
using Microsoft.AspNetCore.Http;

namespace BlueprintOS.UnitTests.Api.Identity;

public sealed class AuthCookieTests
{
    [Fact]
    public void BuildOptions_Should_Set_Expected_Security_Flags()
    {
        var options = AuthCookie.BuildOptions(TimeSpan.FromHours(12));

        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.Strict, options.SameSite);
        Assert.Equal("/", options.Path);
    }

    [Fact]
    public void BuildDeleteOptions_Should_Also_Set_Expected_Security_Flags()
    {
        var options = AuthCookie.BuildDeleteOptions();

        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.Strict, options.SameSite);
    }
}
