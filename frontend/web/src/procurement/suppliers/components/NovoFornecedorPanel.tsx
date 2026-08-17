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
          <div className="section-title">Revisao</div>
          <h2>Nenhum fornecedor cadastrado para este documento</h2>
        </div>
      </div>
      <p>
        Revise e complemente os dados retornados pela consulta antes de cadastrar. Nenhuma escrita foi
        realizada no +Compras ate aqui — a consulta e apenas leitura.
      </p>

      <div className="data-grid">
        <label className="field-editable">
          <span>RazaoSocial</span>
          <input value={draft.razaoSocial} onChange={(event) => update("razaoSocial", event.target.value)} />
        </label>
        <label className="field-editable">
          <span>NomeFantasia</span>
          <input value={draft.nomeFantasia} onChange={(event) => update("nomeFantasia", event.target.value)} />
        </label>
        <label className="field-editable">
          <span>Email</span>
          <input value={draft.email} onChange={(event) => update("email", event.target.value)} />
        </label>
        <label className="field-editable">
          <span>Telefone (DDD+numero)</span>
          <input value={draft.telefone} onChange={(event) => update("telefone", event.target.value)} />
        </label>
        <label className="field-editable">
          <span>CEP</span>
          <input value={draft.cep} onChange={(event) => update("cep", event.target.value)} />
        </label>
        <label className="field-editable">
          <span>Logradouro</span>
          <input value={draft.logradouro} onChange={(event) => update("logradouro", event.target.value)} />
        </label>
        <label className="field-editable">
          <span>Numero</span>
          <input value={draft.numero} onChange={(event) => update("numero", event.target.value)} />
        </label>
        <label className="field-editable">
          <span>Complemento</span>
          <input value={draft.complemento} onChange={(event) => update("complemento", event.target.value)} />
        </label>
        <label className="field-editable">
          <span>Bairro</span>
          <input value={draft.bairro} onChange={(event) => update("bairro", event.target.value)} />
        </label>
        <label className="field-editable">
          <span>Cidade</span>
          <input value={draft.cidade} onChange={(event) => update("cidade", event.target.value)} />
        </label>
        <label className="field-editable">
          <span>UF</span>
          <input value={draft.estado} onChange={(event) => update("estado", event.target.value)} />
        </label>
      </div>

      {confirmacaoNecessaria && (
        <div className="notice notice-warn">
          <strong>Atencao:</strong> situacao cadastral {situacaoCadastral}.
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
