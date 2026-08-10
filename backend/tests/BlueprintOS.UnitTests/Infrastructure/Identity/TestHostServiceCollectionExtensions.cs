using BlueprintOS.Infrastructure.DependencyInjection;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlueprintOS.UnitTests.Infrastructure.Identity;

/// <summary>Os testes de <c>FailClosedHostStartupTests</c>/<c>BootstrapSecretOptionsValidatorTests</c>
/// constroem um <see cref="IHost"/> real via <c>Host.CreateDefaultBuilder()</c>, que por padrão habilita
/// <c>ServiceProviderOptions.ValidateOnBuild</c> — logo <c>Build()</c> falha se algum serviço registrado por
/// <c>AddIdentityAuthCore</c> (repositórios que dependem de <see cref="BlueprintOSDbContext"/>) não puder ser
/// resolvido, mesmo antes de qualquer verificação de Options. Este helper registra um DbContext InMemory
/// só para satisfazer essa validação estrutural do host — não participa da lógica de negócio testada.</summary>
internal static class TestHostServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityAuthCoreWithTestDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BlueprintOSDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityAuthCore(configuration);
        return services;
    }
}
