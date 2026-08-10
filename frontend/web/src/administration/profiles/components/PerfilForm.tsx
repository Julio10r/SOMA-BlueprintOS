import { FormEvent, useState } from "react";
import { groupPermissionsByRecurso, permissionCatalog } from "../services/permissionCatalog";
import type { Perfil, PerfilInput, StatusPerfil } from "../types/perfilTypes";

const gruposPermissoes = groupPermissionsByRecurso(permissionCatalog);

export function PerfilForm({ perfil, error, loading, onSubmit, onCancel }: {
  perfil?: Perfil;
  error: string | null;
  loading: boolean;
  onSubmit: (input: PerfilInput) => void;
  onCancel: () => void;
}) {
  const [nome, setNome] = useState(perfil?.nome ?? "");
  const [descricao, setDescricao] = useState(perfil?.descricao ?? "");
  const [status, setStatus] = useState<StatusPerfil>(perfil?.status ?? "Ativo");
  const [unidadeNegocio, setUnidadeNegocio] = useState(perfil?.unidadeNegocio ?? "SOMA");
  const [permissoes, setPermissoes] = useState<string[]>(perfil?.permissoes ?? []);

  function togglePermissao(id: string) {
    setPermissoes((current) => (current.includes(id) ? current.filter((item) => item !== id) : [...current, id]));
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({ nome, descricao, status, unidadeNegocio, permissoes });
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

      <div className="input-row">
        <label>
          Unidade de Negocio
          <input value={unidadeNegocio} onChange={(event) => setUnidadeNegocio(event.target.value)} required disabled={loading} />
        </label>
        <label>
          Status
          <select value={status} onChange={(event) => setStatus(event.target.value as StatusPerfil)} disabled={loading}>
            <option value="Ativo">Ativo</option>
            <option value="Inativo">Inativo</option>
          </select>
        </label>
      </div>

      <div className="data-block">
        <div className="section-title">Permissoes</div>
        {gruposPermissoes.map(([recurso, permissoesDoRecurso]) => (
          <div key={recurso} className="data-block">
            <div className="section-title">{recurso}</div>
            <div className="data-grid">
              {permissoesDoRecurso.map((permissao) => (
                <label key={permissao.id} className="field-readonly">
                  <input
                    type="checkbox"
                    checked={permissoes.includes(permissao.id)}
                    onChange={() => togglePermissao(permissao.id)}
                    disabled={loading}
                  />
                  <strong>{permissao.acao}</strong>
                  <span>{permissao.descricao}</span>
                </label>
              ))}
            </div>
          </div>
        ))}
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
