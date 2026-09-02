import { useCallback, useEffect, useState } from "react";
import { listItensFiscais, toggleStatusItemFiscal } from "../services/itensFiscaisApi";
import type { ItemFiscal } from "../types/itemFiscalTypes";

export function useItensFiscais() {
  const [itensFiscais, setItensFiscais] = useState<ItemFiscal[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setItensFiscais(await listItensFiscais());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar itens fiscais.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  async function toggleStatus(item: ItemFiscal): Promise<void> {
    await toggleStatusItemFiscal(item);
    await reload();
  }

  return { itensFiscais, loading, error, reload, toggleStatus };
}
