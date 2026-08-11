import type { Parametro, ParametroAtualizarInput, ParametroCriarInput } from "../types/parametroTypes";

/**
 * Cliente HTTP de Parametros gerais (O1.11), globais ou por Unidade de Negocio. Protegido por
 * `Sistema.Gerenciar`. Unico modulo desta Work Order com exclusao fisica real (decisao explicita da
 * Work Order O1.11: Parametro nao e dado mestre de ERP nem possui historico externo a preservar).
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";
const BASE = "/api/administracao/parametros";

type ApiErrorBody = { code?: string; message?: string };

type ParametroApiDto = { id: string; chave: string; valor: string; descricao: string; unidadeNegocioId: string | null };

export class ParametroApiError extends Error {
  readonly code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = "ParametroApiError";
    this.code = code;
  }
}

export class ParametroAcessoNegadoError extends ParametroApiError {
  constructor(message = "Voce nao tem permissao para acessar os Parametros.") {
    super(message, "acesso_negado");
    this.name = "ParametroAcessoNegadoError";
  }
}

export class ParametroNaoAutenticadoError extends ParametroApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "ParametroNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new ParametroNaoAutenticadoError();
  if (response.status === 403) throw new ParametroAcessoNegadoError();
  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new ParametroApiError(message, code);
}

function paraParametro(dto: ParametroApiDto): Parametro {
  return { id: dto.id, chave: dto.chave, valor: dto.valor, descricao: dto.descricao, unidadeNegocioId: dto.unidadeNegocioId };
}

export async function listParametros(unidadeNegocioId?: string): Promise<Parametro[]> {
  const query = unidadeNegocioId ? `?unidadeNegocioId=${encodeURIComponent(unidadeNegocioId)}` : "";
  const response = await fetch(`${BASE}${query}`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar Parametros.");
  const data = (await response.json()) as ParametroApiDto[];
  return data.map(paraParametro);
}

export async function createParametro(input: ParametroCriarInput): Promise<Parametro> {
  const response = await fetch(BASE, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(input)
  });
  if (!response.ok) await lerErro(response, "Falha ao criar Parametro.");
  return paraParametro((await response.json()) as ParametroApiDto);
}

export async function updateParametro(id: string, input: ParametroAtualizarInput): Promise<Parametro> {
  const response = await fetch(`${BASE}/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(input)
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar Parametro.");
  return paraParametro((await response.json()) as ParametroApiDto);
}

export async function deleteParametro(id: string): Promise<void> {
  const response = await fetch(`${BASE}/${encodeURIComponent(id)}`, {
    method: "DELETE",
    credentials: "include",
    headers: { [CSRF_HEADER]: "1" }
  });
  if (!response.ok && response.status !== 204) await lerErro(response, "Falha ao excluir Parametro.");
}
