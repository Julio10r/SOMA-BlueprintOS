import { useEffect, useState } from "react";
import { listMinhasUnidadesNegocio } from "../services/minhasUnidadesNegocioApi";
import type { UnidadeNegocioSelecionavel } from "../types/unidadeNegocioSelecaoTypes";

export function useMinhasUnidadesNegocio() {
  const [unidades, setUnidades] = useState<UnidadeNegocioSelecionavel[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let ativo = true;
    (async () => {
      const dados = await listMinhasUnidadesNegocio();
      if (ativo) {
        setUnidades(dados);
        setLoading(false);
      }
    })();
    return () => {
      ativo = false;
    };
  }, []);

  return { unidades, loading };
}
