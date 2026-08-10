import { CadastroFornecedor } from "../components/CadastroFornecedor";

/**
 * Modulo Fornecedores: unica vertical slice com integracao real ao
 * backend (BlueprintOS.Api / Suppliers). O fluxo de cadastro + consulta de
 * CNPJ + comparacao de divergencias + aprovacao/rejeicao vive em
 * procurement/suppliers/CadastroFornecedor.tsx, componentizado em
 * src/components (SupplierCard, CnpjSearch, SupplierComparison,
 * ApprovalPanel, StatusBadge).
 */
export function FornecedoresPage() {
  return <CadastroFornecedor />;
}
