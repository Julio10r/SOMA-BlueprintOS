import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { getFilial } from "../services/filiaisApi";
import { statusFilial, type Filial } from "../types/filialTypes";

/**
 * Visualizacao somente leitura de uma Filial: mantem a separacao entre
 * "Dados do ERP" e "Dados +Compras" tambem na tela de detalhe, reforcando
 * que Codigo CliFor/Nome CliFor/Unidade de Negocio vem do ERP e nunca sao
 * alterados aqui.
 */
export function FilialDetalhesPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [filial, setFilial] = useState<Filial | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getFilial(id).then((found) => {
      if (!found) {
        setError("Filial não encontrada.");
        return;
      }
      setFilial(found);
    }).catch((e) => setError(e instanceof Error ? e.message : "Erro ao carregar a filial."))
      .finally(() => setLoading(false));
  }, [id]);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Detalhes da filial</h1>
        <p>Os dados de origem do ERP são somente leitura. Alterações realizadas no +Compras não modificam o ERP.</p>
      </header>

      {error && <div className="notice notice-crit">{error}</div>}
      {loading && <div className="empty-state">Carregando filial...</div>}

      {filial && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">{filial.unidadeNegocioId}</div>
              <h2>{filial.nomeCliFor}</h2>
            </div>
            <StatusBadge value={statusFilial(filial)} tone="situacao" />
          </div>

          <div className="data-block">
            <div className="section-title">Dados do ERP (somente leitura)</div>
            <div className="data-grid">
              <div className="field-readonly">
                <span>Código CliFor</span>
                <strong>{filial.codigoCliFor}</strong>
              </div>
              <div className="field-readonly">
                <span>Nome CliFor</span>
                <strong>{filial.nomeCliFor}</strong>
              </div>
              <div className="field-readonly">
                <span>Unidade de Negocio</span>
                <strong>{filial.unidadeNegocioId}</strong>
              </div>
            </div>
          </div>

          <div className="data-block">
            <div className="section-title">Dados +Compras</div>
            <div className="data-grid">
              <div className="field-readonly">
                <span>Descrição +Compras</span>
                <strong>{filial.descricaoMaisCompras || "Sem descrição +Compras"}</strong>
              </div>
              <div className="field-readonly">
                <span>Status no +Compras</span>
                <strong>{statusFilial(filial)}</strong>
              </div>
              <div className="field-readonly">
                <span>Atualizado em</span>
                <strong>{new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" }).format(new Date(filial.atualizadoEm))}</strong>
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
