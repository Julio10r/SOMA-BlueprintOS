import type { ConfiguracaoErp, ConfiguracaoErpInput } from "../types/configuracaoErpTypes";

/**
 * Cliente HTTP da Configuracao de ERP por Unidade de Negocio (O1.11, relacao 1:1). Protegido por
 * `ConfiguracaoErp.Gerenciar`. 404 (`configuracao_erp_nao_encontrada`) e tratado como estado "nao
 * configurado" — nunca como erro. Segredo nunca devolvido (apenas `parametrosConfigurados: boolean`).
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";
const BASE = "/api/administracao/unidades-negocio";

type ApiErrorBody = { code?: string; message?: string };

type ConfiguracaoErpApiDto = {
  id: string;
  unidadeNegocioId: string;
  sistemaErp: string;
  parametrosConfigurados: boolean;
  ativo: boolean;
};

export class ConfiguracaoErpApiError extends Error {
  readonly code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = "ConfiguracaoErpApiError";
    this.code = code;
  }
}

export class ConfiguracaoErpAcessoNegadoError extends ConfiguracaoErpApiError {
  constructor(message = "Você não tem permissão para acessar a Configuração de ERP.") {
    super(message, "acesso_negado");
    this.name = "ConfiguracaoErpAcessoNegadoError";
  }
}

export class ConfiguracaoErpNaoAutenticadoError extends ConfiguracaoErpApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "ConfiguracaoErpNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new ConfiguracaoErpNaoAutenticadoError();
  if (response.status === 403) throw new ConfiguracaoErpAcessoNegadoError();
  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new ConfiguracaoErpApiError(message, code);
}

function paraConfiguracaoErp(dto: ConfiguracaoErpApiDto): ConfiguracaoErp {
  return {
    id: dto.id,
    unidadeNegocioId: dto.unidadeNegocioId,
    sistemaErp: dto.sistemaErp,
    parametrosConfigurados: dto.parametrosConfigurados,
    status: dto.ativo ? "Ativo" : "Inativo"
  };
}

export async function getConfiguracaoErp(unidadeNegocioId: string): Promise<ConfiguracaoErp | null> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/configuracao-erp`, { credentials: "include" });
  if (response.status === 404) return null;
  if (!response.ok) await lerErro(response, "Falha ao carregar Configuração de ERP.");
  return paraConfiguracaoErp((await response.json()) as ConfiguracaoErpApiDto);
}

export async function salvarConfiguracaoErp(unidadeNegocioId: string, input: ConfiguracaoErpInput): Promise<ConfiguracaoErp> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/configuracao-erp`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ sistemaErp: input.sistemaErp, parametrosConexao: input.parametrosConexao || undefined })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar Configuração de ERP.");
  return paraConfiguracaoErp((await response.json()) as ConfiguracaoErpApiDto);
}

export async function toggleStatusConfiguracaoErp(unidadeNegocioId: string, configuracao: ConfiguracaoErp): Promise<ConfiguracaoErp> {
  const proximoAtivo = configuracao.status !== "Ativo";
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/configuracao-erp/status`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ ativo: proximoAtivo })
  });
  if (!response.ok) await lerErro(response, "Falha ao alterar o status da Configuração de ERP.");
  return paraConfiguracaoErp((await response.json()) as ConfiguracaoErpApiDto);
}
