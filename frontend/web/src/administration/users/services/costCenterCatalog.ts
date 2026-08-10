/**
 * Recorte mockado do cadastro integrado de Centro de Custo (ADR-0020: cadastro
 * integrado do ERP, imutavel no +Compras). O cadastro mestre real ainda nao
 * existe (modulo administration/cost-centers e esqueleto vazio); este
 * catalogo serve apenas para exercitar a selecao de acesso do usuario.
 */
export type CentroCustoOption = {
  id: string;
  codigo: string;
  descricao: string;
};

export const costCenterCatalog: CentroCustoOption[] = [
  { id: "cc-001", codigo: "CC-001", descricao: "Compras Corporativo" },
  { id: "cc-002", codigo: "CC-002", descricao: "Logistica e Distribuicao" },
  { id: "cc-003", codigo: "CC-003", descricao: "Varejo Fisico" },
  { id: "cc-004", codigo: "CC-004", descricao: "E-commerce" },
  { id: "cc-005", codigo: "CC-005", descricao: "Marketing" }
];
