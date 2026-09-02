import { FormEvent, useState } from "react";
import type { UnidadeMedida, UnidadeMedidaUpdateInput } from "../types/unidadeMedidaTypes";

/**
 * Edicao de Unidade de Medida: separacao visual clara entre "Dados do ERP" (somente leitura) e "Dados
 * +Compras" (editaveis). CodigoErp e DescricaoErp nunca sao editaveis nesta tela.
 */
export function UnidadeMedidaForm({ unidade, error, loading, onSubmit, onCancel }: {
  unidade: UnidadeMedida;
  error: string | null;
  loading: boolean;
  onSubmit: (input: UnidadeMedidaUpdateInput) => void;
  onCancel: () => void;
}) {
  const [descricaoMaisCompras, setDescricaoMaisCompras] = useState(unidade.descricaoMaisCompras ?? "");
  const [ativoNoMaisCompras, setAtivoNoMaisCompras] = useState(unidade.ativoNoMaisCompras);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({
      descricaoMaisCompras: descricaoMaisCompras.trim() ? descricaoMaisCompras.trim() : undefined,
      ativoNoMaisCompras
    });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>Editar unidade de medida</h2>
      </div>

      <div className="notice notice-warn">
        Os dados de origem do ERP são somente leitura. Alterações realizadas no +Compras não modificam o ERP.
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

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
        <div className="section-title">Dados +Compras (editáveis)</div>

        <label>
          Descrição +Compras
          <input
            value={descricaoMaisCompras}
            onChange={(event) => setDescricaoMaisCompras(event.target.value)}
            placeholder="Opcional - não substitui a Descrição ERP"
            disabled={loading}
          />
        </label>

        <label className="field-readonly">
          <input
            type="checkbox"
            checked={ativoNoMaisCompras}
            onChange={(event) => setAtivoNoMaisCompras(event.target.checked)}
            disabled={loading}
          />
          <strong>Ativo no +Compras</strong>
          <span>Controla apenas o uso desta unidade no +Compras; não altera o cadastro no ERP.</span>
        </label>
      </div>

      <div className="actions">
        <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? "Salvando..." : "Salvar"}
        </button>
      </div>
    </form>
  );
}
