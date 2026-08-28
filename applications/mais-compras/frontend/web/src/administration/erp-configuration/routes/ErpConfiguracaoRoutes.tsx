import { Route, Routes } from "react-router-dom";
import { ErpConfiguracaoPage } from "../pages/ErpConfiguracaoPage";

export function ErpConfiguracaoRoutes() {
  return (
    <Routes>
      <Route index element={<ErpConfiguracaoPage />} />
    </Routes>
  );
}
