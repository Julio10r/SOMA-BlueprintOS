import { useCallback, useEffect, useState } from "react";
import { getConfiguracaoErp, toggleStatusConfiguracaoErp } from "../services/configuracaoErpApi";
import type { ConfiguracaoErp } from "../types/configuracaoErpTypes";

export function useConfiguracaoErp(unidadeNegocioId: string | null) {
  const [configuracao, setConfiguracao] = useState<ConfiguracaoErp | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    if (!unidadeNegocioId) {
      setConfiguracao(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      setConfiguracao(await getConfiguracaoErp(unidadeNegocioId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar Configuracao de ERP.");
    } finally {
      setLoading(false);
    }
  }, [unidadeNegocioId]);

  useEffect(() => {
    reload();
  }, [reload]);

  async function toggleStatus(): Promise<void> {
    if (!unidadeNegocioId || !configuracao) return;
    await toggleStatusConfiguracaoErp(unidadeNegocioId, configuracao);
    await reload();
  }

  return { configuracao, loading, error, reload, toggleStatus };
}
