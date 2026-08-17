import type { SituacaoCadastralCnpj } from "../types/linxSupplierContract";

/**
 * Painel de aprovacao/rejeicao do conjunto de divergencias selecionadas.
 * Tambem concentra o alerta de situacao cadastral BAIXADA/Suspensa/Inapta,
 * que exige confirmacao explicita do usuario antes de decidir (mas nunca
 * bloqueia o cadastro). Nao contem regra de negocio propria: apenas
 * repassa a decisao ao handler do container (CadastroFornecedor.tsx), que
 * mantem as chamadas de API existentes intactas.
 */
export function ApprovalPanel({
  alertas,
  situacaoCadastral,
  baixadaConfirmed,
  onBaixadaConfirmChange,
  selectedFieldsCount,
  loading,
  onApprove,
  onReject
}: {
  alertas: string[];
  situacaoCadastral?: SituacaoCadastralCnpj | null;
  baixadaConfirmed: boolean;
  onBaixadaConfirmChange: (checked: boolean) => void;
  selectedFieldsCount: number;
  loading: boolean;
  onApprove: () => void;
  onReject: () => void;
}) {
  const requiresConfirmation = situacaoCadastral === "Baixada" || situacaoCadastral === "Suspensa" || situacaoCadastral === "Inapta";

  return (
    <>
      {requiresConfirmation && (
        <div className="notice notice-warn">
          <strong>Atenção:</strong> Fornecedor possui situação cadastral {situacaoCadastral}. Deseja continuar?
          <label className="check-line">
            <input
              type="checkbox"
              checked={baixadaConfirmed}
              onChange={(event) => onBaixadaConfirmChange(event.target.checked)}
            />
            Confirmar continuidade
          </label>
        </div>
      )}

      {alertas.map((alerta) => <div className="notice notice-warn" key={alerta}>{alerta}</div>)}

      <div className="actions">
        <button className="btn btn-reject" disabled={loading || selectedFieldsCount === 0} onClick={onReject}>
          <XIcon /> Rejeitar
        </button>
        <button className="btn btn-approve" disabled={loading || selectedFieldsCount === 0} onClick={onApprove}>
          <CheckIcon /> Aceitar
        </button>
      </div>
    </>
  );
}

function CheckIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><polyline points="20 6 9 17 4 12" /></svg>;
}

function XIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" /></svg>;
}
