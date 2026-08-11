import { Route, Routes } from "react-router-dom";
import { ConfiguracaoNotificacaoPage } from "../pages/ConfiguracaoNotificacaoPage";

export function ConfiguracaoNotificacaoRoutes() {
  return (
    <Routes>
      <Route index element={<ConfiguracaoNotificacaoPage />} />
    </Routes>
  );
}
