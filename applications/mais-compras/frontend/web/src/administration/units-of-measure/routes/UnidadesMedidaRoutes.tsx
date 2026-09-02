import { Route, Routes } from "react-router-dom";
import { UnidadeMedidaDetalhesPage } from "../pages/UnidadeMedidaDetalhesPage";
import { UnidadeMedidaEditarPage } from "../pages/UnidadeMedidaEditarPage";
import { UnidadesMedidaPage } from "../pages/UnidadesMedidaPage";

/**
 * Nao ha rota de criacao ("novo"): Unidade de Medida e um cadastro de apoio integrado do ERP e nunca e
 * criada pelo +Compras.
 */
export function UnidadesMedidaRoutes() {
  return (
    <Routes>
      <Route index element={<UnidadesMedidaPage />} />
      <Route path=":id" element={<UnidadeMedidaDetalhesPage />} />
      <Route path=":id/editar" element={<UnidadeMedidaEditarPage />} />
    </Routes>
  );
}
