using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>B3 — Bloco 5A: sincronização de Referências de Item Fiscal por Fornecedor
/// (`ITEM_FISCAL_REF_FORNECEDOR`). Diferente de <see cref="ISincronizarItensFiscaisErpUseCase"/>: esta
/// tabela já tem regra de conflito homologada (`ADR-0024`, sem timestamp confiável — Linx prevalece), então
/// uma referência local já existente com <c>CodigoItemFornecedor</c> divergente É atualizada para o valor
/// do Linx, nunca apenas reportada como conflito. Uma resolução de Fornecedor ambígua ou o Item Fiscal
/// pai/Fornecedor ainda não sincronizados localmente permanecem conflito (nunca associação
/// automática/arbitrária).</summary>
public interface ISincronizarItemFiscalReferenciasFornecedorErpUseCase
{
    Task<SincronizacaoItemFiscalReferenciasFornecedorErpResumo> ExecuteAsync(SincronizarItemFiscalReferenciasFornecedorErpDto dto, CancellationToken cancellationToken = default);
}
