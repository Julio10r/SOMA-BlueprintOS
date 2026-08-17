import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ConfirmToggleAtivoUsuarioModal } from "../components/ConfirmToggleAtivoUsuarioModal";
import { UsuarioTable } from "../components/UsuarioTable";
import { useUsuarios } from "../hooks/useUsuarios";
import { statusDoUsuario, type StatusUsuario, type Usuario } from "../types/userTypes";

/**
 * Listagem de Usuarios (Gestao de Usuarios). A partir da O1.6, consome a API real
 * (`administracao/usuarios`), substituindo o mock em memoria da fundacao visual
 * (Sprint O1.3.2) — mesmo padrao de integracao de `administration/profiles` (O1.5).
 *
 * Usuarios nunca sao excluidos fisicamente: permanecem auditaveis e apenas
 * transitam entre Ativo/Inativo, mesmo padrao de Filiais, Centros de Custo
 * e Unidades de Alocacao.
 */
export function UsuariosPage() {
  const navigate = useNavigate();
  const { usuarios, loading, error, acessoNegado, toggleAtivo } = useUsuarios();
  const [usuarioParaAlternar, setUsuarioParaAlternar] = useState<Usuario | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [erroToggle, setErroToggle] = useState<string | null>(null);
  const [busca, setBusca] = useState("");
  const [statusFiltro, setStatusFiltro] = useState<StatusUsuario | "Todos">("Todos");

  const usuariosFiltrados = useMemo(() => {
    const termo = busca.trim().toLowerCase();
    return usuarios.filter((usuario) => {
      const combinaBusca =
        !termo || usuario.nome.toLowerCase().includes(termo) || usuario.email.toLowerCase().includes(termo);
      const combinaStatus = statusFiltro === "Todos" || statusDoUsuario(usuario) === statusFiltro;
      return combinaBusca && combinaStatus;
    });
  }, [usuarios, busca, statusFiltro]);

  async function confirmarToggleAtivo() {
    if (!usuarioParaAlternar) return;
    setSalvando(true);
    setErroToggle(null);
    try {
      await toggleAtivo(usuarioParaAlternar);
      setUsuarioParaAlternar(null);
    } catch (err) {
      setErroToggle(err instanceof Error ? err.message : "Falha ao alterar o status do usuário.");
    } finally {
      setSalvando(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Gestão de Usuários</h1>
        <p>Usuários recebem acesso ao +Compras por meio de Perfis e Centros de Custo. Nunca há permissão individual.</p>
      </header>

      {acessoNegado ? (
        <section className="card">
          <div className="notice notice-crit">Você não tem permissão para acessar a Gestão de Usuários.</div>
        </section>
      ) : (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">Usuários</div>
              <h2>Usuários cadastrados</h2>
            </div>
            <button type="button" className="btn btn-primary" onClick={() => navigate("novo")}>
              Novo usuário
            </button>
          </div>

          <div className="input-row">
            <label>
              Pesquisar
              <input
                type="text"
                value={busca}
                onChange={(event) => setBusca(event.target.value)}
                placeholder="Nome ou e-mail"
              />
            </label>
            <label>
              Status
              <select
                value={statusFiltro}
                onChange={(event) => setStatusFiltro(event.target.value as StatusUsuario | "Todos")}
              >
                <option value="Todos">Todos</option>
                <option value="Ativo">Ativo</option>
                <option value="Inativo">Inativo</option>
              </select>
            </label>
          </div>

          {error ? (
            <div className="notice notice-crit">{error}</div>
          ) : loading ? (
            <div className="empty-state">Carregando usuários...</div>
          ) : (
            <UsuarioTable
              usuarios={usuariosFiltrados}
              onVisualizar={(usuario) => navigate(usuario.id)}
              onEditar={(usuario) => navigate(`${usuario.id}/editar`)}
              onToggleAtivo={(usuario) => {
                setErroToggle(null);
                setUsuarioParaAlternar(usuario);
              }}
            />
          )}
        </section>
      )}

      {usuarioParaAlternar && (
        <ConfirmToggleAtivoUsuarioModal
          usuario={usuarioParaAlternar}
          error={erroToggle}
          loading={salvando}
          onConfirm={confirmarToggleAtivo}
          onCancel={() => setUsuarioParaAlternar(null)}
        />
      )}
    </div>
  );
}
