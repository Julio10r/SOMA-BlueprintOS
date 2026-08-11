import type { UnidadeNegocio } from "../../business-units/types/unidadeNegocioTypes";
import type { FeatureFlag } from "../types/featureFlagTypes";

export function FeatureFlagTable({ flags, unidadesNegocio, onAlterarStatus }: {
  flags: FeatureFlag[];
  unidadesNegocio: UnidadeNegocio[];
  onAlterarStatus: (flagId: string, unidadeNegocioId: string, ativa: boolean) => void;
}) {
  if (flags.length === 0) {
    return <div className="empty-state">Nenhuma feature flag cadastrada.</div>;
  }

  return (
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Nome</th>
          <th>Descricao</th>
          {unidadesNegocio.map((un) => (
            <th key={un.id}>{un.nome}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {flags.map((flag) => (
          <tr key={flag.id}>
            <td>{flag.nome}</td>
            <td>{flag.descricao}</td>
            {unidadesNegocio.map((un) => {
              const statusUn = flag.status.find((s) => s.unidadeNegocioId === un.id);
              const ativa = statusUn?.ativa ?? false;
              return (
                <td key={un.id}>
                  <label className="toggle">
                    <input
                      type="checkbox"
                      checked={ativa}
                      onChange={(e) => onAlterarStatus(flag.id, un.id, e.target.checked)}
                    />
                  </label>
                </td>
              );
            })}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
