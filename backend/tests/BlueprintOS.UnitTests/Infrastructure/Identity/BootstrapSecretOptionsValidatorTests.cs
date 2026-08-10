using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Identity;

/// <summary>Itens 18/20 do plano de testes da Work Order O1.4.3 (seção 18) — fail-closed de
/// <c>BootstrapSecretOptions</c> ausente fora de Development. Mesmo padrão de rigor de
/// <c>FailClosedHostStartupTests</c> (O1.4.2.1, Etapa 5): constrói um <see cref="IHost"/> real e chama
/// <c>StartAsync()</c> — o mesmo mecanismo que <c>ValidateOnStart()</c> aciona em produção.</summary>
public sealed class BootstrapSecretOptionsValidatorTests
{
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Host_Should_Fail_To_Start_Outside_Development_Without_Bootstrap_Secret(string environmentName)
    {
        using var host = Host.CreateDefaultBuilder()
            .UseEnvironment(environmentName)
            .ConfigureServices((context, services) => services.AddIdentityAuthCore(context.Configuration))
            .Build();

        var exception = await Record.ExceptionAsync(() => host.StartAsync());

        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Host_Should_Start_Outside_Development_When_Bootstrap_Secret_Is_Configured(string environmentName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Bootstrap:Secret"] = "um-segredo-de-alta-entropia-qualquer" })
            .Build();

        using var host = Host.CreateDefaultBuilder()
            .UseEnvironment(environmentName)
            .ConfigureServices(services => services.AddIdentityAuthCore(configuration))
            .Build();

        var exception = await Record.ExceptionAsync(() => host.StartAsync());

        Assert.Null(exception);
        await host.StopAsync();
    }

    [Fact]
    public async Task Host_Should_Start_In_Development_Even_Without_Bootstrap_Secret()
    {
        using var host = Host.CreateDefaultBuilder()
            .UseEnvironment(Environments.Development)
            .ConfigureServices((context, services) =>
            {
                services.AddIdentityAuthCoreWithTestDbContext(context.Configuration);
                // AddIdentityAuthCore não registra nenhum IOtpEmailSender por si só — em produção, a
                // composição raiz (Program.cs) escolhe o sender por ambiente. Reproduz aqui a escolha
                // real para Development.
                services.AddSingleton<DevelopmentOtpInspectionStore>();
                services.AddScoped<IOtpEmailSender, DevelopmentOtpEmailSender>();
            })
            .Build();

        var exception = await Record.ExceptionAsync(() => host.StartAsync());

        Assert.Null(exception);
        await host.StopAsync();
    }
}
