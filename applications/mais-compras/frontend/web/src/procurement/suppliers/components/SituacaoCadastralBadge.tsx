import type { SituacaoCadastralCnpj } from "../types/linxSupplierContract";

type SituacaoCadastralSpec = { label: string; className: string };

/**
 * Mapping explicito situacao cadastral -> label/classe. Nunca deriva a classe
 * concatenando o valor recebido (ex. `status-${value.toLowerCase()}`): isso
 * exigiria que `value` fosse sempre string, o que ja causou o crash real
 * `value.toLowerCase is not a function` quando o backend enviava o codigo
 * numerico bruto do enum antes da B2.5. Situacao ausente (consulta com falha)
 * ou nao reconhecida cai no estado neutro "Desconhecida", sem lancar excecao.
 */
const specs: Record<SituacaoCadastralCnpj, SituacaoCadastralSpec> = {
  Ativa: { label: "Ativa", className: "status status-ativa" },
  Baixada: { label: "Baixada", className: "status status-baixada" },
  Suspensa: { label: "Suspensa", className: "status status-suspensa" },
  Inapta: { label: "Inapta", className: "status status-inapta" },
  Nula: { label: "Nula", className: "status status-nula" },
  Desconhecida: { label: "Desconhecida", className: "status status-desconhecida" }
};

const desconhecida: SituacaoCadastralSpec = { label: "Desconhecida", className: "status status-desconhecida" };

export function SituacaoCadastralBadge({ value }: { value: SituacaoCadastralCnpj | null | undefined }) {
  const spec = resolve(value);
  return <span className={spec.className}>{spec.label}</span>;
}

function resolve(value: SituacaoCadastralCnpj | null | undefined): SituacaoCadastralSpec {
  if (!value) return desconhecida;
  return specs[value] ?? desconhecida;
}
