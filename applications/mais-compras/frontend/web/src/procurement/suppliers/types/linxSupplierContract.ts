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

/**
 * B3 — Bloco 5A.9: um vínculo Linx do Fornecedor (1 CNPJ/CPF = 1 Fornecedor, N vínculos — GAPs
 * KALUNGA/PLATINUM). `dataParaTransferencia` ("mais recente") e `principal` são conceitos
 * INDEPENDENTES — nunca tratar um como sinônimo do outro na UI.
 */
export type FornecedorLinxVinculo = {
  id: string;
  erpSistema: string;
  codigoErp: string;
  nomeClifor: string;
  ativo: boolean;
  principal: boolean;
  dataParaTransferencia?: string | null;
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
  /** DDI (código de discagem internacional, ex: "+55") — campo próprio, sempre enviado junto do
   * número (Gate de homologação, 2026-09-01: telefone precisa de DDI). */
  telefoneDdi: string;
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

/** Item do catálogo pré-cadastrado de Categoria de Fornecedor (GET /fornecedores/categorias). */
export type CategoriaFornecedorOption = { codigo: string; descricao: string };

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

/** Aplica máscara de CNPJ (00.000.000/0000-00) ou CPF (000.000.000-00) para exibição — nunca
 * altera o valor armazenado/enviado ao backend, só a apresentação (ex: listagem de fornecedores).
 * CNPJ alfanumérico (Instrução Normativa RFB nº 2.229/2024, vigente a partir de julho/2026): as 12
 * primeiras posições podem ter letras — a máscara preserva letras, só os separadores mudam. */
export function formatarDocumento(value: string | null | undefined): string {
  const chars = (value ?? "").toUpperCase().replace(/[^0-9A-Z]/g, "");
  if (chars.length === 14) {
    return chars.replace(/^(.{2})(.{3})(.{3})(.{4})(\d{2})$/, "$1.$2.$3/$4-$5");
  }
  if (chars.length === 11 && /^\d{11}$/.test(chars)) {
    return chars.replace(/^(\d{3})(\d{3})(\d{3})(\d{2})$/, "$1.$2.$3-$4");
  }
  return value ?? "";
}

export function isNomeFantasiaEditable(source: "ERP" | "MaisCompras" | string): boolean {
  return source === "ERP";
}

/** Validação de dígitos verificadores de CNPJ (algoritmo padrão da Receita Federal). Suporta o
 * CNPJ alfanumérico (Instrução Normativa RFB nº 2.229/2024, vigente a partir de julho/2026): as 12
 * primeiras posições podem ser letras (A-Z) ou dígitos; os 2 dígitos verificadores continuam
 * sempre numéricos. Cada caractere é convertido pelo valor ASCII menos 48 (dígitos 0-9 continuam
 * valendo 0-9; letras A-Z valem 17-42) — mesmos pesos e regra de módulo 11 de sempre, então CNPJs
 * puramente numéricos (todos os já emitidos) continuam validando exatamente como antes. */
export function isValidCnpjChecksum(value: string): boolean {
  const chars = value.toUpperCase().replace(/[^0-9A-Z]/g, "");
  if (chars.length !== 14 || /^(.)\1{13}$/.test(chars)) return false;
  // Os 2 dígitos verificadores nunca são letras, mesmo num CNPJ alfanumérico.
  if (!/^\d{2}$/.test(chars.slice(12))) return false;

  function valorCaractere(char: string): number {
    return char.charCodeAt(0) - 48;
  }

  function calcDigit(base: string, weights: number[]): number {
    const sum = base.split("").reduce((acc, char, index) => acc + valorCaractere(char) * weights[index], 0);
    const remainder = sum % 11;
    return remainder < 2 ? 0 : 11 - remainder;
  }

  const weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const d1 = calcDigit(chars.slice(0, 12), weights1);
  const d2 = calcDigit(chars.slice(0, 12) + d1, weights2);
  return chars === chars.slice(0, 12) + String(d1) + String(d2);
}

/** Validação de dígitos verificadores de CPF (algoritmo padrão da Receita Federal) — antes só
 * existia validação de CNPJ; Pessoa Física (11 dígitos) não tinha nenhuma checagem de dígito
 * verificador, então um CPF inválido nunca era sinalizado ao usuário. */
export function isValidCpfChecksum(value: string): boolean {
  const digits = value.replace(/\D/g, "");
  if (digits.length !== 11 || /^(\d)\1{10}$/.test(digits)) return false;

  function calcDigit(base: string, pesoInicial: number): number {
    const sum = base.split("").reduce((acc, digit, index) => acc + Number(digit) * (pesoInicial - index), 0);
    const remainder = (sum * 10) % 11;
    return remainder === 10 ? 0 : remainder;
  }

  const d1 = calcDigit(digits.slice(0, 9), 10);
  const d2 = calcDigit(digits.slice(0, 9) + d1, 11);
  return digits === digits.slice(0, 9) + String(d1) + String(d2);
}

/** Valida o dígito verificador do CNPJ/CPF de acordo com a quantidade de dígitos — nunca aplica o
 * algoritmo errado (CNPJ tem 14 dígitos e regras próprias; CPF tem 11 e regras diferentes). */
export function isValidDocumentoChecksum(value: string): boolean {
  const chars = value.toUpperCase().replace(/[^0-9A-Z]/g, "");
  if (chars.length === 14) return isValidCnpjChecksum(chars);
  if (chars.length === 11) return isValidCpfChecksum(chars);
  return false;
}

export type TipoErroConsultaCep =
  | "CepInvalido" | "NaoEncontrado" | "FonteIndisponivel" | "Timeout" | "RespostaInvalida" | "ErroInterno";

/** Espelha ConsultaCepResultado (backend). Consulta de CEP (Gate homologação, 2026-09-01, item 6)
 * usa ViaCEP pelo backend — nunca chamada direta do frontend (achado 2, docs/audits/Discovery-Fornecedor-Tela-001016G1.md). */
export type ConsultaCepResultado = {
  cep: string;
  logradouro?: string | null;
  bairro?: string | null;
  complemento?: string | null;
  cidade?: string | null;
  estado?: string | null;
  fonteConsulta: string;
  dataConsulta: string;
  statusConsulta: "Sucesso" | "Falha" | string;
  mensagemErro?: string | null;
  tipoErro?: TipoErroConsultaCep | null;
  sucesso: boolean;
};

/**
 * UFs reais do Brasil + "EX" (exterior) — mesmo valor usado pelo Linx para fornecedor
 * estrangeiro (achado 13, docs/audits/Discovery-Fornecedor-Tela-001016G1.md: NIF só habilitado
 * quando UF='EX'). Lista fechada porque UF é código estruturado, não texto livre — mesmo
 * raciocínio de por que CEP/CNPJ têm formato validado.
 */
export const UNIDADES_FEDERACAO: ReadonlyArray<{ value: string; label: string }> = [
  { value: "AC", label: "AC — Acre" },
  { value: "AL", label: "AL — Alagoas" },
  { value: "AP", label: "AP — Amapá" },
  { value: "AM", label: "AM — Amazonas" },
  { value: "BA", label: "BA — Bahia" },
  { value: "CE", label: "CE — Ceará" },
  { value: "DF", label: "DF — Distrito Federal" },
  { value: "ES", label: "ES — Espírito Santo" },
  { value: "GO", label: "GO — Goiás" },
  { value: "MA", label: "MA — Maranhão" },
  { value: "MT", label: "MT — Mato Grosso" },
  { value: "MS", label: "MS — Mato Grosso do Sul" },
  { value: "MG", label: "MG — Minas Gerais" },
  { value: "PA", label: "PA — Pará" },
  { value: "PB", label: "PB — Paraíba" },
  { value: "PR", label: "PR — Paraná" },
  { value: "PE", label: "PE — Pernambuco" },
  { value: "PI", label: "PI — Piauí" },
  { value: "RJ", label: "RJ — Rio de Janeiro" },
  { value: "RN", label: "RN — Rio Grande do Norte" },
  { value: "RS", label: "RS — Rio Grande do Sul" },
  { value: "RO", label: "RO — Rondônia" },
  { value: "RR", label: "RR — Roraima" },
  { value: "SC", label: "SC — Santa Catarina" },
  { value: "SP", label: "SP — São Paulo" },
  { value: "SE", label: "SE — Sergipe" },
  { value: "TO", label: "TO — Tocantins" },
  { value: "EX", label: "EX — Exterior" }
];

const UF_VALIDAS = new Set(UNIDADES_FEDERACAO.map((uf) => uf.value));

/**
 * DDI padrão do Brasil — preenchido automaticamente quando a UF é uma unidade federativa real
 * (não "EX") ou o País é "Brasil" (feedback do homologador, 2026-09-01: "se UF ou país é Brasil,
 * já preenche sozinho +55"). Para UF="EX" o DDI fica editável (mesmo padrão de Cidade/País).
 */
export const DDI_PADRAO_BRASIL = "+55";

/** Aplica máscara de CNPJ (00.000.000/0000-00) ou CPF (000.000.000-00) enquanto o usuário digita
 * — identifica sozinho qual dos dois formatos usar pela quantidade de dígitos (até 11 = CPF, mais
 * de 11 = CNPJ), mesmo critério já usado para Tipo de pessoa (PF/PJ). */
export function aplicarMascaraCnpjCpf(value: string): string {
  const chars = value.toUpperCase().replace(/[^0-9A-Z]/g, "").slice(0, 14);
  // CPF é sempre numérico — só aplica a máscara de CPF quando ainda não apareceu nenhuma letra
  // (uma letra digitada em qualquer posição só é possível num CNPJ, mesmo com poucos caracteres).
  if (chars.length <= 11 && !/[A-Z]/.test(chars)) {
    return chars
      .replace(/^(\d{3})(\d)/, "$1.$2")
      .replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3")
      .replace(/^(\d{3})\.(\d{3})\.(\d{3})(\d)/, "$1.$2.$3-$4");
  }
  // CNPJ alfanumérico (Instrução Normativa RFB nº 2.229/2024, vigente a partir de julho/2026): as
  // 12 primeiras posições podem ser letras ou dígitos; os 2 dígitos verificadores finais
  // continuam sempre numéricos (CNPJs puramente numéricos, já emitidos, continuam funcionando
  // exatamente como antes — letra é só uma possibilidade a mais, nunca obrigatória).
  return chars
    .replace(/^([0-9A-Z]{2})([0-9A-Z])/, "$1.$2")
    .replace(/^([0-9A-Z]{2})\.([0-9A-Z]{3})([0-9A-Z])/, "$1.$2.$3")
    .replace(/^([0-9A-Z]{2})\.([0-9A-Z]{3})\.([0-9A-Z]{3})([0-9A-Z])/, "$1.$2.$3/$4")
    .replace(/^([0-9A-Z]{2})\.([0-9A-Z]{3})\.([0-9A-Z]{3})\/([0-9A-Z]{4})([0-9A-Z])/, "$1.$2.$3/$4-$5");
}

/** Aplica máscara de CEP (00000-000) enquanto o usuário digita. */
export function aplicarMascaraCep(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 8);
  if (digits.length <= 5) return digits;
  return `${digits.slice(0, 5)}-${digits.slice(5)}`;
}

