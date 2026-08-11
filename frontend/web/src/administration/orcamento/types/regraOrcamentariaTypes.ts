/**
 * Espelha PeriodoOrcamentario (BlueprintOS.Domain.Identity, O1.12). O backend nao usa
 * JsonStringEnumConverter — o enum viaja como numero inteiro no JSON.
 */
export const PERIODO_ORCAMENTARIO = { Mensal: 0, Trimestral: 1, Anual: 2 } as const;
export type PeriodoOrcamentario = 0 | 1 | 2;
export const PERIODO_ORCAMENTARIO_LABELS: Record<PeriodoOrcamentario, string> = {
  0: "Mensal",
  1: "Trimestral",
  2: "Anual"
};

/** Espelha RegraOrcamentariaDto do backend (O1.12). Apenas o cadastro — nenhum saldo/consumo/reserva. */
export type RegraOrcamentaria = {
  id: string;
  unidadeNegocioId: string;
  nome: string;
  centroCustoMetadadoId: string;
  valorLimite: number;
  periodo: PeriodoOrcamentario;
  status: "Ativo" | "Inativo";
  criadoEm: string;
  atualizadoEm: string;
};

export type RegraOrcamentariaInput = {
  nome: string;
  centroCustoMetadadoId: string;
  valorLimite: number;
  periodo: PeriodoOrcamentario;
};
