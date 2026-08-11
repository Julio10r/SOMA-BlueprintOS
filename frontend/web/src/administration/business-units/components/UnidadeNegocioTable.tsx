import { StatusBadge } from "../../../shared/components/StatusBadge";
import type { UnidadeNegocio } from "../types/unidadeNegocioTypes";

export function UnidadeNegocioTable({ unidadesNegocio, onEditar, onToggleStatus }: {
  unidadesNegocio: UnidadeNegocio[];
  onEditar: (unidadeNegocio: UnidadeNegocio) => void;
  onToggleStatus: (unidadeNegocio: UnidadeNegocio) => void;
}) {
  if (unidadesNegocio.length === 0) return <div className="empty-state">Nenhuma Unidade de Negocio encontrada.</div>;
  return (
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Nome</th>
          <th>Slug</th>
          <th>Status</th>
          <th>Acoes</th>
        </tr>
      </thead>
      <tbody>
        {unidadesNegocio.map((unidadeNegocio) => (
          <tr key={unidadeNegocio.id}>
            <td>{unidadeNegocio.nome}</td>
            <td>{unidadeNegocio.slug}</td>
            <td><StatusBadge value={unidadeNegocio.status} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(unidadeNegocio)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleStatus(unidadeNegocio)}>
                  {unidadeNegocio.status === "Ativo" ? "Inativar" : "Ativar"}
                </button>
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
