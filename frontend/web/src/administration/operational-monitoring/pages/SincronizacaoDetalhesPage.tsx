import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { StatusExecucaoBadge } from "../components/StatusExecucaoBadge";
import { obterSincronizacaoFornecedor } from "../services/monitoramentoApi";
import type { SincronizacaoFornecedorDetalhe } from "../types/monitoramentoTypes";

function formatarData(data: string | null): string {
  if (!data) return "—";
  return new Date(data).toLocaleString("pt-BR");
}

/**
 * O1.13 — Monitor de Filas e Reprocessamentos (#31): detalhe de uma execucao de sincronizacao, com a
 * fila de itens que falharam (`Erros`). A `Mensagem` ja chega sanitizada do backend
 * (`SincronizacaoFornecedor.RegistrarErro`); `StackTrace` nunca e exibido na UI.
 */
export function SincronizacaoDetalhesPage() {
  const { id } = useParams<{ id: string }>();
  const [detalhe, setDetalhe] = useState<SincronizacaoFornecedorDetalhe | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    setError(null);
    try {
      setDetalhe(await obterSincronizacaoFornecedor(id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar o detalhe da execucao.");
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Detalhe da Execucao de Sincronizacao</h1>
        <Link to="/administracao/monitoramento">Voltar para o Monitor de Integracoes</Link>
      </header>

      {error && <div className="notice notice-crit">{error}</div>}

      {!error && loading && <div className="empty-state">Carregando detalhe da execucao...</div>}

      {!error && !loading && detalhe && (
        <>
          <section className="card">
            <div className="data-grid">
              <div className="field-readonly">
                <span>Sistema de Origem</span>
                <strong>{detalhe.sistemaOrigem}</strong>
              </div>
              <div className="field-readonly">
                <span>Unidade de Negocio</span>
                <strong>{detalhe.businessUnit}</strong>
              </div>
              <div className="field-readonly">
                <span>Status</span>
                <strong><StatusExecucaoBadge status={detalhe.status} /></strong>
              </div>
              <div className="field-readonly">
                <span>Duracao (ms)</span>
                <strong>{detalhe.tempoExecucaoMs}</strong>
              </div>
              <div className="field-readonly">
                <span>Inicio</span>
                <strong>{formatarData(detalhe.dataInicio)}</strong>
              </div>
              <div className="field-readonly">
                <span>Fim</span>
                <strong>{formatarData(detalhe.dataFim)}</strong>
              </div>
              <div className="field-readonly">
                <span>Consultados / Incluidos / Atualizados / Sem Alteracao</span>
                <strong>{detalhe.totalConsultado} / {detalhe.totalIncluido} / {detalhe.totalAtualizado} / {detalhe.totalSemAlteracao}</strong>
              </div>
              <div className="field-readonly">
                <span>Erros</span>
                <strong>{detalhe.totalErro}</strong>
              </div>
            </div>
          </section>

          <section className="card">
            <div className="card-heading">
              <h2>Fila de itens com falha</h2>
            </div>

            {detalhe.erros.length === 0 ? (
              <div className="empty-state">Nenhum erro registrado nesta execucao.</div>
            ) : (
              <div className="table-scroll">
              <table className="divergence-table">
                <thead>
                  <tr>
                    <th>Fornecedor</th>
                    <th>Mensagem</th>
                    <th>Data/Hora</th>
                  </tr>
                </thead>
                <tbody>
                  {detalhe.erros.map((erro) => (
                    <tr key={erro.id}>
                      <td>{erro.fornecedorIdentificacao ?? "—"}</td>
                      <td>{erro.mensagem}</td>
                      <td>{formatarData(erro.dataHora)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}
