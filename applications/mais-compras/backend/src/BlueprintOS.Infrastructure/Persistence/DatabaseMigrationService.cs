using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueprintOS.Infrastructure.Persistence;

/// <summary>Applies pending migrations only after a successful connection to an existing SQL Server database.</summary>
public static class DatabaseMigrationService
{
    public static async Task ValidateMaisComprasConnectivityAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BlueprintOSDbContext>();
        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            throw new InvalidOperationException("The configured +Compras SQL Server database is unreachable.");
        }
    }

    public static async Task ApplyPendingMigrationsAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BlueprintOSDbContext>();
        if (!await context.Database.CanConnectAsync(cancellationToken)) throw new InvalidOperationException("The configured +Compras SQL Server database is unreachable. No migration was applied.");

        await context.Database.MigrateAsync(cancellationToken);
    }
}
