import { useNavigate } from "react-router-dom";
import { UnidadeNegocioTable } from "../components/UnidadeNegocioTable";
import { useUnidadesNegocio } from "../hooks/useUnidadesNegocio";

export function UnidadesNegocioPage() {
  const navigate = useNavigate();
  const { unidadesNegocio, loading, error, toggleStatus } = useUnidadesNegocio();

  async function handleToggleStatus(unidadeNegocio: Parameters<typeof toggleStatus>[0]) {
    try {
      await toggleStatus(unidadeNegocio);
    } catch {
      // erro de toggle exibido via `error` do proprio hook na proxima leitura, mantendo simetria com allocation-units.
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Gestão de Unidades de Negócio</h1>
        <p>Unidades de Negócio corporativas do +Compras. Não há exclusão física — apenas Ativar/Inativar.</p>
      </header>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Unidades de Negócio</div>
            <h2>Unidades de Negócio cadastradas</h2>
          </div>
          <button type="button" className="btn btn-primary" onClick={() => navigate("novo")}>
            Nova unidade de negócio
          </button>
        </div>

        {error && <div className="notice notice-crit">{error}</div>}

        {loading ? (
          <div className="empty-state">Carregando Unidades de Negócio...</div>
        ) : (
          <UnidadeNegocioTable
            unidadesNegocio={unidadesNegocio}
            onEditar={(unidadeNegocio) => navigate(`${unidadeNegocio.id}/editar`)}
            onToggleStatus={handleToggleStatus}
          />
        )}
      </section>
    </div>
  );
}
