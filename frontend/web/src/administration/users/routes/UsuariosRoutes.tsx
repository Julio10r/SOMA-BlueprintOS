import { Route, Routes } from "react-router-dom";
import { UsuarioDetalhesPage } from "../pages/UsuarioDetalhesPage";
import { UsuarioFormPage } from "../pages/UsuarioFormPage";
import { UsuariosPage } from "../pages/UsuariosPage";

export function UsuariosRoutes() {
  return (
    <Routes>
      <Route index element={<UsuariosPage />} />
      <Route path="novo" element={<UsuarioFormPage />} />
      <Route path=":id" element={<UsuarioDetalhesPage />} />
      <Route path=":id/editar" element={<UsuarioFormPage />} />
    </Routes>
  );
}
