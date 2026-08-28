import { Route, Routes } from "react-router-dom";
import { ParametroFormPage } from "../pages/ParametroFormPage";
import { ParametrosPage } from "../pages/ParametrosPage";

export function ParametrosRoutes() {
  return (
    <Routes>
      <Route index element={<ParametrosPage />} />
      <Route path="novo" element={<ParametroFormPage />} />
      <Route path=":id/editar" element={<ParametroFormPage />} />
    </Routes>
  );
}
