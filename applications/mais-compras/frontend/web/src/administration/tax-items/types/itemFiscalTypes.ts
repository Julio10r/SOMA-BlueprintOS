export type StatusItemFiscal = "Ativo" | "Inativo";

/**
 * Item Fiscal (B3 - Bloco 3, Discovery homologado). Cadastro unico do +Compras - nao existem cadastros
 * mestres separados de "Material" e "Servico" (Discovery B3, secao Material x Servico).
 *
 * Granularidade de `codigo`/`descricao` e livre - decisao da area de Compras, o +Compras nao impoe nivel
 * de detalhe (generico como "Notebook" ou especifico como "MacBook Pro 14" sao igualmente validos).
 *
 * `unidadeMedidaCodigoErp`/`contaContabilCodigoErp` sao os codigos dos cadastros de apoio dos Blocos 1/2
 * (obrigatorios); `unidadeMedidaDescricao`/`contaContabilDescricao` sao as descricoes do ERP, trazidas
 * apenas para exibicao.
 *
 * `codigo` e imutavel apos a criacao (nao aparece em `ItemFiscalUpdateInput`).
 *
 * Bloco 3 e exclusivamente local: sem sincronizacao com o Linx nesta etapa (Bloco 5, ainda nao
 * iniciado) - `ativo` e local ao +Compras.
 */
export type ItemFiscal = {
  id: string;
  codigo: string;
  descricao: string;
  unidadeMedidaCodigoErp: string;
  unidadeMedidaDescricao?: string;
  contaContabilCodigoErp: string;
  contaContabilDescricao?: string;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

export type ItemFiscalCreateInput = {
  codigo: string;
  descricao: string;
  unidadeMedidaCodigoErp: string;
  contaContabilCodigoErp: string;
};

/** Sem `codigo`: imutavel apos a criacao. */
export type ItemFiscalUpdateInput = {
  descricao: string;
  unidadeMedidaCodigoErp: string;
  contaContabilCodigoErp: string;
};

export function statusItemFiscal(item: ItemFiscal): StatusItemFiscal {
  return item.ativo ? "Ativo" : "Inativo";
}
