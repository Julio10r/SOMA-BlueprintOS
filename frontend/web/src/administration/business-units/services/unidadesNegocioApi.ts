import type { UnidadeNegocio, UnidadeNegocioCriarInput, UnidadeNegocioEditarInput } from "../types/unidadeNegocioTypes";

/**
 * Cliente HTTP do CRUD de Unidades de Negocio (O1.11). Recurso corporativo protegido por
 * `UnidadeNegocio.Gerenciar` — nunca escopado pela UN de quem administra. Sem exclusao fisica: apenas
 * Criar, Editar (somente Nome — Slug e imutavel) e Ativar/Inativar. Mesmo padrao de sessao via cookie
 * HttpOnly e cabecalho CSRF de `administration/allocation-units/services/unidadesAlocacaoApi.ts`.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";
const BASE = "/api/administracao/unidades-negocio";

type ApiErrorBody = { code?: string; message?: string };

type UnidadeNegocioApiDto = { id: string; nome: string; slug: string; ativa: boolean };

export class UnidadeNegocioApiError extends Error {
  readonly code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = "UnidadeNegocioApiError";
    this.code = code;
  }
}

export class UnidadeNegocioAcessoNegadoError extends UnidadeNegocioApiError {
  constructor(message = "Voce nao tem permissao para acessar o Cadastro de Unidades de Negocio.") {
    super(message, "acesso_negado");
    this.name = "UnidadeNegocioAcessoNegadoError";
  }
}

export class UnidadeNegocioNaoAutenticadoError extends UnidadeNegocioApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "UnidadeNegocioNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new UnidadeNegocioNaoAutenticadoError();
  if (response.status === 403) throw new UnidadeNegocioAcessoNegadoError();
  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new UnidadeNegocioApiError(message, code);
}

function paraUnidadeNegocio(dto: UnidadeNegocioApiDto): UnidadeNegocio {
  return { id: dto.id, nome: dto.nome, slug: dto.slug, status: dto.ativa ? "Ativo" : "Inativo" };
}

export async function listUnidadesNegocio(): Promise<UnidadeNegocio[]> {
  const response = await fetch(BASE, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar Unidades de Negocio.");
  const data = (await response.json()) as UnidadeNegocioApiDto[];
  return data.map(paraUnidadeNegocio);
}

export async function createUnidadeNegocio(input: UnidadeNegocioCriarInput): Promise<UnidadeNegocio> {
  const response = await fetch(BASE, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ nome: input.nome, slug: input.slug })
  });
  if (!response.ok) await lerErro(response, "Falha ao criar Unidade de Negocio.");
  return paraUnidadeNegocio((await response.json()) as UnidadeNegocioApiDto);
}

export async function updateUnidadeNegocio(id: string, input: UnidadeNegocioEditarInput): Promise<UnidadeNegocio> {
  const response = await fetch(`${BASE}/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ nome: input.nome })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar Unidade de Negocio.");
  return paraUnidadeNegocio((await response.json()) as UnidadeNegocioApiDto);
}

export async function toggleStatusUnidadeNegocio(unidadeNegocio: UnidadeNegocio): Promise<UnidadeNegocio> {
  const proximoAtivo = unidadeNegocio.status !== "Ativo";
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocio.id)}/status`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ ativa: proximoAtivo })
  });
  if (!response.ok) await lerErro(response, "Falha ao alterar o status da Unidade de Negocio.");
  return paraUnidadeNegocio((await response.json()) as UnidadeNegocioApiDto);
}
