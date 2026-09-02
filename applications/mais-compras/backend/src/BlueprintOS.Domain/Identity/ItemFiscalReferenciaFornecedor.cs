namespace BlueprintOS.Domain.Identity;

/// <summary>Referência de Item Fiscal por Fornecedor — DE/PARA entre o código interno do Item Fiscal e o
/// código que o próprio Fornecedor usa para o mesmo item (B3 — Bloco 4, Discovery homologado, espelho local
/// de `ITEM_FISCAL_REF_FORNECEDOR`). Sem sincronização com o Linx nesta etapa (Bloco 5A/5B).
///
/// <see cref="ItemFiscalId"/> e <see cref="FornecedorId"/> são imutáveis após a criação (mesma decisão de
/// <c>ItemFiscal.Codigo</c>) — apenas <see cref="CodigoItemFornecedor"/> pode ser corrigido; para associar a
/// outro Item Fiscal ou Fornecedor, a referência deve ser removida e recriada.</summary>
public sealed class ItemFiscalReferenciaFornecedor
{
    public Guid Id { get; private set; }
    public Guid ItemFiscalId { get; private set; }
    public Guid FornecedorId { get; private set; }
    public string CodigoItemFornecedor { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private ItemFiscalReferenciaFornecedor()
    {
        CodigoItemFornecedor = string.Empty;
    }

    public ItemFiscalReferenciaFornecedor(Guid itemFiscalId, Guid fornecedorId, string codigoItemFornecedor, DateTimeOffset agora)
    {
        if (itemFiscalId == Guid.Empty) throw new ArgumentException("Item Fiscal é obrigatório.", nameof(itemFiscalId));
        if (fornecedorId == Guid.Empty) throw new ArgumentException("Fornecedor é obrigatório.", nameof(fornecedorId));
        if (string.IsNullOrWhiteSpace(codigoItemFornecedor)) throw new ArgumentException("Código do item no fornecedor é obrigatório.", nameof(codigoItemFornecedor));

        Id = Guid.NewGuid();
        ItemFiscalId = itemFiscalId;
        FornecedorId = fornecedorId;
        CodigoItemFornecedor = codigoItemFornecedor.Trim();
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    /// <summary>Não altera <see cref="ItemFiscalId"/>/<see cref="FornecedorId"/> — imutáveis após a criação.</summary>
    public void Atualizar(string codigoItemFornecedor, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(codigoItemFornecedor)) throw new ArgumentException("Código do item no fornecedor é obrigatório.", nameof(codigoItemFornecedor));

        CodigoItemFornecedor = codigoItemFornecedor.Trim();
        AtualizadoEm = agora;
    }
}
