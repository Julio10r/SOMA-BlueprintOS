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

    public Task<IReadOnlyList<Perfil>> ObterPorIdsEUnidadeNegocioAsync(IReadOnlyCollection<Guid> ids, Guid unidadeNegocioId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Perfil>>(All.Where(x => ids.Contains(x.Id) && x.UnidadeNegocioId == unidadeNegocioId).ToArray());

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

/// <summary>Test double em memória de <see cref="IUsuarioRepository"/> para os testes da Gestão de
/// Usuários (O1.6) — completo, ao contrário dos fakes mínimos de Auth/Bootstrap (O1.4.x), que só
/// exercitam a criação e a leitura por e-mail/id.</summary>
public sealed class FakeUsuarioRepositoryCompleto : IUsuarioRepository
{
    public List<Usuario> All { get; } = [];
    public List<UsuarioPerfil> Perfis { get; } = [];
    public List<UsuarioCentroCusto> CentrosCusto { get; } = [];
    public int Salvamentos { get; private set; }

    /// <summary>DEB-15/M2 — simula, na chamada final de <c>SalvarAlteracoesAsync</c> do caso de uso (que
    /// agora também persiste a eventual ancoragem "sob demanda" de CentroCustoMetadado), uma falha real de
    /// persistência (ex.: corrida no índice único de e-mail). Permite testar que o caso de uso chamador
    /// trata a falha de forma controlada (retorna <c>Erro</c>), em vez de deixar a exceção subir crua.</summary>
    public Func<Exception>? FalharAoSalvar { get; set; }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) =>
        Task.FromResult(All.SingleOrDefault(x => x.Email == email));

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(All.SingleOrDefault(x => x.Id == id));

    public Task AdicionarAsync(Usuario usuario, CancellationToken ct) { All.Add(usuario); return Task.CompletedTask; }

    public Task<IReadOnlyList<Usuario>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Usuario>>(All.Where(x => x.UnidadeNegocioId == unidadeNegocioId).OrderBy(x => x.Nome).ToArray());

    public Task<Usuario?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        Task.FromResult(All.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

    public Task<Usuario?> ObterPorEmailEUnidadeNegocioAsync(string email, Guid unidadeNegocioId, CancellationToken ct) =>
        Task.FromResult(All.SingleOrDefault(x => x.Email == email && x.UnidadeNegocioId == unidadeNegocioId));

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<BlueprintOS.Application.Identity.Models.UsuarioPerfilResumoDto>>> ObterPerfisPorUsuarioAsync(
        IReadOnlyCollection<Guid> usuarioIds, CancellationToken ct)
    {
        var mapa = Perfis
            .Where(v => usuarioIds.Contains(v.UsuarioId))
            .GroupBy(v => v.UsuarioId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<BlueprintOS.Application.Identity.Models.UsuarioPerfilResumoDto>)g
                    .Select(v => PerfilLookup?.SingleOrDefault(p => p.Id == v.PerfilId))
                    .Where(p => p is not null)
                    .Select(p => new BlueprintOS.Application.Identity.Models.UsuarioPerfilResumoDto(p!.Id, p.Nome, p.Ativo))
                    .ToArray());

        return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<BlueprintOS.Application.Identity.Models.UsuarioPerfilResumoDto>>>(mapa);
    }

    /// <summary>Catálogo de Perfis usado apenas para resolver Nome/Ativo na projeção acima — os testes
    /// que exercitam vínculos populam esta lista a partir do mesmo <see cref="FakePerfilRepository"/>
    /// usado no cenário.</summary>
    public List<Perfil>? PerfilLookup { get; set; }

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ObterCentrosCustoPorUsuarioAsync(
        IReadOnlyCollection<Guid> usuarioIds, CancellationToken ct)
    {
        var mapa = CentrosCusto
            .Where(v => usuarioIds.Contains(v.UsuarioId))
            .GroupBy(v => v.UsuarioId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(v => v.CentroCustoCodigoErp).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>(mapa);
    }

    public Task SubstituirPerfisAsync(Guid usuarioId, IReadOnlyCollection<Guid> perfilIds, CancellationToken ct)
    {
        Perfis.RemoveAll(v => v.UsuarioId == usuarioId && !perfilIds.Contains(v.PerfilId));
        foreach (var perfilId in perfilIds.Where(id => !Perfis.Any(v => v.UsuarioId == usuarioId && v.PerfilId == id)))
        {
            Perfis.Add(new UsuarioPerfil(usuarioId, perfilId));
        }
        return Task.CompletedTask;
    }

    public Task SubstituirCentrosCustoAsync(Guid usuarioId, IReadOnlyCollection<string> codigosErp, CancellationToken ct)
    {
        CentrosCusto.RemoveAll(v => v.UsuarioId == usuarioId && !codigosErp.Contains(v.CentroCustoCodigoErp, StringComparer.OrdinalIgnoreCase));
        foreach (var codigo in codigosErp.Where(c => !CentrosCusto.Any(v => v.UsuarioId == usuarioId && string.Equals(v.CentroCustoCodigoErp, c, StringComparison.OrdinalIgnoreCase))))
        {
            CentrosCusto.Add(new UsuarioCentroCusto(usuarioId, codigo));
        }
        return Task.CompletedTask;
    }

    public Task<int> ContarAdministradoresSeniorAtivosAsync(Guid unidadeNegocioId, Guid? excluirUsuarioId, CancellationToken ct)
    {
        var candidatos = All.Where(u => u.UnidadeNegocioId == unidadeNegocioId && u.EstaAtivo());
        if (excluirUsuarioId is not null) candidatos = candidatos.Where(u => u.Id != excluirUsuarioId.Value);

        var idsComPerfilAdmin = Perfis
            .Where(v => PerfilLookup is not null && PerfilLookup.Any(p =>
                p.Id == v.PerfilId && p.UnidadeNegocioId == unidadeNegocioId && p.Ativo && p.Nome == Perfil.AdministradorSenior))
            .Select(v => v.UsuarioId)
            .ToHashSet();

        return Task.FromResult(candidatos.Count(u => idsComPerfilAdmin.Contains(u.Id)));
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct)
    {
        if (FalharAoSalvar is { } fabricarErro) throw fabricarErro();
        Salvamentos++;
        return Task.CompletedTask;
    }
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
