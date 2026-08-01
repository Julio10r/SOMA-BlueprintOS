import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { CadastroFornecedor } from "./procurement/suppliers/CadastroFornecedor";
import "./styles.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <CadastroFornecedor />
  </StrictMode>
);
