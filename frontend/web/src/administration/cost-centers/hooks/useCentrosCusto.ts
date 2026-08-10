import { useCallback, useEffect, useState } from "react";
import { listCentrosCusto, updateCentroCusto } from "../services/centrosCustoMockApi";
import type { CentroCusto, CentroCustoUpdateInput } from "../types/centroCustoTypes";

export function useCentrosCusto() {
  const [centrosCusto, setCentrosCusto] = useState<CentroCusto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setCentrosCusto(await listCentrosCusto());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar centros de custo.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  async function save(id: string, input: CentroCustoUpdateInput): Promise<void> {
    await updateCentroCusto(id, input);
    await reload();
  }

  /**
   * Ativa/inativa o centro de custo no +Compras diretamente pela
   * listagem, sem abrir o formulario de edicao. Preserva a Descricao
   * +Compras existente — apenas o campo AtivoNoMaisCompras e alterado.
   * Nunca escreve no ERP.
   */
  async function toggleAtivo(centroCusto: CentroCusto): Promise<void> {
    await save(centroCusto.id, {
      descricaoMaisCompras: centroCusto.descricaoMaisCompras,
      ativoNoMaisCompras: !centroCusto.ativoNoMaisCompras
    });
  }

  return { centrosCusto, loading, error, reload, save, toggleAtivo };
}
