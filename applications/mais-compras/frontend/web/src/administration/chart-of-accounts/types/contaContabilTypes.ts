export type StatusContaContabil = "Ativo" | "Inativo";

/**
 * Conta Contabil (B3 - Bloco 1, Discovery homologado). Cadastro de apoio
 * originado do Linx (CTB_CONTA_PLANO): CodigoErp e DescricaoErp nunca sao
 * alterados/normalizados pelo +Compras. O +Compras armazena apenas os
 * metadados locais permitidos: DescricaoMaisCompras (opcional) e
 * AtivoNoMaisCompras (controlado pelo +Compras).
 *
 * Diferente de Filial/Centro de Custo: o Linx possui um status real
 * (InativaNoErp). `ativoEfetivo` aplica a ADR-0024 (Linx prevalece) - nunca
 * fica verdadeiro quando `inativaNoErp` e verdadeiro, mesmo que
 * `ativoNoMaisCompras` esteja marcado como ativo.
 *
 * `id` e sempre igual a `codigoErp` (nao existe Id local proprio).
 * `temMetadadoLocal` indica se ja existe um registro de metadados locais
 * para este codigo.
 */
export type ContaContabil = {
  id: string;
  codigoErp: string;
  descricaoErp: string;
  inativaNoErp: boolean;
  descricaoMaisCompras?: string;
  ativoNoMaisCompras: boolean;
  ativoEfetivo: boolean;
  temMetadadoLocal: boolean;
  atualizadoEm: string;
};

/**
 * Entrada de edicao permitida pelo +Compras. Nao existe entrada de criacao:
 * Conta Contabil nunca e criada pelo +Compras (Discovery B3 homologado) e
 * CodigoErp/DescricaoErp/InativaNoErp sao somente leitura, de origem ERP.
 */
export type ContaContabilUpdateInput = {
  descricaoMaisCompras?: string;
  ativoNoMaisCompras: boolean;
};

export function statusContaContabilErp(conta: ContaContabil): StatusContaContabil {
  return conta.inativaNoErp ? "Inativo" : "Ativo";
}

export function statusContaContabilEfetivo(conta: ContaContabil): StatusContaContabil {
  return conta.ativoEfetivo ? "Ativo" : "Inativo";
}
