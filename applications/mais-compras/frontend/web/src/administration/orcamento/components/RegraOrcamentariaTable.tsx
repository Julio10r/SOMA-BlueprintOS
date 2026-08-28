import { StatusBadge } from "../../../shared/components/StatusBadge";
import { PERIODO_ORCAMENTARIO_LABELS } from "../types/regraOrcamentariaTypes";
import type { RegraOrcamentaria } from "../types/regraOrcamentariaTypes";

export function RegraOrcamentariaTable({ regras, onEditar, onToggleStatus }: {
  regras: RegraOrcamentaria[];
  onEditar: (regra: RegraOrcamentaria) => void;
  onToggleStatus: (regra: RegraOrcamentaria) => void;
}) {
  if (regras.length === 0) {
    return <div className="empty-state">Nenhuma Regra Orçamentária cadastrada para esta Unidade de Negócio.</div>;
  }

  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Nome</th>
          <th>Centro de Custo</th>
          <th>Valor limite</th>
          <th>Período</th>
          <th>Status</th>
          <th>Ações</th>
        </tr>
      </thead>
      <tbody>
        {regras.map((regra) => (
          <tr key={regra.id}>
            <td>{regra.nome}</td>
            <td>{regra.centroCustoMetadadoId}</td>
            <td>{regra.valorLimite.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}</td>
            <td>{PERIODO_ORCAMENTARIO_LABELS[regra.periodo]}</td>
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
