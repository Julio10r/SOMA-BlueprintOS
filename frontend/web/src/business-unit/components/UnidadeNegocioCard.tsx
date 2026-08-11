import type { UnidadeNegocioSelecionavel } from "../types/unidadeNegocioSelecaoTypes";

/** Card de selecao de Unidade de Negocio, reaproveitando o padrao visual `card auth-card` do Login. */
export function UnidadeNegocioCard({ unidadeNegocio, onSelecionar }: {
  unidadeNegocio: UnidadeNegocioSelecionavel;
  onSelecionar: (unidadeNegocio: UnidadeNegocioSelecionavel) => void;
}) {
  return (
    <button
      type="button"
      className="card auth-card unidade-negocio-card"
      onClick={() => onSelecionar(unidadeNegocio)}
      disabled={!unidadeNegocio.ativa}
    >
      <h2>{unidadeNegocio.nome}</h2>
      <p>{unidadeNegocio.slug}</p>
    </button>
  );
}
