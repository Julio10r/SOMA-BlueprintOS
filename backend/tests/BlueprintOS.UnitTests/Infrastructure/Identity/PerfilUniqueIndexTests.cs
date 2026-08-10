using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BlueprintOS.UnitTests.Infrastructure.Identity;

/// <summary>Fecha a divergência nº 4 da Work Order O1.4.3 (seção 4/12) — índice único de <c>Perfil</c> por
/// (<c>UnidadeNegocioId</c>, <c>Nome</c>). O provider InMemory do EF Core NÃO aplica índices/constraints
/// únicos relacionais (é um provider não relacional) — a suposição anterior de que ele rejeitaria a
/// duplicidade com <c>DbUpdateException</c> era falsa; <c>SaveChangesAsync()</c> simplesmente aceita as
/// duas linhas. Esta suíte, portanto, valida a DECLARAÇÃO do índice no modelo EF (que é o que de fato
/// gera a constraint única no SQL Server via migration) em vez de depender do InMemory para "aplicar" uma
/// regra que ele não aplica. A garantia de enforcement em runtime é responsabilidade do SQL Server/da
/// migration gerada — não comprovada por este teste unitário.</summary>
public sealed class PerfilUniqueIndexTests
{
    [Fact]
    public void Should_Declare_Unique_Index_On_UnidadeNegocioId_And_Nome()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());

        var index = db.Model.FindEntityType(typeof(Perfil))!
            .GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Perfil.UnidadeNegocioId), nameof(Perfil.Nome) }));

        Assert.True(index.IsUnique);
        Assert.Null(index.GetFilter());
    }

    [Fact]
    public async Task Should_Allow_Same_Perfil_Name_In_Different_UnidadesNegocio()
    {
        var dbName = Guid.NewGuid().ToString();

        await using var db = CreateContext(dbName);
        db.Perfis.Add(new Perfil(Perfil.AdministradorSenior, Guid.NewGuid()));
        db.Perfis.Add(new Perfil(Perfil.AdministradorSenior, Guid.NewGuid()));

        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Perfis.CountAsync());
    }

    private static BlueprintOSDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new BlueprintOSDbContext(options);
    }
}
