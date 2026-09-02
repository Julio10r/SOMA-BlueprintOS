import { type ReactNode, useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/hooks/useAuth";
import { PERMISSOES } from "../auth/types/authTypes";
import { UserMenu } from "./components/UserMenu";
import { NavIcon, type NavIconKey } from "./components/NavIcons";

const SIDEBAR_COLAPSADA_STORAGE_KEY = "maisCompras.sidebarColapsada";

function lerPreferenciaSidebarColapsada(): boolean {
  try {
    return window.localStorage.getItem(SIDEBAR_COLAPSADA_STORAGE_KEY) === "true";
  } catch {
    // localStorage pode nao estar disponivel (ex: navegacao privada) — degrada para expandida.
    return false;
  }
}

type NavItem = {
  to: string;
  label: string;
  end?: boolean;
  /** Icone semantico do item (Redesign Fase 1 — Shell/DS, sidebar "afirmativa"). */
  icon: NavIconKey;
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
    titulo: "Início",
    itens: [{ to: "/", label: "Dashboard", end: true, icon: "dashboard" }]
  },
  {
    titulo: "Fornecedores",
    itens: [{ to: "/fornecedores", label: "Fornecedores", icon: "building" }]
  },
  {
    // B3 — Bloco 3 (Discovery homologado): Item Fiscal é cadastro primário do +Compras (cria/edita/inativa
    // localmente), por isso tem seção própria — diferente de Conta Contábil/Unidade de Medida
    // (Blocos 1/2), que são cadastros de apoio somente-leitura do ERP e permanecem em "Administração".
    titulo: "Cadastros",
    itens: [{ to: "/cadastros/itens-fiscais", label: "Itens Fiscais", icon: "briefcase", permissao: PERMISSOES.itemFiscalVisualizar }]
  },
  {
    titulo: "Compras",
    itens: [
      { to: "/pedidos", label: "Pedidos", icon: "cart" },
      { to: "/negociacoes", label: "Negociações", icon: "chat" },
      { to: "/indicadores", label: "Indicadores", icon: "chart" }
    ]
  },
  {
    titulo: "Governança de Compras",
    itens: [
      {
        to: "/administracao/regras-workflow",
        label: "Regras de Workflow",
        icon: "workflow",
        permissao: PERMISSOES.workflowGerenciar
      },
      {
        to: "/administracao/alcadas-aprovacao",
        label: "Alçadas de Aprovação",
        icon: "shield",
        permissao: PERMISSOES.alcadaGerenciar
      },
      {
        to: "/administracao/regras-orcamentarias",
        label: "Regras Orçamentárias",
        icon: "wallet",
        permissao: PERMISSOES.orcamentoGerenciar
      }
    ]
  },
  {
    titulo: "Administração",
    itens: [
      { to: "/administracao/perfis", label: "Perfis", icon: "users", permissao: PERMISSOES.perfilGerenciar },
      { to: "/administracao/usuarios", label: "Usuários", icon: "user", permissao: PERMISSOES.usuarioGerenciar },
      { to: "/administracao/filiais", label: "Filiais", icon: "mapPin", permissao: PERMISSOES.filialGerenciar },
      {
        to: "/administracao/centros-custo",
        label: "Centros de Custo",
        icon: "tag",
        permissao: PERMISSOES.centroCustoGerenciar
      },
      {
        to: "/administracao/contas-contabeis",
        label: "Contas Contábeis",
        icon: "wallet",
        permissao: PERMISSOES.contaContabilGerenciar
      },
      {
        to: "/administracao/unidades-medida",
        label: "Unidades de Medida",
        icon: "sliders",
        permissao: PERMISSOES.unidadeMedidaGerenciar
      },
      {
        to: "/administracao/unidades-alocacao",
        label: "Unidades de Alocação",
        icon: "layers",
        permissao: PERMISSOES.unidadeAlocacaoGerenciar
      },
      {
        to: "/administracao/unidades-negocio",
        label: "Unidades de Negócio",
        icon: "briefcase",
        permissao: PERMISSOES.unidadeNegocioGerenciar
      },
      {
        to: "/administracao/configuracao-erp",
        label: "Configuração do ERP",
        icon: "server",
        permissao: PERMISSOES.configuracaoErpGerenciar
      },
      {
        to: "/administracao/identity-providers",
        label: "Identity Providers",
        icon: "key",
        permissao: PERMISSOES.sistemaGerenciar
      },
      { to: "/administracao/parametros", label: "Parâmetros", icon: "sliders", permissao: PERMISSOES.sistemaGerenciar },
      {
        to: "/administracao/feature-flags",
        label: "Feature Flags",
        icon: "flag",
        permissao: PERMISSOES.sistemaGerenciar
      },
      {
        to: "/administracao/configuracao-notificacao",
        label: "Configuração de Notificações",
        icon: "bell",
        permissao: PERMISSOES.sistemaGerenciar
      },
      {
        to: "/administracao/monitoramento",
        label: "Monitoramento",
        icon: "activity",
        permissao: PERMISSOES.sistemaGerenciar
      }
    ]
  },
  {
    titulo: "Agentes IA",
    itens: [{ to: "/agentes-ia", label: "Agentes IA", icon: "cpu" }]
  }
];

