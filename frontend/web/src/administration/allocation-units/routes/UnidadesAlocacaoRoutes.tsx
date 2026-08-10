import { Route, Routes } from "react-router-dom";
import { UnidadeAlocacaoDetalhesPage } from "../pages/UnidadeAlocacaoDetalhesPage";
import { UnidadeAlocacaoFormPage } from "../pages/UnidadeAlocacaoFormPage";
import { UnidadesAlocacaoPage } from "../pages/UnidadesAlocacaoPage";

export function UnidadesAlocacaoRoutes() {
  return (
    <Routes>
      <Route index element={<UnidadesAlocacaoPage />} />
      <Route path="novo" element={<UnidadeAlocacaoFormPage />} />
      <Route path=":id" element={<UnidadeAlocacaoDetalhesPage />} />
      <Route path=":id/editar" element={<UnidadeAlocacaoFormPage />} />
    </Routes>
  );
}
