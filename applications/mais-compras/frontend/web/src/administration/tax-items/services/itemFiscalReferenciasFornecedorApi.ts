import { ItemFiscalApiError, ItemFiscalAcessoNegadoError, ItemFiscalNaoAutenticadoError } from "./itensFiscaisApi";
import type {
  ItemFiscalReferenciaFornecedor,
  ItemFiscalReferenciaFornecedorCreateInput,
  ItemFiscalReferenciaFornecedorUpdateInput
} from "../types/itemFiscalReferenciaFornecedorTypes";

/**
 * Cliente HTTP real das Referências de Item Fiscal por Fornecedor (B3 - Bloco 4, Discovery homologado).
 * Sub-recurso de Item Fiscal - reaproveita as mesmas classes de erro de `itensFiscaisApi.ts` (mesmo
 * backend, mesmas permissões `ItemFiscal.Visualizar`/`ItemFiscal.Editar` - nenhuma permissão nova).
 *
 * Bloco 4 é exclusivamente local: sem sincronização com o Linx nesta etapa.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

function base(itemFiscalId: string): string {
  return `/api/administracao/itens-fiscais/${encodeURIComponent(itemFiscalId)}/referencias-fornecedor`;
}

type ApiErrorBody = { code?: string; message?: string };

type ItemFiscalReferenciaFornecedorApiDto = {
  id: string;
  itemFiscalId: string;
  fornecedorId: string;
  fornecedorNome: string;
  codigoItemFornecedor: string;
  criadoEm: string;
  atualizadoEm: string;
};

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

function paraReferencia(dto: ItemFiscalReferenciaFornecedorApiDto): ItemFiscalReferenciaFornecedor {
  return {
    id: dto.id,
    itemFiscalId: dto.itemFiscalId,
    fornecedorId: dto.fornecedorId,
    fornecedorNome: dto.fornecedorNome,
    codigoItemFornecedor: dto.codigoItemFornecedor,
    criadoEm: dto.criadoEm,
    atualizadoEm: dto.atualizadoEm
  };
}

export async function listReferenciasFornecedor(itemFiscalId: string): Promise<ItemFiscalReferenciaFornecedor[]> {
  const response = await fetch(base(itemFiscalId), { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar as referências por fornecedor.");
  const data = (await response.json()) as ItemFiscalReferenciaFornecedorApiDto[];
  return data.map(paraReferencia);
}

export async function createReferenciaFornecedor(
  itemFiscalId: string,
  input: ItemFiscalReferenciaFornecedorCreateInput
): Promise<ItemFiscalReferenciaFornecedor> {
  const response = await fetch(base(itemFiscalId), {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(input)
  });
  if (!response.ok) await lerErro(response, "Falha ao incluir a referência do fornecedor.");
  return paraReferencia((await response.json()) as ItemFiscalReferenciaFornecedorApiDto);
}

/** Nunca envia `fornecedorId`: imutável após a criação. */
export async function updateReferenciaFornecedor(
  itemFiscalId: string,
  id: string,
  input: ItemFiscalReferenciaFornecedorUpdateInput
): Promise<ItemFiscalReferenciaFornecedor> {
  const response = await fetch(`${base(itemFiscalId)}/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify(input)
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar a referência do fornecedor.");
  return paraReferencia((await response.json()) as ItemFiscalReferenciaFornecedorApiDto);
}

/** Remoção FÍSICA (não é inativação) - espelha a ausência de coluna de status comprovada em
 * `ITEM_FISCAL_REF_FORNECEDOR`. */
export async function deleteReferenciaFornecedor(itemFiscalId: string, id: string): Promise<void> {
  const response = await fetch(`${base(itemFiscalId)}/${encodeURIComponent(id)}`, {
    method: "DELETE",
    credentials: "include",
    headers: { [CSRF_HEADER]: "1" }
  });
  if (!response.ok && response.status !== 404) await lerErro(response, "Falha ao remover a referência do fornecedor.");
}
