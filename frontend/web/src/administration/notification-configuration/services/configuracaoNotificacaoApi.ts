import type { ConfiguracaoNotificacao, ConfiguracaoNotificacaoInput } from "../types/configuracaoNotificacaoTypes";

/**
 * Cliente HTTP da Configuracao de Notificacoes por Unidade de Negocio (O1.11, item #24, relacao 1:1).
 * Protegido por `Sistema.Gerenciar` (mesma permissao de Identity Providers/Parametros/Feature Flags).
 * 404 (`configuracao_notificacao_nao_encontrada`) e tratado como estado "nao configurado" — nunca como
 * erro. ESCOPO MINIMO DE FUNDACAO: nenhum envio real de e-mail acontece por meio desta tela/API.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";
const BASE = "/api/administracao/unidades-negocio";

type ApiErrorBody = { code?: string; message?: string };

type ConfiguracaoNotificacaoApiDto = {
  id: string;
  unidadeNegocioId: string;
  emailAtivado: boolean;
  emailRemetente: string | null;
  nomeRemetente: string | null;
};

export class ConfiguracaoNotificacaoApiError extends Error {
  readonly code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = "ConfiguracaoNotificacaoApiError";
    this.code = code;
  }
}

export class ConfiguracaoNotificacaoAcessoNegadoError extends ConfiguracaoNotificacaoApiError {
  constructor(message = "Voce nao tem permissao para acessar a Configuracao de Notificacoes.") {
    super(message, "acesso_negado");
    this.name = "ConfiguracaoNotificacaoAcessoNegadoError";
  }
}

export class ConfiguracaoNotificacaoNaoAutenticadoError extends ConfiguracaoNotificacaoApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "ConfiguracaoNotificacaoNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new ConfiguracaoNotificacaoNaoAutenticadoError();
  if (response.status === 403) throw new ConfiguracaoNotificacaoAcessoNegadoError();
  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new ConfiguracaoNotificacaoApiError(message, code);
}

function paraConfiguracaoNotificacao(dto: ConfiguracaoNotificacaoApiDto): ConfiguracaoNotificacao {
  return {
    id: dto.id,
    unidadeNegocioId: dto.unidadeNegocioId,
    emailAtivado: dto.emailAtivado,
    emailRemetente: dto.emailRemetente,
    nomeRemetente: dto.nomeRemetente
  };
}

export async function getConfiguracaoNotificacao(unidadeNegocioId: string): Promise<ConfiguracaoNotificacao | null> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/configuracao-notificacao`, { credentials: "include" });
  if (response.status === 404) return null;
  if (!response.ok) await lerErro(response, "Falha ao carregar Configuracao de Notificacoes.");
  return paraConfiguracaoNotificacao((await response.json()) as ConfiguracaoNotificacaoApiDto);
}

export async function salvarConfiguracaoNotificacao(
  unidadeNegocioId: string,
  input: ConfiguracaoNotificacaoInput
): Promise<ConfiguracaoNotificacao> {
  const response = await fetch(`${BASE}/${encodeURIComponent(unidadeNegocioId)}/configuracao-notificacao`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({
      emailAtivado: input.emailAtivado,
      emailRemetente: input.emailRemetente || undefined,
      nomeRemetente: input.nomeRemetente || undefined
    })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar Configuracao de Notificacoes.");
  return paraConfiguracaoNotificacao((await response.json()) as ConfiguracaoNotificacaoApiDto);
}
