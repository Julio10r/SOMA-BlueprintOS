import type {
  ConsultaCnpjResultado,
  Fornecedor,
  FornecedorEnriquecimentoAnalise,
  FornecedorPesquisaPaginada,
  FornecedorPesquisaParametros,
  ManualFornecedorDraft
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

/**
 * Pesquisa paginada, filtrável por status e ordenável de Fornecedores (tela de listagem/browse).
 * Consome GET /fornecedores?q=&status=&sort=&page=&pageSize= (backend O1.x).
 */
export async function searchFornecedoresPaginado(
  params: FornecedorPesquisaParametros,
  signal?: AbortSignal
): Promise<FornecedorPesquisaPaginada> {
  const query = new URLSearchParams();
  if (params.q) query.set("q", params.q);
  if (params.status && params.status !== "Todos") query.set("status", params.status);
  if (params.sort) query.set("sort", params.sort);
  query.set("page", String(params.page ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  return request<FornecedorPesquisaPaginada>(apiUrl(`/fornecedores?${query.toString()}`), { signal });
}

export async function getFornecedor(id: string): Promise<Fornecedor | null> {
  const response = await fetch(apiUrl(`/fornecedores/${encodeURIComponent(id)}`));
  if (response.status === 404) return null;
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message ?? `Falha HTTP ${response.status}`);
  }
  return response.json() as Promise<Fornecedor>;
}

/** Ativa/inativa um Fornecedor (PATCH /fornecedores/{id}/status) — nunca exclui (DR-18). */
export async function alterarStatusFornecedor(id: string, ativo: boolean): Promise<Fornecedor> {
  return request<Fornecedor>(apiUrl(`/fornecedores/${encodeURIComponent(id)}/status`), {
    method: "PATCH",
    headers,
    body: JSON.stringify({ ativo })
  });
}

export async function updateFornecedor(id: string, draft: ManualFornecedorDraft): Promise<Fornecedor> {
  return request<Fornecedor>(apiUrl(`/fornecedores/${encodeURIComponent(id)}`), {
    method: "PUT",
    headers,
    body: JSON.stringify(manualDraftToRequestBody(draft))
  });
}

/**
 * Cadastro manual de Fornecedor (fluxo "Preencher manualmente") — não depende de uma consulta de CNPJ
 * prévia, mas persiste através do mesmo endpoint POST /fornecedores usado pelo fluxo de consulta.
 */
export async function createFornecedorManual(draft: ManualFornecedorDraft): Promise<Fornecedor> {
  return request<Fornecedor>(apiUrl("/fornecedores"), {
    method: "POST",
    headers,
    body: JSON.stringify({ ...manualDraftToRequestBody(draft), status: "Ativo" })
  });
}

function manualDraftToRequestBody(draft: ManualFornecedorDraft) {
  const cnpjDigits = normalizeDocument(draft.cnpj_Cpf);
  return {
    nome: draft.razaoSocial,
    cnpj: cnpjDigits,
    cnpj_Cpf: cnpjDigits,
    razaoSocial: draft.razaoSocial,
    nomeFantasia: draft.nomeFantasia || null,
    tipoPessoa: draft.tipoPessoa || resolveTipoPessoa(cnpjDigits),
    categoria: draft.categoria || null,
    email: draft.email || null,
    telefone: draft.telefone || null,
    website: draft.website || null,
    cidade: draft.cidade || null,
    estado: draft.estado || null,
    pais: draft.pais || "BR",
    cep: draft.cep || null,
    logradouro: draft.logradouro || null,
    numero: draft.numero || null,
    complemento: draft.complemento || null,
    bairro: draft.bairro || null,
    cnaePrincipalCodigo: draft.cnaePrincipalCodigo || null,
    cnaePrincipalDescricao: draft.cnaePrincipalDescricao || null,
    beneficiador: false,
    licenciado: false
  };
}

/**
 * Dispara a operação explícita "garantir/atualizar fornecedor no ERP" (B2.9,
 * POST /api/fornecedores/{id}/garantir-erp) — reaproveitada aqui como ação "Sincronizar com ERP" da
 * tela de detalhe. Nunca disparada implicitamente por consulta/edição (B2.6).
 */
export async function garantirFornecedorNoErp(id: string, businessUnit: string, correlationId: string): Promise<unknown> {
  return request<unknown>(apiUrl(`/api/fornecedores/${encodeURIComponent(id)}/garantir-erp`), {
    method: "POST",
    headers,
    body: JSON.stringify({ businessUnit, correlationId })
  });
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
      // Valores revisados pelo usuario na Review (B2.9, secao 5): o que e persistido e o que o
      // usuario confirmou em tela, nunca a sugestao original do provider por si so.
      nomeFantasia: consulta?.nomeFantasia ?? null,
      cep: consulta?.cep ?? null,
      logradouro: consulta?.logradouro ?? null,
      numero: consulta?.numero ?? null,
      complemento: consulta?.complemento ?? null,
      bairro: consulta?.bairro ?? null,
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
