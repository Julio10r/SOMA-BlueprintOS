import { Route, Routes } from "react-router-dom";
import { IdentityProvidersPage } from "../pages/IdentityProvidersPage";

export function IdentityProvidersRoutes() {
  return (
    <Routes>
      <Route index element={<IdentityProvidersPage />} />
    </Routes>
  );
}
