import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { getUnidadeAlocacao } from "../services/unidadesAlocacaoApi";
import type { UnidadeAlocacao } from "../types/unidadeAlocacaoTypes";

export function UnidadeAlocacaoDetalhesPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [unidadeAlocacao, setUnidadeAlocacao] = useState<UnidadeAlocacao | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getUnidadeAlocacao(id).then((found) => {
      if (!found) {
        setError("Unidade de alocacao nao encontrada.");
        return;
      }
      setUnidadeAlocacao(found);
    }).finally(() => setLoading(false));
  }, [id]);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administracao</div>
        <h1>Detalhes da unidade de alocacao</h1>
        <p>Visualizacao somente leitura dos dados cadastrados no +Compras.</p>
      </header>

      {error && <div className="notice notice-crit">{error}</div>}
      {loading && <div className="empty-state">Carregando unidade de alocacao...</div>}

      {unidadeAlocacao && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">Unidade de Alocacao</div>
              <h2>{unidadeAlocacao.nome}</h2>
            </div>
            <StatusBadge value={unidadeAlocacao.status} tone="situacao" />
          </div>
          <p>{unidadeAlocacao.descricao}</p>
          <div className="data-grid">
            <div className="field-readonly">
              <span>Status</span>
              <strong>{unidadeAlocacao.status}</strong>
            </div>
            <div className="field-readonly">
              <span>Atualizado em</span>
              <strong>
                {new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" }).format(new Date(unidadeAlocacao.atualizadoEm))}
              </strong>
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
