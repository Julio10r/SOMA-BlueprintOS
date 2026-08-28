import { Route, Routes } from "react-router-dom";
import { LoginPage } from "../pages/LoginPage";

export function AuthRoutes() {
  return (
    <Routes>
      <Route path="/" element={<LoginPage />} />
    </Routes>
  );
}
