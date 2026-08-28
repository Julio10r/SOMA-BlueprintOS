import { useCallback, useEffect, useState } from "react";
import {
  alterarStatusPerfil,
  listPerfis,
  listPermissoes,
  PerfilAcessoNegadoError
} from "../services/perfisApi";
import type { Perfil, Permissao } from "../types/perfilTypes";

/**
 * Estados reais tratados pelas telas de Perfis (O1.5): carregando, sucesso, vazio,
 * erro e acesso negado. `acessoNegado` e distinto de `error` porque um 403 nao e uma
 * falha a ser reportada como problema — e a resposta correta do backend para um
 * usuario autenticado sem a permissao `Perfil.Gerenciar`.
 */
export type EstadoCarregamento<T> = {
  dados: T;
  loading: boolean;
  error: string | null;
  acessoNegado: boolean;
};

function mensagemDe(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback;
}

export function usePerfis() {
  const [perfis, setPerfis] = useState<Perfil[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [acessoNegado, setAcessoNegado] = useState(false);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    setAcessoNegado(false);
    try {
      setPerfis(await listPerfis());
    } catch (err) {
      if (err instanceof PerfilAcessoNegadoError) {
        setAcessoNegado(true);
        setPerfis([]);
      } else {
        setError(mensagemDe(err, "Falha ao carregar perfis."));
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  const alterarStatus = useCallback(async (id: string, ativo: boolean): Promise<void> => {
    await alterarStatusPerfil(id, ativo);
    await reload();
  }, [reload]);

  return { perfis, loading, error, acessoNegado, reload, alterarStatus };
}

/** Catalogo de permissoes vindo do backend, usado pelo formulario e pelo resumo. */
export function usePermissionCatalog() {
  const [permissoes, setPermissoes] = useState<Permissao[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [acessoNegado, setAcessoNegado] = useState(false);

  useEffect(() => {
    let ativo = true;
    (async () => {
      try {
        const catalogo = await listPermissoes();
        if (ativo) setPermissoes(catalogo);
      } catch (err) {
        if (!ativo) return;
        if (err instanceof PerfilAcessoNegadoError) setAcessoNegado(true);
        else setError(mensagemDe(err, "Falha ao carregar o catálogo de permissões."));
      } finally {
        if (ativo) setLoading(false);
      }
    })();
    return () => {
      ativo = false;
    };
  }, []);

  return { permissoes, loading, error, acessoNegado };
}
