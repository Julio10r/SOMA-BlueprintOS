using BlueprintOS.Infrastructure.Identity;

namespace BlueprintOS.UnitTests.Infrastructure.Identity;

public sealed class CorporateOtpEmailSenderOptionsValidatorTests
{
    [Fact]
    public void Validate_Should_Fail_When_Provider_Is_Not_Configured()
    {
        var validator = new CorporateOtpEmailSenderOptionsValidator();
        var result = validator.Validate(null, new CorporateOtpEmailSenderOptions());

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_Should_Succeed_When_Provider_Is_Configured()
    {
        var validator = new CorporateOtpEmailSenderOptionsValidator();
        var result = validator.Validate(null, new CorporateOtpEmailSenderOptions { Provider = "MicrosoftGraph" });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task UnconfiguredCorporateOtpEmailSender_Should_Always_Throw()
    {
        var sender = new UnconfiguredCorporateOtpEmailSender();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync("ana@somagrupo.com.br", "123456", CancellationToken.None));
    }
}
