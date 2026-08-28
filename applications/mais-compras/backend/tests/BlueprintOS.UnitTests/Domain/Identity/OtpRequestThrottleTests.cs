using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Domain.Identity;

public sealed class OtpRequestThrottleTests
{
    private static readonly TimeSpan Janela = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
    private const int Limite = 3;

    [Fact]
    public void TentarRegistrar_Should_Allow_First_Request()
    {
        var agora = DateTimeOffset.UtcNow;
        var throttle = OtpRequestThrottle.Novo("ana@somagrupo.com.br", agora);

        Assert.True(throttle.TentarRegistrar(agora.AddSeconds(61), Janela, Limite, Cooldown));
    }

    [Fact]
    public void TentarRegistrar_Should_Reject_Within_Cooldown()
    {
        var agora = DateTimeOffset.UtcNow;
        var throttle = OtpRequestThrottle.Novo("ana@somagrupo.com.br", agora);

        Assert.False(throttle.TentarRegistrar(agora.AddSeconds(30), Janela, Limite, Cooldown));
    }

    [Fact]
    public void TentarRegistrar_Should_Reject_After_Limit_Within_Window()
    {
        var agora = DateTimeOffset.UtcNow;
        var throttle = OtpRequestThrottle.Novo("ana@somagrupo.com.br", agora);

        Assert.True(throttle.TentarRegistrar(agora.AddMinutes(1), Janela, Limite, Cooldown));
        Assert.True(throttle.TentarRegistrar(agora.AddMinutes(2), Janela, Limite, Cooldown));
        Assert.False(throttle.TentarRegistrar(agora.AddMinutes(3), Janela, Limite, Cooldown));
    }

    [Fact]
    public void TentarRegistrar_Should_Reset_Window_After_Expiration()
    {
        var agora = DateTimeOffset.UtcNow;
        var throttle = OtpRequestThrottle.Novo("ana@somagrupo.com.br", agora);
        throttle.TentarRegistrar(agora.AddMinutes(1), Janela, Limite, Cooldown);
        throttle.TentarRegistrar(agora.AddMinutes(2), Janela, Limite, Cooldown);

        Assert.True(throttle.TentarRegistrar(agora.AddMinutes(16), Janela, Limite, Cooldown));
    }
}
