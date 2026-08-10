import { StatusBadge } from "../../../shared/components/StatusBadge";
import type { Perfil } from "../types/perfilTypes";

export function PerfilTable({ perfis, onVisualizar, onEditar, onExcluir }: {
  perfis: Perfil[];
  onVisualizar: (perfil: Perfil) => void;
  onEditar: (perfil: Perfil) => void;
  onExcluir: (perfil: Perfil) => void;
}) {
  if (perfis.length === 0) return <div className="empty-state">Nenhum perfil cadastrado.</div>;
  return (
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Nome</th>
          <th>Descricao</th>
          <th>Unidade de Negocio</th>
          <th>Permissoes</th>
          <th>Usuarios vinculados</th>
          <th>Status</th>
          <th>Acoes</th>
        </tr>
      </thead>
      <tbody>
        {perfis.map((perfil) => (
          <tr key={perfil.id}>
            <td>{perfil.nome}</td>
            <td>{perfil.descricao}</td>
            <td>{perfil.unidadeNegocio}</td>
            <td>{perfil.permissoes.length}</td>
            <td>{perfil.usuariosVinculados}</td>
            <td><StatusBadge value={perfil.status} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onVisualizar(perfil)}>
                  Visualizar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(perfil)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onExcluir(perfil)}>
                  Excluir
                </button>
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
