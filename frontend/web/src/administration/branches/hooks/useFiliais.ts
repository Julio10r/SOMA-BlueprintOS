import { useCallback, useEffect, useState } from "react";
import { listFiliais, updateFilial } from "../services/filiaisMockApi";
import type { Filial, FilialUpdateInput } from "../types/filialTypes";

export function useFiliais() {
  const [filiais, setFiliais] = useState<Filial[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setFiliais(await listFiliais());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar filiais.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  async function save(id: string, input: FilialUpdateInput): Promise<void> {
    await updateFilial(id, input);
    await reload();
  }

  /**
   * Ativa/inativa a filial no +Compras diretamente pela listagem, sem
   * abrir o formulario de edicao. Preserva a Descricao +Compras existente
   * — apenas o campo AtivoNoMaisCompras e alterado. Nunca escreve no ERP.
   */
  async function toggleAtivo(filial: Filial): Promise<void> {
    await save(filial.id, {
      descricaoMaisCompras: filial.descricaoMaisCompras,
      ativoNoMaisCompras: !filial.ativoNoMaisCompras
    });
  }

  return { filiais, loading, error, reload, save, toggleAtivo };
}
