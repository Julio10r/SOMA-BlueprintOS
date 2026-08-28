import type { RegraWorkflow, RegraWorkflowInput } from "../types/regraWorkflowTypes";

/**
 * Cliente HTTP das Regras de Workflow por Unidade de Negocio (O1.12, ADR-0020 revisao R1.1). Protegido
 * por `Workflow.Gerenciar`. CRUD administrativo sem exclusao fisica (apenas ativar/inativar) — mesmo
 * padrao de `identity-providers/services/identityProvidersApi.ts`. Nenhum motor de execucao de workflow e
 * acionado por estas chamadas.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";
const BASE = "/api/administracao/unidades-negocio";

type ApiErrorBody = { code?: string; message?: string };

type RegraWorkflowApiDto = {
  id: string;
  unidadeNegocioId: string;
  nome: string;
  tipoProcesso: string;
  ordem: number;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

export class RegraWorkflowApiError extends Error {
  readonly code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = "RegraWorkflowApiError";
    this.code = code;
  }
}

export class RegraWorkflowAcessoNegadoError extends RegraWorkflowApiError {
  constructor(message = "Você não tem permissão para acessar as Regras de Workflow.") {
    super(message, "acesso_negado");
    this.name = "RegraWorkflowAcessoNegadoError";
  }
}

export class RegraWorkflowNaoAutenticadoError extends RegraWorkflowApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "RegraWorkflowNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new RegraWorkflowNaoAutenticadoError();
  if (response.status === 403) throw new RegraWorkflowAcessoNegadoError();
  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new RegraWorkflowApiError(message, code);
}

function paraRegraWorkflow(dto: RegraWorkflowApiDto): RegraWorkflow {
  return {
    id: dto.id,
    unidadeNegocioId: dto.unidadeNegocioId,
    nome: dto.nome,
    tipoProcesso: dto.tipoProcesso,
    ordem: dto.ordem,
    status: dto.ativo ? "Ativo" : "Inativo",
    criadoEm: dto.criadoEm,
    atualizadoEm: dto.atualizadoEm
  };
}

export async function listRegrasWorkflow(unidadeNegocioId: string): Promise<RegraWorkflow[]> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/regras-workflow`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar Regras de Workflow.");
  const data = (await response.json()) as RegraWorkflowApiDto[];
  return data.map(paraRegraWorkflow);
}

export async function createRegraWorkflow(unidadeNegocioId: string, input: RegraWorkflowInput): Promise<RegraWorkflow> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/regras-workflow`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ nome: input.nome, tipoProcesso: input.tipoProcesso, ordem: input.ordem })
  });
  if (!response.ok) await lerErro(response, "Falha ao criar Regra de Workflow.");
  return paraRegraWorkflow((await response.json()) as RegraWorkflowApiDto);
}

export async function updateRegraWorkflow(unidadeNegocioId: string, id: string, input: RegraWorkflowInput): Promise<RegraWorkflow> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/regras-workflow/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ nome: input.nome, tipoProcesso: input.tipoProcesso, ordem: input.ordem })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar Regra de Workflow.");
  return paraRegraWorkflow((await response.json()) as RegraWorkflowApiDto);
}

export async function toggleStatusRegraWorkflow(unidadeNegocioId: string, regra: RegraWorkflow): Promise<RegraWorkflow> {
  const proximoAtivo = regra.status !== "Ativo";
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/regras-workflow/${encodeURIComponent(regra.id)}/status`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ ativo: proximoAtivo })
  });
  if (!response.ok) await lerErro(response, "Falha ao alterar o status da Regra de Workflow.");
  return paraRegraWorkflow((await response.json()) as RegraWorkflowApiDto);
}
