import { useCallback, useEffect, useState } from "react";
import { listUnidadesMedida, updateUnidadeMedida } from "../services/unidadesMedidaApi";
import type { UnidadeMedida, UnidadeMedidaUpdateInput } from "../types/unidadeMedidaTypes";

export function useUnidadesMedida() {
  const [unidadesMedida, setUnidadesMedida] = useState<UnidadeMedida[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setUnidadesMedida(await listUnidadesMedida());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar unidades de medida.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  async function save(id: string, input: UnidadeMedidaUpdateInput): Promise<void> {
    await updateUnidadeMedida(id, input);
    await reload();
  }

  /**
   * Ativa/inativa a unidade de medida no +Compras diretamente pela listagem, sem abrir o formulario de
   * edicao. Preserva a Descricao +Compras existente. Nunca escreve no ERP.
   */
  async function toggleAtivo(unidade: UnidadeMedida): Promise<void> {
    await save(unidade.id, {
      descricaoMaisCompras: unidade.descricaoMaisCompras,
      ativoNoMaisCompras: !unidade.ativoNoMaisCompras
    });
  }

  return { unidadesMedida, loading, error, reload, save, toggleAtivo };
}