/** Aplica máscara de DDI enquanto o usuário digita: "+" seguido de 1 a 3 dígitos (códigos de
 * discagem internacional têm no máximo 3 dígitos, ex: +1, +55, +351, +971). */
export function aplicarMascaraDdi(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 3);
  return digits ? `+${digits}` : "";
}

/** Aplica máscara de telefone brasileiro enquanto o usuário digita: (00) 0000-0000 (fixo, 10
 * dígitos) ou (00) 00000-0000 (celular, 11 dígitos). Não mascara números de outros países (DDI
 * diferente de +55) — nesse caso o valor é mantido como o usuário digitou, só limitando dígitos. */
export function aplicarMascaraTelefone(value: string, ddi: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 11);
  if (ddi.trim() !== DDI_PADRAO_BRASIL) return value.replace(/[^\d\s()+-]/g, "");
  if (digits.length <= 2) return digits.length ? `(${digits}` : "";
  if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
  if (digits.length <= 10) return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
}

/** Validação simples de formato de e-mail — não garante entregabilidade, só formato plausível. */
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Lista de países (nomes em português) — combo com filtro de pesquisa (item de feedback do
 * homologador, 2026-09-01). "Brasil" é o valor padrão (draft.pais), consistente com o que a
 * consulta de CNPJ (BrasilAPI) já retorna nesse campo.
 */
