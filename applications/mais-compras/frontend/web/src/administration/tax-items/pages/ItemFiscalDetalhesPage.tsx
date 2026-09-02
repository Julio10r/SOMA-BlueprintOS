import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { getItemFiscal } from "../services/itensFiscaisApi";
import { statusItemFiscal, type ItemFiscal } from "../types/itemFiscalTypes";

export function ItemFiscalDetalhesPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [item, setItem] = useState<ItemFiscal | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getItemFiscal(id).then((found) => {
      if (!found) {
        setError("Item fiscal não encontrado.");
        return;
      }
      setItem(found);
    }).catch((e) => setError(e instanceof Error ? e.message : "Erro ao carregar o item fiscal."))
      .finally(() => setLoading(false));
  }, [id]);

  return (
    <div className="page-stack">
      <header className="page-header">
        <div className="section-title">Cadastros</div>
        <h1>Detalhes do item fiscal</h1>
        <p>Visualização somente leitura dos dados cadastrados no +Compras.</p>
      </header>

      {error && <div className="notice notice-crit">{error}</div>}
      {loading && <div className="empty-state">Carregando item fiscal...</div>}

      {item && (
        <section className="card">
          <div className="card-heading">
            <div>
              <div className="section-title">{item.codigo}</div>
              <h2>{item.descricao}</h2>
            </div>
            <StatusBadge value={statusItemFiscal(item)} tone="situacao" />
          </div>

          <div className="data-grid">
            <div className="field-readonly">
              <span>Código</span>
              <strong>{item.codigo}</strong>
            </div>
            <div className="field-readonly">
              <span>Descrição</span>
              <strong>{item.descricao}</strong>
            </div>
            <div className="field-readonly">
              <span>Unidade</span>
              <strong>{item.unidadeMedidaDescricao ?? item.unidadeMedidaCodigoErp} ({item.unidadeMedidaCodigoErp})</strong>
            </div>
            <div className="field-readonly">
              <span>Conta Contábil</span>
              <strong>{item.contaContabilDescricao ?? item.contaContabilCodigoErp} ({item.contaContabilCodigoErp})</strong>
            </div>
            <div className="field-readonly">
              <span>Status</span>
              <strong>{statusItemFiscal(item)}</strong>
            </div>
            <div className="field-readonly">
              <span>Atualizado em</span>
              <strong>{new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" }).format(new Date(item.atualizadoEm))}</strong>
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
