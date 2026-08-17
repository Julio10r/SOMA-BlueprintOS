import { useNavigate } from "react-router-dom";
import { ParametroTable } from "../components/ParametroTable";
import { useParametros } from "../hooks/useParametros";
import type { Parametro } from "../types/parametroTypes";

export function ParametrosPage() {
  const navigate = useNavigate();
  const { parametros, loading, error, remover } = useParametros();

  async function handleExcluir(parametro: Parametro) {
    if (!window.confirm(`Excluir o parametro "${parametro.chave}"?`)) return;
    await remover(parametro.id);
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Parametros</h1>
        <p>Parametros tecnicos globais ou por Unidade de Negocio. Catalogo nasce vazio.</p>
      </header>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Parametros</div>
            <h2>Parametros cadastrados</h2>
          </div>
          <button type="button" className="btn btn-primary" onClick={() => navigate("novo")}>
            Novo Parametro
          </button>
        </div>

        {error && <div className="notice notice-crit">{error}</div>}

        {loading ? (
          <div className="empty-state">Carregando Parametros...</div>
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
