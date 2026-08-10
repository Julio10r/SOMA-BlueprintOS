using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Infrastructure.DependencyInjection;
using BlueprintOS.Infrastructure.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Identity;

/// <summary>Fail-closed real (O1.4.2.1, Etapa 5 — MÉDIO/N da Security Validation): a Security Validation
/// apontou que os testes anteriores só verificavam o TIPO registrado via <c>BuildServiceProvider()</c>,
/// nunca comprovando que o host de fato falha ao iniciar. Estes testes constroem um
/// <see cref="IHost"/> real (via <c>Host.CreateDefaultBuilder()</c>) e chamam <c>StartAsync()</c> — o
/// mesmo mecanismo que <c>ValidateOnStart()</c> aciona em produção. Não requerem banco de dados: o
/// registro exercitado aqui é apenas o do provider de e-mail corporativo, anterior a qualquer
/// <c>AddInfrastructure</c>/conexão real.</summary>
public sealed class FailClosedHostStartupTests
{
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Host_Should_Fail_To_Start_Outside_Development_Without_Corporate_Provider(string environmentName)
    {
        // Bootstrap:Secret é fornecido para isolar exclusivamente a ausência do Corporate OTP
        // Provider — este teste não deve depender de nenhum outro requisito fail-closed.
        var configuration = ValidBootstrapSecretConfiguration();

        using var host = Host.CreateDefaultBuilder()
            .UseEnvironment(environmentName)
            .ConfigureServices(services =>
            {
                services.AddIdentityAuthCoreWithTestDbContext(configuration);
                services.AddUnconfiguredCorporateOtpEmailSender(configuration);
            })
            .Build();

        var exception = await Record.ExceptionAsync(() => host.StartAsync());

        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Host_Should_Start_Outside_Development_When_Corporate_Provider_Is_Configured(string environmentName)
    {
        // Provar que o Corporate Provider permite o startup exige também satisfazer os demais
        // requisitos fail-closed independentes (Bootstrap:Secret) — senão o teste estaria provando
        // uma propriedade que seu nome não declara.
        var configuration = ValidBootstrapSecretConfiguration();

        using var host = Host.CreateDefaultBuilder()
            .UseEnvironment(environmentName)
            .ConfigureServices(services =>
            {
                services.AddIdentityAuthCoreWithTestDbContext(configuration);
                services.AddUnconfiguredCorporateOtpEmailSender(configuration);
                // Simula a configuração real (Identity:Otp:Corporate:Provider) sem exigir um provedor
                // de configuração em memória adicional — reconfigura as Options após o Bind() inicial.
                services.PostConfigure<CorporateOtpEmailSenderOptions>(o => o.Provider = "MicrosoftGraph");
            })
            .Build();

        var exception = await Record.ExceptionAsync(() => host.StartAsync());

        Assert.Null(exception);
        await host.StopAsync();
    }

    [Fact]
    public async Task Host_Should_Never_Fall_Back_To_DevelopmentOtpEmailSender_Outside_Development()
    {
        var configuration = ValidBootstrapSecretConfiguration();

        using var host = Host.CreateDefaultBuilder()
            .UseEnvironment("Production")
            .ConfigureServices(services =>
            {
                services.AddIdentityAuthCoreWithTestDbContext(configuration);
                services.AddUnconfiguredCorporateOtpEmailSender(configuration);
                // Simula a configuração real (Identity:Otp:Corporate:Provider) sem exigir um provedor
                // de configuração em memória adicional — reconfigura as Options após o Bind() inicial.
                services.PostConfigure<CorporateOtpEmailSenderOptions>(o => o.Provider = "MicrosoftGraph");
            })
            .Build();

        await host.StartAsync();

        var sender = host.Services.GetRequiredService<IOtpEmailSender>();
        Assert.IsType<UnconfiguredCorporateOtpEmailSender>(sender);
        Assert.IsNotType<DevelopmentOtpEmailSender>(sender);

        await host.StopAsync();
    }

    private static IConfiguration ValidBootstrapSecretConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Bootstrap:Secret"] = "um-segredo-de-alta-entropia-qualquer" })
            .Build();
}
