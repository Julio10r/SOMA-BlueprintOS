import { FormEvent, useState } from "react";
import { useCentrosCusto } from "../../cost-centers/hooks/useCentrosCusto";
import { PERIODO_ORCAMENTARIO, PERIODO_ORCAMENTARIO_LABELS } from "../types/regraOrcamentariaTypes";
import type { PeriodoOrcamentario, RegraOrcamentaria, RegraOrcamentariaInput } from "../types/regraOrcamentariaTypes";

/**
 * O backend (O1.12) exige `centroCustoMetadadoId` (Guid interno de `CentroCustoMetadado`), agora exposto por
 * `administration/cost-centers` (O1.7/O1.12) como `CentroCusto.centroCustoMetadadoId`. Esse campo e
 * `undefined` para Centros de Custo sem metadado local (`temMetadadoLocal === false`) — o seletor abaixo
 * desabilita essas opcoes, pois nao ha Guid para enviar ao backend.
 */
export function RegraOrcamentariaForm({ regra, error, loading, onSubmit, onCancel }: {
  regra?: RegraOrcamentaria;
  error: string | null;
  loading: boolean;
  onSubmit: (input: RegraOrcamentariaInput) => void;
  onCancel: () => void;
}) {
  const { centrosCusto } = useCentrosCusto();

  const [nome, setNome] = useState(regra?.nome ?? "");
  const [centroCustoMetadadoId, setCentroCustoMetadadoId] = useState(regra?.centroCustoMetadadoId ?? "");
  const [valorLimite, setValorLimite] = useState<string>(regra?.valorLimite != null ? String(regra.valorLimite) : "");
  const [periodo, setPeriodo] = useState<PeriodoOrcamentario>(regra?.periodo ?? PERIODO_ORCAMENTARIO.Mensal);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({ nome, centroCustoMetadadoId, valorLimite: Number(valorLimite), periodo });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>{regra ? "Editar Regra Orcamentaria" : "Nova Regra Orcamentaria"}</h2>
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <label>
        Nome
        <input value={nome} onChange={(event) => setNome(event.target.value)} disabled={loading} required />
      </label>

      <label>
        Centro de Custo
        <select value={centroCustoMetadadoId} onChange={(event) => setCentroCustoMetadadoId(event.target.value)} disabled={loading} required>
          <option value="" disabled>
            Selecione...
          </option>
          {centrosCusto.map((centroCusto) => (
            <option
              key={centroCusto.id}
              value={centroCusto.centroCustoMetadadoId ?? ""}
              disabled={!centroCusto.centroCustoMetadadoId}
            >
              {centroCusto.codigoErp} — {centroCusto.descricaoErp}
              {!centroCusto.centroCustoMetadadoId
                ? " (disponivel apenas apos primeira edicao em Gestao de Centros de Custo)"
                : ""}
            </option>
          ))}
        </select>
      </label>

      <label>
        Valor limite
        <input
          type="number"
          step="0.01"
          min={0}
          value={valorLimite}
          onChange={(event) => setValorLimite(event.target.value)}
          disabled={loading}
          required
        />
      </label>

      <label>
        Periodo
        <select value={periodo} onChange={(event) => setPeriodo(Number(event.target.value) as PeriodoOrcamentario)} disabled={loading}>
          {(Object.entries(PERIODO_ORCAMENTARIO_LABELS) as [string, string][]).map(([valor, label]) => (
            <option key={valor} value={valor}>
              {label}
            </option>
          ))}
        </select>
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
