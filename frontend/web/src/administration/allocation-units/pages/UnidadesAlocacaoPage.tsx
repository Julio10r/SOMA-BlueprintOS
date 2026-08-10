import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { UnidadeAlocacaoTable } from "../components/UnidadeAlocacaoTable";
import { useUnidadesAlocacao } from "../hooks/useUnidadesAlocacao";
import type { StatusUnidadeAlocacao } from "../types/unidadeAlocacaoTypes";

/**
 * Listagem de Unidades de Alocacao (Gestao de Unidades de Alocacao,
 * ADR-0020 item 4/5). Fundacao visual da Sprint O1.3.5: dados mockados em
 * memoria (services/unidadesAlocacaoMockApi.ts), sem integracao com API
 * real. Ao contrario de Filiais/Centros de Custo, existe acao de criacao:
 * Unidade de Alocacao pertence exclusivamente ao +Compras.
 */
export function UnidadesAlocacaoPage() {
  const navigate = useNavigate();
  const { unidadesAlocacao, loading, error, toggleStatus } = useUnidadesAlocacao();
  const [busca, setBusca] = useState("");
  const [statusFiltro, setStatusFiltro] = useState<StatusUnidadeAlocacao | "Todos">("Todos");
  const [toggleErro, setToggleErro] = useState<string | null>(null);

  const unidadesAlocacaoFiltradas = useMemo(() => {
    const termo = busca.trim().toLowerCase();
    return unidadesAlocacao.filter((unidadeAlocacao) => {
      const combinaBusca =
        !termo ||
        unidadeAlocacao.nome.toLowerCase().includes(termo) ||
        unidadeAlocacao.descricao.toLowerCase().includes(termo);
      const combinaStatus = statusFiltro === "Todos" || unidadeAlocacao.status === statusFiltro;
      return combinaBusca && combinaStatus;
    });
  }, [unidadesAlocacao, busca, statusFiltro]);

  async function handleToggleStatus(unidadeAlocacao: Parameters<typeof toggleStatus>[0]) {
    setToggleErro(null);
    try {
      await toggleStatus(unidadeAlocacao);
    } catch (err) {
      setToggleErro(err instanceof Error ? err.message : "Falha ao alterar o status da unidade de alocacao.");
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Gestao de Unidades de Alocacao</h1>
        <p>
          Unidades de Alocacao pertencem ao +Compras e nao sao integradas do ERP. Podem representar agrupamentos
          administrativos usados para orcamento, gestao, relatorios, consolidacao e classificacao operacional.
        </p>
      </header>

      <section className="card">
        <div className="card-heading">
          <div>
            <div className="section-title">Unidades de Alocacao</div>
            <h2>Unidades de Alocacao cadastradas</h2>
          </div>
          <button type="button" className="btn btn-primary" onClick={() => navigate("novo")}>
            Nova unidade de alocacao
          </button>
        </div>

        <div className="input-row">
          <label>
            Pesquisar
            <input
              type="text"
              value={busca}
              onChange={(event) => setBusca(event.target.value)}
              placeholder="Nome ou descricao"
            />
          </label>
          <label>
            Status
            <select
              value={statusFiltro}
              onChange={(event) => setStatusFiltro(event.target.value as StatusUnidadeAlocacao | "Todos")}
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
          <div className="empty-state">Carregando unidades de alocacao...</div>
        ) : (
          <UnidadeAlocacaoTable
            unidadesAlocacao={unidadesAlocacaoFiltradas}
            onVisualizar={(unidadeAlocacao) => navigate(unidadeAlocacao.id)}
            onEditar={(unidadeAlocacao) => navigate(`${unidadeAlocacao.id}/editar`)}
            onToggleStatus={handleToggleStatus}
          />
        )}
      </section>
    </div>
  );
}
