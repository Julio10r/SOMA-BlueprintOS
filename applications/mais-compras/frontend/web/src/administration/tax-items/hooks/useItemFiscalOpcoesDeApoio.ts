import { useEffect, useState } from "react";
import { listContasContabeis } from "../../chart-of-accounts/services/contasContabeisApi";
import { listUnidadesMedida } from "../../units-of-measure/services/unidadesMedidaApi";

export type OpcaoApoio = { codigo: string; descricao: string };

/**
 * Opções selecionáveis de Conta Contábil e Unidade de Medida para o formulário de Item Fiscal (B3 -
 * Bloco 3). Reaproveita diretamente os clientes HTTP já existentes dos Blocos 1/2 (nunca duplica a
 * leitura ERP+metadados locais).
 *
 * Filtra apenas o que é efetivamente selecionável: Conta Contábil usa `ativoEfetivo` (ADR-0024 - uma
 * conta inativa no Linx nunca aparece como opção, mesmo com metadado local "ativo"); Unidade de Medida
 * usa `ativoNoMaisCompras` (sem status no Linx, comprovado no Bloco 2). O backend valida de qualquer
 * forma - este filtro é só para não oferecer, na UI, uma opção que o backend rejeitaria.
 */
export function useItemFiscalOpcoesDeApoio() {
  const [contasContabeis, setContasContabeis] = useState<OpcaoApoio[]>([]);
  const [unidadesMedida, setUnidadesMedida] = useState<OpcaoApoio[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelado = false;
    async function carregar() {
      setLoading(true);
      setError(null);
      try {
        const [contas, unidades] = await Promise.all([listContasContabeis(), listUnidadesMedida()]);
        if (cancelado) return;
        setContasContabeis(
          contas.filter((c) => c.ativoEfetivo).map((c) => ({ codigo: c.codigoErp, descricao: c.descricaoErp }))
        );
        setUnidadesMedida(
          unidades.filter((u) => u.ativoNoMaisCompras).map((u) => ({ codigo: u.codigoErp, descricao: u.descricaoErp }))
        );
      } catch (err) {
        if (!cancelado) setError(err instanceof Error ? err.message : "Falha ao carregar Conta Contábil/Unidade de Medida.");
      } finally {
        if (!cancelado) setLoading(false);
      }
    }
    carregar();
    return () => {
      cancelado = true;
    };
  }, []);

  return { contasContabeis, unidadesMedida, loading, error };
}
