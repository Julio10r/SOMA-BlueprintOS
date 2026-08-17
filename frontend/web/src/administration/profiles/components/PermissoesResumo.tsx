import { groupPermissionsByRecurso } from "../services/permissionCatalog";
import type { Permissao } from "../types/perfilTypes";

/**
 * Visualizacao somente leitura das permissoes de um Perfil, agrupadas por recurso.
 * Reflete a regra RBAC da ADR-0020 (item 8): permissoes sempre pertencem ao Perfil,
 * nunca a um usuario individualmente.
 *
 * `catalogo` vem do backend (O1.5). Um codigo atribuido ao Perfil mas ausente do
 * catalogo e exibido pelo proprio codigo, em vez de desaparecer silenciosamente — um
 * acesso concedido nunca deve ficar invisivel para quem audita a tela.
 */
export function PermissoesResumo({ permissoes, catalogo }: { permissoes: string[]; catalogo: Permissao[] }) {
  if (permissoes.length === 0) {
    return <div className="empty-state">Nenhuma permissão atribuída a este perfil.</div>;
  }

  const porCodigo = new Map(catalogo.map((permissao) => [permissao.codigo, permissao]));
  const atribuidas: Permissao[] = permissoes.map((codigo) =>
    porCodigo.get(codigo) ?? { codigo, recurso: "Não catalogada", acao: codigo, descricao: "Permissão fora do catálogo atual." }
  );

  const grupos = groupPermissionsByRecurso(atribuidas);
  return (
    <div className="data-block">
      {grupos.map(([recurso, permissoesDoRecurso]) => (
        <div key={recurso} className="data-block">
          <div className="section-title">{recurso}</div>
          <div className="data-grid">
            {permissoesDoRecurso.map((permissao) => (
              <div className="field-readonly" key={permissao.codigo}>
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
