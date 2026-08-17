import { Link } from "react-router-dom";
import { StatusExecucaoBadge } from "./StatusExecucaoBadge";
import type { SincronizacaoFornecedorResumo } from "../types/monitoramentoTypes";

function formatarData(data: string | null): string {
  if (!data) return "—";
  return new Date(data).toLocaleString("pt-BR");
}

export function SincronizacoesFornecedoresTable({ itens }: { itens: SincronizacaoFornecedorResumo[] }) {
  if (itens.length === 0) {
    return <div className="empty-state">Nenhuma execução de sincronização de fornecedores encontrada.</div>;
  }

  return (
    <div className="table-scroll">
    <table className="divergence-table">
      <thead>
        <tr>
          <th>Sistema de Origem</th>
          <th>Unidade de Negócio</th>
          <th>Status</th>
          <th>Início</th>
          <th>Fim</th>
          <th>Incluídos</th>
          <th>Atualizados</th>
          <th>Erros</th>
          <th>Duração (ms)</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        {itens.map((execucao) => (
          <tr key={execucao.id}>
            <td>{execucao.sistemaOrigem}</td>
            <td>{execucao.businessUnit}</td>
            <td><StatusExecucaoBadge status={execucao.status} /></td>
            <td>{formatarData(execucao.dataInicio)}</td>
            <td>{formatarData(execucao.dataFim)}</td>
            <td>{execucao.totalIncluido}</td>
            <td>{execucao.totalAtualizado}</td>
            <td>{execucao.totalErro}</td>
            <td>{execucao.tempoExecucaoMs}</td>
            <td>
              <Link className="btn btn-secondary" to={`/administracao/monitoramento/${execucao.id}`}>
                Ver detalhe
              </Link>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
    </div>
  );
}
