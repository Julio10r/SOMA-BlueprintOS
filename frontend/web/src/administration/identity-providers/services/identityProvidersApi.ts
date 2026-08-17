import type { IdentityProvider, IdentityProviderInput } from "../types/identityProviderTypes";

/**
 * Cliente HTTP dos Identity Providers por Unidade de Negocio (O1.11). Protegido por
 * `Sistema.Gerenciar`. O segredo (`parametros`) nunca e devolvido pela API — apenas
 * `parametrosConfigurados: boolean`; o formulario nunca pre-preenche este campo.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";
const BASE = "/api/administracao/unidades-negocio";

type ApiErrorBody = { code?: string; message?: string };

type IdentityProviderApiDto = {
  id: string;
  unidadeNegocioId: string;
  tipo: string;
  dominiosAutorizados: string[];
  parametrosConfigurados: boolean;
  ativo: boolean;
};

export class IdentityProviderApiError extends Error {
  readonly code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = "IdentityProviderApiError";
    this.code = code;
  }
}

export class IdentityProviderAcessoNegadoError extends IdentityProviderApiError {
  constructor(message = "Você não tem permissão para acessar os Identity Providers.") {
    super(message, "acesso_negado");
    this.name = "IdentityProviderAcessoNegadoError";
  }
}

export class IdentityProviderNaoAutenticadoError extends IdentityProviderApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "IdentityProviderNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new IdentityProviderNaoAutenticadoError();
  if (response.status === 403) throw new IdentityProviderAcessoNegadoError();
  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new IdentityProviderApiError(message, code);
}

function paraIdentityProvider(dto: IdentityProviderApiDto): IdentityProvider {
  return {
    id: dto.id,
    unidadeNegocioId: dto.unidadeNegocioId,
    tipo: dto.tipo,
    dominiosAutorizados: dto.dominiosAutorizados,
    parametrosConfigurados: dto.parametrosConfigurados,
    status: dto.ativo ? "Ativo" : "Inativo"
  };
}

export async function listIdentityProviders(unidadeNegocioId: string): Promise<IdentityProvider[]> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/identity-providers`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar Identity Providers.");
  const data = (await response.json()) as IdentityProviderApiDto[];
  return data.map(paraIdentityProvider);
}

function corpoInput(input: IdentityProviderInput) {
  return { tipo: input.tipo, dominiosAutorizados: input.dominiosAutorizados, parametros: input.parametros || undefined };
}

export async function createIdentityProvider(unidadeNegocioId: string, input: IdentityProviderInput): Promise<IdentityProvider> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/identity-providers`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(corpoInput(input))
  });
  if (!response.ok) await lerErro(response, "Falha ao criar Identity Provider.");
  return paraIdentityProvider((await response.json()) as IdentityProviderApiDto);
}

export async function updateIdentityProvider(unidadeNegocioId: string, id: string, input: IdentityProviderInput): Promise<IdentityProvider> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/identity-providers/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(corpoInput(input))
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar Identity Provider.");
  return paraIdentityProvider((await response.json()) as IdentityProviderApiDto);
}

export async function toggleStatusIdentityProvider(unidadeNegocioId: string, provider: IdentityProvider): Promise<IdentityProvider> {
  const proximoAtivo = provider.status !== "Ativo";
  const response = await fetch(
    `${BASE}/${encodeURIComponent(unidadeNegocioId)}/identity-providers/${encodeURIComponent(provider.id)}/status`,
    {
      method: "PATCH",
      credentials: "include",
      headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
      body: JSON.stringify({ ativo: proximoAtivo })
    }
  );
  if (!response.ok) await lerErro(response, "Falha ao alterar o status do Identity Provider.");
  return paraIdentityProvider((await response.json()) as IdentityProviderApiDto);
}
