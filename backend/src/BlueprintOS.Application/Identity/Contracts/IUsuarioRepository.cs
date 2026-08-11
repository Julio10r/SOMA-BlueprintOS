using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct);
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Rastreia a criação (O1.4.3.2) — a escrita real ocorre junto com as demais entidades da
    /// transação de conclusão do Bootstrap (Work Order O1.4.3, seção 13), via
    /// <see cref="IBootstrapEstadoRepository.SalvarAlteracoesAsync"/> no mesmo <c>DbContext</c> compartilhado.</summary>
    Task AdicionarAsync(Usuario usuario, CancellationToken ct);

    // ---- O1.6 — Gestão de Usuários (Backend Real) ----
    // Todas as leituras abaixo são obrigatoriamente escopadas por UnidadeNegocioId: um administrador de
    // uma Unidade de Negócio nunca enumera nem altera Usuários de outra (mesmo cuidado de IPerfilRepository).

    Task<IReadOnlyList<Usuario>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<Usuario?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    Task<Usuario?> ObterPorEmailEUnidadeNegocioAsync(string email, Guid unidadeNegocioId, CancellationToken ct);

    /// <summary>Perfis vinculados a cada Usuário informado, para montar a listagem sem N+1.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<UsuarioPerfilResumoDto>>> ObterPerfisPorUsuarioAsync(
        IReadOnlyCollection<Guid> usuarioIds, CancellationToken ct);

    /// <summary>Códigos ERP de Centro de Custo vinculados a cada Usuário informado.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ObterCentrosCustoPorUsuarioAsync(
        IReadOnlyCollection<Guid> usuarioIds, CancellationToken ct);

    /// <summary>Substitui integralmente o conjunto de Perfis vinculados a um Usuário. Idempotente.</summary>
    Task SubstituirPerfisAsync(Guid usuarioId, IReadOnlyCollection<Guid> perfilIds, CancellationToken ct);

    /// <summary>Substitui integralmente o conjunto de Centros de Custo vinculados a um Usuário. Idempotente.</summary>
    Task SubstituirCentrosCustoAsync(Guid usuarioId, IReadOnlyCollection<string> codigosErp, CancellationToken ct);

    /// <summary>Quantidade de Usuários ATIVOS vinculados ao Perfil "Administrador Sênior" (por nome) na
    /// Unidade de Negócio informada, excluindo opcionalmente um Usuário específico do cálculo (o próprio
    /// Usuário sob edição, cujo estado futuro já foi decidido pelo caso de uso antes de perguntar ao banco).
    /// Alimenta <see cref="BlueprintOS.Domain.Identity.AdministradorSeniorInvariantService"/>.</summary>
    Task<int> ContarAdministradoresSeniorAtivosAsync(Guid unidadeNegocioId, Guid? excluirUsuarioId, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
