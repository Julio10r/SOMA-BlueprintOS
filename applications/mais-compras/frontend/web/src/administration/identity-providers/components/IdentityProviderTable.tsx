import { StatusBadge } from "../../../shared/components/StatusBadge";
import type { IdentityProvider } from "../types/identityProviderTypes";

export function IdentityProviderTable({ providers, onEditar, onToggleStatus }: {
  providers: IdentityProvider[];
  onEditar: (provider: IdentityProvider) => void;
  onToggleStatus: (provider: IdentityProvider) => void;
}) {
  if (providers.length === 0) return <div className="empty-state">Nenhum Identity Provider cadastrado para esta Unidade de Negócio.</div>;
  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Tipo</th>
          <th>Domínios autorizados</th>
          <th>Configuração</th>
          <th>Status</th>
          <th>Ações</th>
        </tr>
      </thead>
      <tbody>
        {providers.map((provider) => (
          <tr key={provider.id}>
            <td>{provider.tipo}</td>
            <td>{provider.dominiosAutorizados.join(", ")}</td>
            <td>{provider.parametrosConfigurados ? "Já configurado" : "Não configurado"}</td>
            <td><StatusBadge value={provider.status} tone="situacao" /></td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(provider)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onToggleStatus(provider)}>
                  {provider.status === "Ativo" ? "Inativar" : "Ativar"}
                </button>
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
    </div>
  );
}
