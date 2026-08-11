import { useCallback, useEffect, useState } from "react";
import { getConfiguracaoNotificacao } from "../services/configuracaoNotificacaoApi";
import type { ConfiguracaoNotificacao } from "../types/configuracaoNotificacaoTypes";

export function useConfiguracaoNotificacao(unidadeNegocioId: string | null) {
  const [configuracao, setConfiguracao] = useState<ConfiguracaoNotificacao | null>(null);
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
      setConfiguracao(await getConfiguracaoNotificacao(unidadeNegocioId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar Configuracao de Notificacoes.");
    } finally {
      setLoading(false);
    }
  }, [unidadeNegocioId]);

  useEffect(() => {
    reload();
  }, [reload]);

  return { configuracao, loading, error, reload };
}
