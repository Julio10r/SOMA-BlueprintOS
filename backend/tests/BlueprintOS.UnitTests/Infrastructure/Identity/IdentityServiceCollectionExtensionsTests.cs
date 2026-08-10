using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Infrastructure.DependencyInjection;
using BlueprintOS.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.UnitTests.Infrastructure.Identity;

/// <summary>Cobre a seleção exclusiva por ambiente do IOtpEmailSender feita na composição raiz
/// (Program.cs): reproduz aqui os dois ramos que lá são condicionados a IHostEnvironment.IsDevelopment().</summary>
public sealed class IdentityServiceCollectionExtensionsTests
{
    [Fact]
    public void Development_Branch_Should_Register_Exactly_One_Development_Sender()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddIdentityAuthCore(configuration);
        services.AddSingleton<DevelopmentOtpInspectionStore>();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        services.AddScoped<IOtpEmailSender, DevelopmentOtpEmailSender>();

        var provider = services.BuildServiceProvider();
        var senders = provider.GetServices<IOtpEmailSender>().ToList();

        Assert.Single(senders);
        Assert.IsType<DevelopmentOtpEmailSender>(senders[0]);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "BlueprintOS.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public void NonDevelopment_Branch_Should_Register_Exactly_One_Unconfigured_Corporate_Sender()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddIdentityAuthCore(configuration);
        services.AddUnconfiguredCorporateOtpEmailSender(configuration);

        var provider = services.BuildServiceProvider();
        var senders = provider.GetServices<IOtpEmailSender>().ToList();

        Assert.Single(senders);
        Assert.IsType<UnconfiguredCorporateOtpEmailSender>(senders[0]);
    }
}
