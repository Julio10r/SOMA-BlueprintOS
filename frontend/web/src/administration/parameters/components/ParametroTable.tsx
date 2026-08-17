import type { Parametro } from "../types/parametroTypes";

export function ParametroTable({ parametros, onEditar, onExcluir }: {
  parametros: Parametro[];
  onEditar: (parametro: Parametro) => void;
  onExcluir: (parametro: Parametro) => void;
}) {
  if (parametros.length === 0) return <div className="empty-state">Nenhum parametro encontrado.</div>;
  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Chave</th>
          <th>Valor</th>
          <th>Descricao</th>
          <th>Ambito</th>
          <th>Acoes</th>
        </tr>
      </thead>
      <tbody>
        {parametros.map((parametro) => (
          <tr key={parametro.id}>
            <td>{parametro.chave}</td>
            <td>{parametro.valor}</td>
            <td>{parametro.descricao}</td>
            <td>{parametro.unidadeNegocioId ? "Por Unidade de Negocio" : "Global"}</td>
            <td>
              <div className="actions">
                <button type="button" className="btn btn-secondary" onClick={() => onEditar(parametro)}>
                  Editar
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => onExcluir(parametro)}>
                  Excluir
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
