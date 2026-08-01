using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence;

public sealed class BlueprintOSDbContext(DbContextOptions<BlueprintOSDbContext> options) : DbContext(options)
{
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<FornecedorDescoberto> FornecedoresDescobertos => Set<FornecedorDescoberto>();
    public DbSet<FornecedorSincronizacao> FornecedoresSincronizacoes => Set<FornecedorSincronizacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BlueprintOSDbContext).Assembly);
    }
}
