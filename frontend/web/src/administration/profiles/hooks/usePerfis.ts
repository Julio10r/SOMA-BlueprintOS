import { useCallback, useEffect, useState } from "react";
import { deletePerfil, listPerfis } from "../services/perfisMockApi";
import type { Perfil } from "../types/perfilTypes";

export function usePerfis() {
  const [perfis, setPerfis] = useState<Perfil[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setPerfis(await listPerfis());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar perfis.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  async function remove(id: string): Promise<void> {
    await deletePerfil(id);
    await reload();
  }

  return { perfis, loading, error, reload, remove };
}
