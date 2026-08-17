import { useCallback, useEffect, useState } from "react";
import { listUnidadesAlocacao, toggleStatusUnidadeAlocacao } from "../services/unidadesAlocacaoApi";
import type { UnidadeAlocacao } from "../types/unidadeAlocacaoTypes";

export function useUnidadesAlocacao() {
  const [unidadesAlocacao, setUnidadesAlocacao] = useState<UnidadeAlocacao[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setUnidadesAlocacao(await listUnidadesAlocacao());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar unidades de alocação.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  async function toggleStatus(unidadeAlocacao: UnidadeAlocacao): Promise<void> {
    await toggleStatusUnidadeAlocacao(unidadeAlocacao);
    await reload();
  }

  return { unidadesAlocacao, loading, error, reload, toggleStatus };
}
