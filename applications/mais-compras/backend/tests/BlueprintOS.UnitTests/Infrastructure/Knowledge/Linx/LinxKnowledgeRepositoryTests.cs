using BlueprintOS.Application.Knowledge.Linx.Models;
using BlueprintOS.Domain.Knowledge.Linx;
using BlueprintOS.Infrastructure.Knowledge.Linx;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Infrastructure.Knowledge.Linx;

/// <summary>O1.13.5 — persistência real (EF Core InMemory) de <see cref="LinxKnowledgeRepository"/>: cada
/// versão é uma linha própria, o histórico nunca é perdido, e a busca nunca mistura versões obsoletas com
/// a versão atual.</summary>
public sealed class LinxKnowledgeRepositoryTests
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.Parse("2026-08-11T12:00:00Z");

    private static BlueprintOSDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(dbName).Options);

    private static LinxKnowledgeEntry NovaEntrada(Guid? unidadeNegocioId = null, string conteudo = "COD_CLIFOR identifica o fornecedor.") =>
        LinxKnowledgeEntry.Criar(
            LinxEspecialista.LinxDatabaseSpecialist, LinxConhecimentoCategoria.SchemaTabelaColuna,
            "Estrutura de Fornecedor", conteudo, LinxConhecimentoProveniencia.Descoberto,
            "SomaFornecedorReader", "agent", unidadeNegocioId, ["fornecedor"], Agora);

    [Fact]
    public async Task AdicionarAsync_Should_Persist_The_Entry()
    {
        var dbName = Guid.NewGuid().ToString();
        var entrada = NovaEntrada();

        await using (var db = CreateContext(dbName))
        {
            await new LinxKnowledgeRepository(db).AdicionarAsync(entrada, CancellationToken.None);
        }

        await using var verificacao = CreateContext(dbName);
        var persistida = await verificacao.LinxConhecimentoEntradas.FirstOrDefaultAsync(x => x.Id == entrada.Id);
        Assert.NotNull(persistida);
        Assert.Equal(entrada.Conteudo, persistida!.Conteudo);
        Assert.Equal(entrada.Proveniencia, persistida.Proveniencia);
    }

    [Fact]
    public async Task NovaVersao_Should_Add_A_New_Row_Never_Replacing_The_Previous_One()
    {
        var dbName = Guid.NewGuid().ToString();
        var v1 = NovaEntrada();
        var v2 = v1.NovaVersao("Refinamento: também indica o tipo de pessoa.", LinxConhecimentoProveniencia.Inferido, "nova fonte", "agent", null, Agora.AddDays(1));

        await using (var db = CreateContext(dbName))
        {
            var repo = new LinxKnowledgeRepository(db);
            await repo.AdicionarAsync(v1, CancellationToken.None);
            await repo.AdicionarAsync(v2, CancellationToken.None);
        }

        await using var verificacao = CreateContext(dbName);
        var linhas = await verificacao.LinxConhecimentoEntradas.Where(x => x.VersaoRaizId == v1.VersaoRaizId).ToListAsync();
        Assert.Equal(2, linhas.Count);
        Assert.Contains(linhas, x => x.Id == v1.Id && x.Versao == 1);
        Assert.Contains(linhas, x => x.Id == v2.Id && x.Versao == 2);
    }

    [Fact]
    public async Task ObterUltimaVersaoAsync_Should_Return_The_Highest_Version()
    {
        var dbName = Guid.NewGuid().ToString();
        var v1 = NovaEntrada();
        var v2 = v1.NovaVersao("v2", LinxConhecimentoProveniencia.Inferido, "fonte", "agent", null, Agora.AddDays(1));
        var v3 = v2.NovaVersao("v3", LinxConhecimentoProveniencia.Descoberto, "fonte", "agent", null, Agora.AddDays(2));

        await using var db = CreateContext(dbName);
        var repo = new LinxKnowledgeRepository(db);
        await repo.AdicionarAsync(v1, CancellationToken.None);
        await repo.AdicionarAsync(v2, CancellationToken.None);
        await repo.AdicionarAsync(v3, CancellationToken.None);

        var ultima = await repo.ObterUltimaVersaoAsync(v1.VersaoRaizId, CancellationToken.None);

        Assert.NotNull(ultima);
        Assert.Equal(v3.Id, ultima!.Id);
        Assert.Equal(3, ultima.Versao);
    }

    [Fact]
    public async Task ObterHistoricoAsync_Should_Return_Every_Version_In_Ascending_Order()
    {
        var dbName = Guid.NewGuid().ToString();
        var v1 = NovaEntrada();
        var v2 = v1.NovaVersao("v2", LinxConhecimentoProveniencia.Inferido, "fonte", "agent", null, Agora.AddDays(1));

        await using var db = CreateContext(dbName);
        var repo = new LinxKnowledgeRepository(db);
        await repo.AdicionarAsync(v1, CancellationToken.None);
        await repo.AdicionarAsync(v2, CancellationToken.None);

        var historico = await repo.ObterHistoricoAsync(v1.VersaoRaizId, CancellationToken.None);

        Assert.Equal(2, historico.Count);
        Assert.Equal(1, historico[0].Versao);
        Assert.Equal(2, historico[1].Versao);
    }

    [Fact]
    public async Task BuscarUltimasVersoesAsync_Should_Never_Return_An_Obsolete_Version_Alongside_The_Current_One()
    {
        var dbName = Guid.NewGuid().ToString();
        var v1 = NovaEntrada(conteudo: "conteúdo antigo");
        var v2 = v1.NovaVersao("conteúdo atual", LinxConhecimentoProveniencia.Inferido, "fonte", "agent", null, Agora.AddDays(1));

        await using var db = CreateContext(dbName);
        var repo = new LinxKnowledgeRepository(db);
        await repo.AdicionarAsync(v1, CancellationToken.None);
        await repo.AdicionarAsync(v2, CancellationToken.None);

        var resultados = await repo.BuscarUltimasVersoesAsync(new LinxKnowledgeFiltro(), CancellationToken.None);

        var doGrupo = resultados.Where(x => x.VersaoRaizId == v1.VersaoRaizId).ToArray();
        Assert.Single(doGrupo);
        Assert.Equal(v2.Id, doGrupo[0].Id);
    }

    [Fact]
    public async Task BuscarUltimasVersoesAsync_Should_Scope_By_UnidadeNegocio_Including_Global_Entries()
    {
        var dbName = Guid.NewGuid().ToString();
        var bu = Guid.NewGuid();
        var outraBu = Guid.NewGuid();

        await using var db = CreateContext(dbName);
        var repo = new LinxKnowledgeRepository(db);
        await repo.AdicionarAsync(NovaEntrada(bu, "config da BU"), CancellationToken.None);
        await repo.AdicionarAsync(NovaEntrada(outraBu, "config da OUTRA BU"), CancellationToken.None);
        await repo.AdicionarAsync(NovaEntrada(null, "conceito global"), CancellationToken.None);

        var resultados = await repo.BuscarUltimasVersoesAsync(
            new LinxKnowledgeFiltro(UnidadeNegocioId: bu), CancellationToken.None);

        Assert.Equal(2, resultados.Count);
        Assert.DoesNotContain(resultados, x => x.Conteudo.Contains("OUTRA BU"));
    }

    [Fact]
    public async Task AtualizarProvenienciaAsync_Should_Persist_The_Promotion()
    {
        var dbName = Guid.NewGuid().ToString();
        var entrada = NovaEntrada();

        await using (var db = CreateContext(dbName))
        {
            await new LinxKnowledgeRepository(db).AdicionarAsync(entrada, CancellationToken.None);
        }

        entrada.Promover(LinxConhecimentoProveniencia.Validado, "revisor", Agora.AddHours(1));

        await using (var db = CreateContext(dbName))
        {
            await new LinxKnowledgeRepository(db).AtualizarProvenienciaAsync(entrada, CancellationToken.None);
        }

        await using var verificacao = CreateContext(dbName);
        var persistida = await verificacao.LinxConhecimentoEntradas.FirstAsync(x => x.Id == entrada.Id);
        Assert.Equal(LinxConhecimentoProveniencia.Validado, persistida.Proveniencia);
    }
}
