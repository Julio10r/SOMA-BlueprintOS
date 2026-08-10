import type { Permissao } from "../types/perfilTypes";

/**
 * Catalogo estatico de permissoes disponiveis (ADR-0020, item 8: permissoes
 * pertencem exclusivamente a Perfis). Conteudo definitivo do catalogo
 * permanece pendencia de produto registrada em PROJECT_STATE.md; este
 * conjunto cobre os dominios ja funcionais/planejados da Onda 1.
 */
export const permissionCatalog: Permissao[] = [
  { id: "fornecedores.criar", recurso: "Fornecedores", acao: "Criar", descricao: "Cadastrar novo fornecedor" },
  { id: "fornecedores.editar", recurso: "Fornecedores", acao: "Editar", descricao: "Atualizar dados de fornecedor" },
  { id: "fornecedores.aprovar", recurso: "Fornecedores", acao: "Aprovar", descricao: "Aprovar divergencias de enriquecimento" },
  { id: "pedidos.criar", recurso: "Pedidos", acao: "Criar", descricao: "Criar pedido de compra" },
  { id: "pedidos.aprovar", recurso: "Pedidos", acao: "Aprovar", descricao: "Aprovar pedido de compra" },
  { id: "pedidos.cancelar", recurso: "Pedidos", acao: "Cancelar", descricao: "Cancelar pedido de compra" },
  { id: "perfis.gerenciar", recurso: "Perfis", acao: "Gerenciar", descricao: "Criar, editar e excluir perfis" },
  { id: "usuarios.gerenciar", recurso: "Usuarios", acao: "Gerenciar", descricao: "Vincular perfis e centros de custo a usuarios" },
  { id: "centros-custo.gerenciar", recurso: "Centros de Custo", acao: "Gerenciar", descricao: "Ativar/inativar centros de custo no +Compras" }
];

export function groupPermissionsByRecurso(permissoes: Permissao[]): Array<[string, Permissao[]]> {
  const groups = new Map<string, Permissao[]>();
  for (const permissao of permissoes) {
    const group = groups.get(permissao.recurso) ?? [];
    group.push(permissao);
    groups.set(permissao.recurso, group);
  }
  return Array.from(groups.entries());
}
