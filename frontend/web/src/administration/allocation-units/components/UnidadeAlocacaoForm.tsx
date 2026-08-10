import { FormEvent, useState } from "react";
import type { StatusUnidadeAlocacao, UnidadeAlocacao, UnidadeAlocacaoInput } from "../types/unidadeAlocacaoTypes";

/**
 * Cadastro/edicao de Unidade de Alocacao. Ao contrario de Filial e Centro
 * de Custo, todos os campos aqui sao editaveis pelo +Compras — Unidade de
 * Alocacao nunca e integrada do ERP (ADR-0020, item 4).
 */
export function UnidadeAlocacaoForm({ unidadeAlocacao, error, loading, onSubmit, onCancel }: {
  unidadeAlocacao?: UnidadeAlocacao;
  error: string | null;
  loading: boolean;
  onSubmit: (input: UnidadeAlocacaoInput) => void;
  onCancel: () => void;
}) {
  const [nome, setNome] = useState(unidadeAlocacao?.nome ?? "");
  const [descricao, setDescricao] = useState(unidadeAlocacao?.descricao ?? "");
  const [unidadeNegocio, setUnidadeNegocio] = useState(unidadeAlocacao?.unidadeNegocio ?? "SOMA");
  const [status, setStatus] = useState<StatusUnidadeAlocacao>(unidadeAlocacao?.status ?? "Ativo");

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({ nome, descricao, unidadeNegocio, status });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>{unidadeAlocacao ? "Editar unidade de alocacao" : "Nova unidade de alocacao"}</h2>
      </div>

      <div className="notice notice-warn">
        Unidades de Alocacao pertencem exclusivamente ao +Compras e podem ser usadas por diversos Centros de Custo
        para orcamento, gestao, relatorios, consolidacao e classificacao operacional.
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <label>
        Nome
        <input value={nome} onChange={(event) => setNome(event.target.value)} required disabled={loading} />
      </label>

      <label>
        Descricao
        <input value={descricao} onChange={(event) => setDescricao(event.target.value)} required disabled={loading} />
      </label>

      <div className="input-row">
        <label>
          Unidade de Negocio
          <input
            value={unidadeNegocio}
            onChange={(event) => setUnidadeNegocio(event.target.value)}
            required
            disabled={loading}
          />
        </label>
        <label>
          Status
          <select
            value={status}
            onChange={(event) => setStatus(event.target.value as StatusUnidadeAlocacao)}
            disabled={loading}
          >
            <option value="Ativo">Ativo</option>
            <option value="Inativo">Inativo</option>
          </select>
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
