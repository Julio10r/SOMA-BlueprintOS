import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ConfirmDialog } from "../../../shared/components/ConfirmDialog";
import { ParametroTable } from "../components/ParametroTable";
import { useParametros } from "../hooks/useParametros";
import type { Parametro } from "../types/parametroTypes";

export function ParametrosPage() {
  const navigate = useNavigate();
  const { parametros, loading, error, remover } = useParametros();
  // Gate de homologação (2026-09-01): nunca window.confirm nativo do navegador — confirmação de
  // exclusão é sempre um modal da própria aplicação, em todas as telas.
  const [parametroParaExcluir, setParametroParaExcluir] = useState<Parametro | null>(null);

  async function confirmarExclusao() {
    if (!parametroParaExcluir) return;
    await remover(parametroParaExcluir.id);
    setParametroParaExcluir(null);
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Parâmetros</h1>
        <p>Parâmetros técnicos globais ou por Unidade de Negócio. Catálogo nasce vazio.</p>
      </header>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Parâmetros</div>
            <h2>Parâmetros cadastrados</h2>
          </div>
          <button type="button" className="btn btn-primary" onClick={() => navigate("novo")}>
            Novo parâmetro
          </button>
        </div>

        {error && <div className="notice notice-crit">{error}</div>}

        {loading ? (
          <div className="empty-state">Carregando Parâmetros...</div>
        ) : (
          <ParametroTable
            parametros={parametros}
            onEditar={(parametro) => navigate(`${parametro.id}/editar`)}
            onExcluir={(parametro) => setParametroParaExcluir(parametro)}
          />
        )}
      </section>

      {parametroParaExcluir && (
        <ConfirmDialog
          title="Excluir parâmetro"
          message={`Excluir o parâmetro "${parametroParaExcluir.chave}"?`}
          confirmLabel="Excluir"
          destructive
          onConfirm={confirmarExclusao}
          onCancel={() => setParametroParaExcluir(null)}
        />
      )}
    </div>
  );
}
