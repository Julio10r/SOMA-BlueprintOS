import { Route, Routes } from "react-router-dom";
import { ItemFiscalDetalhesPage } from "../pages/ItemFiscalDetalhesPage";
import { ItemFiscalFormPage } from "../pages/ItemFiscalFormPage";
import { ItensFiscaisPage } from "../pages/ItensFiscaisPage";

export function ItensFiscaisRoutes() {
  return (
    <Routes>
      <Route index element={<ItensFiscaisPage />} />
      <Route path="novo" element={<ItemFiscalFormPage />} />
      <Route path=":id" element={<ItemFiscalDetalhesPage />} />
      <Route path=":id/editar" element={<ItemFiscalFormPage />} />
    </Routes>
  );
}
