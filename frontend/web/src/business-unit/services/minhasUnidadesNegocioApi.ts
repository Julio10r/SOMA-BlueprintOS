import type { UnidadeNegocioSelecionavel } from "../types/unidadeNegocioSelecaoTypes";

/**
 * Cliente HTTP de `GET /me/unidades-negocio` (O1.11 — Selecao da Unidade de Negocio). Nao exige
 * permissao especial, apenas sessao valida — mesmo padrao de `GET /auth/me`.
 */
export async function listMinhasUnidadesNegocio(): Promise<UnidadeNegocioSelecionavel[]> {
  const response = await fetch("/me/unidades-negocio", { credentials: "include" });
  if (!response.ok) return [];
  return (await response.json()) as UnidadeNegocioSelecionavel[];
}
