import { Route, Routes } from "react-router-dom";
import { AlcadasAprovacaoPage } from "../pages/AlcadasAprovacaoPage";

export function AlcadasAprovacaoRoutes() {
  return (
    <Routes>
      <Route index element={<AlcadasAprovacaoPage />} />
    </Routes>
  );
}
