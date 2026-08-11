import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ConfirmStatusModal } from "../components/ConfirmStatusModal";
import { PerfilTable } from "../components/PerfilTable";
import { usePerfis } from "../hooks/usePerfis";
import type { Perfil } from "../types/perfilTypes";

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
        <div className="section-title">Administracao</div>
        <h1>Gestao de Perfis</h1>
        <p>Perfis agrupam as permissoes do +Compras. Usuarios nunca recebem permissao individual.</p>
      </header>

      {acessoNegado ? (
        <section className="card">
          <div className="notice notice-warn">
            Voce nao tem permissao para acessar a Gestao de Perfis. Solicite a um administrador o vinculo com um
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

          {error && <div className="notice notice-crit">{error}</div>}
          {loading ? (
            <div className="empty-state">Carregando perfis...</div>
          ) : (
            <PerfilTable
              perfis={perfis}
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
