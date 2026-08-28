import { useAuth } from "./useAuth";

/**
 * Reflexo de UX das permissoes efetivas do usuario autenticado (O1.5). Comparacao
 * case-insensitive, igual a do backend, para que a caixa do codigo nunca altere a
 * decisao exibida.
 *
 * ATENCAO: este hook NAO autoriza nada. Ele apenas evita oferecer ao usuario uma acao
 * que o backend recusaria. A autorizacao real e sempre a policy do ASP.NET Core no
 * servidor; esconder um botao aqui nunca substitui essa barreira.
 */
export function usePermissao(codigo: string): boolean {
  const { usuario } = useAuth();
  if (!usuario) return false;
  return usuario.permissoes.some((atual) => atual.toLowerCase() === codigo.toLowerCase());
}
