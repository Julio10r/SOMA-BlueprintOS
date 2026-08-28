import { useNavigate } from "react-router-dom";
import { ParametroTable } from "../components/ParametroTable";
import { useParametros } from "../hooks/useParametros";
import type { Parametro } from "../types/parametroTypes";

export function ParametrosPage() {
  const navigate = useNavigate();
  const { parametros, loading, error, remover } = useParametros();

  async function handleExcluir(parametro: Parametro) {
    if (!window.confirm(`Excluir o parâmetro "${parametro.chave}"?`)) return;
    await remover(parametro.id);
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
            onExcluir={handleExcluir}
          />
        )}
      </section>
    </div>
  );
}
