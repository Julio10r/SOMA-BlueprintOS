using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class PerfilRepository(BlueprintOSDbContext db) : IPerfilRepository
{
    public Task<Perfil?> ObterPorNomeEUnidadeNegocioAsync(string nome, Guid unidadeNegocioId, CancellationToken ct) =>
        db.Perfis.SingleOrDefaultAsync(x => x.Nome == nome && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(Perfil perfil, CancellationToken ct)
    {
        db.Perfis.Add(perfil);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Perfil>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        await db.Perfis
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

    public Task<Perfil?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        db.Perfis.SingleOrDefaultAsync(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public async Task<IReadOnlyList<Perfil>> ObterPorIdsEUnidadeNegocioAsync(
        IReadOnlyCollection<Guid> ids, Guid unidadeNegocioId, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        return await db.Perfis
            .Where(x => ids.Contains(x.Id) && x.UnidadeNegocioId == unidadeNegocioId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ObterPermissoesPorPerfilAsync(
        IReadOnlyCollection<Guid> perfilIds, CancellationToken ct)
    {
        if (perfilIds.Count == 0) return new Dictionary<Guid, IReadOnlyList<string>>();

        var pares = await db.PerfisPermissoes
            .Where(vinculo => perfilIds.Contains(vinculo.PerfilId))
            .Join(db.Permissoes, vinculo => vinculo.PermissaoId, permissao => permissao.Id,
                (vinculo, permissao) => new { vinculo.PerfilId, permissao.Codigo })
            .ToListAsync(ct);

        return pares
            .GroupBy(x => x.PerfilId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => (IReadOnlyList<string>)grupo.Select(x => x.Codigo).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    public async Task<IReadOnlyDictionary<Guid, int>> ContarUsuariosPorPerfilAsync(
        IReadOnlyCollection<Guid> perfilIds, CancellationToken ct)
    {
        if (perfilIds.Count == 0) return new Dictionary<Guid, int>();

        var contagens = await db.UsuariosPerfis
            .Where(vinculo => perfilIds.Contains(vinculo.PerfilId))
            .GroupBy(vinculo => vinculo.PerfilId)
            .Select(grupo => new { PerfilId = grupo.Key, Total = grupo.Count() })
            .ToListAsync(ct);

        return contagens.ToDictionary(x => x.PerfilId, x => x.Total);
    }

    public async Task SubstituirPermissoesAsync(Guid perfilId, IReadOnlyCollection<Guid> permissaoIds, CancellationToken ct)
    {
        var atuais = await db.PerfisPermissoes.Where(x => x.PerfilId == perfilId).ToListAsync(ct);

        var remover = atuais.Where(x => !permissaoIds.Contains(x.PermissaoId)).ToArray();
        if (remover.Length > 0) db.PerfisPermissoes.RemoveRange(remover);

        var jaVinculadas = atuais.Select(x => x.PermissaoId).ToHashSet();
        foreach (var permissaoId in permissaoIds.Where(x => !jaVinculadas.Contains(x)))
        {
            db.PerfisPermissoes.Add(new PerfilPermissao(perfilId, permissaoId));
        }
    }

    public async Task VincularPermissoesAsync(Guid perfilId, IReadOnlyCollection<Guid> permissaoIds, CancellationToken ct)
    {
        if (permissaoIds.Count == 0) return;

        var jaVinculadas = await db.PerfisPermissoes
            .Where(x => x.PerfilId == perfilId && permissaoIds.Contains(x.PermissaoId))
            .Select(x => x.PermissaoId)
            .ToListAsync(ct);

        foreach (var permissaoId in permissaoIds.Except(jaVinculadas))
        {
            db.PerfisPermissoes.Add(new PerfilPermissao(perfilId, permissaoId));
        }
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class PermissaoRepository(BlueprintOSDbContext db) : IPermissaoRepository
{
    public async Task<IReadOnlyList<Permissao>> ObterPorCodigosAsync(IReadOnlyCollection<string> codigos, CancellationToken ct)
    {
        if (codigos.Count == 0) return [];
        return await db.Permissoes.Where(x => codigos.Contains(x.Codigo)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Permissao>> ListarAsync(CancellationToken ct) =>
        await db.Permissoes.OrderBy(x => x.Codigo).ToListAsync(ct);
}

/// <summary>Resolve as permissões efetivas do usuário exclusivamente a partir do banco (O1.5).
///
/// Regras materializadas nesta consulta:
/// - composição por UNIÃO dos Perfis vinculados (ADR-0020, itens 8/10) — <c>Distinct</c> ao final;
/// - Perfil inativo não contribui nenhuma permissão (<c>x.Ativo</c>);
/// - somente Perfis da Unidade de Negócio DA SESSÃO contribuem: permissão concedida em outra Unidade
///   nunca autoriza uma ação sobre os dados desta (as leituras já são escopadas à Unidade da sessão);
/// - nenhuma permissão individual por usuário existe no modelo, por decisão arquitetural — não há
///   nenhuma tabela `UsuarioPermissao` a consultar aqui, e isso é intencional.</summary>
public sealed class PermissoesEfetivasResolver(BlueprintOSDbContext db) : IPermissoesEfetivasResolver
{
    public async Task<IReadOnlyList<string>> ResolverAsync(Guid usuarioId, Guid unidadeNegocioId, CancellationToken ct)
    {
        var codigos = await db.UsuariosPerfis
            .Where(vinculo => vinculo.UsuarioId == usuarioId)
            .Join(db.Perfis.Where(x => x.Ativo && x.UnidadeNegocioId == unidadeNegocioId),
                vinculo => vinculo.PerfilId, perfil => perfil.Id,
                (vinculo, perfil) => perfil.Id)
            .Join(db.PerfisPermissoes, perfilId => perfilId, pp => pp.PerfilId, (perfilId, pp) => pp.PermissaoId)
            .Join(db.Permissoes, permissaoId => permissaoId, permissao => permissao.Id, (permissaoId, permissao) => permissao.Codigo)
            .Distinct()
            .ToListAsync(ct);

        codigos.Sort(StringComparer.Ordinal);
        return codigos;
    }
}
