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

type NavGroup = {
  titulo: string;
  itens: NavItem[];
};

/**
 * Navegacao agrupada por contexto (Design Review Pos-Onda 1, lote DR.1).
 * A lista de rotas e a logica de RBAC por item (`permissao` +
 * `permissoesEfetivas`) permanecem exatamente como antes — apenas a
 * apresentacao visual passou de lista plana para grupos com titulo.
 */
const navGroups: NavGroup[] = [
  {
    titulo: "Inicio",
    itens: [{ to: "/", label: "Dashboard", end: true }]
  },
  {
    titulo: "Fornecedores",
    itens: [{ to: "/fornecedores", label: "Fornecedores" }]
  },
  {
    titulo: "Compras",
    itens: [
      { to: "/pedidos", label: "Pedidos" },
      { to: "/negociacoes", label: "Negociacoes" },
      { to: "/indicadores", label: "Indicadores" }
    ]
  },
  {
    titulo: "Governanca de Compras",
    itens: [
      { to: "/administracao/regras-workflow", label: "Regras de Workflow", permissao: PERMISSOES.workflowGerenciar },
      { to: "/administracao/alcadas-aprovacao", label: "Alcadas de Aprovacao", permissao: PERMISSOES.alcadaGerenciar },
      { to: "/administracao/regras-orcamentarias", label: "Regras Orcamentarias", permissao: PERMISSOES.orcamentoGerenciar }
    ]
  },
  {
    titulo: "Administracao",
    itens: [
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
      { to: "/administracao/monitoramento", label: "Monitoramento", permissao: PERMISSOES.sistemaGerenciar }
    ]
  },
  {
    titulo: "Agentes IA",
    itens: [{ to: "/agentes-ia", label: "Agentes IA" }]
  },
  {
    titulo: "Configuracoes",
    itens: [{ to: "/configuracoes", label: "Configuracoes" }]
  }
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
          {navGroups.map((grupo) => {
            const itensVisiveis = grupo.itens.filter(
              (item) => !item.permissao || permissoesEfetivas.includes(item.permissao.toLowerCase())
            );
            if (itensVisiveis.length === 0) return null;
            return (
              <div className="app-nav-group" key={grupo.titulo}>
                <div className="app-nav-section section-title">{grupo.titulo}</div>
                <ul>
                  {itensVisiveis.map((item) => (
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
              </div>
            );
          })}
        </nav>
        <main className="app-content">{children}</main>
      </div>
    </div>
  );
}