export const PAISES: readonly string[] = [
  "Brasil", "Afeganistão", "África do Sul", "Albânia", "Alemanha", "Andorra", "Angola",
  "Antígua e Barbuda", "Arábia Saudita", "Argélia", "Argentina", "Armênia", "Austrália", "Áustria",
  "Azerbaijão", "Bahamas", "Bahrein", "Bangladesh", "Barbados", "Bélgica", "Belize", "Benin",
  "Bielorrússia", "Bolívia", "Bósnia e Herzegovina", "Botsuana", "Brunei", "Bulgária",
  "Burkina Faso", "Burundi", "Butão", "Cabo Verde", "Camarões", "Camboja", "Canadá", "Catar",
  "Cazaquistão", "Chade", "Chile", "China", "Chipre", "Colômbia", "Comores", "Congo",
  "Coreia do Norte", "Coreia do Sul", "Costa do Marfim", "Costa Rica", "Croácia", "Cuba",
  "Dinamarca", "Djibuti", "Dominica", "Egito", "El Salvador", "Emirados Árabes Unidos", "Equador",
  "Eritreia", "Eslováquia", "Eslovênia", "Espanha", "Estados Unidos", "Estônia", "Eswatini",
  "Etiópia", "Fiji", "Filipinas", "Finlândia", "França", "Gabão", "Gâmbia", "Gana", "Geórgia",
  "Ghana", "Granada", "Grécia", "Guatemala", "Guiana", "Guiné", "Guiné-Bissau",
  "Guiné Equatorial", "Haiti", "Holanda", "Honduras", "Hungria", "Iêmen", "Ilhas Marshall",
  "Ilhas Salomão", "Índia", "Indonésia", "Irã", "Iraque", "Irlanda", "Islândia", "Israel",
  "Itália", "Jamaica", "Japão", "Jordânia", "Kiribati", "Kuwait", "Laos", "Lesoto", "Letônia",
  "Líbano", "Libéria", "Líbia", "Liechtenstein", "Lituânia", "Luxemburgo", "Macedônia do Norte",
  "Madagascar", "Malásia", "Malawi", "Maldivas", "Mali", "Malta", "Marrocos", "Maurícia",
  "Mauritânia", "México", "Mianmar", "Micronésia", "Moçambique", "Moldávia", "Mônaco", "Mongólia",
  "Montenegro", "Namíbia", "Nauru", "Nepal", "Nicarágua", "Níger", "Nigéria", "Noruega",
  "Nova Zelândia", "Omã", "Palau", "Panamá", "Papua-Nova Guiné", "Paquistão", "Paraguai", "Peru",
  "Polônia", "Portugal", "Quênia", "Quirguistão", "Reino Unido", "República Centro-Africana",
  "República Democrática do Congo", "República Dominicana", "República Tcheca", "Romênia",
  "Ruanda", "Rússia", "Samoa", "San Marino", "Santa Lúcia", "São Cristóvão e Neves", "São Tomé e Príncipe",
  "São Vicente e Granadinas", "Seicheles", "Senegal", "Serra Leoa", "Sérvia", "Singapura",
  "Síria", "Somália", "Sri Lanka", "Sudão", "Sudão do Sul", "Suécia", "Suíça", "Suriname",
  "Tailândia", "Taiwan", "Tajiquistão", "Tanzânia", "Timor-Leste", "Togo", "Tonga",
  "Trinidad e Tobago", "Tunísia", "Turcomenistão", "Turquia", "Tuvalu", "Ucrânia", "Uganda",
  "Uruguai", "Uzbequistão", "Vanuatu", "Vaticano", "Venezuela", "Vietnã", "Zâmbia", "Zimbábue"
];

