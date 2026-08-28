import { useCallback, useEffect, useState } from "react";
import { listRegrasOrcamentarias, toggleStatusRegraOrcamentaria } from "../services/regrasOrcamentariasApi";
import type { RegraOrcamentaria } from "../types/regraOrcamentariaTypes";

export function useRegrasOrcamentarias(unidadeNegocioId: string | null) {
  const [regras, setRegras] = useState<RegraOrcamentaria[]>([]);
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
      setRegras(await listRegrasOrcamentarias(unidadeNegocioId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar Regras Orcamentarias.");
    } finally {
      setLoading(false);
    }
  }, [unidadeNegocioId]);

  useEffect(() => {
    reload();
  }, [reload]);

  async function toggleStatus(regra: RegraOrcamentaria): Promise<void> {
    if (!unidadeNegocioId) return;
    await toggleStatusRegraOrcamentaria(unidadeNegocioId, regra);
    await reload();
  }

  return { regras, loading, error, reload, toggleStatus };
}
