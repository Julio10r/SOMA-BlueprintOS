import { useCallback, useEffect, useState } from "react";
import { listRegrasWorkflow, toggleStatusRegraWorkflow } from "../services/regrasWorkflowApi";
import type { RegraWorkflow } from "../types/regraWorkflowTypes";

export function useRegrasWorkflow(unidadeNegocioId: string | null) {
  const [regras, setRegras] = useState<RegraWorkflow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    if (!unidadeNegocioId) {
      setRegras([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      setRegras(await listRegrasWorkflow(unidadeNegocioId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar Regras de Workflow.");
    } finally {
      setLoading(false);
    }
  }, [unidadeNegocioId]);

  useEffect(() => {
    reload();
  }, [reload]);

  async function toggleStatus(regra: RegraWorkflow): Promise<void> {
    if (!unidadeNegocioId) return;
    await toggleStatusRegraWorkflow(unidadeNegocioId, regra);
    await reload();
  }

  return { regras, loading, error, reload, toggleStatus };
}
