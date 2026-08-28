import { useNavigate } from "react-router-dom";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { labelStatusSincronizacao, type Fornecedor } from "../types/linxSupplierContract";

function statusDoFornecedor(fornecedor: Fornecedor): "Ativo" | "Inativo" {
  return fornecedor.status === "Inativo" ? "Inativo" : "Ativo";
}

/**
 * Tabela paginada da listagem de Fornecedores. Clicar em uma linha navega para o detalhe
 * (`/fornecedores/:id`); a querystring da listagem (busca/filtro/página) é preservada porque
 * ela vive na URL da própria listagem, não em estado local perdido na navegação.
 */
export function FornecedorTable({
  fornecedores,
  onToggleAtivo,
  toQueryString
}: {
  fornecedores: Fornecedor[];
  onToggleAtivo: (fornecedor: Fornecedor) => void;
  toQueryString: string;
}) {
  const navigate = useNavigate();

  return (
    <div className="table-scroll">
      <table className="divergence-table">
        <thead>
          <tr>
            <th>Fornecedor</th>
            <th>CNPJ</th>
            <th>Status +Compras</th>
            <th>Status sincronização ERP</th>
            <th>Ações</th>
          </tr>
        </thead>
        <tbody>
          {fornecedores.map((fornecedor) => {
            const status = statusDoFornecedor(fornecedor);
            return (
              <tr
                key={fornecedor.id}
                className="table-row-clickable"
                onClick={() => navigate(`/fornecedores/${fornecedor.id}${toQueryString}`)}
              >
                <td>
                  <strong>{fornecedor.razaoSocial}</strong>
                  {fornecedor.nomeFantasia ? <div className="table-subtext">{fornecedor.nomeFantasia}</div> : null}
                </td>
                <td>{fornecedor.cnpj_Cpf}</td>
                <td><StatusBadge value={status} tone="situacao" /></td>
                <td><StatusBadge value={labelStatusSincronizacao(fornecedor.statusSincronizacao)} tone="situacao" /></td>
                <td>
                  <div className="actions" onClick={(event) => event.stopPropagation()}>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      onClick={() => navigate(`/fornecedores/${fornecedor.id}${toQueryString}`)}
                    >
                      Ver
                    </button>
                    <button type="button" className="btn btn-secondary" onClick={() => onToggleAtivo(fornecedor)}>
                      {status === "Ativo" ? "Inativar" : "Ativar"}
                    </button>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
