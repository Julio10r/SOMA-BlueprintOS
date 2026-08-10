import { useEffect, useState } from "react";
import { consultarEstado } from "../services/bootstrapApi";
import type { BootstrapEstado } from "../types/bootstrapTypes";

/**
 * Consulta única (no mount) de `GET /bootstrap/estado`. Usado apenas como
 * decisão de UX de roteamento (`/login` vs. `/bootstrap`) — nunca como
 * barreira de segurança; a autorização real de qualquer endpoint de negócio
 * vive inteiramente no backend (`BootstrapAuthenticated`, `Concluido`).
 */
export function useBootstrapEstado() {
  const [estado, setEstado] = useState<BootstrapEstado | null>(null);
  const [carregando, setCarregando] = useState(true);

  useEffect(() => {
    let ativo = true;
    (async () => {
      const atual = await consultarEstado();
      if (ativo) {
        setEstado(atual);
        setCarregando(false);
      }
    })();
    return () => {
      ativo = false;
    };
  }, []);

  return { estado, carregando };
}
