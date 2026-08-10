import type { Perfil } from "../types/perfilTypes";

/**
 * Fluxo visual de exclusao: confirma a acao antes de remover o perfil.
 * Bloqueia visualmente perfis com usuarios vinculados, refletindo a regra
 * de que um usuario pode ter multiplos perfis (ADR-0020, item 9) e por isso
 * a exclusao de um perfil ainda em uso exige desvincula-lo antes.
 */
export function ConfirmExclusaoModal({ perfil, error, loading, onConfirm, onCancel }: {
  perfil: Perfil;
  error: string | null;
  loading: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  const bloqueado = perfil.usuariosVinculados > 0;
  return (
    <div className="modal-overlay" role="dialog" aria-modal="true" aria-labelledby="confirm-exclusao-title">
      <div className="card modal-card">
        <div className="card-heading">
          <h2 id="confirm-exclusao-title">Excluir perfil</h2>
        </div>
        <p>
          Tem certeza que deseja excluir o perfil <strong>{perfil.nome}</strong>? Esta acao nao pode ser desfeita.
        </p>
        {bloqueado && (
          <div className="notice notice-warn">
            Este perfil possui {perfil.usuariosVinculados} usuario(s) vinculado(s). Remova o vinculo antes de excluir.
          </div>
        )}
        {error && <div className="notice notice-crit">{error}</div>}
        <div className="actions">
          <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
            Cancelar
          </button>
          <button type="button" className="btn btn-primary" onClick={onConfirm} disabled={loading || bloqueado}>
            {loading ? "Excluindo..." : "Excluir"}
          </button>
        </div>
      </div>
    </div>
  );
}
