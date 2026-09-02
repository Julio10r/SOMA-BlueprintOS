export type StatusUnidadeMedida = "Ativo" | "Inativo";

/**
 * Unidade de Medida (B3 - Bloco 2, Discovery homologado). Cadastro de apoio originado do Linx (UNIDADES):
 * CodigoErp e DescricaoErp nunca sao alterados/normalizados pelo +Compras. O +Compras armazena apenas os
 * metadados locais permitidos: DescricaoMaisCompras (opcional) e AtivoNoMaisCompras (controlado pelo
 * +Compras).
 *
 * Diferente de Conta Contabil: `UNIDADES` nao possui nenhum status/ativo/inativo no Linx (comprovado por
 * schema discovery dedicado) - por isso nao existe aqui um "status ERP" nem "status efetivo" distinto;
 * `ativoNoMaisCompras` e a unica fonte de ativo/inativo.
 *
 * `id` e sempre igual a `codigoErp`. `temMetadadoLocal` indica se ja existe um registro de metadados
 * locais para este codigo.
 */
export type UnidadeMedida = {
  id: string;
  codigoErp: string;
  descricaoErp: string;
  descricaoMaisCompras?: string;
  ativoNoMaisCompras: boolean;
  temMetadadoLocal: boolean;
  atualizadoEm: string;
};

/**
 * Entrada de edicao permitida pelo +Compras. Nao existe entrada de criacao: Unidade de Medida nunca e
 * criada pelo +Compras.
 */
export type UnidadeMedidaUpdateInput = {
  descricaoMaisCompras?: string;
  ativoNoMaisCompras: boolean;
};

export function statusUnidadeMedida(unidade: UnidadeMedida): StatusUnidadeMedida {
  return unidade.ativoNoMaisCompras ? "Ativo" : "Inativo";
}
