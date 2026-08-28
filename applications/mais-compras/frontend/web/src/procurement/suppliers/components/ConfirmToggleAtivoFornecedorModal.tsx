import type { Fornecedor } from "../types/linxSupplierContract";

/**
 * Confirmação de ativação/inativação de Fornecedor — nunca "excluir" (DR-18: o backend só marca
 * Status=Inativo via PATCH /fornecedores/{id}/status, nunca remove a linha fisicamente).
 */
export function ConfirmToggleAtivoFornecedorModal({
  fornecedor,
  ativando,
  error,
  loading,
  onConfirm,
  onCancel
}: {
  fornecedor: Fornecedor;
  ativando: boolean;
  error: string | null;
  loading: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card card">
        <h2>{ativando ? "Ativar fornecedor?" : "Inativar fornecedor?"}</h2>
        <p>
          {ativando
            ? `"${fornecedor.razaoSocial}" voltará a ficar disponível para novas operações no +Compras.`
            : `"${fornecedor.razaoSocial}" deixará de ficar disponível para novas operações no +Compras. O histórico é mantido.`}
        </p>
        {error && <div className="notice notice-crit">{error}</div>}
        <div className="actions">
          <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
            Cancelar
          </button>
          <button type="button" className="btn btn-primary" onClick={onConfirm} disabled={loading}>
            {loading ? "Processando..." : ativando ? "Ativar fornecedor" : "Inativar fornecedor"}
          </button>
        </div>
      </div>
    </div>
  );
}
