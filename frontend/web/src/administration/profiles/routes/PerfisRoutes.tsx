import { Route, Routes } from "react-router-dom";
import { PerfilDetalhesPage } from "../pages/PerfilDetalhesPage";
import { PerfilFormPage } from "../pages/PerfilFormPage";
import { PerfisPage } from "../pages/PerfisPage";

export function PerfisRoutes() {
  return (
    <Routes>
      <Route index element={<PerfisPage />} />
      <Route path="novo" element={<PerfilFormPage />} />
      <Route path=":id" element={<PerfilDetalhesPage />} />
      <Route path=":id/editar" element={<PerfilFormPage />} />
    </Routes>
  );
}
