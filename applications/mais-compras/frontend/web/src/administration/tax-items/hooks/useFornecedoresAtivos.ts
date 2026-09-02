import { useEffect, useState } from "react";
import { listSuppliers } from "../../../procurement/suppliers/services/supplierEnrichmentApi";

export type OpcaoFornecedor = { id: string; nome: string };

/**
 * Fornecedores ATIVOS do cadastro já existente do +Compras, para o seletor de Referências por Fornecedor
 * (B3 - Bloco 4). Reaproveita o cliente de listagem já existente (`listSuppliers`) - nenhum cadastro novo
 * de Fornecedor é criado por esta funcionalidade.
 */
export function useFornecedoresAtivos(): { opcoes: OpcaoFornecedor[]; loading: boolean; error: string | null } {
  const [opcoes, setOpcoes] = useState<OpcaoFornecedor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelado = false;
    setLoading(true);
    listSuppliers()
      .then((fornecedores) => {
        if (cancelado) return;
        setOpcoes(
          fornecedores
            .filter((f) => (f.status ?? "Ativo") === "Ativo")
            .map((f) => ({ id: f.id, nome: f.nomeFantasia?.trim() || f.razaoSocial }))
        );
      })
      .catch((e) => {
        if (!cancelado) setError(e instanceof Error ? e.message : "Falha ao carregar fornecedores.");
      })
      .finally(() => {
        if (!cancelado) setLoading(false);
      });
    return () => {
      cancelado = true;
    };
  }, []);

  return { opcoes, loading, error };
}
