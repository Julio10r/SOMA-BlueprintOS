import { useCallback, useEffect, useState } from "react";
import { listIdentityProviders, toggleStatusIdentityProvider } from "../services/identityProvidersApi";
import type { IdentityProvider } from "../types/identityProviderTypes";

export function useIdentityProviders(unidadeNegocioId: string | null) {
  const [providers, setProviders] = useState<IdentityProvider[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    if (!unidadeNegocioId) {
      setProviders([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      setProviders(await listIdentityProviders(unidadeNegocioId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar Identity Providers.");
    } finally {
      setLoading(false);
    }
  }, [unidadeNegocioId]);

  useEffect(() => {
    reload();
  }, [reload]);

  async function toggleStatus(provider: IdentityProvider): Promise<void> {
    if (!unidadeNegocioId) return;
    await toggleStatusIdentityProvider(unidadeNegocioId, provider);
    await reload();
  }

  return { providers, loading, error, reload, toggleStatus };
}
