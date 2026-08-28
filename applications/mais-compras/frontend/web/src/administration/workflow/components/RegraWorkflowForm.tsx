import { FormEvent, useState } from "react";
import type { RegraWorkflow, RegraWorkflowInput } from "../types/regraWorkflowTypes";

export function RegraWorkflowForm({ regra, error, loading, onSubmit, onCancel }: {
  regra?: RegraWorkflow;
  error: string | null;
  loading: boolean;
  onSubmit: (input: RegraWorkflowInput) => void;
  onCancel: () => void;
}) {
  const [nome, setNome] = useState(regra?.nome ?? "");
  const [tipoProcesso, setTipoProcesso] = useState(regra?.tipoProcesso ?? "");
  const [ordem, setOrdem] = useState(regra?.ordem ?? 1);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({ nome, tipoProcesso, ordem: Number(ordem) });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>{regra ? "Editar Regra de Workflow" : "Nova Regra de Workflow"}</h2>
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <label>
        Nome
        <input value={nome} onChange={(event) => setNome(event.target.value)} disabled={loading} required />
      </label>

      <label>
        Tipo de processo
        <input value={tipoProcesso} onChange={(event) => setTipoProcesso(event.target.value)} disabled={loading} required />
      </label>

      <label>
        Ordem
        <input
          type="number"
          min={1}
          value={ordem}
          onChange={(event) => setOrdem(Number(event.target.value))}
          disabled={loading}
          required
        />
      </label>

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
