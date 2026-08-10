import { StatusBadge } from "../../../shared/components/StatusBadge";
import type { Usuario } from "../types/userTypes";

export function UsuarioTable({ usuarios, onVisualizar, onEditar, onToggleAtivo }: {
  usuarios: Usuario[];
  onVisualizar: (usuario: Usuario) => void;
  onEditar: (usuario: Usuario) => void;
  onToggleAtivo: (usuario: Usuario) => void;
}) {
  if (usuarios.length === 0) return <div className="empty-state">Nenhum usuario cadastrado.</div>;
  return (
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Nome</th>
          <th>E-mail</th>
          <th>Perfis</th>
          <th>Centros de Custo</th>
          <th>Status</th>
          <th>Acoes</th>
        </tr>
      </thead>
      <tbody>
        {usuarios.map((usuario) => (
          <tr key={usuario.id}>
            <td>{usuario.nome}</td>
            <td>{usuario.email}</td>
            <td>{usuario.perfis.length}</td>
            <td>{usuario.todosCentrosCusto ? "Todos" : usuario.centrosCusto.length}</td>
            <td><StatusBadge value={usuario.status} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onVisualizar(usuario)}>
                  Visualizar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(usuario)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleAtivo(usuario)}>
                  {usuario.status === "Ativo" ? "Inativar" : "Ativar"}
                </button>
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
