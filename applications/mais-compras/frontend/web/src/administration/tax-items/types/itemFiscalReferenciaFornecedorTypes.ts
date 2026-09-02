/**
 * Referencia de Item Fiscal por Fornecedor (B3 - Bloco 4, Discovery homologado). DE/PARA entre o codigo
 * interno do Item Fiscal e o codigo que o proprio fornecedor usa para o mesmo item - espelho local de
 * `ITEM_FISCAL_REF_FORNECEDOR`. Sem sincronizacao com o Linx nesta etapa (Bloco 5A/5B).
 *
 * `fornecedorId` e imutavel apos a criacao (nao aparece em `ItemFiscalReferenciaFornecedorUpdateInput`) -
 * para associar a outro fornecedor, remova e recrie a referencia.
 */
export type ItemFiscalReferenciaFornecedor = {
  id: string;
  itemFiscalId: string;
  fornecedorId: string;
  fornecedorNome: string;
  codigoItemFornecedor: string;
  criadoEm: string;
  atualizadoEm: string;
};

export type ItemFiscalReferenciaFornecedorCreateInput = {
  fornecedorId: string;
  codigoItemFornecedor: string;
};

/** Sem `fornecedorId`: imutavel apos a criacao. */
export type ItemFiscalReferenciaFornecedorUpdateInput = {
  codigoItemFornecedor: string;
};
