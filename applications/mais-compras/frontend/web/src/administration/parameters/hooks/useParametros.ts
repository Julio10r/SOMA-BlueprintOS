import { useCallback, useEffect, useState } from "react";
import { createParametro, deleteParametro, listParametros, updateParametro } from "../services/parametrosApi";
import type { ParametroAtualizarInput, ParametroCriarInput } from "../types/parametroTypes";

export function useParametros(unidadeNegocioId?: string) {
  const [parametros, setParametros] = useState<Awaited<ReturnType<typeof listParametros>>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setParametros(await listParametros(unidadeNegocioId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar Parâmetros.");
    } finally {
      setLoading(false);
    }
  }, [unidadeNegocioId]);

  useEffect(() => {
    reload();
  }, [reload]);

  async function criar(input: ParametroCriarInput): Promise<void> {
    await createParametro(input);
    await reload();
  }

  async function atualizar(id: string, input: ParametroAtualizarInput): Promise<void> {
    await updateParametro(id, input);
    await reload();
  }

  async function remover(id: string): Promise<void> {
    await deleteParametro(id);
    await reload();
  }

  return { parametros, loading, error, reload, criar, atualizar, remover };
}
