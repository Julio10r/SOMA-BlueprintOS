import { Route, Routes } from "react-router-dom";
import { RegrasOrcamentariasPage } from "../pages/RegrasOrcamentariasPage";

export function RegrasOrcamentariasRoutes() {
  return (
    <Routes>
      <Route index element={<RegrasOrcamentariasPage />} />
    </Routes>
  );
}
