import { FormEvent, useState } from "react";
import type { ManualFornecedorDraft, ManualFornecedorValidationResult } from "../types/linxSupplierContract";
import { validateManualFornecedor } from "../types/linxSupplierContract";

/**
 * Formulário completo de Fornecedor, organizado em seções lógicas (Identificação, Endereço, Contato,
 * Atividade econômica, Dados +Compras). Reaproveitado tanto pelo cadastro manual ("Preencher
 * manualmente", sem consulta de CNPJ prévia) quanto pelo modo de edição da tela de detalhe — mesmo
 * layout de campos nos dois casos, só o rótulo do botão principal e a edição do CNPJ mudam.
 */
export function ManualFornecedorForm({
  draft,
  onDraftChange,
  onSubmit,
  onCancel,
  loading,
  error,
  submitLabel = "Cadastrar fornecedor",
  cnpjEditavel = true
}: {
  draft: ManualFornecedorDraft;
  onDraftChange: (draft: ManualFornecedorDraft) => void;
  onSubmit: (draft: ManualFornecedorDraft) => void;
  onCancel: () => void;
  loading: boolean;
  error?: string | null;
  submitLabel?: string;
  cnpjEditavel?: boolean;
}) {
  const [errors, setErrors] = useState<ManualFornecedorValidationResult[]>([]);

  function update<K extends keyof ManualFornecedorDraft>(field: K, value: ManualFornecedorDraft[K]) {
    onDraftChange({ ...draft, [field]: value });
  }

  function errorFor(field: keyof ManualFornecedorDraft): string | undefined {
    return errors.find((item) => item.field === field)?.message;
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const validation = validateManualFornecedor(draft);
    setErrors(validation);
    if (validation.length > 0) return;
    onSubmit(draft);
  }

  return (
    <form className="card" onSubmit={handleSubmit} noValidate>
      {error && <div className="notice notice-crit">{error}</div>}

      <div className="data-block">
        <div className="section-title">Identificação</div>
        <div className="data-grid">
          <label className="field-editable" htmlFor="manual-fornecedor-cnpj">
            <span>CNPJ *</span>
            <input
              id="manual-fornecedor-cnpj"
              name="cnpj_Cpf"
              value={draft.cnpj_Cpf}
              disabled={!cnpjEditavel}
              onChange={(event) => update("cnpj_Cpf", event.target.value)}
              aria-invalid={!!errorFor("cnpj_Cpf")}
            />
            {errorFor("cnpj_Cpf") && <span className="field-error">{errorFor("cnpj_Cpf")}</span>}
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-razao-social">
            <span>Razão Social *</span>
            <input
              id="manual-fornecedor-razao-social"
              name="razaoSocial"
              value={draft.razaoSocial}
              onChange={(event) => update("razaoSocial", event.target.value)}
              aria-invalid={!!errorFor("razaoSocial")}
            />
            {errorFor("razaoSocial") && <span className="field-error">{errorFor("razaoSocial")}</span>}
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-nome-fantasia">
            <span>Nome Fantasia *</span>
            <input
              id="manual-fornecedor-nome-fantasia"
              name="nomeFantasia"
              value={draft.nomeFantasia}
              onChange={(event) => update("nomeFantasia", event.target.value)}
              aria-invalid={!!errorFor("nomeFantasia")}
            />
            {errorFor("nomeFantasia") && <span className="field-error">{errorFor("nomeFantasia")}</span>}
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-tipo-pessoa">
            <span>Tipo de pessoa</span>
            <select
              id="manual-fornecedor-tipo-pessoa"
              name="tipoPessoa"
              value={draft.tipoPessoa}
              onChange={(event) => update("tipoPessoa", event.target.value)}
            >
              <option value="PJ">Pessoa Jurídica</option>
              <option value="PF">Pessoa Física</option>
            </select>
          </label>
        </div>
      </div>

      <div className="data-block">
        <div className="section-title">Endereço</div>
        <div className="data-grid">
          <label className="field-editable" htmlFor="manual-fornecedor-cep">
            <span>CEP</span>
            <input id="manual-fornecedor-cep" value={draft.cep} onChange={(event) => update("cep", event.target.value)} />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-logradouro">
            <span>Logradouro</span>
            <input id="manual-fornecedor-logradouro" value={draft.logradouro} onChange={(event) => update("logradouro", event.target.value)} />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-numero">
            <span>Número</span>
            <input id="manual-fornecedor-numero" value={draft.numero} onChange={(event) => update("numero", event.target.value)} />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-complemento">
            <span>Complemento</span>
            <input id="manual-fornecedor-complemento" value={draft.complemento} onChange={(event) => update("complemento", event.target.value)} />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-bairro">
            <span>Bairro</span>
            <input id="manual-fornecedor-bairro" value={draft.bairro} onChange={(event) => update("bairro", event.target.value)} />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-cidade">
            <span>Cidade</span>
            <input id="manual-fornecedor-cidade" value={draft.cidade} onChange={(event) => update("cidade", event.target.value)} />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-estado">
            <span>UF</span>
            <input id="manual-fornecedor-estado" value={draft.estado} onChange={(event) => update("estado", event.target.value)} />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-pais">
            <span>País</span>
            <input id="manual-fornecedor-pais" value={draft.pais} onChange={(event) => update("pais", event.target.value)} />
          </label>
        </div>
      </div>

      <div className="data-block">
        <div className="section-title">Contato</div>
        <div className="data-grid">
          <label className="field-editable" htmlFor="manual-fornecedor-email">
            <span>E-mail</span>
            <input id="manual-fornecedor-email" type="email" value={draft.email} onChange={(event) => update("email", event.target.value)} />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-telefone">
            <span>Telefone</span>
            <input id="manual-fornecedor-telefone" value={draft.telefone} onChange={(event) => update("telefone", event.target.value)} />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-website">
            <span>Website</span>
            <input id="manual-fornecedor-website" value={draft.website} onChange={(event) => update("website", event.target.value)} />
          </label>
        </div>
      </div>

      <div className="data-block">
        <div className="section-title">Atividade econômica</div>
        <div className="data-grid">
          <label className="field-editable" htmlFor="manual-fornecedor-cnae-codigo">
            <span>CNAE principal (código)</span>
            <input id="manual-fornecedor-cnae-codigo" value={draft.cnaePrincipalCodigo} onChange={(event) => update("cnaePrincipalCodigo", event.target.value)} />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-cnae-descricao">
            <span>CNAE principal (descrição)</span>
            <input id="manual-fornecedor-cnae-descricao" value={draft.cnaePrincipalDescricao} onChange={(event) => update("cnaePrincipalDescricao", event.target.value)} />
          </label>
        </div>
      </div>

      <div className="data-block">
        <div className="section-title">Dados +Compras</div>
        <div className="data-grid">
          <label className="field-editable" htmlFor="manual-fornecedor-categoria">
            <span>Categoria</span>
            <input id="manual-fornecedor-categoria" value={draft.categoria} onChange={(event) => update("categoria", event.target.value)} />
          </label>
        </div>
      </div>

      <div className="actions">
        <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? "Salvando..." : submitLabel}
        </button>
      </div>
    </form>
  );
}

export const manualFornecedorDraftInicial: ManualFornecedorDraft = {
  razaoSocial: "",
  nomeFantasia: "",
  cnpj_Cpf: "",
  tipoPessoa: "PJ",
  email: "",
  telefone: "",
  website: "",
  cep: "",
  logradouro: "",
  numero: "",
  complemento: "",
  bairro: "",
  cidade: "",
  estado: "",
  pais: "BR",
  categoria: "",
  cnaePrincipalCodigo: "",
  cnaePrincipalDescricao: ""
};
