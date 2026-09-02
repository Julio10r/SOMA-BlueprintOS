import { FormEvent, useState } from "react";
import type { ContaContabil, ContaContabilUpdateInput } from "../types/contaContabilTypes";

/**
 * Edicao de Conta Contabil: separacao visual clara entre "Dados do ERP" (somente leitura) e "Dados
 * +Compras" (editaveis). CodigoErp, DescricaoErp e InativaNoErp nunca sao editaveis nesta tela.
 */
export function ContaContabilForm({ conta, error, loading, onSubmit, onCancel }: {
  conta: ContaContabil;
  error: string | null;
  loading: boolean;
  onSubmit: (input: ContaContabilUpdateInput) => void;
  onCancel: () => void;
}) {
  const [descricaoMaisCompras, setDescricaoMaisCompras] = useState(conta.descricaoMaisCompras ?? "");
  const [ativoNoMaisCompras, setAtivoNoMaisCompras] = useState(conta.ativoNoMaisCompras);

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
        <h2>Editar conta contábil</h2>
      </div>

      <div className="notice notice-warn">
        Os dados de origem do ERP são somente leitura. Alterações realizadas no +Compras não modificam o ERP.
      </div>

      {conta.inativaNoErp && (
        <div className="notice notice-warn">
          Esta conta está inativa no Linx. Mesmo marcando como ativa no +Compras, ela não ficará efetivamente
          ativa (ADR-0024 — em conflito de autoridade, o Linx prevalece).
        </div>
      )}

      {error && <div className="notice notice-crit">{error}</div>}

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
            <strong>{conta.inativaNoErp ? "Inativo" : "Ativo"}</strong>
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
          <span>Controla apenas o uso desta conta no +Compras; não altera o cadastro no ERP.</span>
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
