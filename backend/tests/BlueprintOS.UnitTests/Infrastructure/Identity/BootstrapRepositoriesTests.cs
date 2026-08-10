using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Infrastructure.Identity;

/// <summary>Testes de integração leves (EF Core InMemory) das implementações reais de
/// <see cref="IBootstrapEstadoRepository"/>/<see cref="IBootstrapSessaoRepository"/> criadas em O1.4.3.1.</summary>
public sealed class BootstrapRepositoriesTests
{
    [Fact]
    public async Task BootstrapEstadoRepository_Should_Return_Null_When_Row_Is_Absent()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var repo = new BootstrapEstadoRepository(db);

        var estado = await repo.ObterAsync(CancellationToken.None);

        Assert.Null(estado);
    }

    [Fact]
    public async Task BootstrapEstadoRepository_Should_Return_Seeded_Row_By_Fixed_Id()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var setupDb = CreateContext(dbName))
        {
            setupDb.BootstrapEstados.Add(BootstrapEstado.CriarInicial());
            await setupDb.SaveChangesAsync();
        }

        await using var db = CreateContext(dbName);
        var repo = new BootstrapEstadoRepository(db);
        var estado = await repo.ObterAsync(CancellationToken.None);

        Assert.NotNull(estado);
        Assert.Equal(BootstrapEstado.IdFixo, estado!.Id);
        Assert.False(estado.Concluido);
    }

    [Fact]
    public async Task BootstrapSessaoRepository_Should_Roundtrip_By_IdentificadorHash()
    {
        var dbName = Guid.NewGuid().ToString();
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", "hash-de-teste", DateTimeOffset.UtcNow);

        await using (var setupDb = CreateContext(dbName))
        {
            var repo = new BootstrapSessaoRepository(setupDb);
            await repo.AdicionarAsync(sessao, CancellationToken.None);
            await repo.SalvarAlteracoesAsync(CancellationToken.None);
        }

        await using var db = CreateContext(dbName);
        var readRepo = new BootstrapSessaoRepository(db);
        var encontrada = await readRepo.ObterPorIdentificadorHashAsync("hash-de-teste", CancellationToken.None);

        Assert.NotNull(encontrada);
        Assert.Equal(sessao.Id, encontrada!.Id);
    }

    [Fact]
    public async Task BootstrapSessaoRepository_Should_Not_Return_Used_Or_Revoked_Session_As_Active_For_Same_Email()
    {
        var dbName = Guid.NewGuid().ToString();
        var agora = DateTimeOffset.UtcNow;
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", "hash-de-teste-2", agora);
        sessao.MarcarUsada(agora);

        await using (var setupDb = CreateContext(dbName))
        {
            setupDb.BootstrapSessoes.Add(sessao);
            await setupDb.SaveChangesAsync();
        }

        await using var db = CreateContext(dbName);
        var repo = new BootstrapSessaoRepository(db);
        var ativa = await repo.ObterAtivaPorEmailCandidatoAsync("candidato@somagrupo.com.br", CancellationToken.None);

        Assert.Null(ativa);
    }

    private static BlueprintOSDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new BlueprintOSDbContext(options);
    }
}
