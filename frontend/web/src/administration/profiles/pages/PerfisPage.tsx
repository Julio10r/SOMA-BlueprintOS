import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ConfirmStatusModal } from "../components/ConfirmStatusModal";
import { PerfilTable } from "../components/PerfilTable";
import { usePerfis } from "../hooks/usePerfis";
import { statusDoPerfil, type Perfil, type StatusPerfil } from "../types/perfilTypes";

/**
 * Listagem de Perfis (Gestao de Perfis, ADR-0020 item 8). A partir da O1.5 os dados vem
 * de `GET /administracao/perfis` (API real, persistencia em SQL Server) — o mock em
 * memoria da fundacao visual foi removido.
 *
 * O estado "acesso negado" reflete um 403 real do backend. Esconder a tela e apenas UX:
 * a barreira efetiva e a policy `Perfil.Gerenciar` no servidor, que nega a chamada
 * mesmo se a interface fosse burlada.
 */
export function PerfisPage() {
  const navigate = useNavigate();
  const { perfis, loading, error, acessoNegado, alterarStatus } = usePerfis();
  const [perfilEmTransicao, setPerfilEmTransicao] = useState<Perfil | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [erroStatus, setErroStatus] = useState<string | null>(null);
  const [busca, setBusca] = useState("");
  const [statusFiltro, setStatusFiltro] = useState<StatusPerfil | "Todos">("Todos");

  const perfisFiltrados = useMemo(() => {
    const termo = busca.trim().toLowerCase();
    return perfis.filter((perfil) => {
      const combinaBusca =
        !termo || perfil.nome.toLowerCase().includes(termo) || perfil.descricao.toLowerCase().includes(termo);
      const combinaStatus = statusFiltro === "Todos" || statusDoPerfil(perfil) === statusFiltro;
      return combinaBusca && combinaStatus;
    });
  }, [perfis, busca, statusFiltro]);

  async function confirmarStatus() {
    if (!perfilEmTransicao) return;
    setSalvando(true);
    setErroStatus(null);
    try {
      await alterarStatus(perfilEmTransicao.id, !perfilEmTransicao.ativo);
      setPerfilEmTransicao(null);
    } catch (err) {
      setErroStatus(err instanceof Error ? err.message : "Falha ao alterar o status do perfil.");
    } finally {
      setSalvando(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Gestão de Perfis</h1>
        <p>Perfis agrupam as permissões do +Compras. Usuários nunca recebem permissão individual.</p>
      </header>

      {acessoNegado ? (
        <section className="card">
          <div className="notice notice-warn">
            Você não tem permissão para acessar a Gestão de Perfis. Solicite a um administrador o vinculo com um
            perfil que possua a permissao <strong>Perfil.Gerenciar</strong>.
          </div>
        </section>
      ) : (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">Perfis</div>
              <h2>Perfis cadastrados</h2>
            </div>
            <button type="button" className="btn btn-primary" onClick={() => navigate("novo")}>
              Novo perfil
            </button>
          </div>

          <div className="input-row">
            <label>
              Pesquisar
              <input
                type="text"
                value={busca}
                onChange={(event) => setBusca(event.target.value)}
                placeholder="Nome ou descrição"
              />
            </label>
            <label>
              Status
              <select
                value={statusFiltro}
                onChange={(event) => setStatusFiltro(event.target.value as StatusPerfil | "Todos")}
              >
                <option value="Todos">Todos</option>
                <option value="Ativo">Ativo</option>
                <option value="Inativo">Inativo</option>
              </select>
            </label>
          </div>

          {error && <div className="notice notice-crit">{error}</div>}
          {loading ? (
            <div className="empty-state">Carregando perfis...</div>
          ) : (
            <PerfilTable
              perfis={perfisFiltrados}
              onVisualizar={(perfil) => navigate(perfil.id)}
              onEditar={(perfil) => navigate(`${perfil.id}/editar`)}
              onAlternarStatus={(perfil) => {
                setErroStatus(null);
                setPerfilEmTransicao(perfil);
              }}
            />
          )}
        </section>
      )}

      {perfilEmTransicao && (
        <ConfirmStatusModal
          perfil={perfilEmTransicao}
          error={erroStatus}
          loading={salvando}
          onConfirm={confirmarStatus}
          onCancel={() => setPerfilEmTransicao(null)}
        />
      )}
    </div>
  );
}
