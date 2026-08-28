import { useState } from "react";
import { SincronizacoesFornecedoresTable } from "../components/SincronizacoesFornecedoresTable";
import { useMonitorSincronizacoes } from "../hooks/useMonitorSincronizacoes";
import { dispararSincronizacaoErp } from "../services/monitoramentoApi";

const STATUS_OPCOES = ["Sucesso", "Parcial", "Erro"];

/**
 * O1.13 — Monitor de Integracoes (#30) e ponto de entrada do Monitor de Filas/Reprocessamentos (#31).
 * Le exclusivamente as execucoes em lote de sincronizacao de fornecedores ja persistidas por B2.1.3
 * (`SincronizacaoFornecedor`) — nenhum motor novo, nenhum mock. "Reprocessar" apenas dispara novamente
 * o endpoint real `GET /api/fornecedores/sincronizar-erp` para a Unidade de Negocio informada.
 */
export function MonitorIntegracoesPage() {
  const {
    itens, totalRegistros, pagina, tamanhoPagina, setPagina,
    status, setStatus, businessUnit, setBusinessUnit, loading, error, reload
  } = useMonitorSincronizacoes();

  const [businessUnitReprocessar, setBusinessUnitReprocessar] = useState("");
  const [reprocessando, setReprocessando] = useState(false);
  const [reprocessarErro, setReprocessarErro] = useState<string | null>(null);
  const [reprocessarSucesso, setReprocessarSucesso] = useState<string | null>(null);

  const acessoNegado = error?.toLowerCase().includes("permiss") ?? false;

  async function handleReprocessar() {
    if (!businessUnitReprocessar.trim()) return;
    setReprocessando(true);
    setReprocessarErro(null);
    setReprocessarSucesso(null);
    try {
      const resultado = await dispararSincronizacaoErp(businessUnitReprocessar.trim());
      setReprocessarSucesso(
        `Sincronização disparada. Status: ${resultado.status}. Consultados: ${resultado.consultados}. Erros: ${resultado.erros}.`
      );
      await reload();
    } catch (err) {
      setReprocessarErro(err instanceof Error ? err.message : "Falha ao disparar a sincronização.");
    } finally {
      setReprocessando(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Monitor de Integrações</h1>
        <p>
          Execuções em lote de sincronização de fornecedores com o ERP. Reaproveita integralmente a
          infraestrutura real de sincronização (B2.1.3) — apenas consulta, nenhum motor novo.
        </p>
      </header>

      <section className="card">
        <div className="card-heading">
          <h2>Reprocessar sincronização</h2>
        </div>
        <div className="actions" style={{ justifyContent: "flex-start" }}>
          <label>
            Unidade de Negócio (BusinessUnit)
            <input
              type="text"
              value={businessUnitReprocessar}
              onChange={(e) => setBusinessUnitReprocessar(e.target.value)}
              placeholder="Ex.: DEFAULT"
            />
          </label>
          <button type="button" className="btn btn-primary" disabled={reprocessando} onClick={handleReprocessar}>
            {reprocessando ? "Reprocessando..." : "Reprocessar sincronização"}
          </button>
        </div>
        {reprocessarErro && <div className="notice notice-crit">{reprocessarErro}</div>}
        {reprocessarSucesso && <div className="notice notice-warn">{reprocessarSucesso}</div>}
      </section>

      <section className="card">
        <div className="card-heading">
          <h2>Execuções de sincronização</h2>
        </div>

        <div className="actions" style={{ justifyContent: "flex-start" }}>
          <label>
            Status
            <select value={status ?? ""} onChange={(e) => setStatus(e.target.value || null)}>
              <option value="">Todos</option>
              {STATUS_OPCOES.map((opcao) => (
                <option key={opcao} value={opcao}>{opcao}</option>
              ))}
            </select>
          </label>
          <label>
            Unidade de Negócio (Filtro)
            <input
              type="text"
              value={businessUnit ?? ""}
              onChange={(e) => setBusinessUnit(e.target.value || null)}
              placeholder="Filtrar por BusinessUnit"
            />
          </label>
        </div>

        {error && <div className="notice notice-crit">{error}</div>}

        {!error && loading && <div className="empty-state">Carregando execuções de sincronização...</div>}

        {!error && !loading && <SincronizacoesFornecedoresTable itens={itens} />}

        {!acessoNegado && !loading && !error && totalRegistros > tamanhoPagina && (
          <div className="actions">
            <button type="button" className="btn btn-secondary" disabled={pagina <= 1} onClick={() => setPagina(pagina - 1)}>
              Anterior
            </button>
            <span>Página {pagina} de {Math.ceil(totalRegistros / tamanhoPagina)}</span>
            <button
              type="button"
              className="btn btn-secondary"
              disabled={pagina >= Math.ceil(totalRegistros / tamanhoPagina)}
              onClick={() => setPagina(pagina + 1)}
            >
              Próxima
            </button>
          </div>
        )}
      </section>
    </div>
  );
}
