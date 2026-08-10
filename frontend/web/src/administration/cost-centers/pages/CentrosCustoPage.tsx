import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { CentroCustoTable } from "../components/CentroCustoTable";
import { useCentrosCusto } from "../hooks/useCentrosCusto";
import { statusCentroCusto, type StatusCentroCusto } from "../types/centroCustoTypes";

/**
 * Listagem de Centros de Custo (Gestao de Centros de Custo, ADR-0020
 * item 3). Fundacao visual da Sprint O1.3.4: dados mockados em memoria
 * (services/centrosCustoMockApi.ts), sem integracao com API real e sem
 * escrita no ERP.
 *
 * Nao existe acao de criacao: Centro de Custo e um dado mestre integrado
 * do ERP, nunca criado pelo +Compras (por isso nao ha botao "Novo Centro
 * de Custo" nesta pagina, ao contrario de Perfis/Usuarios).
 */
export function CentrosCustoPage() {
  const navigate = useNavigate();
  const { centrosCusto, loading, error, toggleAtivo } = useCentrosCusto();
  const [busca, setBusca] = useState("");
  const [statusFiltro, setStatusFiltro] = useState<StatusCentroCusto | "Todos">("Todos");
  const [toggleErro, setToggleErro] = useState<string | null>(null);

  const centrosCustoFiltrados = useMemo(() => {
    const termo = busca.trim().toLowerCase();
    return centrosCusto.filter((centroCusto) => {
      const combinaBusca =
        !termo ||
        centroCusto.codigoErp.toLowerCase().includes(termo) ||
        centroCusto.descricaoErp.toLowerCase().includes(termo) ||
        (centroCusto.descricaoMaisCompras ?? "").toLowerCase().includes(termo);
      const combinaStatus = statusFiltro === "Todos" || statusCentroCusto(centroCusto) === statusFiltro;
      return combinaBusca && combinaStatus;
    });
  }, [centrosCusto, busca, statusFiltro]);

  async function handleToggleAtivo(centroCusto: Parameters<typeof toggleAtivo>[0]) {
    setToggleErro(null);
    try {
      await toggleAtivo(centroCusto);
    } catch (err) {
      setToggleErro(err instanceof Error ? err.message : "Falha ao alterar o status do centro de custo no +Compras.");
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Gestao de Centros de Custo</h1>
        <p>
          Centros de Custo sao dados mestres do ERP. O +Compras nao cria nem altera dados mestres do ERP: apenas
          administra metadados locais (descricao +Compras e status de uso no +Compras).
        </p>
      </header>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Centros de Custo</div>
            <h2>Centros de Custo integrados do ERP</h2>
          </div>
        </div>

        <div className="input-row">
          <label>
            Pesquisar
            <input
              type="text"
              value={busca}
              onChange={(event) => setBusca(event.target.value)}
              placeholder="Codigo, Descricao ERP ou Descricao +Compras"
            />
          </label>
          <label>
            Status no +Compras
            <select
              value={statusFiltro}
              onChange={(event) => setStatusFiltro(event.target.value as StatusCentroCusto | "Todos")}
            >
              <option value="Todos">Todos</option>
              <option value="Ativo">Ativo</option>
              <option value="Inativo">Inativo</option>
            </select>
          </label>
        </div>

        {error && <div className="notice notice-crit">{error}</div>}
        {toggleErro && <div className="notice notice-crit">{toggleErro}</div>}

        {loading ? (
          <div className="empty-state">Carregando centros de custo...</div>
        ) : (
          <CentroCustoTable
            centrosCusto={centrosCustoFiltrados}
            onVisualizar={(centroCusto) => navigate(centroCusto.id)}
            onEditar={(centroCusto) => navigate(`${centroCusto.id}/editar`)}
            onToggleAtivo={handleToggleAtivo}
          />
        )}
      </section>
    </div>
  );
}
