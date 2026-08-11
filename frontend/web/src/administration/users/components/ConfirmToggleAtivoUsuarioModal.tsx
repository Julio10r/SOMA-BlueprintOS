import type { Usuario } from "../types/userTypes";

/**
 * Fluxo visual de ativacao/inativacao de usuario: confirma a acao antes de
 * alterar o status. Usuarios nunca sao excluidos fisicamente — permanecem
 * auditaveis, mesmo padrao de Filiais, Centros de Custo e Unidades de
 * Alocacao.
 */
export function ConfirmToggleAtivoUsuarioModal({ usuario, error, loading, onConfirm, onCancel }: {
  usuario: Usuario;
  error: string | null;
  loading: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  const ativando = !usuario.ativo;
  return (
    <div className="modal-overlay" role="dialog" aria-modal="true" aria-labelledby="confirm-toggle-usuario-title">
      <div className="card modal-card">
        <div className="card-heading">
          <h2 id="confirm-toggle-usuario-title">{ativando ? "Ativar usuario" : "Inativar usuario"}</h2>
        </div>
        <p>
          Tem certeza que deseja {ativando ? "ativar" : "inativar"} o usuario <strong>{usuario.nome}</strong>?
        </p>
        {error && <div className="notice notice-crit">{error}</div>}
        <div className="actions">
          <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
            Cancelar
          </button>
          <button type="button" className="btn btn-primary" onClick={onConfirm} disabled={loading}>
            {loading ? "Salvando..." : ativando ? "Ativar" : "Inativar"}
          </button>
        </div>
      </div>
    </div>
  );
}
