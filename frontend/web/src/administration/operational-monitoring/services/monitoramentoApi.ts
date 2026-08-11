import type {
  DispararSincronizacaoErpResultado,
  FornecedorSincronizacaoHistorico,
  ListarSincronizacoesFiltro,
  ListarSincronizacoesResultado,
  SincronizacaoFornecedorDetalhe
} from "../types/monitoramentoTypes";

/**
 * Cliente HTTP da Administracao Operacional e Monitoramento (O1.13). Reaproveita 100% a infraestrutura
 * real de sincronizacao de fornecedores de B2.1.3 — nenhum mock, nenhum motor novo. Protegido por
 * `Sistema.Gerenciar`, mesmo padrao de `feature-flags/services/featureFlagsApi.ts`.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";
const MONITOR_BASE = "/api/administracao/monitoramento";
const FORNECEDORES_BASE = "/api/fornecedores";

type ApiErrorBody = { code?: string; message?: string };

export class MonitoramentoApiError extends Error {
  readonly code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = "MonitoramentoApiError";
    this.code = code;
  }
}

export class MonitoramentoAcessoNegadoError extends MonitoramentoApiError {
  constructor(message = "Voce nao tem permissao para acessar o Monitoramento Operacional.") {
    super(message, "acesso_negado");
    this.name = "MonitoramentoAcessoNegadoError";
  }
}

export class MonitoramentoNaoAutenticadoError extends MonitoramentoApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "MonitoramentoNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new MonitoramentoNaoAutenticadoError();
  if (response.status === 403) throw new MonitoramentoAcessoNegadoError();
  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new MonitoramentoApiError(message, code);
}

export async function listarSincronizacoesFornecedores(
  filtro: ListarSincronizacoesFiltro = {}
): Promise<ListarSincronizacoesResultado> {
  const params = new URLSearchParams();
  if (filtro.status) params.set("status", filtro.status);
  if (filtro.businessUnit) params.set("businessUnit", filtro.businessUnit);
  params.set("pagina", String(filtro.pagina ?? 1));
  params.set("tamanhoPagina", String(filtro.tamanhoPagina ?? 20));

  const response = await fetch(`${MONITOR_BASE}/sincronizacoes-fornecedores?${params.toString()}`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar as execucoes de sincronizacao de fornecedores.");
  return (await response.json()) as ListarSincronizacoesResultado;
}

export async function obterSincronizacaoFornecedor(id: string): Promise<SincronizacaoFornecedorDetalhe> {
  const response = await fetch(`${MONITOR_BASE}/sincronizacoes-fornecedores/${encodeURIComponent(id)}`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar o detalhe da execucao de sincronizacao.");
  return (await response.json()) as SincronizacaoFornecedorDetalhe;
}

export async function dispararSincronizacaoErp(businessUnit: string): Promise<DispararSincronizacaoErpResultado> {
  const params = new URLSearchParams({ businessUnit });
  const response = await fetch(`${FORNECEDORES_BASE}/sincronizar-erp?${params.toString()}`, {
    method: "GET",
    credentials: "include",
    headers: { [CSRF_HEADER]: "1" }
  });
  if (!response.ok) await lerErro(response, "Falha ao disparar a sincronizacao de fornecedores.");
  return (await response.json()) as DispararSincronizacaoErpResultado;
}

export async function obterHistoricoFornecedor(fornecedorId: string): Promise<FornecedorSincronizacaoHistorico[]> {
  const response = await fetch(`${FORNECEDORES_BASE}/${encodeURIComponent(fornecedorId)}/sincronizacoes`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar o historico de sincronizacao do fornecedor.");
  return (await response.json()) as FornecedorSincronizacaoHistorico[];
}
