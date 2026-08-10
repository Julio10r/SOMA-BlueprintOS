import type { FornecedorCampoDecisao, SituacaoCadastralCnpj } from "../../procurement/suppliers/types/linxSupplierContract";

/**
 * Badge visual generico. Usado tanto para a situacao cadastral retornada
 * pela consulta de CNPJ (Ativa/Baixada/Suspensa/Inapta/NaoEncontrada) quanto
 * para o status de decisao de uma divergencia (Pendente/Aceito/Rejeitado).
 * Nao contem regra de negocio: apenas mapeia o valor recebido para as
 * classes de status ja definidas no design system (colors_and_type.css).
 */
export function StatusBadge({ value, tone = "auto" }: {
  value: SituacaoCadastralCnpj | FornecedorCampoDecisao | string;
  tone?: "situacao" | "decisao" | "auto";
}) {
  const resolvedTone = tone === "auto" ? inferTone(value) : tone;
  const className = resolvedTone === "situacao" ? `status status-${value.toLowerCase()}` : `badge ${decisaoClass(value)}`;
  return <span className={className}>{value}</span>;
}

function inferTone(value: string): "situacao" | "decisao" {
  const situacoes = new Set(["Ativa", "Baixada", "Suspensa", "Inapta", "NaoEncontrada"]);
  return situacoes.has(value) ? "situacao" : "decisao";
}

function decisaoClass(value: string): string {
  if (value === "Aceito") return "badge-aceito";
  if (value === "Rejeitado") return "badge-rejeitado";
  return "";
}
