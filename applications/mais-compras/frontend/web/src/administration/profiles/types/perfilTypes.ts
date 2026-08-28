export type StatusPerfil = "Ativo" | "Inativo";

/**
 * Uma permissao do catalogo global, como devolvida por GET /administracao/permissoes.
 * O `codigo` (formato `Recurso.Acao`) e a unica identidade da permissao; `recurso`,
 * `acao` e `descricao` existem apenas para agrupamento e apresentacao.
 */
export type Permissao = {
  codigo: string;
  recurso: string;
  acao: string;
  descricao: string;
};

/** Espelha PerfilDto do backend (BlueprintOS.Application.Identity.Models). */
export type Perfil = {
  id: string;
  nome: string;
  descricao: string;
  unidadeNegocioId: string;
  ativo: boolean;
  permissoes: string[];
  usuariosVinculados: number;
  criadoEm: string;
  atualizadoEm: string;
};

/**
 * Entrada de criacao/edicao. Sem `unidadeNegocioId` de proposito: o backend usa
 * sempre a Unidade de Negocio da sessao autenticada e ignora qualquer valor que o
 * cliente tentasse enviar.
 */
export type PerfilInput = {
  nome: string;
  descricao: string;
  permissoes: string[];
};

export function statusDoPerfil(perfil: Perfil): StatusPerfil {
  return perfil.ativo ? "Ativo" : "Inativo";
}
