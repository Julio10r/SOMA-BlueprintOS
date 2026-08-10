import type { Fornecedor } from "../types/linxSupplierContract";

/**
 * Exibicao resumida de um fornecedor ja cadastrado no +Compras.
 * Puramente apresentacional: recebe o contrato Fornecedor (linxSupplierContract.ts)
 * e nao realiza chamadas de API nem decisoes de negocio.
 */
export function SupplierCard({ supplier }: { supplier: Fornecedor }) {
  const localizacao = [supplier.cidade, supplier.estado].filter(Boolean).join(" / ") || "Nao informado";
  return (
    <div className="card supplier-card">
      <div className="card-heading">
        <div>
          <div className="section-title">Fornecedor</div>
          <h2>{supplier.razaoSocial}</h2>
          {supplier.nomeFantasia && <p className="caption">{supplier.nomeFantasia}</p>}
        </div>
      </div>
      <div className="data-grid supplier-card-grid">
        <div className="field-readonly">
          <span>Cnpj_Cpf</span>
          <strong className="mono">{supplier.cnpj_Cpf}</strong>
        </div>
        <div className="field-readonly">
          <span>Tipo de pessoa</span>
          <strong>{supplier.tipoPessoa || "Nao informado"}</strong>
        </div>
        <div className="field-readonly">
          <span>Localizacao</span>
          <strong>{localizacao}</strong>
        </div>
        <div className="field-readonly">
          <span>Contato</span>
          <strong>{supplier.email || supplier.telefone || "Nao informado"}</strong>
        </div>
      </div>
    </div>
  );
}
