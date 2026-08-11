import { useCallback, useEffect, useState } from "react";
import { createFeatureFlag, listFeatureFlags, setFeatureFlagStatus } from "../services/featureFlagsApi";
import type { FeatureFlagCriarInput } from "../types/featureFlagTypes";

export function useFeatureFlags() {
  const [flags, setFlags] = useState<Awaited<ReturnType<typeof listFeatureFlags>>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setFlags(await listFeatureFlags());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar Feature Flags.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  async function criar(input: FeatureFlagCriarInput): Promise<void> {
    await createFeatureFlag(input);
    await reload();
  }

  async function alterarStatus(id: string, unidadeNegocioId: string, ativa: boolean): Promise<void> {
    await setFeatureFlagStatus(id, unidadeNegocioId, ativa);
    await reload();
  }

  return { flags, loading, error, reload, criar, alterarStatus };
}
