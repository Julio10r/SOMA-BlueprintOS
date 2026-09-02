import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { getUnidadeMedida } from "../services/unidadesMedidaApi";
import { statusUnidadeMedida, type UnidadeMedida } from "../types/unidadeMedidaTypes";

/**
 * Visualizacao somente leitura de uma Unidade de Medida: mantem a separacao entre "Dados do ERP" e
 * "Dados +Compras" tambem na tela de detalhe.
 */
export function UnidadeMedidaDetalhesPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [unidade, setUnidade] = useState<UnidadeMedida | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getUnidadeMedida(id).then((found) => {
      if (!found) {
        setError("Unidade de medida não encontrada.");
        return;
      }
      setUnidade(found);
    }).catch((e) => setError(e instanceof Error ? e.message : "Erro ao carregar a unidade de medida."))
      .finally(() => setLoading(false));
  }, [id]);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Detalhes da unidade de medida</h1>
        <p>Os dados de origem do ERP são somente leitura. Alterações realizadas no +Compras não modificam o ERP.</p>
      </header>

      {error && <div className="notice notice-crit">{error}</div>}
      {loading && <div className="empty-state">Carregando unidade de medida...</div>}

      {unidade && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">{unidade.codigoErp}</div>
              <h2>{unidade.descricaoErp || "Sem descrição ERP"}</h2>
            </div>
            <StatusBadge value={statusUnidadeMedida(unidade)} tone="situacao" />
          </div>

          <div className="data-block">
            <div className="section-title">Dados do ERP (somente leitura)</div>
            <div className="data-grid">
              <div className="field-readonly">
                <span>Código</span>
                <strong>{unidade.codigoErp}</strong>
              </div>
              <div className="field-readonly">
                <span>Descrição ERP</span>
                <strong>{unidade.descricaoErp || "Sem descrição ERP"}</strong>
              </div>
            </div>
          </div>

          <div className="data-block">
            <div className="section-title">Dados +Compras</div>
            <div className="data-grid">
              <div className="field-readonly">
                <span>Descrição +Compras</span>
                <strong>{unidade.descricaoMaisCompras || "Sem descrição +Compras"}</strong>
              </div>
              <div className="field-readonly">
                <span>Status no +Compras</span>
                <strong>{statusUnidadeMedida(unidade)}</strong>
              </div>
              <div className="field-readonly">
                <span>Atualizado em</span>
                <strong>{new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" }).format(new Date(unidade.atualizadoEm))}</strong>
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
