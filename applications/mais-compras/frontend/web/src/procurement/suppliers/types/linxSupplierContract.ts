export type TipoPessoa = "PF" | "PJ" | string;

export type SupplierDomainOption = {
  id: string;
  codigoERP: string;
  descricao: string;
  businessUnit: string;
  erpSistema: string;
  status: string;
};

export type LinxSupplierFormModel = {
  razaoSocial: string;
  nomeFantasia?: string;
  cnpj_cpf: string;
  tipoPessoa: TipoPessoa;
  beneficiador: boolean;
  licenciado: boolean;
  condicaoPagamentoDominioId?: string;
  tipoFornecedorDominioId?: string;
  subtipoFornecedorDominioId?: string;
};

export type Fornecedor = {
  id: string;
  razaoSocial: string;
  nomeFantasia?: string | null;
  cnpj_Cpf: string;
  tipoPessoa?: TipoPessoa | null;
  cep?: string | null;
  logradouro?: string | null;
  numero?: string | null;
  complemento?: string | null;
  bairro?: string | null;
  cidade?: string | null;
  estado?: string | null;
  email?: string | null;
  telefone?: string | null;
  cnaePrincipalCodigo?: string | null;
  cnaePrincipalDescricao?: string | null;
  status?: string | null;
  erpSistema?: string | null;
  erpFornecedorId?: string | null;
  createdAt?: string | null;
  updatedAt?: string | null;
  categoria?: string | null;
  website?: string | null;
  pais?: string | null;
  statusSincronizacao?: FornecedorStatusSincronizacao | null;
  ultimaSincronizacaoEm?: string | null;
  mensagemErroSincronizacao?: string | null;
};

/** Espelha Fornecedor.StatusSincronizacao (backend) — nunca inferido no cliente a partir de erpFornecedorId. */
export type FornecedorStatusSincronizacao = "Pendente" | "Sincronizado" | "Falhou" | string;

const statusSincronizacaoLabels: Record<string, string> = {
  Pendente: "Pendente",
  Sincronizado: "Sincronizado",
  Falhou: "Erro de sincronização"
};

/** Traduz o valor real de StatusSincronizacao (backend) para o rótulo PT-BR exibido na UI. */
export function labelStatusSincronizacao(status?: FornecedorStatusSincronizacao | null): string {
  if (!status) return statusSincronizacaoLabels.Pendente;
  return statusSincronizacaoLabels[status] ?? status;
}

/** Filtro de status usado pela listagem paginada (GET /fornecedores). */
export type FornecedorStatusFiltro = "Todos" | "Ativo" | "Inativo";

