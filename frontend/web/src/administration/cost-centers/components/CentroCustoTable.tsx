import { StatusBadge } from "../../../shared/components/StatusBadge";
import { statusCentroCusto, type CentroCusto } from "../types/centroCustoTypes";

/**
 * Listagem de Centros de Custo. Preserva sempre as colunas exigidas —
 * codigo ERP, descricao ERP e descricao +Compras — nunca ocultando ou
 * substituindo a descricao oficial do ERP (ADR-0020, item 2). Nao existe
 * acao de criacao nem de exclusao: apenas Visualizar, Editar (metadados
 * locais) e Ativar/Inativar no +Compras. A coluna de Unidade de Alocacao
 * padrao representa, o vínculo real de Unidade de Alocação padrão (ADR-0020, item 5): o campo vem da API real e aparece vazio até que o vínculo seja cadastrado, o relacionamento ainda nao
 * implementado com o modulo Unidades de Alocacao (ADR-0020, item 5).
 */
export function CentroCustoTable({ centrosCusto, onVisualizar, onEditar, onToggleAtivo }: {
  centrosCusto: CentroCusto[];
  onVisualizar: (centroCusto: CentroCusto) => void;
  onEditar: (centroCusto: CentroCusto) => void;
  onToggleAtivo: (centroCusto: CentroCusto) => void;
}) {
  if (centrosCusto.length === 0) return <div className="empty-state">Nenhum centro de custo encontrado.</div>;
  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Codigo</th>
          <th>Descricao ERP</th>
          <th>Descricao +Compras</th>
          <th>Unidade de Negocio</th>
          <th>Unidade de Alocacao padrao</th>
          <th>Status</th>
          <th>Acoes</th>
        </tr>
      </thead>
      <tbody>
        {centrosCusto.map((centroCusto) => (
          <tr key={centroCusto.id}>
            <td className="mono">{centroCusto.codigoErp}</td>
            <td>{centroCusto.descricaoErp}</td>
            <td>{centroCusto.descricaoMaisCompras || <span className="empty-state">Sem descricao +Compras</span>}</td>
            <td>{centroCusto.unidadeNegocioId}</td>
            <td>
              {centroCusto.unidadeAlocacaoPadraoNome || <span className="empty-state">Sem unidade padrao</span>}
            </td>
            <td><StatusBadge value={statusCentroCusto(centroCusto)} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onVisualizar(centroCusto)}>
                  Visualizar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(centroCusto)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleAtivo(centroCusto)}>
                  {centroCusto.ativoNoMaisCompras ? "Inativar no +Compras" : "Ativar no +Compras"}
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
