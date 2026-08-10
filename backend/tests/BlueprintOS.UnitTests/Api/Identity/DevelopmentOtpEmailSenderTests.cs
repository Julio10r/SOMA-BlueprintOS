using BlueprintOS.Api.Identity;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Api.Identity;

public sealed class DevelopmentOtpEmailSenderTests
{
    [Fact]
    public async Task SendAsync_Should_Store_Code_In_Development()
    {
        var store = new DevelopmentOtpInspectionStore(TimeProvider.System);
        var sender = new DevelopmentOtpEmailSender(store, new FakeHostEnvironment(Environments.Development));

        var resultado = await sender.SendAsync("ana@somagrupo.com.br", "123456", CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.True(store.TryTakeOnce("ana@somagrupo.com.br", out var codigo));
        Assert.Equal("123456", codigo);
    }

    [Fact]
    public async Task SendAsync_Should_Throw_Outside_Development()
    {
        var store = new DevelopmentOtpInspectionStore(TimeProvider.System);
        var sender = new DevelopmentOtpEmailSender(store, new FakeHostEnvironment(Environments.Production));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync("ana@somagrupo.com.br", "123456", CancellationToken.None));
    }

    [Fact]
    public void TryTakeOnce_Should_Be_Single_Use()
    {
        var store = new DevelopmentOtpInspectionStore(TimeProvider.System);
        store.Store("ana@somagrupo.com.br", "654321");

        Assert.True(store.TryTakeOnce("ana@somagrupo.com.br", out _));
        Assert.False(store.TryTakeOnce("ana@somagrupo.com.br", out _));
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "BlueprintOS.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
