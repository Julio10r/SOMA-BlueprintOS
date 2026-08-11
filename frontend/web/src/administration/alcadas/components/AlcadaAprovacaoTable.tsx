import { StatusBadge } from "../../../shared/components/StatusBadge";
import { CRITERIO_ALCADA_LABELS } from "../types/alcadaAprovacaoTypes";
import type { AlcadaAprovacao } from "../types/alcadaAprovacaoTypes";

export function AlcadaAprovacaoTable({ alcadas, onEditar, onToggleStatus }: {
  alcadas: AlcadaAprovacao[];
  onEditar: (alcada: AlcadaAprovacao) => void;
  onToggleStatus: (alcada: AlcadaAprovacao) => void;
}) {
  if (alcadas.length === 0) {
    return <div className="empty-state">Nenhuma Alcada de Aprovacao cadastrada para esta Unidade de Negocio.</div>;
  }

  return (
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Nome</th>
          <th>Criterio</th>
          <th>Nivel</th>
          <th>Aprovador</th>
          <th>Status</th>
          <th>Acoes</th>
        </tr>
      </thead>
      <tbody>
        {alcadas.map((alcada) => (
          <tr key={alcada.id}>
            <td>{alcada.nome}</td>
            <td>{CRITERIO_ALCADA_LABELS[alcada.criterio]}</td>
            <td>{alcada.nivel}</td>
            <td>{alcada.aprovadorUsuarioId ? "Usuario" : "Perfil"}</td>
            <td><StatusBadge value={alcada.status} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(alcada)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleStatus(alcada)}>
                  {alcada.status === "Ativo" ? "Inativar" : "Ativar"}
                </button>
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
