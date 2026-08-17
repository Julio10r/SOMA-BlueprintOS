import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { getCentroCusto } from "../services/centrosCustoApi";
import { statusCentroCusto, type CentroCusto } from "../types/centroCustoTypes";

/**
 * Visualizacao somente leitura de um Centro de Custo: mantem a separacao
 * entre "Dados do ERP" e "Dados +Compras" tambem na tela de detalhe,
 * reforcando que Codigo/Descricao ERP/Unidade de Negocio vem do ERP e
 * nunca sao alterados aqui. Exibe tambem o resumo real do vinculo N:N
 * com Unidade de Alocacao (O1.9, ADR-0020 item 6).
 */
export function CentroCustoDetalhesPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [centroCusto, setCentroCusto] = useState<CentroCusto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getCentroCusto(id).then((found) => {
      if (!found) {
        setError("Centro de custo não encontrado.");
        return;
      }
      setCentroCusto(found);
    }).catch((e) => setError(e instanceof Error ? e.message : "Erro ao carregar o centro de custo."))
      .finally(() => setLoading(false));
  }, [id]);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Detalhes do centro de custo</h1>
        <p>Os dados de origem do ERP são somente leitura. Alterações realizadas no +Compras não modificam o ERP.</p>
      </header>

      {error && <div className="notice notice-crit">{error}</div>}
      {loading && <div className="empty-state">Carregando centro de custo...</div>}

      {centroCusto && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">{centroCusto.unidadeNegocioId}</div>
              <h2>{centroCusto.descricaoErp}</h2>
            </div>
            <StatusBadge value={statusCentroCusto(centroCusto)} tone="situacao" />
          </div>

          <div className="data-block">
            <div className="section-title">Dados do ERP (somente leitura)</div>
            <div className="data-grid">
              <div className="field-readonly">
                <span>Código Centro de Custo</span>
                <strong>{centroCusto.codigoErp}</strong>
              </div>
              <div className="field-readonly">
                <span>Descrição ERP</span>
                <strong>{centroCusto.descricaoErp}</strong>
              </div>
              <div className="field-readonly">
                <span>Unidade de Negócio</span>
                <strong>{centroCusto.unidadeNegocioId}</strong>
              </div>
            </div>
          </div>

          <div className="data-block">
            <div className="section-title">Dados +Compras</div>
            <div className="data-grid">
              <div className="field-readonly">
                <span>Descrição +Compras</span>
                <strong>{centroCusto.descricaoMaisCompras || "Sem descrição +Compras"}</strong>
              </div>
              <div className="field-readonly">
                <span>Status no +Compras</span>
                <strong>{statusCentroCusto(centroCusto)}</strong>
              </div>
              <div className="field-readonly">
                <span>Atualizado em</span>
                <strong>
                  {new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" }).format(new Date(centroCusto.atualizadoEm))}
                </strong>
              </div>
            </div>
          </div>

          <div className="data-block">
            <div className="section-title">Unidades de Alocação vinculadas</div>
            <div className="data-grid">
              <div className="field-readonly">
                <span>Unidade de Alocação padrão</span>
                <strong>{centroCusto.unidadeAlocacaoPadraoNome || "Sem unidade padrão"}</strong>
              </div>
              <div className="field-readonly">
                <span>Unidades de Alocação vinculadas</span>
                <strong>{centroCusto.quantidadeUnidadesAlocacaoVinculadas}</strong>
              </div>
            </div>
          </div>

          <div className="actions">
            <button type="button" className="btn btn-secondary" onClick={() => navigate("..", { relative: "path" })}>
              Voltar
            </button>
            <button type="button" className="btn btn-primary" onClick={() => navigate("editar", { relative: "path" })}>
              Editar
            </button>
          </div>
        </section>
      )}
    </div>
  );
}
