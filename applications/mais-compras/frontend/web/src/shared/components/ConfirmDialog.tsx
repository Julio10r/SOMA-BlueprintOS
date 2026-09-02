/**
 * Modal de confirmação genérico da própria aplicação — nunca `window.confirm`/`alert`/`prompt`
 * nativos do navegador em nenhuma tela do +Compras (padrão fixado pelo homologador, 2026-09-01).
 * Substitui qualquer uso pontual de diálogo nativo por este componente compartilhado.
 */
export function ConfirmDialog({
  title,
  message,
  confirmLabel = "Confirmar",
  cancelLabel = "Cancelar",
  destructive = false,
  onConfirm,
  onCancel
}: {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Estiliza o botão de confirmação como ação destrutiva (ex: excluir). */
  destructive?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card card">
        <h2>{title}</h2>
        <p>{message}</p>
        <div className="actions">
          <button type="button" className="btn btn-secondary" onClick={onCancel}>
            {cancelLabel}
          </button>
          <button type="button" className={destructive ? "btn btn-reject" : "btn btn-primary"} onClick={onConfirm}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
