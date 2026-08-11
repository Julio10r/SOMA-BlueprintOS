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
import { UnidadesNegocioRoutes } from "../administration/business-units/routes/UnidadesNegocioRoutes";
import { IdentityProvidersRoutes } from "../administration/identity-providers/routes/IdentityProvidersRoutes";
import { ErpConfiguracaoRoutes } from "../administration/erp-configuration/routes/ErpConfiguracaoRoutes";
import { ParametrosRoutes } from "../administration/parameters/routes/ParametrosRoutes";
import { FeatureFlagsRoutes } from "../administration/feature-flags/routes/FeatureFlagsRoutes";
import { ConfiguracaoNotificacaoRoutes } from "../administration/notification-configuration/routes/ConfiguracaoNotificacaoRoutes";
import { BusinessUnitGate } from "../business-unit/components/BusinessUnitGate";
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
              <BusinessUnitGate>
                <AppShell>
                  <Routes>
                    <Route path="/" element={<Dashboard />} />
                    <Route path="/administracao/perfis/*" element={<PerfisRoutes />} />
                    <Route path="/administracao/usuarios/*" element={<UsuariosRoutes />} />
                    <Route path="/administracao/filiais/*" element={<FiliaisRoutes />} />
                    <Route path="/administracao/centros-custo/*" element={<CentrosCustoRoutes />} />
                    <Route path="/administracao/unidades-alocacao/*" element={<UnidadesAlocacaoRoutes />} />
                    <Route path="/administracao/unidades-negocio/*" element={<UnidadesNegocioRoutes />} />
                    <Route path="/administracao/identity-providers/*" element={<IdentityProvidersRoutes />} />
                    <Route path="/administracao/configuracao-erp/*" element={<ErpConfiguracaoRoutes />} />
                    <Route path="/administracao/parametros/*" element={<ParametrosRoutes />} />
                    <Route path="/administracao/feature-flags/*" element={<FeatureFlagsRoutes />} />
                    <Route path="/administracao/configuracao-notificacao/*" element={<ConfiguracaoNotificacaoRoutes />} />
                    <Route path="/fornecedores" element={<FornecedoresPage />} />
                    <Route path="/pedidos" element={<PedidosPage />} />
                    <Route path="/negociacoes" element={<NegociacoesPage />} />
                    <Route path="/indicadores" element={<IndicadoresPage />} />
                    <Route path="/agentes-ia" element={<AgentesIAPage />} />
                    <Route path="/configuracoes" element={<ConfiguracoesPage />} />
                  </Routes>
                </AppShell>
              </BusinessUnitGate>
            </RequireAuth>
          }
        />
      </Routes>
    </BootstrapGate>
  );
}
