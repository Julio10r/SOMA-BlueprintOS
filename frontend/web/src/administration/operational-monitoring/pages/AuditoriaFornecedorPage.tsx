import { useState } from "react";
import { obterHistoricoFornecedor } from "../services/monitoramentoApi";
import type { FornecedorSincronizacaoHistorico } from "../types/monitoramentoTypes";

function formatarData(data: string): string {
  return new Date(data).toLocaleString("pt-BR");
}

/**
 * O1.13 — Auditoria e Historico de Sincronizacoes (#32). Reaproveita o endpoint real
 * `GET /api/fornecedores/{fornecedorId}/sincronizacoes` (B2.1.3, `FornecedorSincronizacaoRepository`) —
 * granularidade por fornecedor, distinta das execucoes em lote do Monitor de Integracoes.
 *
 * Decisao de UX: implementada como pagina standalone (busca manual por `fornecedorId`) em vez de aba na
 * tela de detalhe do fornecedor, porque `procurement/suppliers` ainda nao possui uma tela de detalhe por
 * fornecedor (apenas listagem/cadastro em `FornecedoresPage.tsx`) — criar essa tela estaria fora do
 * escopo desta sprint. `DadosAntes`/`DadosDepois` (JSON completo do snapshot) nao sao exibidos: apenas
 * `CamposAlterados` (resumo) e os demais campos de auditoria.
 */
export function AuditoriaFornecedorPage() {
  const [fornecedorId, setFornecedorId] = useState("");
  const [historico, setHistorico] = useState<FornecedorSincronizacaoHistorico[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [buscou, setBuscou] = useState(false);

  async function handleBuscar() {
    if (!fornecedorId.trim()) return;
    setLoading(true);
    setError(null);
    setBuscou(true);
    try {
      setHistorico(await obterHistoricoFornecedor(fornecedorId.trim()));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar o historico de sincronizacao do fornecedor.");
      setHistorico([]);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Auditoria de Fornecedor</h1>
        <p>Historico detalhado de sincronizacao por fornecedor (direcao, decisao, campos alterados, tentativas).</p>
      </header>

      <section className="card">
        <div className="actions" style={{ justifyContent: "flex-start" }}>
          <label>
            Id do Fornecedor
            <input
              type="text"
              value={fornecedorId}
              onChange={(e) => setFornecedorId(e.target.value)}
              placeholder="GUID do fornecedor"
            />
          </label>
          <button type="button" className="btn btn-primary" disabled={loading} onClick={handleBuscar}>
            {loading ? "Buscando..." : "Buscar historico"}
          </button>
        </div>

        {error && <div className="notice notice-crit">{error}</div>}

        {!error && buscou && !loading && historico.length === 0 && (
          <div className="empty-state">Nenhum registro de sincronizacao encontrado para este fornecedor.</div>
        )}

        {!error && historico.length > 0 && (
          <div className="table-scroll">
          <table className="divergence-table">
            <thead>
              <tr>
                <th>Direcao</th>
                <th>Status</th>
                <th>Decisao</th>
                <th>Campos Alterados</th>
                <th>Tentativa</th>
                <th>Executada Em</th>
                <th>Duracao (ms)</th>
              </tr>
            </thead>
            <tbody>
              {historico.map((item) => (
                <tr key={item.id}>
                  <td>{item.direcao}</td>
                  <td>{item.status}</td>
                  <td>{item.decisao}</td>
                  <td>{item.camposAlterados ?? "—"}</td>
                  <td>{item.tentativa}</td>
                  <td>{formatarData(item.executadaEm)}</td>
                  <td>{item.duracaoMs}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        )}
      </section>
    </div>
  );
}
