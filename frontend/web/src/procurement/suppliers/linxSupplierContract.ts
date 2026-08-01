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
