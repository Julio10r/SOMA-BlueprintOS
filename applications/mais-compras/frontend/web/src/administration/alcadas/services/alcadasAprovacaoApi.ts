import type { AlcadaAprovacao, AlcadaAprovacaoInput, CriterioAlcada } from "../types/alcadaAprovacaoTypes";

/**
 * Cliente HTTP das Alcadas de Aprovacao por Unidade de Negocio (O1.12, ADR-0020 revisao R1.1). Protegido
 * por `Alcada.Gerenciar`. CRUD administrativo sem exclusao fisica (apenas ativar/inativar) — mesmo padrao
 * de `identity-providers/services/identityProvidersApi.ts`. Nenhum motor de avaliacao/execucao de
 * aprovacao e acionado por estas chamadas.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";
const BASE = "/api/administracao/unidades-negocio";

type ApiErrorBody = { code?: string; message?: string };

type AlcadaAprovacaoApiDto = {
  id: string;
  unidadeNegocioId: string;
  nome: string;
  criterio: CriterioAlcada;
  valorMinimo: number | null;
  valorMaximo: number | null;
  centroCustoMetadadoId: string | null;
  nivel: number;
  aprovadorUsuarioId: string | null;
  aprovadorPerfilId: string | null;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

export class AlcadaAprovacaoApiError extends Error {
  readonly code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = "AlcadaAprovacaoApiError";
    this.code = code;
  }
}

export class AlcadaAprovacaoAcessoNegadoError extends AlcadaAprovacaoApiError {
  constructor(message = "Você não tem permissão para acessar as Alçadas de Aprovação.") {
    super(message, "acesso_negado");
    this.name = "AlcadaAprovacaoAcessoNegadoError";
  }
}

export class AlcadaAprovacaoNaoAutenticadoError extends AlcadaAprovacaoApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "AlcadaAprovacaoNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new AlcadaAprovacaoNaoAutenticadoError();
  if (response.status === 403) throw new AlcadaAprovacaoAcessoNegadoError();
  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new AlcadaAprovacaoApiError(message, code);
}

function paraAlcadaAprovacao(dto: AlcadaAprovacaoApiDto): AlcadaAprovacao {
  return {
    id: dto.id,
    unidadeNegocioId: dto.unidadeNegocioId,
    nome: dto.nome,
    criterio: dto.criterio,
    valorMinimo: dto.valorMinimo,
    valorMaximo: dto.valorMaximo,
    centroCustoMetadadoId: dto.centroCustoMetadadoId,
    nivel: dto.nivel,
    aprovadorUsuarioId: dto.aprovadorUsuarioId,
    aprovadorPerfilId: dto.aprovadorPerfilId,
    status: dto.ativo ? "Ativo" : "Inativo",
    criadoEm: dto.criadoEm,
    atualizadoEm: dto.atualizadoEm
  };
}

function corpoInput(input: AlcadaAprovacaoInput) {
  return {
    nome: input.nome,
    criterio: input.criterio,
    valorMinimo: input.valorMinimo ?? null,
    valorMaximo: input.valorMaximo ?? null,
    centroCustoMetadadoId: input.centroCustoMetadadoId || null,
    nivel: input.nivel,
    aprovadorUsuarioId: input.aprovadorUsuarioId || null,
    aprovadorPerfilId: input.aprovadorPerfilId || null
  };
}

export async function listAlcadasAprovacao(unidadeNegocioId: string): Promise<AlcadaAprovacao[]> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/alcadas-aprovacao`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar Alçadas de Aprovação.");
  const data = (await response.json()) as AlcadaAprovacaoApiDto[];
  return data.map(paraAlcadaAprovacao);
}

export async function createAlcadaAprovacao(unidadeNegocioId: string, input: AlcadaAprovacaoInput): Promise<AlcadaAprovacao> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/alcadas-aprovacao`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(corpoInput(input))
  });
  if (!response.ok) await lerErro(response, "Falha ao criar Alçada de Aprovação.");
  return paraAlcadaAprovacao((await response.json()) as AlcadaAprovacaoApiDto);
}

export async function updateAlcadaAprovacao(unidadeNegocioId: string, id: string, input: AlcadaAprovacaoInput): Promise<AlcadaAprovacao> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/alcadas-aprovacao/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(corpoInput(input))
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar Alçada de Aprovação.");
  return paraAlcadaAprovacao((await response.json()) as AlcadaAprovacaoApiDto);
}

export async function toggleStatusAlcadaAprovacao(unidadeNegocioId: string, alcada: AlcadaAprovacao): Promise<AlcadaAprovacao> {
  const proximoAtivo = alcada.status !== "Ativo";
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/alcadas-aprovacao/${encodeURIComponent(alcada.id)}/status`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ ativo: proximoAtivo })
  });
  if (!response.ok) await lerErro(response, "Falha ao alterar o status da Alçada de Aprovação.");
  return paraAlcadaAprovacao((await response.json()) as AlcadaAprovacaoApiDto);
}
