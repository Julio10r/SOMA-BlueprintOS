import type { Perfil } from "../types/perfilTypes";

/**
 * Confirmacao de ativacao/inativacao de Perfil. Substitui o antigo
 * `ConfirmExclusaoModal` (removido na O1.5): o backend nao expoe exclusao de Perfil, e
 * `ComprasFuncional.md` ("Gestao de Perfis") lista Ativar/Inativar como a acao oficial.
 *
 * Inativar nao e uma acao inofensiva: um Perfil inativo deixa de contribuir qualquer
 * permissao efetiva a TODOS os usuarios vinculados (a resolucao no backend ignora
 * Perfis inativos). Por isso a contagem de usuarios impactados e exibida — como aviso,
 * nao como bloqueio, porque revogar acesso em massa e exatamente o uso legitimo desta
 * acao. A protecao real contra auto-bloqueio administrativo esta no backend, que recusa
 * inativar o ultimo Perfil ativo com `Perfil.Gerenciar`.
 */
export function ConfirmStatusModal({ perfil, error, loading, onConfirm, onCancel }: {
  perfil: Perfil;
  error: string | null;
  loading: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  const inativando = perfil.ativo;
  const titulo = inativando ? "Inativar perfil" : "Ativar perfil";

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true" aria-labelledby="confirm-status-title">
      <div className="card modal-card">
        <div className="card-heading">
          <h2 id="confirm-status-title">{titulo}</h2>
        </div>
        <p>
          {inativando ? "Deseja inativar o perfil " : "Deseja ativar o perfil "}
          <strong>{perfil.nome}</strong>?
        </p>
        {inativando && perfil.usuariosVinculados > 0 && (
          <div className="notice notice-warn">
            {perfil.usuariosVinculados} usuario(s) vinculado(s) perderao as permissoes deste perfil imediatamente.
            As permissoes concedidas por outros perfis do mesmo usuario permanecem.
          </div>
        )}
        {error && <div className="notice notice-crit">{error}</div>}
        <div className="actions">
          <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
            Cancelar
          </button>
          <button type="button" className="btn btn-primary" onClick={onConfirm} disabled={loading}>
            {loading ? "Salvando..." : titulo}
          </button>
        </div>
      </div>
    </div>
  );
}
