import { Route, Routes } from "react-router-dom";
import { AuditoriaFornecedorPage } from "../pages/AuditoriaFornecedorPage";
import { MonitorIntegracoesPage } from "../pages/MonitorIntegracoesPage";
import { SincronizacaoDetalhesPage } from "../pages/SincronizacaoDetalhesPage";

export function OperationalMonitoringRoutes() {
  return (
    <Routes>
      <Route index element={<MonitorIntegracoesPage />} />
      <Route path=":id" element={<SincronizacaoDetalhesPage />} />
      <Route path="auditoria-fornecedor" element={<AuditoriaFornecedorPage />} />
    </Routes>
  );
}
