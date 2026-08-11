import { FormEvent, useState } from "react";
import type { CentroCusto } from "../../cost-centers/types/centroCustoTypes";
import type { Perfil } from "../../profiles/types/perfilTypes";
import type { Usuario, UsuarioInput } from "../types/userTypes";

export function UsuarioForm({ usuario, perfisDisponiveis, centrosCustoDisponiveis, error, loading, onSubmit, onCancel }: {
  usuario?: Usuario;
  perfisDisponiveis: Perfil[];
  centrosCustoDisponiveis: CentroCusto[];
  error: string | null;
  loading: boolean;
  onSubmit: (input: UsuarioInput) => void;
  onCancel: () => void;
}) {
  const [nome, setNome] = useState(usuario?.nome ?? "");
  // O e-mail nao e editavel na edicao (O1.6, identifica a conta e o fluxo de Login OTP) —
  // o campo permanece somente leitura quando `usuario` existe.
  const [email, setEmail] = useState(usuario?.email ?? "");
  const [perfis, setPerfis] = useState<string[]>(usuario?.perfis.map((p) => p.id) ?? []);
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
      perfis,
      todosCentrosCusto,
      centrosCusto: todosCentrosCusto ? [] : centrosCusto
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
        <input
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
          disabled={loading || Boolean(usuario)}
          readOnly={Boolean(usuario)}
        />
      </label>
      {usuario && (
        <p className="empty-state">
          O e-mail nao pode ser alterado apos a criacao. Use a acao Ativar/Inativar para revogar o acesso.
        </p>
      )}

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
            {centrosCustoDisponiveis.map((centroCusto) => (
              <label key={centroCusto.id} className="field-readonly">
                <input
                  type="checkbox"
                  checked={centrosCusto.includes(centroCusto.id)}
                  onChange={() => toggleCentroCusto(centroCusto.id)}
                  disabled={loading}
                />
                <strong>{centroCusto.codigoErp}</strong>
                <span>{centroCusto.descricaoMaisCompras || centroCusto.descricaoErp}</span>
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
