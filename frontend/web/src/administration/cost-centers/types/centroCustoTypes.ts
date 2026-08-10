export type StatusCentroCusto = "Ativo" | "Inativo";

/**
 * Centro de Custo (Gestao de Centros de Custo, ADR-0020 item 3). Dado
 * mestre integrado do ERP: CodigoErp e DescricaoErp vem da sincronizacao
 * com o ERP e nunca sao alterados/normalizados pelo +Compras. O +Compras
 * armazena apenas os metadados locais permitidos: DescricaoMaisCompras
 * (opcional) e AtivoNoMaisCompras (controlado exclusivamente pelo
 * +Compras, sem refletir no ERP).
 *
 * unidadeAlocacaoPadraoNome e quantidadeUnidadesAlocacaoVinculadas
 * representam, com dados mockados, o relacionamento N:N com Unidade de
 * Alocacao previsto pela ADR-0020 (item 5) — o modulo Unidades de
 * Alocacao ainda nao foi implementado; apenas a relacao e preparada aqui.
 */
export type CentroCusto = {
  id: string;
  codigoErp: string;
  descricaoErp: string;
  descricaoMaisCompras?: string;
  ativoNoMaisCompras: boolean;
  unidadeNegocioId: string;
  unidadeAlocacaoPadraoNome?: string;
  quantidadeUnidadesAlocacaoVinculadas: number;
  criadoEm: string;
  atualizadoEm: string;
};

/**
 * Entrada de edicao permitida pelo +Compras. Nao existe entrada de
 * criacao: Centro de Custo nunca e criado pelo +Compras (ADR-0020, item 3)
 * e CodigoErp/DescricaoErp/UnidadeNegocioId sao somente leitura, de origem
 * ERP, e por isso nao aparecem aqui. O vinculo com Unidade de Alocacao
 * tambem nao e editavel nesta etapa (modulo ainda nao implementado).
 */
export type CentroCustoUpdateInput = {
  descricaoMaisCompras?: string;
  ativoNoMaisCompras: boolean;
};

export function statusCentroCusto(centroCusto: CentroCusto): StatusCentroCusto {
  return centroCusto.ativoNoMaisCompras ? "Ativo" : "Inativo";
}
