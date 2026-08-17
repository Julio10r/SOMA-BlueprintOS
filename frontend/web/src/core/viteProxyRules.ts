/** Regra de bypass do proxy Vite para "/fornecedores" (dev-only) — extraída de vite.config.ts
 * para ser testável. "/fornecedores" é ao mesmo tempo prefixo de API e rota da SPA: sem este
 * bypass, uma navegação de página (F5/deep-link) é encaminhada ao backend e retorna o JSON da
 * API em vez do shell React (regressão observada em validação E2E da B2.9). */
export function shouldBypassFornecedoresProxy(method: string, acceptHeader: string | undefined): boolean {
  return method === "GET" && !!acceptHeader?.includes("text/html");
}
