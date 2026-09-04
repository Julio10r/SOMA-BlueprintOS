using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Repositório das referências de Item Fiscal por Fornecedor (B3 — Bloco 4). Toda consulta é
/// escopada por <c>itemFiscalId</c> — a rota é sempre um sub-recurso de um Item Fiscal específico.</summary>
public interface IItemFiscalReferenciaFornecedorRepository
{
    Task<IReadOnlyList<ItemFiscalReferenciaFornecedor>> ListarPorItemFiscalAsync(Guid itemFiscalId, CancellationToken ct);

    Task<ItemFiscalReferenciaFornecedor?> ObterPorIdAsync(Guid id, Guid itemFiscalId, CancellationToken ct);

    /// <summary>B3 — Bloco 5A: usado pela sincronização de Referências (`ADR-0024`: sem timestamp
    /// confiável nesta tabela, Linx prevalece em divergência) para decidir entre criar uma referência nova
    /// ou atualizar <see cref="ItemFiscalReferenciaFornecedor.CodigoItemFornecedor"/> de uma já existente.</summary>
    Task<ItemFiscalReferenciaFornecedor?> ObterPorItemEFornecedorAsync(Guid itemFiscalId, Guid fornecedorId, CancellationToken ct);

    /// <summary>Comprovado em Linx (`KeyFieldList = FORNECEDOR, CODIGO_ITEM`): um Fornecedor tem no máximo
    /// uma referência por Item Fiscal.</summary>
    Task<bool> ExisteParaFornecedorNoItemAsync(Guid itemFiscalId, Guid fornecedorId, Guid? excluirId, CancellationToken ct);

    /// <summary>Decisão do Product Owner (homologação do Bloco 4): unicidade GLOBAL de
    /// (FornecedorId, CodigoItemFornecedor) — não escopada a um Item Fiscal específico.</summary>
    Task<bool> ExisteCodigoParaFornecedorAsync(Guid fornecedorId, string codigoItemFornecedor, Guid? excluirId, CancellationToken ct);

    Task AdicionarAsync(ItemFiscalReferenciaFornecedor referencia, CancellationToken ct);

    Task RemoverAsync(ItemFiscalReferenciaFornecedor referencia, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
