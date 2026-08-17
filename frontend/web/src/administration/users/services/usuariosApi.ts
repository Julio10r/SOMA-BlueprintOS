import type { Usuario, UsuarioInput } from "../types/userTypes";

/**
 * Cliente HTTP real da Gestao de Usuarios (O1.6 — Backend Real). Substitui integralmente
 * o antigo `usuariosMockApi.ts` (dados em memoria), removido nesta sprint.
 *
 * Mesmo padrao de `administration/profiles/services/perfisApi.ts` (O1.5): sessao via
 * cookie HttpOnly (`credentials: "include"`), cabecalho CSRF nas escritas, e nenhuma
 * decisao de autorizacao acontece aqui — o backend exige a permissao `Usuario.Gerenciar`
 * em todos estes endpoints e responde 401/403 independentemente do que a interface faca.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

/** Espelha `PerfisController.BaseRoute`/`UsuariosController` no backend. */
const BASE = "/api/administracao";

type ApiErrorBody = { code?: string; message?: string };

export class UsuarioApiError extends Error {
  readonly code?: string;

  constructor(message: string, code?: string) {
    super(message);
    this.name = "UsuarioApiError";
    this.code = code;
  }
}

/** 403 — sessao valida, porem sem a permissao `Usuario.Gerenciar`. */
export class UsuarioAcessoNegadoError extends UsuarioApiError {
  constructor(message = "Você não tem permissão para acessar a Gestão de Usuários.") {
    super(message, "acesso_negado");
    this.name = "UsuarioAcessoNegadoError";
  }
}

/** 401 — sem sessao autenticada. */
export class UsuarioNaoAutenticadoError extends UsuarioApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "UsuarioNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new UsuarioNaoAutenticadoError();
  if (response.status === 403) throw new UsuarioAcessoNegadoError();

  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new UsuarioApiError(message, code);
}

async function getJson<T>(path: string, fallback: string): Promise<T> {
  const response = await fetch(path, { credentials: "include" });
  if (!response.ok) await lerErro(response, fallback);
  return (await response.json()) as T;
}

async function sendJson<T>(path: string, method: string, body: unknown, fallback: string): Promise<T> {
  const response = await fetch(path, {
    method,
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(body)
  });
  if (!response.ok) await lerErro(response, fallback);
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export function listUsuarios(): Promise<Usuario[]> {
  return getJson<Usuario[]>(`${BASE}/usuarios`, "Falha ao carregar usuários.");
}

export async function getUsuario(id: string): Promise<Usuario | null> {
  const response = await fetch(`${BASE}/usuarios/${encodeURIComponent(id)}`, { credentials: "include" });
  if (response.status === 404) return null;
  if (!response.ok) await lerErro(response, "Falha ao carregar usuário.");
  return (await response.json()) as Usuario;
}

export function createUsuario(input: UsuarioInput): Promise<Usuario> {
  return sendJson<Usuario>(`${BASE}/usuarios`, "POST", input, "Falha ao criar usuário.");
}

export function updateUsuario(id: string, input: UsuarioInput): Promise<Usuario> {
  return sendJson<Usuario>(`${BASE}/usuarios/${encodeURIComponent(id)}`, "PUT", input, "Falha ao salvar usuário.");
}

/**
 * Ativacao/inativacao logica. Nao existe exclusao de Usuario: a Work Order O1.6 lista
 * exclusao fisica como explicitamente fora de escopo — mesmo padrao dos demais modulos
 * administrativos (Perfis, Filiais, Centros de Custo, Unidades de Alocacao).
 */
export function alterarStatusUsuario(id: string, ativo: boolean): Promise<Usuario> {
  return sendJson<Usuario>(
    `${BASE}/usuarios/${encodeURIComponent(id)}/status`,
    "PATCH",
    { ativo },
    ativo ? "Falha ao ativar usuário." : "Falha ao inativar usuário."
  );
}
