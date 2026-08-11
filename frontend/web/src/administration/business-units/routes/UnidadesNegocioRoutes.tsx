import { Route, Routes } from "react-router-dom";
import { UnidadeNegocioFormPage } from "../pages/UnidadeNegocioFormPage";
import { UnidadesNegocioPage } from "../pages/UnidadesNegocioPage";

export function UnidadesNegocioRoutes() {
  return (
    <Routes>
      <Route index element={<UnidadesNegocioPage />} />
      <Route path="novo" element={<UnidadeNegocioFormPage />} />
      <Route path=":id/editar" element={<UnidadeNegocioFormPage />} />
    </Routes>
  );
}
