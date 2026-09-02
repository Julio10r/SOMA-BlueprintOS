import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { getContaContabil } from "../services/contasContabeisApi";
import { statusContaContabilEfetivo, statusContaContabilErp, type ContaContabil } from "../types/contaContabilTypes";

/**
 * Visualizacao somente leitura de uma Conta Contabil: mantem a separacao entre "Dados do ERP" e "Dados
 * +Compras" tambem na tela de detalhe.
 */
export function ContaContabilDetalhesPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [conta, setConta] = useState<ContaContabil | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getContaContabil(id).then((found) => {
      if (!found) {
        setError("Conta contábil não encontrada.");
        return;
      }
      setConta(found);
    }).catch((e) => setError(e instanceof Error ? e.message : "Erro ao carregar a conta contábil."))
      .finally(() => setLoading(false));
  }, [id]);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Administração</div>
        <h1>Detalhes da conta contábil</h1>
        <p>Os dados de origem do ERP são somente leitura. Alterações realizadas no +Compras não modificam o ERP.</p>
      </header>

      {error && <div className="notice notice-crit">{error}</div>}
      {loading && <div className="empty-state">Carregando conta contábil...</div>}

      {conta && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">{conta.codigoErp}</div>
              <h2>{conta.descricaoErp}</h2>
            </div>
            <StatusBadge value={statusContaContabilEfetivo(conta)} tone="situacao" />
          </div>

          <div className="data-block">
            <div className="section-title">Dados do ERP (somente leitura)</div>
            <div className="data-grid">
              <div className="field-readonly">
                <span>Código</span>
                <strong>{conta.codigoErp}</strong>
              </div>
              <div className="field-readonly">
                <span>Descrição ERP</span>
                <strong>{conta.descricaoErp}</strong>
              </div>
              <div className="field-readonly">
                <span>Status no Linx</span>
                <strong>{statusContaContabilErp(conta)}</strong>
              </div>
            </div>
          </div>

          <div className="data-block">
            <div className="section-title">Dados +Compras</div>
            <div className="data-grid">
              <div className="field-readonly">
                <span>Descrição +Compras</span>
                <strong>{conta.descricaoMaisCompras || "Sem descrição +Compras"}</strong>
              </div>
              <div className="field-readonly">
                <span>Status no +Compras (local)</span>
                <strong>{conta.ativoNoMaisCompras ? "Ativo" : "Inativo"}</strong>
              </div>
              <div className="field-readonly">
                <span>Status efetivo</span>
                <strong>{statusContaContabilEfetivo(conta)}</strong>
              </div>
              <div className="field-readonly">
                <span>Atualizado em</span>
                <strong>{new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" }).format(new Date(conta.atualizadoEm))}</strong>
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
