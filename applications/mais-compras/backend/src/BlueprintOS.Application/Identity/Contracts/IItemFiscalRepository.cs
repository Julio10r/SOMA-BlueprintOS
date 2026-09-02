using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Repositório do cadastro local de Item Fiscal (B3 — Bloco 3). Leitura escopada por Unidade de
/// Negócio; unicidade de <see cref="ItemFiscal.Codigo"/> é GLOBAL (mesma decisão de
/// <see cref="ExisteComCodigoAsync"/> — ver <c>ItemFiscalConfiguration</c>).</summary>
public interface IItemFiscalRepository
{
    Task<IReadOnlyList<ItemFiscal>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<ItemFiscal?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    Task<bool> ExisteComCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct);

    Task AdicionarAsync(ItemFiscal itemFiscal, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
