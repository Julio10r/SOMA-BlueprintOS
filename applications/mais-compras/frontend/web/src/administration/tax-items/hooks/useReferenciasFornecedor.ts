import { useCallback, useEffect, useState } from "react";
import {
  createReferenciaFornecedor,
  deleteReferenciaFornecedor,
  listReferenciasFornecedor,
  updateReferenciaFornecedor
} from "../services/itemFiscalReferenciasFornecedorApi";
import type {
  ItemFiscalReferenciaFornecedor,
  ItemFiscalReferenciaFornecedorCreateInput,
  ItemFiscalReferenciaFornecedorUpdateInput
} from "../types/itemFiscalReferenciaFornecedorTypes";

/**
 * Referências por Fornecedor de um Item Fiscal específico (B3 - Bloco 4, Discovery homologado). Só faz
 * sentido para um Item Fiscal já existente (persistido) - a aba correspondente no formulário fica
 * desabilitada durante a criação (ver `ItemFiscalForm.tsx`).
 */
export function useReferenciasFornecedor(itemFiscalId: string | undefined) {
  const [referencias, setReferencias] = useState<ItemFiscalReferenciaFornecedor[]>([]);
  const [loading, setLoading] = useState(Boolean(itemFiscalId));
  const [error, setError] = useState<string | null>(null);

  const recarregar = useCallback(() => {
    if (!itemFiscalId) return;
    setLoading(true);
    setError(null);
    listReferenciasFornecedor(itemFiscalId)
      .then(setReferencias)
      .catch((e) => setError(e instanceof Error ? e.message : "Falha ao carregar as referências por fornecedor."))
      .finally(() => setLoading(false));
  }, [itemFiscalId]);

  useEffect(recarregar, [recarregar]);

  async function incluir(input: ItemFiscalReferenciaFornecedorCreateInput) {
    if (!itemFiscalId) return;
    await createReferenciaFornecedor(itemFiscalId, input);
    recarregar();
  }

  async function atualizar(id: string, input: ItemFiscalReferenciaFornecedorUpdateInput) {
    if (!itemFiscalId) return;
    await updateReferenciaFornecedor(itemFiscalId, id, input);
    recarregar();
  }

  async function remover(id: string) {
    if (!itemFiscalId) return;
    await deleteReferenciaFornecedor(itemFiscalId, id);
    recarregar();
  }

  return { referencias, loading, error, incluir, atualizar, remover, recarregar };
}
