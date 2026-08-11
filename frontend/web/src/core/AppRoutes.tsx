import { Route, Routes } from "react-router-dom";
import { AppShell } from "./AppShell";
import { AuthRoutes } from "../auth/routes/AuthRoutes";
import { RequireAuth } from "../auth/components/RequireAuth";
import { BootstrapGate } from "../bootstrap/components/BootstrapGate";
import { BootstrapRoutes } from "../bootstrap/routes/BootstrapRoutes";
import { PerfisRoutes } from "../administration/profiles/routes/PerfisRoutes";
import { UsuariosRoutes } from "../administration/users/routes/UsuariosRoutes";
import { FiliaisRoutes } from "../administration/branches/routes/FiliaisRoutes";
import { CentrosCustoRoutes } from "../administration/cost-centers/routes/CentrosCustoRoutes";
import { UnidadesAlocacaoRoutes } from "../administration/allocation-units/routes/UnidadesAlocacaoRoutes";
import { Dashboard } from "../analytics/pages/Dashboard";
import { FornecedoresPage } from "../procurement/suppliers/pages/FornecedoresPage";
import { PedidosPage } from "../procurement/orders/pages/PedidosPage";
import { NegociacoesPage } from "../negotiations/pages/NegociacoesPage";
import { IndicadoresPage } from "../analytics/pages/IndicadoresPage";
import { AgentesIAPage } from "../ai/pages/AgentesIAPage";
import { ConfiguracoesPage } from "../settings/pages/ConfiguracoesPage";

/**
 * Rotas do Portal +Compras. Fornecedores, Perfis, Usuarios, Filiais, Centros
 * de Custo e Unidades de Alocacao (Administracao) tem integracao real ao
 * backend (BlueprintOS.Api, O1.5-O1.9); Pedidos, Negociacoes, Indicadores e
 * Agentes IA permanecem telas demonstrativas (mock data local) ate a
 * entrega de seus respectivos dominios (O1.2.2/D5, ADR-0021 — migracao
 * estrutural para Vertical Slice concluida na O1.10, sem alteracao de
 * comportamento funcional).
 */
export function AppRoutes() {
  return (
    <BootstrapGate>
      <Routes>
        <Route path="/bootstrap/*" element={<BootstrapRoutes />} />
        <Route path="/login/*" element={<AuthRoutes />} />
        <Route
          path="/*"
          element={
            <RequireAuth>
              <AppShell>
                <Routes>
                  <Route path="/" element={<Dashboard />} />
                  <Route path="/administracao/perfis/*" element={<PerfisRoutes />} />
                  <Route path="/administracao/usuarios/*" element={<UsuariosRoutes />} />
                  <Route path="/administracao/filiais/*" element={<FiliaisRoutes />} />
                  <Route path="/administracao/centros-custo/*" element={<CentrosCustoRoutes />} />
                  <Route path="/administracao/unidades-alocacao/*" element={<UnidadesAlocacaoRoutes />} />
                  <Route path="/fornecedores" element={<FornecedoresPage />} />
                  <Route path="/pedidos" element={<PedidosPage />} />
                  <Route path="/negociacoes" element={<NegociacoesPage />} />
                  <Route path="/indicadores" element={<IndicadoresPage />} />
                  <Route path="/agentes-ia" element={<AgentesIAPage />} />
                  <Route path="/configuracoes" element={<ConfiguracoesPage />} />
                </Routes>
              </AppShell>
            </RequireAuth>
          }
        />
      </Routes>
    </BootstrapGate>
  );
}
