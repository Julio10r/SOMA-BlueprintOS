import { FormEvent, useState } from "react";
import type { CentroCusto, CentroCustoUpdateInput } from "../types/centroCustoTypes";

/**
 * Edicao de Centro de Custo: separacao visual clara entre "Dados do ERP"
 * (somente leitura) e "Dados +Compras" (editaveis), conforme ADR-0020
 * item 2/3. CodigoErp, DescricaoErp e UnidadeNegocioId nunca sao
 * editaveis nesta tela — nao ha nenhum campo de formulario associado a
 * eles. O vinculo com Unidade de Alocacao e exibido apenas como
 * informacao (mockada), sem edicao nesta etapa (modulo ainda nao
 * implementado).
 */
export function CentroCustoForm({ centroCusto, error, loading, onSubmit, onCancel }: {
  centroCusto: CentroCusto;
  error: string | null;
  loading: boolean;
  onSubmit: (input: CentroCustoUpdateInput) => void;
  onCancel: () => void;
}) {
  const [descricaoMaisCompras, setDescricaoMaisCompras] = useState(centroCusto.descricaoMaisCompras ?? "");
  const [ativoNoMaisCompras, setAtivoNoMaisCompras] = useState(centroCusto.ativoNoMaisCompras);

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
        <h2>Editar centro de custo</h2>
      </div>

      <div className="notice notice-warn">
        Os dados de origem do ERP sao somente leitura. Alteracoes realizadas no +Compras nao modificam o ERP.
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <div className="data-block">
        <div className="section-title">Dados do ERP (somente leitura)</div>
        <div className="data-grid">
          <div className="field-readonly">
            <span>Codigo Centro de Custo</span>
            <strong>{centroCusto.codigoErp}</strong>
          </div>
          <div className="field-readonly">
            <span>Descricao ERP</span>
            <strong>{centroCusto.descricaoErp}</strong>
          </div>
          <div className="field-readonly">
            <span>Unidade de Negocio</span>
            <strong>{centroCusto.unidadeNegocioId}</strong>
          </div>
        </div>
      </div>

      <div className="data-block">
        <div className="section-title">Unidades de Alocacao (preparacao do relacionamento)</div>
        <div className="data-grid">
          <div className="field-readonly">
            <span>Unidade de Alocacao padrao</span>
            <strong>{centroCusto.unidadeAlocacaoPadraoNome || "Sem unidade padrao"}</strong>
          </div>
          <div className="field-readonly">
            <span>Unidades de Alocacao vinculadas</span>
            <strong>{centroCusto.quantidadeUnidadesAlocacaoVinculadas}</strong>
          </div>
        </div>
      </div>

      <div className="data-block">
        <div className="section-title">Dados +Compras (editaveis)</div>

        <label>
          Descricao +Compras
          <input
            value={descricaoMaisCompras}
            onChange={(event) => setDescricaoMaisCompras(event.target.value)}
            placeholder="Opcional - nao substitui a Descricao ERP"
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
          <span>Controla apenas o uso deste centro de custo no +Compras; nao altera o cadastro no ERP.</span>
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
