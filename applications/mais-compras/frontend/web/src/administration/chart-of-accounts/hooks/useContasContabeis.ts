import { useCallback, useEffect, useState } from "react";
import { listContasContabeis, updateContaContabil } from "../services/contasContabeisApi";
import type { ContaContabil, ContaContabilUpdateInput } from "../types/contaContabilTypes";

export function useContasContabeis() {
  const [contasContabeis, setContasContabeis] = useState<ContaContabil[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setContasContabeis(await listContasContabeis());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar contas contábeis.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  async function save(id: string, input: ContaContabilUpdateInput): Promise<void> {
    await updateContaContabil(id, input);
    await reload();
  }

  /**
   * Ativa/inativa a conta contabil no +Compras diretamente pela listagem, sem abrir o formulario de
   * edicao. Preserva a Descricao +Compras existente. Nunca escreve no ERP - e nunca torna a conta
   * efetivamente ativa se o Linx ja a marcou como inativa (ADR-0024).
   */
  async function toggleAtivo(conta: ContaContabil): Promise<void> {
    await save(conta.id, {
      descricaoMaisCompras: conta.descricaoMaisCompras,
      ativoNoMaisCompras: !conta.ativoNoMaisCompras
    });
  }

  return { contasContabeis, loading, error, reload, save, toggleAtivo };
}
