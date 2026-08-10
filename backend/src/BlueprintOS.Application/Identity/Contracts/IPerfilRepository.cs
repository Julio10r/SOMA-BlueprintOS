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
}
