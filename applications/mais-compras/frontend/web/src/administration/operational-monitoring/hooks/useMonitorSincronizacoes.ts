import { useCallback, useEffect, useState } from "react";
import { listarSincronizacoesFornecedores } from "../services/monitoramentoApi";
import type { SincronizacaoFornecedorResumo } from "../types/monitoramentoTypes";

export function useMonitorSincronizacoes() {
  const [itens, setItens] = useState<SincronizacaoFornecedorResumo[]>([]);
  const [totalRegistros, setTotalRegistros] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [tamanhoPagina] = useState(20);
  const [status, setStatus] = useState<string | null>(null);
  const [businessUnit, setBusinessUnit] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const resultado = await listarSincronizacoesFornecedores({
        status: status ?? undefined,
        businessUnit: businessUnit ?? undefined,
        pagina,
        tamanhoPagina
      });
      setItens(resultado.itens);
      setTotalRegistros(resultado.totalRegistros);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar as execuções de sincronização.");
      setItens([]);
      setTotalRegistros(0);
    } finally {
      setLoading(false);
    }
  }, [status, businessUnit, pagina, tamanhoPagina]);

  useEffect(() => {
    reload();
  }, [reload]);

  return {
    itens,
    totalRegistros,
    pagina,
    tamanhoPagina,
    setPagina,
    status,
    setStatus: (value: string | null) => {
      setStatus(value);
      setPagina(1);
    },
    businessUnit,
    setBusinessUnit: (value: string | null) => {
      setBusinessUnit(value);
      setPagina(1);
    },
    loading,
    error,
    reload
  };
}
