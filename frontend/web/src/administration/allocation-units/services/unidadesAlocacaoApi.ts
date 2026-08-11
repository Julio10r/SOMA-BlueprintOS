import type { UnidadeAlocacao, UnidadeAlocacaoInput } from "../types/unidadeAlocacaoTypes";

/**
 * Cliente HTTP real da Gestao de Unidades de Alocacao (O1.8 — Persistencia Real). Substitui
 * integralmente o antigo `unidadesAlocacaoMockApi.ts` (dados em memoria), removido nesta sprint.
 *
 * Mesmo padrao de `administration/cost-centers/services/centrosCustoApi.ts` (O1.7) e
 * `administration/users/services/usuariosApi.ts` (O1.6): sessao via cookie HttpOnly
 * (`credentials: "include"`), cabecalho CSRF nas escritas, e nenhuma decisao de autorizacao acontece
 * aqui — o backend exige a permissao `UnidadeAlocacao.Gerenciar` em todos estes endpoints e responde
 * 401/403 independentemente do que a interface faca.
 *
 * Unidade de Alocacao e conceito exclusivo do +Compras (ADR-0020, item 4): ao contrario de Centro de
 * Custo, existe criacao real pela interface. O vinculo N:N com Centro de Custo (ADR-0020, item 5) e
 * escopo da O1.9 — nao existe aqui.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

/** Espelha `PerfisController.BaseRoute`/`UnidadesAlocacaoController` no backend. */
const BASE = "/api/administracao";

type ApiErrorBody = { code?: string; message?: string };

type UnidadeAlocacaoApiDto = {
  id: string;
  nome: string;
  descricao: string;
  unidadeNegocioId: string;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

export class UnidadeAlocacaoApiError extends Error {
  readonly code?: string;

  constructor(message: string, code?: string) {
    super(message);
    this.name = "UnidadeAlocacaoApiError";
    this.code = code;
  }
}

/** 403 — sessao valida, porem sem a permissao `UnidadeAlocacao.Gerenciar`. */
export class UnidadeAlocacaoAcessoNegadoError extends UnidadeAlocacaoApiError {
  constructor(message = "Voce nao tem permissao para acessar a Gestao de Unidades de Alocacao.") {
    super(message, "acesso_negado");
    this.name = "UnidadeAlocacaoAcessoNegadoError";
  }
}

/** 401 — sem sessao autenticada. */
export class UnidadeAlocacaoNaoAutenticadoError extends UnidadeAlocacaoApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "UnidadeAlocacaoNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new UnidadeAlocacaoNaoAutenticadoError();
  if (response.status === 403) throw new UnidadeAlocacaoAcessoNegadoError();

  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new UnidadeAlocacaoApiError(message, code);
}

function paraUnidadeAlocacao(dto: UnidadeAlocacaoApiDto): UnidadeAlocacao {
  return {
    id: dto.id,
    nome: dto.nome,
    descricao: dto.descricao,
    unidadeNegocioId: dto.unidadeNegocioId,
    status: dto.ativo ? "Ativo" : "Inativo",
    criadoEm: dto.criadoEm,
    atualizadoEm: dto.atualizadoEm
  };
}

export async function listUnidadesAlocacao(): Promise<UnidadeAlocacao[]> {
  const response = await fetch(`${BASE}/unidades-alocacao`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar unidades de alocacao.");
  const data = (await response.json()) as UnidadeAlocacaoApiDto[];
  return data.map(paraUnidadeAlocacao);
}

export async function getUnidadeAlocacao(id: string): Promise<UnidadeAlocacao | null> {
  const response = await fetch(`${BASE}/unidades-alocacao/${encodeURIComponent(id)}`, { credentials: "include" });
  if (response.status === 404) return null;
  if (!response.ok) await lerErro(response, "Falha ao carregar unidade de alocacao.");
  return paraUnidadeAlocacao((await response.json()) as UnidadeAlocacaoApiDto);
}

export async function createUnidadeAlocacao(input: UnidadeAlocacaoInput): Promise<UnidadeAlocacao> {
  const response = await fetch(`${BASE}/unidades-alocacao`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ nome: input.nome, descricao: input.descricao })
  });
  if (!response.ok) await lerErro(response, "Falha ao criar unidade de alocacao.");
  return paraUnidadeAlocacao((await response.json()) as UnidadeAlocacaoApiDto);
}

export async function updateUnidadeAlocacao(id: string, input: UnidadeAlocacaoInput): Promise<UnidadeAlocacao> {
  const response = await fetch(`${BASE}/unidades-alocacao/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ nome: input.nome, descricao: input.descricao })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar unidade de alocacao.");
  return paraUnidadeAlocacao((await response.json()) as UnidadeAlocacaoApiDto);
}

export async function toggleStatusUnidadeAlocacao(unidadeAlocacao: UnidadeAlocacao): Promise<UnidadeAlocacao> {
  const proximoAtivo = unidadeAlocacao.status !== "Ativo";
  const response = await fetch(`${BASE}/unidades-alocacao/${encodeURIComponent(unidadeAlocacao.id)}/status`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ ativo: proximoAtivo })
  });
  if (!response.ok) await lerErro(response, "Falha ao alterar o status da unidade de alocacao.");
  return paraUnidadeAlocacao((await response.json()) as UnidadeAlocacaoApiDto);
}
