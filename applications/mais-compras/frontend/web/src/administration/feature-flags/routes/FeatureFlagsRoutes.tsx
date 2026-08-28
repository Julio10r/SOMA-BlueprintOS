import { Route, Routes } from "react-router-dom";
import { FeatureFlagsPage } from "../pages/FeatureFlagsPage";

export function FeatureFlagsRoutes() {
  return (
    <Routes>
      <Route index element={<FeatureFlagsPage />} />
    </Routes>
  );
}
