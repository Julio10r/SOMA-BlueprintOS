import type { FeatureFlag, FeatureFlagCriarInput } from "../types/featureFlagTypes";

/**
 * Cliente HTTP de Feature Flags (O1.11), protegido por `Sistema.Gerenciar`. Catalogo nasce vazio —
 * nenhuma flag ficticia e semeada. Ativacao/desativacao e por Unidade de Negocio (N:N).
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";
const BASE = "/api/administracao/feature-flags";

type ApiErrorBody = { code?: string; message?: string };

export class FeatureFlagApiError extends Error {
  readonly code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = "FeatureFlagApiError";
    this.code = code;
  }
}

export class FeatureFlagAcessoNegadoError extends FeatureFlagApiError {
  constructor(message = "Você não tem permissão para acessar as Feature Flags.") {
    super(message, "acesso_negado");
    this.name = "FeatureFlagAcessoNegadoError";
  }
}

export class FeatureFlagNaoAutenticadoError extends FeatureFlagApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "FeatureFlagNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new FeatureFlagNaoAutenticadoError();
  if (response.status === 403) throw new FeatureFlagAcessoNegadoError();
  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new FeatureFlagApiError(message, code);
}

export async function listFeatureFlags(): Promise<FeatureFlag[]> {
  const response = await fetch(BASE, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar Feature Flags.");
  return (await response.json()) as FeatureFlag[];
}

export async function createFeatureFlag(input: FeatureFlagCriarInput): Promise<FeatureFlag> {
  const response = await fetch(BASE, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(input)
  });
  if (!response.ok) await lerErro(response, "Falha ao criar Feature Flag.");
  return (await response.json()) as FeatureFlag;
}

export async function setFeatureFlagStatus(id: string, unidadeNegocioId: string, ativa: boolean): Promise<FeatureFlag> {
  const response = await fetch(`${BASE}/${encodeURIComponent(id)}/unidades-negocio/${encodeURIComponent(unidadeNegocioId)}/status`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ ativa })
  });
  if (!response.ok) await lerErro(response, "Falha ao alterar status da Feature Flag.");
  return (await response.json()) as FeatureFlag;
}
