import type { Perfil, PerfilInput, Permissao } from "../types/perfilTypes";

/**
 * Cliente HTTP real da Gestao de Perfis (O1.5 — RBAC Real). Substitui integralmente
 * o antigo `perfisMockApi.ts` (dados em memoria), removido nesta sprint.
 *
 * A sessao viaja exclusivamente por cookie HttpOnly (`credentials: "include"`), no
 * mesmo padrao de `auth/services/authApi.ts`. Nenhuma decisao de autorizacao acontece
 * aqui: o backend exige a permissao `Perfil.Gerenciar` em todos estes endpoints e
 * responde 401 (sem sessao) ou 403 (sem permissao) independentemente do que a
 * interface faca. `PerfilAcessoNegadoError` existe apenas para que a tela possa
 * mostrar um estado de acesso negado em vez de um erro genérico.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

/**
 * Prefixo `/api` deliberado (espelha `PerfisController.BaseRoute` no backend): as rotas da
 * SPA usam `/administracao/*`, entao o proxy de desenvolvimento nao pode encaminhar esse
 * prefixo ao backend — faria o React Router perder as telas de Administracao.
 */
const BASE = "/api/administracao";

type ApiErrorBody = { code?: string; message?: string };

export class PerfilApiError extends Error {
  readonly code?: string;

  constructor(message: string, code?: string) {
    super(message);
    this.name = "PerfilApiError";
    this.code = code;
  }
}

/** 403 — sessao valida, porem sem a permissao `Perfil.Gerenciar`. */
export class PerfilAcessoNegadoError extends PerfilApiError {
  constructor(message = "Você não tem permissão para acessar a Gestão de Perfis.") {
    super(message, "acesso_negado");
    this.name = "PerfilAcessoNegadoError";
  }
}

/** 401 — sem sessao autenticada. */
export class PerfilNaoAutenticadoError extends PerfilApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "PerfilNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new PerfilNaoAutenticadoError();
  if (response.status === 403) throw new PerfilAcessoNegadoError();

  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new PerfilApiError(message, code);
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

export function listPerfis(): Promise<Perfil[]> {
  return getJson<Perfil[]>(`${BASE}/perfis`, "Falha ao carregar perfis.");
}

export async function getPerfil(id: string): Promise<Perfil | null> {
  const response = await fetch(`${BASE}/perfis/${encodeURIComponent(id)}`, { credentials: "include" });
  if (response.status === 404) return null;
  if (!response.ok) await lerErro(response, "Falha ao carregar perfil.");
  return (await response.json()) as Perfil;
}

export function listPermissoes(): Promise<Permissao[]> {
  return getJson<Permissao[]>(`${BASE}/permissoes`, "Falha ao carregar o catálogo de permissões.");
}

export function createPerfil(input: PerfilInput): Promise<Perfil> {
  return sendJson<Perfil>(`${BASE}/perfis`, "POST", input, "Falha ao criar perfil.");
}

export function updatePerfil(id: string, input: PerfilInput): Promise<Perfil> {
  return sendJson<Perfil>(`${BASE}/perfis/${encodeURIComponent(id)}`, "PUT", input, "Falha ao salvar perfil.");
}

/**
 * Ativacao/inativacao logica. Nao existe exclusao de Perfil: `ComprasFuncional.md`
 * ("Gestão de Perfis") define como acoes oficiais apenas Criar, Editar e
 * Ativar/Inativar — mesmo padrao dos demais modulos administrativos.
 */
export function alterarStatusPerfil(id: string, ativo: boolean): Promise<Perfil> {
  return sendJson<Perfil>(
    `${BASE}/perfis/${encodeURIComponent(id)}/status`,
    "PATCH",
    { ativo },
    ativo ? "Falha ao ativar perfil." : "Falha ao inativar perfil."
  );
}
