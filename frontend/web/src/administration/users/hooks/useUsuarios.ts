import { useCallback, useEffect, useState } from "react";
import { listUsuarios, setStatusUsuario } from "../services/usuariosMockApi";
import type { Usuario } from "../types/userTypes";

export function useUsuarios() {
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setUsuarios(await listUsuarios());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar usuarios.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  /**
   * Usuarios nunca sao excluidos: apenas ativados/inativados, permanecendo
   * auditaveis (mesmo padrao de Filiais, Centros de Custo e Unidades de
   * Alocacao).
   */
  async function toggleAtivo(usuario: Usuario): Promise<void> {
    await setStatusUsuario(usuario.id, usuario.status === "Ativo" ? "Inativo" : "Ativo");
    await reload();
  }

  return { usuarios, loading, error, reload, toggleAtivo };
}
