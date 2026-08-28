import { StatusBadge } from "../../../shared/components/StatusBadge";
import type { UnidadeAlocacao } from "../types/unidadeAlocacaoTypes";

/**
 * Listagem de Unidades de Alocacao. Nao existe acao de exclusao fisica —
 * apenas Visualizar, Editar e Ativar/Inativar, seguindo o mesmo principio
 * ja aplicado aos demais modulos de Administracao.
 */
export function UnidadeAlocacaoTable({ unidadesAlocacao, onVisualizar, onEditar, onToggleStatus }: {
  unidadesAlocacao: UnidadeAlocacao[];
  onVisualizar: (unidadeAlocacao: UnidadeAlocacao) => void;
  onEditar: (unidadeAlocacao: UnidadeAlocacao) => void;
  onToggleStatus: (unidadeAlocacao: UnidadeAlocacao) => void;
}) {
  if (unidadesAlocacao.length === 0) return <div className="empty-state">Nenhuma unidade de alocação encontrada.</div>;
  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Nome</th>
          <th>Descrição</th>
          <th>Status</th>
          <th>Ações</th>
        </tr>
      </thead>
      <tbody>
        {unidadesAlocacao.map((unidadeAlocacao) => (
          <tr key={unidadeAlocacao.id}>
            <td>{unidadeAlocacao.nome}</td>
            <td>{unidadeAlocacao.descricao}</td>
            <td><StatusBadge value={unidadeAlocacao.status} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onVisualizar(unidadeAlocacao)}>
                  Visualizar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(unidadeAlocacao)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleStatus(unidadeAlocacao)}>
                  {unidadeAlocacao.status === "Ativo" ? "Inativar" : "Ativar"}
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
