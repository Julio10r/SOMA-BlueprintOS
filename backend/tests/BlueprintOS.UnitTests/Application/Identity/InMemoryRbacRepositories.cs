using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>Test double em memória de <see cref="IPerfilRepository"/>, compartilhado pelos testes de
/// Bootstrap (O1.4.3.2) e de RBAC (O1.5). Fake apenas dentro de testes — a implementação real é sempre
/// EF Core/SQL Server (<c>PerfilRepository</c>), nunca substituída em produção.
///
/// Reproduz fielmente as regras que a consulta real aplica e das quais os testes dependem:
/// escopo por Unidade de Negócio, união de permissões e contagem real de vínculos de usuário.</summary>
public sealed class FakePerfilRepository : IPerfilRepository
{
    public List<Perfil> All { get; } = [];
    public List<PerfilPermissao> Vinculos { get; } = [];
    public FakePermissaoRepository Permissoes { get; init; } = new();
    public List<UsuarioPerfil> UsuariosPerfis { get; init; } = [];
    public int Salvamentos { get; private set; }

    public Task<Perfil?> ObterPorNomeEUnidadeNegocioAsync(string nome, Guid unidadeNegocioId, CancellationToken ct) =>
        Task.FromResult(All.SingleOrDefault(x => x.Nome == nome && x.UnidadeNegocioId == unidadeNegocioId));

    public Task AdicionarAsync(Perfil perfil, CancellationToken ct) { All.Add(perfil); return Task.CompletedTask; }

    public Task<IReadOnlyList<Perfil>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Perfil>>(All.Where(x => x.UnidadeNegocioId == unidadeNegocioId).OrderBy(x => x.Nome).ToArray());

    public Task<Perfil?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        Task.FromResult(All.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ObterPermissoesPorPerfilAsync(
        IReadOnlyCollection<Guid> perfilIds, CancellationToken ct)
    {
        var mapa = Vinculos
            .Where(v => perfilIds.Contains(v.PerfilId))
            .GroupBy(v => v.PerfilId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g
                    .Select(v => Permissoes.All.SingleOrDefault(p => p.Id == v.PermissaoId)?.Codigo)
                    .Where(c => c is not null)
                    .Select(c => c!)
                    .OrderBy(c => c, StringComparer.Ordinal)
                    .ToArray());

        return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>(mapa);
    }

    public Task<IReadOnlyDictionary<Guid, int>> ContarUsuariosPorPerfilAsync(
        IReadOnlyCollection<Guid> perfilIds, CancellationToken ct)
    {
        var mapa = UsuariosPerfis
            .Where(v => perfilIds.Contains(v.PerfilId))
            .GroupBy(v => v.PerfilId)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult<IReadOnlyDictionary<Guid, int>>(mapa);
    }

    public Task SubstituirPermissoesAsync(Guid perfilId, IReadOnlyCollection<Guid> permissaoIds, CancellationToken ct)
    {
        Vinculos.RemoveAll(v => v.PerfilId == perfilId && !permissaoIds.Contains(v.PermissaoId));
        return VincularPermissoesAsync(perfilId, permissaoIds, ct);
    }

    public Task VincularPermissoesAsync(Guid perfilId, IReadOnlyCollection<Guid> permissaoIds, CancellationToken ct)
    {
        foreach (var permissaoId in permissaoIds)
        {
            if (Vinculos.Any(v => v.PerfilId == perfilId && v.PermissaoId == permissaoId)) continue;
            Vinculos.Add(new PerfilPermissao(perfilId, permissaoId));
        }

        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) { Salvamentos++; return Task.CompletedTask; }
}

/// <summary>Catálogo persistido simulado. Por padrão contém exatamente o catálogo de
/// <see cref="PermissaoCatalogo"/> com os mesmos Ids estáveis do seed real, para que os testes exercitem
/// os códigos verdadeiros e não um conjunto inventado.</summary>
public sealed class FakePermissaoRepository : IPermissaoRepository
{
    public List<Permissao> All { get; } = PermissaoCatalogo.Todas.Select(Permissao.DoCatalogo).ToList();

    public Task<IReadOnlyList<Permissao>> ObterPorCodigosAsync(IReadOnlyCollection<string> codigos, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Permissao>>(
            All.Where(x => codigos.Contains(x.Codigo, StringComparer.OrdinalIgnoreCase)).ToArray());

    public Task<IReadOnlyList<Permissao>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Permissao>>(All.OrderBy(x => x.Codigo, StringComparer.Ordinal).ToArray());

    public Guid IdDe(string codigo) => All.Single(x => x.Codigo == codigo).Id;
}

/// <summary>Resolver de permissões efetivas em memória. Vazio por padrão: os testes de autenticação
/// pré-existentes (O1.4.x) não exercitam RBAC, e o correto é que um usuário sem Perfil vinculado tenha
/// nenhuma permissão efetiva — fail-closed, nunca um conjunto implícito.</summary>
public sealed class FakePermissoesEfetivasResolver : IPermissoesEfetivasResolver
{
    public Dictionary<Guid, IReadOnlyList<string>> PorUsuario { get; } = [];

    public Task<IReadOnlyList<string>> ResolverAsync(Guid usuarioId, Guid unidadeNegocioId, CancellationToken ct) =>
        Task.FromResult(PorUsuario.TryGetValue(usuarioId, out var codigos) ? codigos : []);
}
