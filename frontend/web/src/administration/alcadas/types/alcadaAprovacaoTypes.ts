/**
 * Espelha CriterioAlcada (BlueprintOS.Domain.Identity, O1.12). O backend nao usa
 * JsonStringEnumConverter — o enum viaja como numero inteiro no JSON.
 */
export const CRITERIO_ALCADA = { Valor: 0, Categoria: 1, CentroCusto: 2 } as const;
export type CriterioAlcada = 0 | 1 | 2;
export const CRITERIO_ALCADA_LABELS: Record<CriterioAlcada, string> = {
  0: "Valor",
  1: "Categoria",
  2: "Centro de Custo"
};

export type TipoAprovador = "Usuario" | "Perfil";

/** Espelha AlcadaAprovacaoDto do backend (O1.12). Exatamente um entre aprovadorUsuarioId/aprovadorPerfilId. */
export type AlcadaAprovacao = {
  id: string;
  unidadeNegocioId: string;
  nome: string;
  criterio: CriterioAlcada;
  valorMinimo: number | null;
  valorMaximo: number | null;
  centroCustoMetadadoId: string | null;
  nivel: number;
  aprovadorUsuarioId: string | null;
  aprovadorPerfilId: string | null;
  status: "Ativo" | "Inativo";
  criadoEm: string;
  atualizadoEm: string;
};

export type AlcadaAprovacaoInput = {
  nome: string;
  criterio: CriterioAlcada;
  valorMinimo?: number;
  valorMaximo?: number;
  centroCustoMetadadoId?: string;
  nivel: number;
  aprovadorUsuarioId?: string;
  aprovadorPerfilId?: string;
};