/** Item de rodape, tratado separadamente do restante da navegacao (ver AppShell). */
const itemConfiguracoes: NavItem = { to: "/configuracoes", label: "Configurações", icon: "settings" };

/**
 * Shell visual do Portal +Compras: header com identidade AZZAS 2154 e
 * sidebar de navegacao entre os modulos do portal (react-router-dom).
 * A area de conteudo renderiza a rota ativa (ver AppRoutes.tsx).
 */
export function AppShell({ children }: { children: ReactNode }) {
  const { usuario, logout } = useAuth();
  const navigate = useNavigate();
  const permissoesEfetivas = (usuario?.permissoes ?? []).map((codigo) => codigo.toLowerCase());
  const [colapsada, setColapsada] = useState(lerPreferenciaSidebarColapsada);

  async function handleLogout() {
    await logout();
    navigate("/login", { replace: true });
  }

  function alternarColapso() {
    setColapsada((atual) => {
      const proximo = !atual;
      try {
        window.localStorage.setItem(SIDEBAR_COLAPSADA_STORAGE_KEY, String(proximo));
      } catch {
        // localStorage indisponivel: a preferencia simplesmente nao persiste entre sessoes.
      }
      return proximo;
    });
  }

  return (
    <div className="app-shell">
      <header className="portal-header">
        <div className="brand-mark">AZZAS 2154</div>
        <div className="logo-suffix">+Compras</div>
        {usuario && <UserMenu usuario={usuario} onLogout={handleLogout} />}
      </header>
      <div className="app-body">
        <nav
          className={`app-sidebar${colapsada ? " app-sidebar-collapsed" : ""}`}
          aria-label="Navegação do portal +Compras"
        >
          <button
            type="button"
            className="app-sidebar-toggle"
            onClick={alternarColapso}
            aria-expanded={!colapsada}
            aria-label={colapsada ? "Expandir menu" : "Recolher menu"}
            title={colapsada ? "Expandir menu" : "Recolher menu"}
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" style={{ width: 14, height: 14 }}>
              {colapsada ? <polyline points="9 18 15 12 9 6" /> : <polyline points="15 18 9 12 15 6" />}
            </svg>
          </button>
          <div className="app-nav-scroll">
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
                          title={colapsada ? item.label : undefined}
                        >
                          <span className="app-nav-left">
                            <NavIcon name={item.icon} />
                            <span className="app-nav-label">{item.label}</span>
                          </span>
                        </NavLink>
                      </li>
                    ))}
                  </ul>
                </div>
              );
            })}
          </div>
          <div className="app-nav-footer">
            <NavLink
              to={itemConfiguracoes.to}
              className={({ isActive }) => `app-nav-link${isActive ? " app-nav-link-active" : ""}`}
              title={colapsada ? itemConfiguracoes.label : undefined}
            >
              <span className="app-nav-left">
                <NavIcon name={itemConfiguracoes.icon} />
                <span className="app-nav-label">{itemConfiguracoes.label}</span>
              </span>
            </NavLink>
          </div>
        </nav>
        <main className="app-content">{children}</main>
      </div>
    </div>
  );
}
