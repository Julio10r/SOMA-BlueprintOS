export type StatusCentroCusto = "Ativo" | "Inativo";

/**
 * Centro de Custo (Gestao de Centros de Custo, ADR-0020 item 3, O1.7). Dado
 * mestre integrado do ERP: CodigoErp e DescricaoErp vem da leitura real do
 * ERP e nunca sao alterados/normalizados pelo +Compras. O +Compras
 * armazena apenas os metadados locais permitidos: DescricaoMaisCompras
 * (opcional) e AtivoNoMaisCompras (controlado exclusivamente pelo
 * +Compras, sem refletir no ERP).
 *
 * `id` e sempre igual a `codigoErp` (nao existe Id local proprio de Centro
 * de Custo). `temMetadadoLocal` indica se ja existe um registro de
 * metadados locais para este codigo — quando `false`, `ativoNoMaisCompras`
 * reflete o padrao "Ativo" definido pelo backend (O1.7).
 *
 * unidadeAlocacaoPadraoNome e quantidadeUnidadesAlocacaoVinculadas
 * representam o relacionamento N:N com Unidade de Alocacao previsto pela
 * ADR-0020 (item 5) — o modulo Unidades de Alocacao e o proprio
 * relacionamento sao escopo da O1.9 (fora de escopo desta sprint); o
 * backend da O1.7 nao devolve esses dados, por isso ficam sempre
 * indefinidos/zero aqui (divida tecnica documentada no relatorio da O1.7).
 */
export type CentroCusto = {
  id: string;
  codigoErp: string;
  descricaoErp: string;
  descricaoMaisCompras?: string;
  ativoNoMaisCompras: boolean;
  unidadeNegocioId: string;
  temMetadadoLocal: boolean;
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
