import { Route, Routes } from "react-router-dom";
import { BootstrapPage } from "../pages/BootstrapPage";

export function BootstrapRoutes() {
  return (
    <Routes>
      <Route path="/" element={<BootstrapPage />} />
    </Routes>
  );
}
