import { StatusBadge } from "../../../shared/components/StatusBadge";
import { statusItemFiscal, type ItemFiscal } from "../types/itemFiscalTypes";

/**
 * Listagem de Itens Fiscais (B3 - Bloco 3). Ao contrário dos cadastros de apoio (Filial/Centro de
 * Custo/Conta Contábil/Unidade de Medida), Item Fiscal É criado pelo +Compras — a listagem inclui ação de
 * criação (botão na página, não nesta tabela) e de edição completa (não só metadados locais).
 */
export function ItemFiscalTable({ itens, onVisualizar, onEditar, onToggleAtivo }: {
  itens: ItemFiscal[];
  onVisualizar: (item: ItemFiscal) => void;
  onEditar: (item: ItemFiscal) => void;
  onToggleAtivo: (item: ItemFiscal) => void;
}) {
  if (itens.length === 0) return <div className="empty-state">Nenhum item fiscal encontrado.</div>;
  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Código</th>
          <th>Descrição</th>
          <th>Unidade</th>
          <th>Conta Contábil</th>
          <th>Status</th>
          <th>Ações</th>
        </tr>
      </thead>
      <tbody>
        {itens.map((item) => (
          <tr key={item.id}>
            <td className="mono">{item.codigo}</td>
            <td>{item.descricao}</td>
            <td>{item.unidadeMedidaDescricao ?? item.unidadeMedidaCodigoErp}</td>
            <td>{item.contaContabilDescricao ?? item.contaContabilCodigoErp}</td>
            <td><StatusBadge value={statusItemFiscal(item)} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onVisualizar(item)}>
                  Visualizar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(item)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleAtivo(item)}>
                  {item.ativo ? "Inativar" : "Ativar"}
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
