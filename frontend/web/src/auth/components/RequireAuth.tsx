import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

/**
 * Guarda de rota: usuário sem sessão é direcionado ao Login. Não implementa
 * ainda matriz de autorização por permissão (RBAC) — apenas presença de
 * sessão válida — para não ampliar o escopo desta sprint; a estrutura já
 * comporta essa evolução futura via o mesmo AuthContext.
 */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { usuario, carregando } = useAuth();
  const location = useLocation();

  if (carregando) {
    return (
      <div className="auth-loading" role="status" aria-live="polite">
        Carregando sessão…
      </div>
    );
  }

  if (!usuario) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <>{children}</>;
}
