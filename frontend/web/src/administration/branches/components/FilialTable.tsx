import { StatusBadge } from "../../../shared/components/StatusBadge";
import { statusFilial, type Filial } from "../types/filialTypes";

/**
 * Listagem de Filiais. Preserva sempre as tres colunas exigidas pela
 * ADR-0020 (item 2): codigo ERP, descricao ERP e descricao +Compras —
 * nunca oculta ou substitui a descricao oficial do ERP. Nao existe acao
 * de criacao nem de exclusao: apenas Visualizar, Editar (metadados
 * locais) e Ativar/Inativar no +Compras.
 */
export function FilialTable({ filiais, onVisualizar, onEditar, onToggleAtivo }: {
  filiais: Filial[];
  onVisualizar: (filial: Filial) => void;
  onEditar: (filial: Filial) => void;
  onToggleAtivo: (filial: Filial) => void;
}) {
  if (filiais.length === 0) return <div className="empty-state">Nenhuma filial encontrada.</div>;
  return (
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Codigo CliFor</th>
          <th>Nome CliFor / Descricao ERP</th>
          <th>Descricao +Compras</th>
          <th>Status no +Compras</th>
          <th>Unidade de Negocio</th>
          <th>Acoes</th>
        </tr>
      </thead>
      <tbody>
        {filiais.map((filial) => (
          <tr key={filial.id}>
            <td className="mono">{filial.codigoCliFor}</td>
            <td>{filial.nomeCliFor}</td>
            <td>{filial.descricaoMaisCompras || <span className="empty-state">Sem descricao +Compras</span>}</td>
            <td><StatusBadge value={statusFilial(filial)} tone="situacao" /></td>
            <td>{filial.unidadeNegocioId}</td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onVisualizar(filial)}>
                  Visualizar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(filial)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleAtivo(filial)}>
                  {filial.ativoNoMaisCompras ? "Inativar no +Compras" : "Ativar no +Compras"}
                </button>
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
