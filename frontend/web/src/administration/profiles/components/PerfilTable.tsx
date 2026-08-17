import { StatusBadge } from "../../../shared/components/StatusBadge";
import { statusDoPerfil, type Perfil } from "../types/perfilTypes";

/**
 * Estrutura visual preservada da fundacao aprovada (O1.3.1). Duas mudancas
 * funcionalmente necessarias na O1.5: a coluna "Unidade de Negocio" deixa de existir
 * como texto livre (o backend escopa tudo a Unidade de Negocio da sessao, e o Id cru
 * nao e informacao util na tela), e a acao "Excluir" e substituida por
 * "Ativar"/"Inativar" — `ComprasFuncional.md` ("Gestao de Perfis") lista como acoes
 * oficiais apenas Criar, Editar e Ativar/Inativar, e o backend nao expoe exclusao.
 */
export function PerfilTable({ perfis, onVisualizar, onEditar, onAlternarStatus }: {
  perfis: Perfil[];
  onVisualizar: (perfil: Perfil) => void;
  onEditar: (perfil: Perfil) => void;
  onAlternarStatus: (perfil: Perfil) => void;
}) {
  if (perfis.length === 0) return <div className="empty-state">Nenhum perfil cadastrado.</div>;
  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Nome</th>
          <th>Descricao</th>
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
            <td>{perfil.permissoes.length}</td>
            <td>{perfil.usuariosVinculados}</td>
            <td><StatusBadge value={statusDoPerfil(perfil)} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onVisualizar(perfil)}>
                  Visualizar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(perfil)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onAlternarStatus(perfil)}>
                  {perfil.ativo ? "Inativar" : "Ativar"}
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
