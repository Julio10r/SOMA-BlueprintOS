import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ConfirmExclusaoModal } from "../components/ConfirmExclusaoModal";
import { PerfilTable } from "../components/PerfilTable";
import { usePerfis } from "../hooks/usePerfis";
import type { Perfil } from "../types/perfilTypes";

/**
 * Listagem de Perfis (Gestao de Perfis, ADR-0020 item 8). Fundacao visual
 * da Sprint O1.2.2: dados mockados em memoria (services/perfisMockApi.ts),
 * sem integracao com API real.
 */
export function PerfisPage() {
  const navigate = useNavigate();
  const { perfis, loading, error, remove } = usePerfis();
  const [perfilParaExcluir, setPerfilParaExcluir] = useState<Perfil | null>(null);
  const [excluindo, setExcluindo] = useState(false);
  const [erroExclusao, setErroExclusao] = useState<string | null>(null);

  async function confirmarExclusao() {
    if (!perfilParaExcluir) return;
    setExcluindo(true);
    setErroExclusao(null);
    try {
      await remove(perfilParaExcluir.id);
      setPerfilParaExcluir(null);
    } catch (err) {
      setErroExclusao(err instanceof Error ? err.message : "Falha ao excluir perfil.");
    } finally {
      setExcluindo(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Gestao de Perfis</h1>
        <p>Perfis agrupam as permissoes do +Compras. Usuarios nunca recebem permissao individual.</p>
      </header>

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
            onExcluir={(perfil) => {
              setErroExclusao(null);
              setPerfilParaExcluir(perfil);
            }}
          />
        )}
      </section>

      {perfilParaExcluir && (
        <ConfirmExclusaoModal
          perfil={perfilParaExcluir}
          error={erroExclusao}
          loading={excluindo}
          onConfirm={confirmarExclusao}
          onCancel={() => setPerfilParaExcluir(null)}
        />
      )}
    </div>
  );
}
