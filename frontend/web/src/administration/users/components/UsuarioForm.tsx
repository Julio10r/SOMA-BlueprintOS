import { FormEvent, useState } from "react";
import type { Perfil } from "../../profiles/types/perfilTypes";
import { costCenterCatalog } from "../services/costCenterCatalog";
import type { StatusUsuario, Usuario, UsuarioInput } from "../types/userTypes";

export function UsuarioForm({ usuario, perfisDisponiveis, error, loading, onSubmit, onCancel }: {
  usuario?: Usuario;
  perfisDisponiveis: Perfil[];
  error: string | null;
  loading: boolean;
  onSubmit: (input: UsuarioInput) => void;
  onCancel: () => void;
}) {
  const [nome, setNome] = useState(usuario?.nome ?? "");
  const [email, setEmail] = useState(usuario?.email ?? "");
  const [status, setStatus] = useState<StatusUsuario>(usuario?.status ?? "Ativo");
  const [unidadeNegocio] = useState(usuario?.unidadeNegocio ?? "SOMA");
  const [perfis, setPerfis] = useState<string[]>(usuario?.perfis ?? []);
  const [todosCentrosCusto, setTodosCentrosCusto] = useState(usuario?.todosCentrosCusto ?? false);
  const [centrosCusto, setCentrosCusto] = useState<string[]>(usuario?.centrosCusto ?? []);

  function togglePerfil(id: string) {
    setPerfis((current) => (current.includes(id) ? current.filter((item) => item !== id) : [...current, id]));
  }

  function toggleCentroCusto(id: string) {
    setCentrosCusto((current) => (current.includes(id) ? current.filter((item) => item !== id) : [...current, id]));
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({
      nome,
      email,
      status,
      perfis,
      todosCentrosCusto,
      centrosCusto: todosCentrosCusto ? [] : centrosCusto,
      filiais: usuario?.filiais ?? [],
      unidadeNegocio
    });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>{usuario ? "Editar usuario" : "Novo usuario"}</h2>
      </div>

      <label>
        Nome
        <input value={nome} onChange={(event) => setNome(event.target.value)} required disabled={loading} />
      </label>

      <label>
        E-mail
        <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required disabled={loading} />
      </label>

      <div className="input-row">
        <label>
          Unidade de Negocio
          <input value={unidadeNegocio} disabled readOnly />
        </label>
        <label>
          Status
          <select value={status} onChange={(event) => setStatus(event.target.value as StatusUsuario)} disabled={loading}>
            <option value="Ativo">Ativo</option>
            <option value="Inativo">Inativo</option>
          </select>
        </label>
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <div className="data-block">
        <div className="section-title">Perfis</div>
        <div className="notice notice-warn">
          O usuario nao recebe nenhuma permissao individual. Todas as permissoes sao herdadas exclusivamente dos
          Perfis selecionados abaixo; um usuario pode ter varios Perfis simultaneamente.
        </div>
        <div className="data-grid">
          {perfisDisponiveis.map((perfil) => (
            <label key={perfil.id} className="field-readonly">
              <input
                type="checkbox"
                checked={perfis.includes(perfil.id)}
                onChange={() => togglePerfil(perfil.id)}
                disabled={loading}
              />
              <strong>{perfil.nome}</strong>
              <span>{perfil.descricao}</span>
            </label>
          ))}
        </div>
      </div>

      <div className="data-block">
        <div className="section-title">Centros de Custo</div>
        <p>Centro de Custo representa autorizacao operacional, independente dos Perfis vinculados.</p>
        <label className="field-readonly">
          <input
            type="checkbox"
            checked={todosCentrosCusto}
            onChange={(event) => setTodosCentrosCusto(event.target.checked)}
            disabled={loading}
          />
          <strong>Acesso a todos os Centros de Custo</strong>
        </label>
        {!todosCentrosCusto && (
          <div className="data-grid">
            {costCenterCatalog.map((centroCusto) => (
              <label key={centroCusto.id} className="field-readonly">
                <input
                  type="checkbox"
                  checked={centrosCusto.includes(centroCusto.id)}
                  onChange={() => toggleCentroCusto(centroCusto.id)}
                  disabled={loading}
                />
                <strong>{centroCusto.codigo}</strong>
                <span>{centroCusto.descricao}</span>
              </label>
            ))}
          </div>
        )}
      </div>

      <div className="data-block">
        <div className="section-title">Filiais</div>
        <div className="empty-state">
          Vinculo com Filiais sera preparado em etapa futura. Estrutura apenas visual, sem regra aplicada nesta fundacao.
        </div>
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
