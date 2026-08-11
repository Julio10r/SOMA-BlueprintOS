using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BlueprintOS.UnitTests.Infrastructure.Identity;

/// <summary>O1.5 — a composição das permissões efetivas é o coração do RBAC: se esta consulta errar,
/// o enforcement erra. Exercita a consulta REAL do <see cref="PermissoesEfetivasResolver"/> (a mesma
/// classe usada em produção) contra o provider InMemory do EF Core.
///
/// Limitação registrada honestamente: InMemory não é um provider relacional — ele não valida FKs nem
/// índices únicos. O que este teste comprova é a lógica de composição (JOINs, filtro de Perfil ativo,
/// união/deduplicação), não o comportamento de constraints do SQL Server.</summary>
public sealed class PermissoesEfetivasResolverTests
{
    private static readonly Guid Bu = Guid.NewGuid();

    [Fact]
    public async Task Should_Return_Empty_For_User_Without_Any_Perfil()
    {
        await using var db = NovoContexto(out var usuarioId);

        var permissoes = await new PermissoesEfetivasResolver(db).ResolverAsync(usuarioId, Bu, CancellationToken.None);

        Assert.Empty(permissoes);
    }

    [Fact]
    public async Task Should_Return_Empty_For_Unknown_User()
    {
        await using var db = NovoContexto(out _);

        var permissoes = await new PermissoesEfetivasResolver(db).ResolverAsync(Guid.NewGuid(), Bu, CancellationToken.None);

        Assert.Empty(permissoes);
    }

    [Fact]
    public async Task Should_Return_Permissions_Of_A_Single_Perfil()
    {
        await using var db = NovoContexto(out var usuarioId);
        await VincularAsync(db, usuarioId, "Analista Jr", ativo: true, PermissaoCatalogo.PedidoCriar);

        var permissoes = await new PermissoesEfetivasResolver(db).ResolverAsync(usuarioId, Bu, CancellationToken.None);

        Assert.Equal([PermissaoCatalogo.PedidoCriar], permissoes);
    }

    /// <summary>ADR-0020, itens 8/10: as permissões efetivas são a UNIÃO das permissões de todos os Perfis
    /// vinculados.</summary>
    [Fact]
    public async Task Should_Union_Permissions_Across_Multiple_Perfis()
    {
        await using var db = NovoContexto(out var usuarioId);
        await VincularAsync(db, usuarioId, "Analista Jr", ativo: true, PermissaoCatalogo.PedidoCriar);
        await VincularAsync(db, usuarioId, "Aprovador", ativo: true, PermissaoCatalogo.PedidoAprovar, PermissaoCatalogo.PedidoCancelar);

        var permissoes = await new PermissoesEfetivasResolver(db).ResolverAsync(usuarioId, Bu, CancellationToken.None);

        Assert.Equal(
            [PermissaoCatalogo.PedidoAprovar, PermissaoCatalogo.PedidoCancelar, PermissaoCatalogo.PedidoCriar],
            permissoes);
    }

    /// <summary>Permissão presente em dois Perfis aparece uma única vez — a união é um conjunto.</summary>
    [Fact]
    public async Task Should_Deduplicate_Permissions_Shared_By_Two_Perfis()
    {
        await using var db = NovoContexto(out var usuarioId);
        await VincularAsync(db, usuarioId, "Perfil A", ativo: true, PermissaoCatalogo.PedidoCriar, PermissaoCatalogo.PedidoAprovar);
        await VincularAsync(db, usuarioId, "Perfil B", ativo: true, PermissaoCatalogo.PedidoCriar);

        var permissoes = await new PermissoesEfetivasResolver(db).ResolverAsync(usuarioId, Bu, CancellationToken.None);

        Assert.Equal([PermissaoCatalogo.PedidoAprovar, PermissaoCatalogo.PedidoCriar], permissoes);
    }

    /// <summary>Inativar um Perfil é o mecanismo de revogação em massa: ele deixa de contribuir qualquer
    /// permissão, sem que nenhum vínculo precise ser apagado (preserva a auditabilidade).</summary>
    [Fact]
    public async Task Inactive_Perfil_Should_Contribute_No_Permission()
    {
        await using var db = NovoContexto(out var usuarioId);
        await VincularAsync(db, usuarioId, "Perfil Inativo", ativo: false, PermissaoCatalogo.SistemaGerenciar);

        var permissoes = await new PermissoesEfetivasResolver(db).ResolverAsync(usuarioId, Bu, CancellationToken.None);

        Assert.Empty(permissoes);
    }

    [Fact]
    public async Task Should_Keep_Only_Active_Perfil_Permissions_When_User_Has_Both()
    {
        await using var db = NovoContexto(out var usuarioId);
        await VincularAsync(db, usuarioId, "Ativo", ativo: true, PermissaoCatalogo.PedidoCriar);
        await VincularAsync(db, usuarioId, "Inativo", ativo: false, PermissaoCatalogo.SistemaGerenciar);

        var permissoes = await new PermissoesEfetivasResolver(db).ResolverAsync(usuarioId, Bu, CancellationToken.None);

        Assert.Equal([PermissaoCatalogo.PedidoCriar], permissoes);
    }