const PAISES_VALIDOS = new Set(PAISES.map((pais) => pais.toLowerCase()));

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
  // CNPJ alfanumérico (Instrução Normativa RFB nº 2.229/2024): nunca usar \D aqui — isso apagaria
  // as letras do CNPJ. CPF continua sempre numérico.
  const cnpjChars = draft.cnpj_Cpf.toUpperCase().replace(/[^0-9A-Z]/g, "");
  if (!cnpjChars) errors.push({ field: "cnpj_Cpf", message: "Informe o CNPJ/CPF." });
  else if (!isValidDocumentoChecksum(cnpjChars)) {
    errors.push({ field: "cnpj_Cpf", message: cnpjChars.length === 11 ? "CPF inválido." : "CNPJ inválido." });
  }

  // Gate de homologação (2026-09-01), item 6: fornecedor não pode ser salvo sem endereço completo
  // e sem contato — lista confirmada pelo Product Owner (mais restritiva que a evidência direta de
  // código Linx lida em docs/audits/Discovery-Fornecedor-Tela-001016G1.md, que só confirma Razão
  // Social/CNPJ/RG-IE/Cidade/País/Endereço como bloqueantes na tela 001016G1). Aplicada aqui (fluxo
  // "Preencher manualmente", mesmo formulário/endpoint usado por "Consultar por CNPJ"). Reteste do
  // Gate (2026-09-01): o backend (CadastrarFornecedorUseCase) agora reforça a mesma exigência de
  // E-mail/Telefone — decisão do PO revertida explicitamente, chamada direta à API não contorna mais
  // esta regra.
  if (!draft.cep.trim()) errors.push({ field: "cep", message: "Informe o CEP." });
  else if (draft.cep.replace(/\D/g, "").length !== 8) errors.push({ field: "cep", message: "CEP deve ter 8 dígitos." });
  if (!draft.logradouro.trim()) errors.push({ field: "logradouro", message: "Informe o logradouro." });
  if (!draft.numero.trim()) errors.push({ field: "numero", message: "Informe o número." });
  if (!draft.bairro.trim()) errors.push({ field: "bairro", message: "Informe o bairro." });
  if (!draft.cidade.trim()) errors.push({ field: "cidade", message: "Informe a cidade." });
  if (!draft.estado.trim()) errors.push({ field: "estado", message: "Informe a UF." });
  else if (!UF_VALIDAS.has(draft.estado.trim().toUpperCase())) errors.push({ field: "estado", message: "UF inválida." });
  if (!draft.pais.trim()) errors.push({ field: "pais", message: "Informe o país." });
  else if (!PAISES_VALIDOS.has(draft.pais.trim().toLowerCase())) errors.push({ field: "pais", message: "Selecione um país válido da lista." });
  if (!draft.email.trim()) errors.push({ field: "email", message: "Informe o e-mail." });
  else if (!draft.email.includes("@") || !EMAIL_PATTERN.test(draft.email.trim())) errors.push({ field: "email", message: "E-mail inválido." });
  if (!draft.telefoneDdi.trim()) errors.push({ field: "telefoneDdi", message: "Informe o DDI." });
  else if (!/^\+\d{1,3}$/.test(draft.telefoneDdi.trim())) errors.push({ field: "telefoneDdi", message: "DDI inválido." });
  if (!draft.telefone.trim()) errors.push({ field: "telefone", message: "Informe o telefone." });
  else {
    const telefoneDigits = draft.telefone.replace(/\D/g, "");
    // Gate de homologação (2026-09-01): dados reais vindos do Linx frequentemente têm telefone sem
    // DDD (ex: "3278-3909", 8 dígitos) — exigir 10+ dígitos bloqueava salvar edições em
    // fornecedores já existentes com esse formato legado. Mínimo relaxado para 8.
    const minimoDigitos = draft.telefoneDdi.trim() === DDI_PADRAO_BRASIL ? 8 : 4;
    if (telefoneDigits.length < minimoDigitos) errors.push({ field: "telefone", message: "Telefone inválido." });
  }
  if (!draft.categoria.trim()) errors.push({ field: "categoria", message: "Selecione a categoria." });
  return errors;
}

/** Divide um telefone persistido ("+55 (11) 98888-7777") em DDI + número para reabrir no
 * formulário de edição — o backend armazena um único campo de texto (Fornecedor.Telefone). */
export function splitTelefone(value: string | null | undefined): { ddi: string; numero: string } {
  const trimmed = (value ?? "").trim();
  const match = trimmed.match(/^(\+\d{1,3})\s*(.*)$/);
  if (match) return { ddi: match[1], numero: match[2].trim() };
  return trimmed ? { ddi: "", numero: trimmed } : { ddi: DDI_PADRAO_BRASIL, numero: "" };
}

/** Combina DDI + número no único campo de texto enviado/persistido como Fornecedor.Telefone. */
export function combinarTelefone(ddi: string, numero: string): string {
  return [ddi.trim(), numero.trim()].filter(Boolean).join(" ");
}
