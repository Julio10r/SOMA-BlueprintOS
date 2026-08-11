using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Contrato mínimo necessário à conclusão do Bootstrap (O1.4.3.2; Work Order O1.4.3, seção 13, passo
/// 2): criar ou reaproveitar a Unidade de Negócio do primeiro Administrador Sênior.</summary>
public interface IUnidadeNegocioRepository
{
    Task<UnidadeNegocio?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Reaproveitamento de Unidade de Negócio existente (security-design-auth-o1.4.md §20.3; Work
    /// Order O1.4.3, seção 13, passo 2) exige que ela ainda não tenha nenhum Administrador Sênior ativo —
    /// avaliado via junção com <c>UsuarioPerfil</c>/<c>Perfil</c>/<c>Usuario</c> (nenhuma tabela de auditoria
    /// separada é necessária para essa checagem).</summary>
    Task<bool> PossuiAdministradorSeniorAtivoAsync(Guid unidadeNegocioId, CancellationToken ct);

    /// <summary>Rastreia a criação — escrita real ocorre junto com as demais entidades da transação de
    /// conclusão (mesmo <c>DbContext</c> compartilhado, Work Order O1.4.3, seção 13).</summary>
    Task AdicionarAsync(UnidadeNegocio unidadeNegocio, CancellationToken ct);

    // ---- O1.11 — Cadastro de Unidades de Negócio (CRUD real) e seleção pelo usuário ----

    /// <summary>Catálogo corporativo completo — usado tanto pela Administração (CRUD, sem escopo por
    /// sessão: a UN administrada é o próprio recurso) quanto por `GET /me/unidades-negocio` (hoje sempre
    /// devolve a única UN do usuário atual, já que o sistema é single-BU-por-usuário).</summary>
    Task<IReadOnlyList<UnidadeNegocio>> ListarTodasAsync(CancellationToken ct);

    /// <summary>Pré-checagem amigável de unicidade de slug; a garantia real é o índice único no banco.</summary>
    Task<bool> ExisteComSlugAsync(string slug, Guid? excluirId, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
