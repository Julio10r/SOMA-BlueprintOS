using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Identity.Security;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>Testes de concorrência REAL (O1.4.3.2; Work Order O1.4.3, seção 18, itens 12/22) — mesmo padrão
/// de <c>AuthConcurrencyTests</c> (O1.4.2.1): provider InMemory do EF Core com múltiplas instâncias de
/// <see cref="BlueprintOSDbContext"/> compartilhando o mesmo banco nomeado, exercitando o compare-and-swap
/// real de <see cref="BootstrapEstado.RowVersion"/> — nenhum fake sequencial, para provar que exatamente uma
/// conclusão sobrevive e nenhuma entidade órfã (Usuario/UnidadeNegocio/Perfil/UsuarioPerfil) persiste na
/// perdedora.</summary>
public sealed class ConcluirBootstrapConcurrencyTests
{
    private const string EmailCandidato = "admin.inicial@example.invalid";

    [Fact]
    public async Task Concluir_Concurrent_Should_Yield_Exactly_One_Success_And_No_Orphan_Entities()
    {
        var dbName = Guid.NewGuid().ToString();
        var rawToken = "token-de-conclusao-concorrente";
        var sessaoId = await SeedEstadoESessaoAsync(dbName, rawToken);

        var unidadeNegocio = new UnidadeNegocioBootstrapPayload(null, "SOMA Matriz", "soma-matriz-concorrencia");
        var administrador = new AdministradorSeniorBootstrapPayload("Administradora Sênior");

        Task<ConcluirBootstrapResultado> ExecutarAsync()
        {
            var useCase = CreateUseCase(dbName);
            return useCase.ExecuteAsync(sessaoId, unidadeNegocio, administrador, CancellationToken.None);
        }

        var resultados = await Task.WhenAll(ExecutarAsync(), ExecutarAsync());

        Assert.Single(resultados, r => r.Sucesso);
        Assert.Single(resultados, r => !r.Sucesso);

        await using var assertDb = CreateContext(dbName);
        Assert.Equal(1, await assertDb.Usuarios.CountAsync());
        Assert.Equal(1, await assertDb.UnidadesNegocio.CountAsync());
        Assert.Equal(1, await assertDb.Perfis.CountAsync());
        Assert.Equal(1, await assertDb.UsuariosPerfis.CountAsync());

        var estadoFinal = await assertDb.BootstrapEstados.SingleAsync(x => x.Id == BootstrapEstado.IdFixo);
        Assert.True(estadoFinal.Concluido);
        Assert.NotNull(estadoFinal.UsuarioAdministradorSeniorId);
    }

    [Fact]
    public async Task Concluir_Should_Reject_Second_Attempt_After_Already_Concluded()
    {
        var dbName = Guid.NewGuid().ToString();
        var sessaoId = await SeedEstadoESessaoAsync(dbName, "token-primeira-tentativa");

        var unidadeNegocio = new UnidadeNegocioBootstrapPayload(null, "SOMA Matriz", "soma-matriz-segunda-tentativa");
        var administrador = new AdministradorSeniorBootstrapPayload("Administradora Sênior");

        var primeira = await CreateUseCase(dbName).ExecuteAsync(sessaoId, unidadeNegocio, administrador, CancellationToken.None);
        Assert.True(primeira.Sucesso);

        // Segunda sessão (uso único já consumiu a primeira) simulando uma tentativa de reabertura.
        var segundaSessaoId = await AdicionarSessaoAsync(dbName, "token-segunda-tentativa");
        var segunda = await CreateUseCase(dbName).ExecuteAsync(segundaSessaoId, unidadeNegocio, administrador, CancellationToken.None);

        Assert.False(segunda.Sucesso);

        await using var assertDb = CreateContext(dbName);
        Assert.Equal(1, await assertDb.Usuarios.CountAsync());
    }

    private static async Task<Guid> SeedEstadoESessaoAsync(string dbName, string rawToken)
    {
        await using var db = CreateContext(dbName);
        db.BootstrapEstados.Add(BootstrapEstado.CriarInicial());
        await db.SaveChangesAsync();
        return await AdicionarSessaoAsync(dbName, rawToken);
    }

    private static async Task<Guid> AdicionarSessaoAsync(string dbName, string rawToken)
    {
        await using var db = CreateContext(dbName);
        var sessao = new BootstrapSessao(EmailCandidato, OpaqueSessionToken.Hash(rawToken), DateTimeOffset.UtcNow);
        db.BootstrapSessoes.Add(sessao);
        await db.SaveChangesAsync();
        return sessao.Id;
    }

    private static ConcluirBootstrapUseCase CreateUseCase(string dbName)
    {
        var db = CreateContext(dbName);
        return new ConcluirBootstrapUseCase(
            new BootstrapEstadoRepository(db),
            new BootstrapSessaoRepository(db),
            new UnidadeNegocioRepository(db),
            new UsuarioRepository(db),
            new PerfilRepository(db),
            new PermissaoRepository(db),
            new UsuarioPerfilRepository(db),
            TimeProvider.System,
            NullLogger<ConcluirBootstrapUseCase>.Instance);
    }

    private static BlueprintOSDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new BlueprintOSDbContext(options);
    }
}
