import { Route, Routes } from "react-router-dom";
import { FilialDetalhesPage } from "../pages/FilialDetalhesPage";
import { FilialEditarPage } from "../pages/FilialEditarPage";
import { FiliaisPage } from "../pages/FiliaisPage";

/**
 * Nao ha rota de criacao ("novo"): Filial e um dado mestre integrado do
 * ERP e nunca e criada pelo +Compras (ADR-0020, item 3).
 */
export function FiliaisRoutes() {
  return (
    <Routes>
      <Route index element={<FiliaisPage />} />
      <Route path=":id" element={<FilialDetalhesPage />} />
      <Route path=":id/editar" element={<FilialEditarPage />} />
    </Routes>
  );
}
