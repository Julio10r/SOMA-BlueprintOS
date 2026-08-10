import { permissionCatalog, groupPermissionsByRecurso } from "../services/permissionCatalog";

/**
 * Visualizacao somente leitura das permissoes de um Perfil, agrupadas por
 * recurso. Reflete a regra RBAC da ADR-0020 (item 8): permissoes sempre
 * pertencem ao Perfil, nunca a um usuario individualmente.
 */
export function PermissoesResumo({ permissoes }: { permissoes: string[] }) {
  const catalogoAtribuido = permissionCatalog.filter((permissao) => permissoes.includes(permissao.id));
  if (catalogoAtribuido.length === 0) {
    return <div className="empty-state">Nenhuma permissao atribuida a este perfil.</div>;
  }
  const grupos = groupPermissionsByRecurso(catalogoAtribuido);
  return (
    <div className="data-block">
      {grupos.map(([recurso, permissoesDoRecurso]) => (
        <div key={recurso} className="data-block">
          <div className="section-title">{recurso}</div>
          <div className="data-grid">
            {permissoesDoRecurso.map((permissao) => (
              <div className="field-readonly" key={permissao.id}>
                <span>{permissao.acao}</span>
                <strong>{permissao.descricao}</strong>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
