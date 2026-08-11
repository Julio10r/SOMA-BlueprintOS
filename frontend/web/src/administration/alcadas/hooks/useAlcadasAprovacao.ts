import { useCallback, useEffect, useState } from "react";
import { listAlcadasAprovacao, toggleStatusAlcadaAprovacao } from "../services/alcadasAprovacaoApi";
import type { AlcadaAprovacao } from "../types/alcadaAprovacaoTypes";

export function useAlcadasAprovacao(unidadeNegocioId: string | null) {
  const [alcadas, setAlcadas] = useState<AlcadaAprovacao[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    if (!unidadeNegocioId) {
      setAlcadas([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      setAlcadas(await listAlcadasAprovacao(unidadeNegocioId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar Alcadas de Aprovacao.");
    } finally {
      setLoading(false);
    }
  }, [unidadeNegocioId]);

  useEffect(() => {
    reload();
  }, [reload]);

  async function toggleStatus(alcada: AlcadaAprovacao): Promise<void> {
    if (!unidadeNegocioId) return;
    await toggleStatusAlcadaAprovacao(unidadeNegocioId, alcada);
    await reload();
  }

  return { alcadas, loading, error, reload, toggleStatus };
}
