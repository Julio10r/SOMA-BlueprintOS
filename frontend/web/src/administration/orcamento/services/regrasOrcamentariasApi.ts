import type { PeriodoOrcamentario, RegraOrcamentaria, RegraOrcamentariaInput } from "../types/regraOrcamentariaTypes";

/**
 * Cliente HTTP das Regras Orcamentarias por Unidade de Negocio (O1.12, ADR-0020 revisao R1.1). Protegido
 * por `Orcamento.Gerenciar`. CRUD administrativo sem exclusao fisica (apenas ativar/inativar) — mesmo
 * padrao de `identity-providers/services/identityProvidersApi.ts`. APENAS o cadastro: nenhuma reserva
 * contabil, consumo real ou bloqueio operacional acontece por meio destas chamadas.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";
const BASE = "/api/administracao/unidades-negocio";

type ApiErrorBody = { code?: string; message?: string };

type RegraOrcamentariaApiDto = {
  id: string;
  unidadeNegocioId: string;
  nome: string;
  centroCustoMetadadoId: string;
  valorLimite: number;
  periodo: PeriodoOrcamentario;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

export class RegraOrcamentariaApiError extends Error {
  readonly code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = "RegraOrcamentariaApiError";
    this.code = code;
  }
}

export class RegraOrcamentariaAcessoNegadoError extends RegraOrcamentariaApiError {
  constructor(message = "Você não tem permissão para acessar as Regras Orçamentárias.") {
    super(message, "acesso_negado");
    this.name = "RegraOrcamentariaAcessoNegadoError";
  }
}

export class RegraOrcamentariaNaoAutenticadoError extends RegraOrcamentariaApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "RegraOrcamentariaNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new RegraOrcamentariaNaoAutenticadoError();
  if (response.status === 403) throw new RegraOrcamentariaAcessoNegadoError();
  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new RegraOrcamentariaApiError(message, code);
}

function paraRegraOrcamentaria(dto: RegraOrcamentariaApiDto): RegraOrcamentaria {
  return {
    id: dto.id,
    unidadeNegocioId: dto.unidadeNegocioId,
    nome: dto.nome,
    centroCustoMetadadoId: dto.centroCustoMetadadoId,
    valorLimite: dto.valorLimite,
    periodo: dto.periodo,
    status: dto.ativo ? "Ativo" : "Inativo",
    criadoEm: dto.criadoEm,
    atualizadoEm: dto.atualizadoEm
  };
}

export async function listRegrasOrcamentarias(unidadeNegocioId: string): Promise<RegraOrcamentaria[]> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/regras-orcamentarias`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar Regras Orçamentárias.");
  const data = (await response.json()) as RegraOrcamentariaApiDto[];
  return data.map(paraRegraOrcamentaria);
}

export async function createRegraOrcamentaria(unidadeNegocioId: string, input: RegraOrcamentariaInput): Promise<RegraOrcamentaria> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/regras-orcamentarias`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({
      nome: input.nome,
      centroCustoMetadadoId: input.centroCustoMetadadoId,
      valorLimite: input.valorLimite,
      periodo: input.periodo
    })
  });
  if (!response.ok) await lerErro(response, "Falha ao criar Regra Orçamentária.");
  return paraRegraOrcamentaria((await response.json()) as RegraOrcamentariaApiDto);
}

export async function updateRegraOrcamentaria(unidadeNegocioId: string, id: string, input: RegraOrcamentariaInput): Promise<RegraOrcamentaria> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/regras-orcamentarias/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({
      nome: input.nome,
      centroCustoMetadadoId: input.centroCustoMetadadoId,
      valorLimite: input.valorLimite,
      periodo: input.periodo
    })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar Regra Orçamentária.");
  return paraRegraOrcamentaria((await response.json()) as RegraOrcamentariaApiDto);
}

export async function toggleStatusRegraOrcamentaria(unidadeNegocioId: string, regra: RegraOrcamentaria): Promise<RegraOrcamentaria> {
  const proximoAtivo = regra.status !== "Ativo";
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/regras-orcamentarias/${encodeURIComponent(regra.id)}/status`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ ativo: proximoAtivo })
  });
  if (!response.ok) await lerErro(response, "Falha ao alterar o status da Regra Orçamentária.");
  return paraRegraOrcamentaria((await response.json()) as RegraOrcamentariaApiDto);
}
