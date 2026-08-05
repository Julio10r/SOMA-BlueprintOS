import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";

type NavItem = {
  to: string;
  label: string;
  end?: boolean;
};

const navItems: NavItem[] = [
  { to: "/", label: "Dashboard", end: true },
  { to: "/fornecedores", label: "Fornecedores" },
  { to: "/pedidos", label: "Pedidos" },
  { to: "/negociacoes", label: "Negociacoes" },
  { to: "/indicadores", label: "Indicadores" },
  { to: "/agentes-ia", label: "Agentes IA" },
  { to: "/configuracoes", label: "Configuracoes" }
];

/**
 * Shell visual do Portal +Compras: header com identidade AZZAS 2154 e
 * sidebar de navegacao entre os modulos do portal (react-router-dom).
 * A area de conteudo renderiza a rota ativa (ver AppRoutes.tsx).
 */
export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="app-shell">
      <header className="portal-header">
        <div className="brand-mark">AZZAS 2154</div>
        <div className="logo-suffix">+Compras</div>
        <div className="user-chip">COMPRAS</div>
      </header>
      <div className="app-body">
        <nav className="app-sidebar" aria-label="Navegacao do portal +Compras">
          <ul>
            {navItems.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) => `app-nav-link${isActive ? " app-nav-link-active" : ""}`}
                >
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
        <main className="app-content">{children}</main>
      </div>
    </div>
  );
}
