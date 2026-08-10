import { FormEvent, useState } from "react";
import type { Filial, FilialUpdateInput } from "../types/filialTypes";

/**
 * Edicao de Filial: separacao visual clara entre "Dados do ERP" (somente
 * leitura) e "Dados +Compras" (editaveis), conforme ADR-0020 item 2/3.
 * CodigoCliFor, NomeCliFor e UnidadeNegocioId nunca sao editaveis nesta
 * tela — nao ha nenhum campo de formulario associado a eles.
 */
export function FilialForm({ filial, error, loading, onSubmit, onCancel }: {
  filial: Filial;
  error: string | null;
  loading: boolean;
  onSubmit: (input: FilialUpdateInput) => void;
  onCancel: () => void;
}) {
  const [descricaoMaisCompras, setDescricaoMaisCompras] = useState(filial.descricaoMaisCompras ?? "");
  const [ativoNoMaisCompras, setAtivoNoMaisCompras] = useState(filial.ativoNoMaisCompras);

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
        <h2>Editar filial</h2>
      </div>

      <div className="notice notice-warn">
        Os dados de origem do ERP sao somente leitura. Alteracoes realizadas no +Compras nao modificam o ERP.
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <div className="data-block">
        <div className="section-title">Dados do ERP (somente leitura)</div>
        <div className="data-grid">
          <div className="field-readonly">
            <span>Codigo CliFor</span>
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
        <div className="section-title">Dados +Compras (editaveis)</div>

        <label>
          Descricao +Compras
          <input
            value={descricaoMaisCompras}
            onChange={(event) => setDescricaoMaisCompras(event.target.value)}
            placeholder="Opcional - nao substitui o Nome CliFor do ERP"
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
          <span>Controla apenas o uso desta filial no +Compras; nao altera o cadastro no ERP.</span>
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