/** Espelha FornecedorPesquisaPaginadaDto (BlueprintOS.Application.Procurement.Suppliers.Contracts). */
export type FornecedorPesquisaPaginada = {
  items: Fornecedor[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type FornecedorPesquisaParametros = {
  q?: string;
  status?: FornecedorStatusFiltro;
  sort?: string;
  page?: number;
  pageSize?: number;
};

/** Dados de entrada do cadastro manual de Fornecedor (sem consulta prévia de CNPJ). */
export type ManualFornecedorDraft = {
  razaoSocial: string;
  nomeFantasia: string;
  cnpj_Cpf: string;
  tipoPessoa: TipoPessoa;
  email: string;
  telefone: string;
  website: string;
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  cidade: string;
  estado: string;
  pais: string;
  categoria: string;
  cnaePrincipalCodigo: string;
  cnaePrincipalDescricao: string;
};

export type SituacaoCadastralCnpj = "Ativa" | "Baixada" | "Suspensa" | "Inapta" | "Nula" | "Desconhecida";
export type StatusConsultaCnpj = "Sucesso" | "Falha" | string;
export type FornecedorCampoDecisao = "Pendente" | "Aceito" | "Rejeitado" | string;
export type TipoErroConsultaCnpj =
  | "CnpjInvalido" | "NaoEncontrado" | "FonteIndisponivel" | "Timeout"
  | "LimiteDeConsultas" | "ErroDeAutenticacaoDoProvider" | "RespostaInvalida" | "ErroInterno";

export type ConsultaCnpjResultado = {
  cnpj_Cpf: string;
  razaoSocial?: string | null;
  nomeFantasia?: string | null;
  tipoPessoa?: TipoPessoa | null;
  situacaoCadastral?: SituacaoCadastralCnpj | null;
  dataSituacaoCadastral?: string | null;
  dataAbertura?: string | null;
  cep?: string | null;
  logradouro?: string | null;
  numero?: string | null;
  complemento?: string | null;
  bairro?: string | null;
  cidade?: string | null;
  estado?: string | null;
  pais?: string | null;
  email?: string | null;
  telefone?: string | null;
  naturezaJuridica?: string | null;
  porteEmpresa?: string | null;
  cnaePrincipalCodigo?: string | null;
  cnaePrincipalDescricao?: string | null;
  fonteConsulta: string;
  dataConsulta: string;
  statusConsulta: StatusConsultaCnpj;
  mensagemErro?: string | null;
  sucesso: boolean;
  tipoErro?: TipoErroConsultaCnpj | null;
  permiteRetry: boolean;
  httpStatusSugerido?: number | null;
};

export type FornecedorCampoDivergencia = {
  campo: string;
  valorAtual?: string | null;
  valorSugerido?: string | null;
  origem: string;
  statusDecisao: FornecedorCampoDecisao;
};

export type FornecedorEnriquecimentoAnalise = {
  fornecedorId: string;
  cnpj_Cpf: string;
  consultaId?: string | null;
  fonteConsulta: string;
  correlationId: string;
  divergencias: FornecedorCampoDivergencia[];
  alertas: string[];
};

export type LinxSupplierValidationResult = {
  field: keyof LinxSupplierFormModel;
  message: string;
};

const documentPattern = /^[A-Za-z0-9]{1,14}$/;

export function validateLinxSupplier(model: LinxSupplierFormModel): LinxSupplierValidationResult[] {
  const errors: LinxSupplierValidationResult[] = [];
  if (!model.razaoSocial.trim()) errors.push({ field: "razaoSocial", message: "Informe a razao social." });
  if (!documentPattern.test(model.cnpj_cpf)) errors.push({ field: "cnpj_cpf", message: "Informe CPF/CNPJ com ate 14 caracteres alfanumericos." });
  if (!model.tipoPessoa.trim()) errors.push({ field: "tipoPessoa", message: "Selecione o tipo de pessoa." });
  if (model.tipoPessoa === "PF" && model.cnpj_cpf.length > 11) errors.push({ field: "cnpj_cpf", message: "CPF deve ter ate 11 caracteres." });
  if (model.tipoPessoa === "PJ" && model.cnpj_cpf.length > 14) errors.push({ field: "cnpj_cpf", message: "CNPJ deve ter ate 14 caracteres." });
  return errors;
}

export function isNomeFantasiaEditable(source: "ERP" | "MaisCompras" | string): boolean {
  return source === "ERP";
}

/** Validação de dígitos verificadores de CNPJ (algoritmo padrão da Receita Federal). */
export function isValidCnpjChecksum(value: string): boolean {
  const digits = value.replace(/\D/g, "");
  if (digits.length !== 14 || /^(\d)\1{13}$/.test(digits)) return false;

  function calcDigit(base: string, weights: number[]): number {
    const sum = base.split("").reduce((acc, digit, index) => acc + Number(digit) * weights[index], 0);
    const remainder = sum % 11;
    return remainder < 2 ? 0 : 11 - remainder;
  }

  const weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const d1 = calcDigit(digits.slice(0, 12), weights1);
  const d2 = calcDigit(digits.slice(0, 12) + d1, weights2);
  return digits === digits.slice(0, 12) + String(d1) + String(d2);
}

export type ManualFornecedorValidationResult = { field: keyof ManualFornecedorDraft; message: string };

/**
 * Validação do formulário de cadastro manual (fluxo "Preencher manualmente" — sem consulta prévia de
 * CNPJ). O backend continua sendo a autoridade final (checksum roda lá independentemente do provider),
 * mas erros de campo aqui evitam uma viagem de rede desnecessária para casos obviamente inválidos.
 */
export function validateManualFornecedor(draft: ManualFornecedorDraft): ManualFornecedorValidationResult[] {
  const errors: ManualFornecedorValidationResult[] = [];
  if (!draft.razaoSocial.trim()) errors.push({ field: "razaoSocial", message: "Informe a razão social." });
  if (!draft.nomeFantasia.trim()) errors.push({ field: "nomeFantasia", message: "Informe o nome fantasia." });
  const cnpjDigits = draft.cnpj_Cpf.replace(/\D/g, "");
  if (!cnpjDigits) errors.push({ field: "cnpj_Cpf", message: "Informe o CNPJ." });
  else if (!isValidCnpjChecksum(cnpjDigits)) errors.push({ field: "cnpj_Cpf", message: "CNPJ inválido." });
  return errors;
}
