import type {
  ConsultaCnpjResultado,
  Fornecedor,
  FornecedorEnriquecimentoAnalise
} from "../types/linxSupplierContract";

type RequestOptions = {
  businessUnit: string;
  erpSistema?: string;
  correlationId: string;
};

const headers = { "Content-Type": "application/json" };

/**
 * Base da API +Compras. Em desenvolvimento/testes, permanece vazia por
 * padrao para que as chamadas usem caminhos relativos (ex: "/fornecedores"),
 * aproveitando o proxy configurado em vite.config.ts. Quando
 * VITE_API_BASE_URL estiver definido (ver .env.example), as chamadas
 * passam a usar essa origem diretamente (util quando frontend e backend
 * rodam em processos/portas separados sem proxy).
 */
const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");

function apiUrl(path: string): string {
  return `${apiBaseUrl}${path}`;
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init);
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message ?? `Falha HTTP ${response.status}`);
  }
  return response.json() as Promise<T>;
}

export async function searchSupplierByDocument(cnpjCpf: string): Promise<Fornecedor | null> {
  const suppliers = await request<Fornecedor[]>(apiUrl(`/fornecedores?q=${encodeURIComponent(cnpjCpf)}`));
  const normalized = normalizeDocument(cnpjCpf);
  return suppliers.find((supplier) => normalizeDocument(supplier.cnpj_Cpf) === normalized) ?? null;
}

/**
 * Lista fornecedores cadastrados (sem filtro), usada apenas para telas de
 * visao executiva (ex: Dashboard). Reutiliza o mesmo endpoint
 * GET /fornecedores?q= ja consumido pelo fluxo de cadastro.
 */
export async function listSuppliers(): Promise<Fornecedor[]> {
  return request<Fornecedor[]>(apiUrl("/fornecedores?q="));
}

export async function createSupplierDraft(cnpjCpf: string, consulta?: ConsultaCnpjResultado): Promise<Fornecedor> {
  const razaoSocial = consulta?.razaoSocial?.trim() || `Fornecedor ${cnpjCpf}`;
  return request<Fornecedor>(apiUrl("/fornecedores"), {
    method: "POST",
    headers,
    body: JSON.stringify({
      nome: razaoSocial,
      cnpj: cnpjCpf,
      cnpj_Cpf: cnpjCpf,
      razaoSocial,
      tipoPessoa: consulta?.tipoPessoa ?? resolveTipoPessoa(cnpjCpf),
      categoria: null,
      email: consulta?.email ?? null,
      telefone: consulta?.telefone ?? null,
      website: null,
      cidade: consulta?.cidade ?? null,
      estado: consulta?.estado ?? null,
      pais: consulta?.pais ?? "BR",
      status: "Ativo",
      scoreIA: null,
      beneficiador: false,
      licenciado: false,
      // CNAE principal (B2.8): vem exclusivamente da consulta oficial ja revisada em tela — nunca
      // editavel neste formulario. So e enviado nesta chamada explicita de cadastro, nunca durante
      // a consulta em si (consultar != persistir).
      cnaePrincipalCodigo: consulta?.cnaePrincipalCodigo ?? null,
      cnaePrincipalDescricao: consulta?.cnaePrincipalDescricao ?? null
    })
  });
}

export async function consultCnpj(cnpjCpf: string, options: RequestOptions): Promise<ConsultaCnpjResultado> {
  return request<ConsultaCnpjResultado>(apiUrl("/fornecedores/consulta-cnpj"), {
    method: "POST",
    headers,
    body: JSON.stringify({
      cnpj_Cpf: cnpjCpf,
      businessUnit: options.businessUnit,
      erpSistema: options.erpSistema,
      correlationId: options.correlationId
    })
  });
}

export async function analyzeEnrichment(
  supplierId: string,
  consulta: ConsultaCnpjResultado,
  options: RequestOptions
): Promise<FornecedorEnriquecimentoAnalise> {
  return request<FornecedorEnriquecimentoAnalise>(apiUrl(`/fornecedores/${supplierId}/enriquecimento-cnpj`), {
    method: "POST",
    headers,
    body: JSON.stringify({ consulta, consultaId: null, ...toBackendOptions(options) })
  });
}

export async function decideEnrichment(
  supplierId: string,
  decision: "aprovar" | "rejeitar",
  consulta: ConsultaCnpjResultado,
  campos: string[],
  options: RequestOptions
): Promise<FornecedorEnriquecimentoAnalise> {
  return request<FornecedorEnriquecimentoAnalise>(apiUrl(`/fornecedores/${supplierId}/enriquecimento-cnpj/${decision}`), {
    method: "POST",
    headers,
    body: JSON.stringify({ consulta, consultaId: null, campos, ...toBackendOptions(options) })
  });
}

function toBackendOptions(options: RequestOptions) {
  return {
    businessUnit: options.businessUnit,
    erpSistema: options.erpSistema,
    correlationId: options.correlationId
  };
}

export function normalizeDocument(value: string): string {
  return value.trim().replace(/[^A-Za-z0-9]/g, "").toUpperCase().slice(0, 14);
}

function resolveTipoPessoa(cnpjCpf: string): string {
  return normalizeDocument(cnpjCpf).length <= 11 ? "PF" : "PJ";
}
