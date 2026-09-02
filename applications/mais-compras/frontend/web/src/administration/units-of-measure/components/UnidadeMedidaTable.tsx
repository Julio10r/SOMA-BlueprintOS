import { StatusBadge } from "../../../shared/components/StatusBadge";
import { statusUnidadeMedida, type UnidadeMedida } from "../types/unidadeMedidaTypes";

/**
 * Listagem de Unidades de Medida. Sem acao de criacao nem de exclusao: apenas Visualizar, Editar
 * (metadados locais) e Ativar/Inativar no +Compras.
 */
export function UnidadeMedidaTable({ unidades, onVisualizar, onEditar, onToggleAtivo }: {
  unidades: UnidadeMedida[];
  onVisualizar: (unidade: UnidadeMedida) => void;
  onEditar: (unidade: UnidadeMedida) => void;
  onToggleAtivo: (unidade: UnidadeMedida) => void;
}) {
  if (unidades.length === 0) return <div className="empty-state">Nenhuma unidade de medida encontrada.</div>;
  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Código</th>
          <th>Descrição ERP</th>
          <th>Descrição +Compras</th>
          <th>Status no +Compras</th>
          <th>Ações</th>
        </tr>
      </thead>
      <tbody>
        {unidades.map((unidade) => (
          <tr key={unidade.id}>
            <td className="mono">{unidade.codigoErp}</td>
            <td>{unidade.descricaoErp || <span className="empty-state">Sem descrição ERP</span>}</td>
            <td>{unidade.descricaoMaisCompras || <span className="empty-state">Sem descrição +Compras</span>}</td>
            <td><StatusBadge value={statusUnidadeMedida(unidade)} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onVisualizar(unidade)}>
                  Visualizar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(unidade)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleAtivo(unidade)}>
                  {unidade.ativoNoMaisCompras ? "Inativar no +Compras" : "Ativar no +Compras"}
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
