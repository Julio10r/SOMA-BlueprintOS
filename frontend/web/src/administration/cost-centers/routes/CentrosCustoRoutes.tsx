import { Route, Routes } from "react-router-dom";
import { CentroCustoDetalhesPage } from "../pages/CentroCustoDetalhesPage";
import { CentroCustoEditarPage } from "../pages/CentroCustoEditarPage";
import { CentrosCustoPage } from "../pages/CentrosCustoPage";

/**
 * Nao ha rota de criacao ("novo"): Centro de Custo e um dado mestre
 * integrado do ERP e nunca e criado pelo +Compras (ADR-0020, item 3).
 */
export function CentrosCustoRoutes() {
  return (
    <Routes>
      <Route index element={<CentrosCustoPage />} />
      <Route path=":id" element={<CentroCustoDetalhesPage />} />
      <Route path=":id/editar" element={<CentroCustoEditarPage />} />
    </Routes>
  );
}
