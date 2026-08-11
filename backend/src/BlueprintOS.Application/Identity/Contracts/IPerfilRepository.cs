using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Contrato mínimo necessário à conclusão do Bootstrap (O1.4.3.2; Work Order O1.4.3, seção 13, passo
/// 4): garantir a existência do Perfil "Administrador Sênior" na Unidade de Negócio escolhida, reaproveitando
/// pelo índice único (<c>UnidadeNegocioId</c>, <c>Nome</c>) fechado em O1.4.3.1.</summary>
public interface IPerfilRepository
{
    Task<Perfil?> ObterPorNomeEUnidadeNegocioAsync(string nome, Guid unidadeNegocioId, CancellationToken ct);

    /// <summary>Rastreia a criação — escrita real ocorre junto com as demais entidades da transação de
    /// conclusão (mesmo <c>DbContext</c> compartilhado, Work Order O1.4.3, seção 13).</summary>
    Task AdicionarAsync(Perfil perfil, CancellationToken ct);

    // ---- O1.5 — RBAC Real (Gestão de Perfis) ----
    // Todas as leituras abaixo são obrigatoriamente escopadas por UnidadeNegocioId: um administrador de
    // uma Unidade de Negócio nunca enumera nem altera Perfis de outra.

    Task<IReadOnlyList<Perfil>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<Perfil?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    /// <summary>Códigos de permissão vinculados a cada Perfil informado, para montar a listagem sem N+1.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ObterPermissoesPorPerfilAsync(
        IReadOnlyCollection<Guid> perfilIds, CancellationToken ct);

    /// <summary>Quantidade de usuários vinculados a cada Perfil informado (contagem real em
    /// <c>UsuariosPerfis</c>, nunca um contador denormalizado).</summary>
    Task<IReadOnlyDictionary<Guid, int>> ContarUsuariosPorPerfilAsync(
        IReadOnlyCollection<Guid> perfilIds, CancellationToken ct);

    /// <summary>Substitui integralmente o conjunto de permissões de um Perfil (remove as ausentes,
    /// adiciona as novas). Idempotente.</summary>
    Task SubstituirPermissoesAsync(Guid perfilId, IReadOnlyCollection<Guid> permissaoIds, CancellationToken ct);

    Task VincularPermissoesAsync(Guid perfilId, IReadOnlyCollection<Guid> permissaoIds, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
