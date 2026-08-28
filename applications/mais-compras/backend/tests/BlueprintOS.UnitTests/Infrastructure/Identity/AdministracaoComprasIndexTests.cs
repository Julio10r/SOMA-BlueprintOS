using System.Linq;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Infrastructure.Identity;

/// <summary>O1.12 — valida a DECLARAÇÃO dos índices de escopo por Unidade de Negócio no modelo EF para
/// <see cref="RegraWorkflow"/>, <see cref="AlcadaAprovacao"/> e <see cref="RegraOrcamentaria"/> (mesmo
/// raciocínio de <c>PerfilUniqueIndexTests</c>: o InMemory provider não aplica constraints relacionais —
/// a garantia real de enforcement fica com o SQL Server/a migration gerada).</summary>
public sealed class AdministracaoComprasIndexTests
{
    [Fact]
    public void RegraWorkflow_Should_Declare_Index_On_UnidadeNegocioId()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());

        var index = db.Model.FindEntityType(typeof(RegraWorkflow))!
            .GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RegraWorkflow.UnidadeNegocioId) }));

        Assert.False(index.IsUnique);
    }

    [Fact]
    public void AlcadaAprovacao_Should_Declare_Index_On_UnidadeNegocioId()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());

        var index = db.Model.FindEntityType(typeof(AlcadaAprovacao))!
            .GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(AlcadaAprovacao.UnidadeNegocioId) }));

        Assert.False(index.IsUnique);
    }

    [Fact]
    public void RegraOrcamentaria_Should_Declare_Composite_Index_On_UnidadeNegocio_CentroCusto_Periodo()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());

        var index = db.Model.FindEntityType(typeof(RegraOrcamentaria))!
            .GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(RegraOrcamentaria.UnidadeNegocioId),
                nameof(RegraOrcamentaria.CentroCustoMetadadoId),
                nameof(RegraOrcamentaria.Periodo),
            }));

        Assert.False(index.IsUnique);
    }

    [Fact]
    public async Task Should_Persist_And_Reload_AlcadaAprovacao_With_Nullable_Approver_Fields()
    {
        var dbName = Guid.NewGuid().ToString();
        var unidadeNegocioId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        await using (var db = CreateContext(dbName))
        {
            db.AlcadasAprovacao.Add(new AlcadaAprovacao(
                "Alçada", unidadeNegocioId, CriterioAlcada.Valor, 0m, 1000m, null, 1, usuarioId, null, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext(dbName))
        {
            var alcada = await db.AlcadasAprovacao.SingleAsync();
            Assert.Equal(usuarioId, alcada.AprovadorUsuarioId);
            Assert.Null(alcada.AprovadorPerfilId);
            Assert.Null(alcada.CentroCustoMetadadoId);
        }
    }

    private static BlueprintOSDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new BlueprintOSDbContext(options);
    }
}
