import { Route, Routes } from "react-router-dom";
import { AppShell } from "./AppShell";
import { Dashboard } from "../pages/Dashboard/Dashboard";
import { FornecedoresPage } from "../pages/Fornecedores/FornecedoresPage";
import { PedidosPage } from "../pages/Pedidos/PedidosPage";
import { NegociacoesPage } from "../pages/Negociacoes/NegociacoesPage";
import { IndicadoresPage } from "../pages/Indicadores/IndicadoresPage";
import { AgentesIAPage } from "../pages/AgentesIA/AgentesIAPage";
import { ConfiguracoesPage } from "../pages/Configuracoes/ConfiguracoesPage";

/**
 * Rotas do Portal +Compras. Fornecedores e o unico modulo com integracao
 * real ao backend (BlueprintOS.Api); os demais sao telas demonstrativas
 * (mock data local) ate a entrega de seus respectivos dominios.
 */
export function AppRoutes() {
  return (
    <AppShell>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/fornecedores" element={<FornecedoresPage />} />
        <Route path="/pedidos" element={<PedidosPage />} />
        <Route path="/negociacoes" element={<NegociacoesPage />} />
        <Route path="/indicadores" element={<IndicadoresPage />} />
        <Route path="/agentes-ia" element={<AgentesIAPage />} />
        <Route path="/configuracoes" element={<ConfiguracoesPage />} />
      </Routes>
    </AppShell>
  );
}
