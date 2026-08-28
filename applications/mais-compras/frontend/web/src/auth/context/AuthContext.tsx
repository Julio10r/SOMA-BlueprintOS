import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import type { UsuarioAutenticado } from "../types/authTypes";
import { fetchCurrentUser, logout as logoutRequest } from "../services/authApi";

type AuthContextValue = {
  usuario: UsuarioAutenticado | null;
  carregando: boolean;
  refresh: () => Promise<void>;
  setUsuario: (usuario: UsuarioAutenticado | null) => void;
  logout: () => Promise<void>;
};

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/**
 * Fonte única de verdade da identidade autenticada no frontend. Nenhum dado de
 * sessão é persistido em localStorage/sessionStorage — apenas estado React em
 * memória, reidratado via `GET /auth/me` (que depende do cookie HttpOnly).
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<UsuarioAutenticado | null>(null);
  const [carregando, setCarregando] = useState(true);

  const refresh = useCallback(async () => {
    const atual = await fetchCurrentUser();
    setUsuario(atual);
  }, []);

  useEffect(() => {
    let ativo = true;
    (async () => {
      const atual = await fetchCurrentUser();
      if (ativo) {
        setUsuario(atual);
        setCarregando(false);
      }
    })();
    return () => {
      ativo = false;
    };
  }, []);

  const logout = useCallback(async () => {
    await logoutRequest();
    setUsuario(null);
  }, []);

  const value = useMemo(
    () => ({ usuario, carregando, refresh, setUsuario, logout }),
    [usuario, carregando, refresh, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
