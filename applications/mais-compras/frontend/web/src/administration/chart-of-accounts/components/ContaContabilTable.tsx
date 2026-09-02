import { StatusBadge } from "../../../shared/components/StatusBadge";
import { statusContaContabilEfetivo, statusContaContabilErp, type ContaContabil } from "../types/contaContabilTypes";

/**
 * Listagem de Contas Contabeis. Mostra Status Linx e Status +Compras separadamente (ADR-0024: o status
 * Linx nunca e sobreposto). Sem acao de criacao nem de exclusao: apenas Visualizar, Editar (metadados
 * locais) e Ativar/Inativar no +Compras.
 */
export function ContaContabilTable({ contas, onVisualizar, onEditar, onToggleAtivo }: {
  contas: ContaContabil[];
  onVisualizar: (conta: ContaContabil) => void;
  onEditar: (conta: ContaContabil) => void;
  onToggleAtivo: (conta: ContaContabil) => void;
}) {
  if (contas.length === 0) return <div className="empty-state">Nenhuma conta contábil encontrada.</div>;
  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Código</th>
          <th>Descrição ERP</th>
          <th>Descrição +Compras</th>
          <th>Status Linx</th>
          <th>Status +Compras</th>
          <th>Ações</th>
        </tr>
      </thead>
      <tbody>
        {contas.map((conta) => (
          <tr key={conta.id}>
            <td className="mono">{conta.codigoErp}</td>
            <td>{conta.descricaoErp}</td>
            <td>{conta.descricaoMaisCompras || <span className="empty-state">Sem descrição +Compras</span>}</td>
            <td><StatusBadge value={statusContaContabilErp(conta)} tone="situacao" /></td>
            <td><StatusBadge value={statusContaContabilEfetivo(conta)} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onVisualizar(conta)}>
                  Visualizar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(conta)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleAtivo(conta)}>
                  {conta.ativoNoMaisCompras ? "Inativar no +Compras" : "Ativar no +Compras"}
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
