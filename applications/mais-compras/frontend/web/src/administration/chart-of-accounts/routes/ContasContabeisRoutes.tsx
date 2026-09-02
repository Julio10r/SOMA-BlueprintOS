import { Route, Routes } from "react-router-dom";
import { ContaContabilDetalhesPage } from "../pages/ContaContabilDetalhesPage";
import { ContaContabilEditarPage } from "../pages/ContaContabilEditarPage";
import { ContasContabeisPage } from "../pages/ContasContabeisPage";

/**
 * Nao ha rota de criacao ("novo"): Conta Contabil e um cadastro de apoio integrado do ERP e nunca e
 * criada pelo +Compras.
 */
export function ContasContabeisRoutes() {
  return (
    <Routes>
      <Route index element={<ContasContabeisPage />} />
      <Route path=":id" element={<ContaContabilDetalhesPage />} />
      <Route path=":id/editar" element={<ContaContabilEditarPage />} />
    </Routes>
  );
}
