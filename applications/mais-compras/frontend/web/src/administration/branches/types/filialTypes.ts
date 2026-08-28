export type StatusFilial = "Ativo" | "Inativo";

/**
 * Filial (Gestao de Filiais, ADR-0020 item 3, O1.7). Dado mestre integrado
 * do ERP: CodigoCliFor e NomeCliFor compoem a referencia de negocio da
 * integracao e nunca sao alterados/normalizados pelo +Compras. O +Compras
 * armazena apenas os metadados locais permitidos: DescricaoMaisCompras
 * (opcional) e AtivoNoMaisCompras (controlado exclusivamente pelo
 * +Compras, sem refletir no ERP).
 *
 * `id` e sempre igual a `codigoCliFor` (nao existe Id local proprio de
 * Filial: o codigo ERP e a unica chave estavel). `temMetadadoLocal`
 * indica se ja existe um registro de metadados locais para este codigo —
 * quando `false`, `ativoNoMaisCompras` reflete o padrao "Ativo" definido
 * pelo backend (O1.7) para codigos ERP ainda nao editados localmente.
 */
export type Filial = {
  id: string;
  codigoCliFor: string;
  nomeCliFor: string;
  descricaoMaisCompras?: string;
  ativoNoMaisCompras: boolean;
  unidadeNegocioId: string;
  temMetadadoLocal: boolean;
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
