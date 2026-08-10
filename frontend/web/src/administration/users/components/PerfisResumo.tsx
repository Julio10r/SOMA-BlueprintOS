import { permissionCatalog } from "../../profiles/services/permissionCatalog";
import type { Perfil } from "../../profiles/types/perfilTypes";

/**
 * Visualizacao somente leitura dos Perfis vinculados a um usuario e das
 * permissoes que eles concedem (agregadas). Reflete a regra RBAC da
 * ADR-0020 (item 8/9): o usuario nunca recebe permissao individual, apenas
 * herda o que os Perfis selecionados concedem.
 */
export function PerfisResumo({ perfilIds, todosPerfis }: { perfilIds: string[]; todosPerfis: Perfil[] }) {
  const perfis = todosPerfis.filter((perfil) => perfilIds.includes(perfil.id));
  if (perfis.length === 0) {
    return <div className="empty-state">Nenhum perfil vinculado a este usuario.</div>;
  }
  const permissoesHerdadas = new Set(perfis.flatMap((perfil) => perfil.permissoes));
  return (
    <div className="data-block">
      <div className="data-grid">
        {perfis.map((perfil) => (
          <div className="field-readonly" key={perfil.id}>
            <span>Perfil</span>
            <strong>{perfil.nome}</strong>
          </div>
        ))}
      </div>
      <div className="notice notice-warn">
        Todas as permissoes deste usuario sao herdadas exclusivamente dos Perfis acima
        ({permissoesHerdadas.size} permissao(oes) no total). Nao existe permissao individual de usuario.
      </div>
      <div className="data-grid">
        {permissionCatalog
          .filter((permissao) => permissoesHerdadas.has(permissao.id))
          .map((permissao) => (
            <div className="field-readonly" key={permissao.id}>
              <span>{permissao.recurso}</span>
              <strong>{permissao.acao}</strong>
            </div>
          ))}
      </div>
    </div>
  );
}
