import { FormEvent, useState } from "react";
import { groupPermissionsByRecurso } from "../services/permissionCatalog";
import type { Perfil, PerfilInput, Permissao } from "../types/perfilTypes";

/**
 * Estrutura visual preservada da fundacao aprovada (O1.3.1). Mudancas funcionalmente
 * necessarias na O1.5:
 * - o catalogo de permissoes chega por props (vindo de `GET /administracao/permissoes`)
 *   em vez de uma lista estatica no frontend;
 * - o campo livre "Unidade de Negocio" foi removido: o backend usa sempre a Unidade de
 *   Negocio da sessao autenticada e ignoraria o valor digitado — manter o campo seria
 *   exibir um controle sem efeito;
 * - o campo "Status" foi removido do formulario: ativacao/inativacao passa a ser uma acao
 *   propria (`PATCH .../status`), com confirmacao, porque revoga acesso de todos os
 *   usuarios vinculados.
 */
export function PerfilForm({ perfil, permissoes, error, loading, onSubmit, onCancel }: {
  perfil?: Perfil;
  permissoes: Permissao[];
  error: string | null;
  loading: boolean;
  onSubmit: (input: PerfilInput) => void;
  onCancel: () => void;
}) {
  const [nome, setNome] = useState(perfil?.nome ?? "");
  const [descricao, setDescricao] = useState(perfil?.descricao ?? "");
  const [selecionadas, setSelecionadas] = useState<string[]>(perfil?.permissoes ?? []);

  const grupos = groupPermissionsByRecurso(permissoes);

  function togglePermissao(codigo: string) {
    setSelecionadas((current) =>
      current.includes(codigo) ? current.filter((item) => item !== codigo) : [...current, codigo]
    );
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({ nome, descricao, permissoes: selecionadas });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>{perfil ? "Editar perfil" : "Novo perfil"}</h2>
      </div>

      <div className="notice notice-warn">
        Permissoes pertencem exclusivamente a este perfil. Um usuario nunca recebe permissao individual: se ele
        precisar de um conjunto diferente de acessos, vincule-o a outro perfil ou crie um novo — um mesmo usuario
        pode ter varios perfis simultaneamente.
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <label>
        Nome
        <input value={nome} onChange={(event) => setNome(event.target.value)} required disabled={loading} />
      </label>

      <label>
        Descricao
        <input value={descricao} onChange={(event) => setDescricao(event.target.value)} required disabled={loading} />
      </label>

      <div className="data-block">
        <div className="section-title">Permissoes</div>
        {grupos.length === 0 ? (
          <div className="empty-state">Nenhuma permissão disponível no catálogo.</div>
        ) : (
          grupos.map(([recurso, permissoesDoRecurso]) => (
            <div key={recurso} className="data-block">
              <div className="section-title">{recurso}</div>
              <div className="data-grid">
                {permissoesDoRecurso.map((permissao) => (
                  <label key={permissao.codigo} className="field-readonly">
                    <input
                      type="checkbox"
                      checked={selecionadas.includes(permissao.codigo)}
                      onChange={() => togglePermissao(permissao.codigo)}
                      disabled={loading}
                    />
                    <strong>{permissao.acao}</strong>
                    <span>{permissao.descricao}</span>
                  </label>
                ))}
              </div>
            </div>
          ))
        )}
      </div>

      <div className="actions">
        <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? "Salvando..." : "Salvar"}
        </button>
      </div>
    </form>
  );
}