    /// <summary>Isolamento: o Perfil de outro usuário nunca vaza para este.</summary>
    [Fact]
    public async Task Should_Not_Leak_Permissions_From_Another_User()
    {
        await using var db = NovoContexto(out var usuarioId);
        var outro = new Usuario("outro@example.invalid", "Outro", Bu);
        db.Usuarios.Add(outro);
        await db.SaveChangesAsync();
        await VincularAsync(db, outro.Id, "Administrador", ativo: true, PermissaoCatalogo.PerfilGerenciar);

        var permissoes = await new PermissoesEfetivasResolver(db).ResolverAsync(usuarioId, Bu, CancellationToken.None);

        Assert.Empty(permissoes);
    }

    /// <summary>Perfil vinculado mas sem nenhuma permissão associada não gera nada — e não quebra.</summary>
    [Fact]
    public async Task Perfil_Without_Permissions_Should_Yield_Empty()
    {
        await using var db = NovoContexto(out var usuarioId);
        await VincularAsync(db, usuarioId, "Vazio", ativo: true);

        var permissoes = await new PermissoesEfetivasResolver(db).ResolverAsync(usuarioId, Bu, CancellationToken.None);

        Assert.Empty(permissoes);
    }

    /// <summary>Modelo: não existe (e não deve existir) nenhuma forma de anexar permissão diretamente a um
    /// usuário — a ADR-0020 (itens 7/8/10) proíbe permissão individual. Este teste falha se alguém
    /// introduzir uma entidade desse tipo no modelo EF.</summary>
    [Fact]
    public void Model_Should_Not_Contain_Any_Per_User_Permission_Entity()
    {
        using var db = NovoContexto(out _);

        var suspeitas = db.Model.GetEntityTypes()
            .Select(e => e.ClrType.Name)
            .Where(nome => nome.Contains("Usuario", StringComparison.OrdinalIgnoreCase)
                        && nome.Contains("Permiss", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(suspeitas);
    }

    private static async Task VincularAsync(
        BlueprintOSDbContext db, Guid usuarioId, string nomePerfil, bool ativo, params string[] codigos)
    {
        var perfil = new Perfil(nomePerfil, "Perfil de teste.", Bu, DateTimeOffset.UtcNow);
        if (!ativo) perfil.Inativar(DateTimeOffset.UtcNow);
        db.Perfis.Add(perfil);
        db.UsuariosPerfis.Add(new UsuarioPerfil(usuarioId, perfil.Id));

        foreach (var codigo in codigos)
        {
            var definicao = PermissaoCatalogo.Obter(codigo)!;
            if (!await db.Permissoes.AnyAsync(x => x.Id == definicao.Id))
            {
                db.Permissoes.Add(Permissao.DoCatalogo(definicao));
            }

            db.PerfisPermissoes.Add(new PerfilPermissao(perfil.Id, definicao.Id));
        }

        await db.SaveChangesAsync();
    }

    private static BlueprintOSDbContext NovoContexto(out Guid usuarioId)
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new BlueprintOSDbContext(options);

        var usuario = new Usuario("titular@example.invalid", "Titular", Bu);
        db.Usuarios.Add(usuario);
        db.SaveChanges();
        usuarioId = usuario.Id;
        return db;
    }
}

/// <summary>Garante que o seed do catálogo está declarado no modelo EF — é ele que gera as linhas de
/// `Permissoes` na migration, e sem elas nenhuma permissão pode ser concedida a nenhum Perfil.</summary>
public sealed class PermissaoSeedTests
{
    [Fact]
    public void Model_Should_Seed_The_Whole_Permission_Catalog_With_Stable_Ids()
    {
        // O modelo em runtime do DbContext é "read-optimized" e não guarda seed data. Aplicar a MESMA
        // classe de configuração usada pelo DbContext sobre um ModelBuilder de design-time expõe o seed
        // exatamente como `dotnet ef migrations add` o consome para gerar os INSERTs.
        var modelBuilder = new Microsoft.EntityFrameworkCore.ModelBuilder();
        new BlueprintOS.Infrastructure.Persistence.Configurations.Identity.PermissaoConfiguration().Configure(modelBuilder.Entity<Permissao>());

        var seed = modelBuilder.Model.FindEntityType(typeof(Permissao))!.GetSeedData().ToArray();

        Assert.Equal(PermissaoCatalogo.Todas.Count, seed.Length);
        foreach (var definicao in PermissaoCatalogo.Todas)
        {
            var linha = Assert.Single(seed, x => (Guid)x[nameof(Permissao.Id)]! == definicao.Id);
            Assert.Equal(definicao.Codigo, linha[nameof(Permissao.Codigo)]);
        }
    }
}
