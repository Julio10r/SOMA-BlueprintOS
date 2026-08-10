using System.Text.RegularExpressions;
using BlueprintOS.Application.Identity.Security;

namespace BlueprintOS.UnitTests.Application.Identity;

public sealed class OtpSecurityTests
{
    [Fact]
    public void OtpCodeGenerator_Should_Produce_Six_Digit_Numeric_Code()
    {
        for (var i = 0; i < 50; i++)
        {
            var codigo = OtpCodeGenerator.Generate();
            Assert.Matches(new Regex("^[0-9]{6}$"), codigo);
        }
    }

    [Fact]
    public void OtpHasher_Should_Verify_Correct_Code()
    {
        var (hash, salt) = OtpHasher.Hash("123456");
        Assert.True(OtpHasher.Verify("123456", hash, salt));
    }

    [Fact]
    public void OtpHasher_Should_Reject_Incorrect_Code()
    {
        var (hash, salt) = OtpHasher.Hash("123456");
        Assert.False(OtpHasher.Verify("654321", hash, salt));
    }

    [Fact]
    public void OtpHasher_Should_Never_Store_Plaintext_In_Hash_Or_Salt()
    {
        var (hash, salt) = OtpHasher.Hash("123456");
        Assert.DoesNotContain("123456", hash);
        Assert.DoesNotContain("123456", salt);
    }

    [Fact]
    public void OpaqueSessionToken_Should_Generate_High_Entropy_Unique_Tokens()
    {
        var a = OpaqueSessionToken.GenerateRawToken();
        var b = OpaqueSessionToken.GenerateRawToken();

        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 32);
    }

    [Fact]
    public void OpaqueSessionToken_Hash_Should_Be_Deterministic_For_Same_Token()
    {
        var token = OpaqueSessionToken.GenerateRawToken();
        Assert.Equal(OpaqueSessionToken.Hash(token), OpaqueSessionToken.Hash(token));
    }

    [Fact]
    public void OpaqueSessionToken_Hash_Should_Never_Contain_Raw_Token()
    {
        var token = OpaqueSessionToken.GenerateRawToken();
        Assert.DoesNotContain(token, OpaqueSessionToken.Hash(token));
    }
}
