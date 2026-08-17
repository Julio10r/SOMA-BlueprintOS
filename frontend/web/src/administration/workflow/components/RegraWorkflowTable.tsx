import { StatusBadge } from "../../../shared/components/StatusBadge";
import type { RegraWorkflow } from "../types/regraWorkflowTypes";

export function RegraWorkflowTable({ regras, onEditar, onToggleStatus }: {
  regras: RegraWorkflow[];
  onEditar: (regra: RegraWorkflow) => void;
  onToggleStatus: (regra: RegraWorkflow) => void;
}) {
  if (regras.length === 0) {
    return <div className="empty-state">Nenhuma Regra de Workflow cadastrada para esta Unidade de Negocio.</div>;
  }

  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Nome</th>
          <th>Tipo de Processo</th>
          <th>Ordem</th>
          <th>Status</th>
          <th>Acoes</th>
        </tr>
      </thead>
      <tbody>
        {regras.map((regra) => (
          <tr key={regra.id}>
            <td>{regra.nome}</td>
            <td>{regra.tipoProcesso}</td>
            <td>{regra.ordem}</td>
            <td><StatusBadge value={regra.status} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(regra)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleStatus(regra)}>
                  {regra.status === "Ativo" ? "Inativar" : "Ativar"}
                </button>
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
    </div>
  );
}
