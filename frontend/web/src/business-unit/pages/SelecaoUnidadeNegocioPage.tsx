import { UnidadeNegocioCard } from "../components/UnidadeNegocioCard";
import type { UnidadeNegocioSelecionavel } from "../types/unidadeNegocioSelecaoTypes";

/**
 * Selecao da Unidade de Negocio pos-login (O1.11). So e exibida quando o usuario possui mais de uma
 * Unidade de Negocio disponivel — hoje o sistema e single-BU-por-usuario, entao esta tela nunca aparece
 * em producao, mas a interface a implementa integralmente para o caso futuro (ver BusinessUnitGate).
 */
export function SelecaoUnidadeNegocioPage({ unidades, onSelecionar }: {
  unidades: UnidadeNegocioSelecionavel[];
  onSelecionar: (unidadeNegocio: UnidadeNegocioSelecionavel) => void;
}) {
  return (
    <div className="auth-page">
      <div className="page-stack">
        <header className="page-header">
          <h1>Selecione a Unidade de Negocio</h1>
          <p>Escolha a Unidade de Negocio com a qual deseja trabalhar nesta sessao.</p>
        </header>
        <div className="unidade-negocio-card-grid">
          {unidades.map((unidadeNegocio) => (
            <UnidadeNegocioCard key={unidadeNegocio.id} unidadeNegocio={unidadeNegocio} onSelecionar={onSelecionar} />
          ))}
        </div>
      </div>
    </div>
  );
}
