/**
 * Projecao minima de Unidade de Negocio usada pela selecao pos-login (O1.11).
 * Espelha `UnidadeNegocioDto` do backend (`GET /me/unidades-negocio`).
 */
export type UnidadeNegocioSelecionavel = {
  id: string;
  nome: string;
  slug: string;
  ativa: boolean;
};
