using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.IntegrationTests.Persistence;

public sealed class FornecedorRepositoryIntegrationTests
{
    [Fact]
    public async Task Repository_Should_Persist_Search_And_Isolate_By_Temporary_User()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context); var user = Guid.NewGuid();
        var supplier = new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", 80, user, DateTimeOffset.UtcNow);
        await repository.AdicionarAsync(supplier);
        Assert.True(await repository.ExisteAsync("12345678000195"));
        Assert.Single(await repository.PesquisarAsync("Alpha", user));
        Assert.Empty(await repository.ListarAsync(Guid.NewGuid()));
        await repository.ExcluirAsync(supplier);
        Assert.Empty(await repository.ListarAsync(user));
    }
}
