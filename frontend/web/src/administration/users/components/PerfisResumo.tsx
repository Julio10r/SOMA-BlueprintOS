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
        {Array.from(permissoesHerdadas)
          .sort()
          .map((codigo) => {
            // O codigo do catalogo tem o formato `Recurso.Acao` (ver PermissaoCatalogo no
            // backend); a decomposicao para exibicao dispensa consultar o catalogo aqui.
            const separador = codigo.indexOf(".");
            const recurso = separador > 0 ? codigo.slice(0, separador) : codigo;
            const acao = separador > 0 ? codigo.slice(separador + 1) : codigo;
            return (
              <div className="field-readonly" key={codigo}>
                <span>{recurso}</span>
                <strong>{acao}</strong>
              </div>
            );
          })}
      </div>
    </div>
  );
}
