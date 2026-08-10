import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useBootstrapEstado } from "../hooks/useBootstrapEstado";

/**
 * Decisão de roteamento raiz (Work Order O1.4.3, seção 16): antes de
 * renderizar qualquer rota, consulta `GET /bootstrap/estado` e decide entre
 * `/bootstrap` e o restante da aplicação (`/login`, área autenticada).
 *
 * Isto é exclusivamente UX — nunca um controle de segurança. O backend já
 * impõe a autorização real: `BootstrapAuthenticated` exige `BootstrapSessao`
 * válida e `BootstrapEstado.Concluido == false`; nenhum endpoint de negócio
 * aceita a sessão de Bootstrap. Mesmo que este componente seja contornado ou
 * removido, nenhuma operação sensível fica exposta.
 */
export function BootstrapGate({ children }: { children: ReactNode }) {
  const { estado, carregando } = useBootstrapEstado();
  const location = useLocation();

  if (carregando) {
    return (
      <div className="auth-loading" role="status" aria-live="polite">
        Verificando configuração inicial…
      </div>
    );
  }

  const disponivel = estado?.disponivel ?? false;
  const emBootstrap = location.pathname.startsWith("/bootstrap");

  if (disponivel && !emBootstrap) {
    return <Navigate to="/bootstrap" replace />;
  }

  if (!disponivel && emBootstrap) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
