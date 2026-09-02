import type { ItemFiscal, ItemFiscalCreateInput, ItemFiscalUpdateInput } from "../types/itemFiscalTypes";

/**
 * Cliente HTTP real do cadastro local de Item Fiscal (B3 - Bloco 3, Discovery homologado). Mesmo padrao
 * de `administration/allocation-units/services/unidadesAlocacaoApi.ts`: sessao via cookie HttpOnly
 * (`credentials: "include"`), cabecalho CSRF nas escritas, nenhuma decisao de autorizacao acontece aqui -
 * o backend exige permissoes separadas por acao (`ItemFiscal.Visualizar`/`Criar`/`Editar`/`Inativar`).
 *
 * Bloco 3 e exclusivamente local: sem sincronizacao com o Linx nesta etapa.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

/** Espelha `PerfisController.BaseRoute`/`ItensFiscaisController` no backend. */
const BASE = "/api/administracao";

type ApiErrorBody = { code?: string; message?: string };

type ItemFiscalApiDto = {
  id: string;
  codigo: string;
  descricao: string;
  unidadeMedidaCodigoErp: string;
  unidadeMedidaDescricao?: string | null;
  contaContabilCodigoErp: string;
  contaContabilDescricao?: string | null;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

export class ItemFiscalApiError extends Error {
  readonly code?: string;

  constructor(message: string, code?: string) {
    super(message);
    this.name = "ItemFiscalApiError";
    this.code = code;
  }
}

/** 403 - sessao valida, porem sem a permissao exigida pela acao. */
export class ItemFiscalAcessoNegadoError extends ItemFiscalApiError {
  constructor(message = "Você não tem permissão para executar esta ação no cadastro de Item Fiscal.") {
    super(message, "acesso_negado");
    this.name = "ItemFiscalAcessoNegadoError";
  }
}

/** 401 - sem sessao autenticada. */
export class ItemFiscalNaoAutenticadoError extends ItemFiscalApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "ItemFiscalNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new ItemFiscalNaoAutenticadoError();
  if (response.status === 403) throw new ItemFiscalAcessoNegadoError();

  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON - mensagem generica mantida.
  }
  throw new ItemFiscalApiError(message, code);
}

function paraItemFiscal(dto: ItemFiscalApiDto): ItemFiscal {
  return {
    id: dto.id,
    codigo: dto.codigo,
    descricao: dto.descricao,
    unidadeMedidaCodigoErp: dto.unidadeMedidaCodigoErp,
    unidadeMedidaDescricao: dto.unidadeMedidaDescricao ?? undefined,
    contaContabilCodigoErp: dto.contaContabilCodigoErp,
    contaContabilDescricao: dto.contaContabilDescricao ?? undefined,
    ativo: dto.ativo,
    criadoEm: dto.criadoEm,
    atualizadoEm: dto.atualizadoEm
  };
}

export async function listItensFiscais(): Promise<ItemFiscal[]> {
  const response = await fetch(`${BASE}/itens-fiscais`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar itens fiscais.");
  const data = (await response.json()) as ItemFiscalApiDto[];
  return data.map(paraItemFiscal);
}

export async function getItemFiscal(id: string): Promise<ItemFiscal | null> {
  const response = await fetch(`${BASE}/itens-fiscais/${encodeURIComponent(id)}`, { credentials: "include" });
  if (response.status === 404) return null;
  if (!response.ok) await lerErro(response, "Falha ao carregar item fiscal.");
  return paraItemFiscal((await response.json()) as ItemFiscalApiDto);
}

export async function createItemFiscal(input: ItemFiscalCreateInput): Promise<ItemFiscal> {
  const response = await fetch(`${BASE}/itens-fiscais`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(input)
  });
  if (!response.ok) await lerErro(response, "Falha ao criar item fiscal.");
  return paraItemFiscal((await response.json()) as ItemFiscalApiDto);
}

/** Nunca envia `codigo`: imutável após a criação. */
export async function updateItemFiscal(id: string, input: ItemFiscalUpdateInput): Promise<ItemFiscal> {
  const response = await fetch(`${BASE}/itens-fiscais/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(input)
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar item fiscal.");
  return paraItemFiscal((await response.json()) as ItemFiscalApiDto);
}

export async function toggleStatusItemFiscal(item: ItemFiscal): Promise<ItemFiscal> {
  const proximoAtivo = !item.ativo;
  const response = await fetch(`${BASE}/itens-fiscais/${encodeURIComponent(item.id)}/status`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ ativo: proximoAtivo })
  });
  if (!response.ok) await lerErro(response, "Falha ao alterar o status do item fiscal.");
  return paraItemFiscal((await response.json()) as ItemFiscalApiDto);
}
