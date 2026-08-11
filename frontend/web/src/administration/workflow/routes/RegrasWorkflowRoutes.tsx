import { Route, Routes } from "react-router-dom";
import { RegrasWorkflowPage } from "../pages/RegrasWorkflowPage";

export function RegrasWorkflowRoutes() {
  return (
    <Routes>
      <Route index element={<RegrasWorkflowPage />} />
    </Routes>
  );
}
