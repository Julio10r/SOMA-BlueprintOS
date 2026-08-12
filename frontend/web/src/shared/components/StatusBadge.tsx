import type { FornecedorCampoDecisao } from "../../procurement/suppliers/types/linxSupplierContract";

/**
 * Badge visual generico. Usado pelas telas de Administracao (Ativo/Inativo de
 * Filial, Centro de Custo, Perfil, Usuario, etc. via tone="situacao") e pelo
 * status de decisao de uma divergencia de enriquecimento de CNPJ
 * (Pendente/Aceito/Rejeitado via tone="decisao"). Nao contem regra de negocio:
 * apenas mapeia o valor recebido para as classes de status ja definidas no
 * design system (colors_and_type.css).
 *
 * A situacao cadastral do CNPJ (Ativa/Baixada/Suspensa/Inapta/Nula/Desconhecida)
 * NAO usa este componente — usa SituacaoCadastralBadge, que tem mapping proprio
 * e nao depende de `.toLowerCase()` sobre um valor externo (ver B2.5).
 */
export function StatusBadge({ value, tone = "decisao" }: {
  value: FornecedorCampoDecisao | string;
  tone?: "situacao" | "decisao";
}) {
  const className = tone === "situacao" ? `status status-${value.toLowerCase()}` : `badge ${decisaoClass(value)}`;
  return <span className={className}>{value}</span>;
}

function decisaoClass(value: string): string {
  if (value === "Aceito") return "badge-aceito";
  if (value === "Rejeitado") return "badge-rejeitado";
  return "";
}
