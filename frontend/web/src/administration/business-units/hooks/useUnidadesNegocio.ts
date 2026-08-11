import { useCallback, useEffect, useState } from "react";
import { listUnidadesNegocio, toggleStatusUnidadeNegocio } from "../services/unidadesNegocioApi";
import type { UnidadeNegocio } from "../types/unidadeNegocioTypes";

export function useUnidadesNegocio() {
  const [unidadesNegocio, setUnidadesNegocio] = useState<UnidadeNegocio[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setUnidadesNegocio(await listUnidadesNegocio());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar Unidades de Negocio.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  async function toggleStatus(unidadeNegocio: UnidadeNegocio): Promise<void> {
    await toggleStatusUnidadeNegocio(unidadeNegocio);
    await reload();
  }

  return { unidadesNegocio, loading, error, reload, toggleStatus };
}
