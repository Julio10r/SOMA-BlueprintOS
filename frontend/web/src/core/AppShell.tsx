import type { ReactNode } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/hooks/useAuth";

type NavItem = {
  to: string;
  label: string;
  end?: boolean;
};

const navItems: NavItem[] = [
  { to: "/", label: "Dashboard", end: true },
  { to: "/administracao/perfis", label: "Perfis" },
  { to: "/administracao/usuarios", label: "Usuarios" },
  { to: "/administracao/filiais", label: "Filiais" },
  { to: "/administracao/centros-custo", label: "Centros de Custo" },
  { to: "/administracao/unidades-alocacao", label: "Unidades de Alocacao" },
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
  const { usuario, logout } = useAuth();
  const navigate = useNavigate();

  async function handleLogout() {
    await logout();
    navigate("/login", { replace: true });
  }

  return (
    <div className="app-shell">
      <header className="portal-header">
        <div className="brand-mark">AZZAS 2154</div>
        <div className="logo-suffix">+Compras</div>
        {usuario && <div className="user-chip">{usuario.nome}</div>}
        <button type="button" className="btn btn-secondary" onClick={handleLogout}>
          Sair
        </button>
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
