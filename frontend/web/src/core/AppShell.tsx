import type { ReactNode } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/hooks/useAuth";
import { PERMISSOES } from "../auth/types/authTypes";

type NavItem = {
  to: string;
  label: string;
  end?: boolean;
  /**
   * Quando presente, o item so aparece se o usuario possuir a permissao efetiva
   * correspondente (O1.5). Isto e exclusivamente UX: a rota e, principalmente, a API
   * continuam protegidas no backend — esconder o item nunca e a barreira de seguranca.
   * Itens sem `permissao` pertencem a modulos que ainda nao tem RBAC real (fora do
   * escopo da O1.5) e permanecem visiveis como antes.
   */
  permissao?: string;
};

const navItems: NavItem[] = [
  { to: "/", label: "Dashboard", end: true },
  { to: "/administracao/perfis", label: "Perfis", permissao: PERMISSOES.perfilGerenciar },
  { to: "/administracao/usuarios", label: "Usuarios", permissao: PERMISSOES.usuarioGerenciar },
  { to: "/administracao/filiais", label: "Filiais", permissao: PERMISSOES.filialGerenciar },
  { to: "/administracao/centros-custo", label: "Centros de Custo", permissao: PERMISSOES.centroCustoGerenciar },
  { to: "/administracao/unidades-alocacao", label: "Unidades de Alocacao", permissao: PERMISSOES.unidadeAlocacaoGerenciar },
  { to: "/administracao/unidades-negocio", label: "Unidades de Negocio", permissao: PERMISSOES.unidadeNegocioGerenciar },
  { to: "/administracao/configuracao-erp", label: "Configuracao de ERP", permissao: PERMISSOES.configuracaoErpGerenciar },
  { to: "/administracao/identity-providers", label: "Identity Providers", permissao: PERMISSOES.sistemaGerenciar },
  { to: "/administracao/parametros", label: "Parametros", permissao: PERMISSOES.sistemaGerenciar },
  { to: "/administracao/feature-flags", label: "Feature Flags", permissao: PERMISSOES.sistemaGerenciar },
  { to: "/administracao/configuracao-notificacao", label: "Configuracao de Notificacoes", permissao: PERMISSOES.sistemaGerenciar },
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
  const permissoesEfetivas = (usuario?.permissoes ?? []).map((codigo) => codigo.toLowerCase());

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
            {navItems
              .filter((item) => !item.permissao || permissoesEfetivas.includes(item.permissao.toLowerCase()))
              .map((item) => (
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
