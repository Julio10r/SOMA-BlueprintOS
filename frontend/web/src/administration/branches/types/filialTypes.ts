export type StatusFilial = "Ativo" | "Inativo";

/**
 * Filial (Gestao de Filiais, ADR-0020 item 3). Dado mestre integrado do
 * ERP: CodigoCliFor e NomeCliFor compoem a referencia de negocio da
 * integracao e nunca sao alterados/normalizados pelo +Compras. O +Compras
 * armazena apenas os metadados locais permitidos: DescricaoMaisCompras
 * (opcional) e AtivoNoMaisCompras (controlado exclusivamente pelo
 * +Compras, sem refletir no ERP).
 */
export type Filial = {
  id: string;
  codigoCliFor: string;
  nomeCliFor: string;
  descricaoMaisCompras?: string;
  ativoNoMaisCompras: boolean;
  unidadeNegocioId: string;
  criadoEm: string;
  atualizadoEm: string;
};

/**
 * Entrada de edicao permitida pelo +Compras. Nao existe entrada de
 * criacao: Filial nunca e criada pelo +Compras (ADR-0020, item 3) e
 * CodigoCliFor/NomeCliFor/UnidadeNegocioId sao somente leitura, de origem
 * ERP, e por isso nao aparecem aqui.
 */
export type FilialUpdateInput = {
  descricaoMaisCompras?: string;
  ativoNoMaisCompras: boolean;
};

export function statusFilial(filial: Filial): StatusFilial {
  return filial.ativoNoMaisCompras ? "Ativo" : "Inativo";
}
