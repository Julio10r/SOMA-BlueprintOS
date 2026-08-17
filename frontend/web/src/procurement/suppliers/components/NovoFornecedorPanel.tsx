import type { SituacaoCadastralCnpj } from "../types/linxSupplierContract";

/**
 * Painel de revisao para CNPJ consultado que ainda NAO corresponde a nenhum
 * Fornecedor existente no +Compras. Estado "Review" da state machine
 * (Idle -> Validating -> Consulting -> Review -> Persisting -> Success/Error):
 * nenhuma escrita ocorre enquanto este painel esta visivel. Os campos abaixo
 * sao editaveis/complementaveis pelo usuario antes da confirmacao explicita
 * ("Cadastrar fornecedor"), que e a unica acao que dispara persistencia
 * (corrige BUG-1 — CONSULTAR nunca significa CADASTRAR).
 */
export type NovoFornecedorDraft = {
  razaoSocial: string;
  nomeFantasia: string;
  email: string;
  telefone: string;
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  cidade: string;
  estado: string;
};

export function NovoFornecedorPanel({
  draft,
  onDraftChange,
  situacaoCadastral,
  confirmacaoNecessaria,
  confirmado,
  onConfirmadoChange,
  loading,
  onCadastrar
}: {
  draft: NovoFornecedorDraft;
  onDraftChange: (draft: NovoFornecedorDraft) => void;
  situacaoCadastral?: SituacaoCadastralCnpj | null;
  confirmacaoNecessaria: boolean;
  confirmado: boolean;
  onConfirmadoChange: (checked: boolean) => void;
  loading: boolean;
  onCadastrar: () => void;
}) {
  function update(field: keyof NovoFornecedorDraft, value: string) {
    onDraftChange({ ...draft, [field]: value });
  }

  const bloqueado = confirmacaoNecessaria && !confirmado;

  return (
    <section className="card">
      <div className="card-heading">
        <div>
          <div className="section-title">Revisão</div>
          <h2>Nenhum fornecedor cadastrado para este documento</h2>
        </div>
      </div>
      <p>
        Revise e complemente os dados retornados pela consulta antes de cadastrar. Nenhuma escrita foi
        realizada no +Compras até aqui — a consulta é apenas leitura.
      </p>

      <div className="data-grid">
        <label className="field-editable" htmlFor="novo-fornecedor-razao-social">
          <span>Razão Social</span>
          <input id="novo-fornecedor-razao-social" name="razaoSocial" value={draft.razaoSocial} onChange={(event) => update("razaoSocial", event.target.value)} />
        </label>
        <label className="field-editable" htmlFor="novo-fornecedor-nome-fantasia">
          <span>Nome Fantasia</span>
          <input id="novo-fornecedor-nome-fantasia" name="nomeFantasia" value={draft.nomeFantasia} onChange={(event) => update("nomeFantasia", event.target.value)} />
        </label>
        <label className="field-editable" htmlFor="novo-fornecedor-email">
          <span>E-mail</span>
          <input id="novo-fornecedor-email" name="email" type="email" value={draft.email} onChange={(event) => update("email", event.target.value)} />
        </label>
        <label className="field-editable" htmlFor="novo-fornecedor-telefone">
          <span>Telefone (DDD + número)</span>
          <input id="novo-fornecedor-telefone" name="telefone" value={draft.telefone} onChange={(event) => update("telefone", event.target.value)} />
        </label>
        <label className="field-editable" htmlFor="novo-fornecedor-cep">
          <span>CEP</span>
          <input id="novo-fornecedor-cep" name="cep" value={draft.cep} onChange={(event) => update("cep", event.target.value)} />
        </label>
        <label className="field-editable" htmlFor="novo-fornecedor-logradouro">
          <span>Logradouro</span>
          <input id="novo-fornecedor-logradouro" name="logradouro" value={draft.logradouro} onChange={(event) => update("logradouro", event.target.value)} />
        </label>
        <label className="field-editable" htmlFor="novo-fornecedor-numero">
          <span>Número</span>
          <input id="novo-fornecedor-numero" name="numero" value={draft.numero} onChange={(event) => update("numero", event.target.value)} />
        </label>
        <label className="field-editable" htmlFor="novo-fornecedor-complemento">
          <span>Complemento</span>
          <input id="novo-fornecedor-complemento" name="complemento" value={draft.complemento} onChange={(event) => update("complemento", event.target.value)} />
        </label>
        <label className="field-editable" htmlFor="novo-fornecedor-bairro">
          <span>Bairro</span>
          <input id="novo-fornecedor-bairro" name="bairro" value={draft.bairro} onChange={(event) => update("bairro", event.target.value)} />
        </label>
        <label className="field-editable" htmlFor="novo-fornecedor-cidade">
          <span>Cidade</span>
          <input id="novo-fornecedor-cidade" name="cidade" value={draft.cidade} onChange={(event) => update("cidade", event.target.value)} />
        </label>
        <label className="field-editable" htmlFor="novo-fornecedor-estado">
          <span>UF</span>
          <input id="novo-fornecedor-estado" name="estado" value={draft.estado} onChange={(event) => update("estado", event.target.value)} />
        </label>
      </div>

      {confirmacaoNecessaria && (
        <div className="notice notice-warn">
          <strong>Atenção:</strong> situação cadastral {situacaoCadastral}.
          Deseja continuar com o cadastro mesmo assim?
          <label className="check-line">
            <input
              type="checkbox"
              checked={confirmado}
              onChange={(event) => onConfirmadoChange(event.target.checked)}
            />
            Confirmar continuidade
          </label>
        </div>
      )}

      <div className="actions">
        <button className="btn btn-primary" disabled={loading || bloqueado} onClick={onCadastrar}>
          Cadastrar fornecedor
        </button>
      </div>
    </section>
  );
}
