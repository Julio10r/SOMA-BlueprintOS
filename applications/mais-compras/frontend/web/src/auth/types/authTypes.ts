export type UsuarioAutenticado = {
  id: string;
  email: string;
  nome: string;
  unidadeNegocioId: string;
  /**
   * Permissoes efetivas resolvidas no BACKEND (uniao dos Perfis ativos vinculados),
   * devolvidas por GET /auth/me a cada reidratacao de sessao (O1.5).
   *
   * Servem exclusivamente para a interface REFLETIR o acesso (esconder menu/acao). Nao
   * sao, e nunca devem ser, a fonte de autorizacao: cada endpoint protegido revalida a
   * permissao por policy no servidor, entao alterar esta lista no navegador nao concede
   * nenhum acesso.
   */
  permissoes: string[];
  /**
   * Escopo administrativo (Gate Final da Onda 1) — "Produto" (Administrador Sênior, cross-BU)
   * ou "Negocio" (administra somente a própria Unidade de Negócio). Exclusivamente informativo
   * para a interface refletir o acesso (ex.: exibir seletor de Unidade de Negócio); o backend
   * nunca confia neste valor de volta — cada endpoint administrativo revalida o escopo a partir
   * da própria sessão.
   */
  escopoAdministrativo: "Produto" | "Negocio";
};

export function ehAdministradorSenior(usuario: Pick<UsuarioAutenticado, "escopoAdministrativo">): boolean {
  return usuario.escopoAdministrativo === "Produto";
}

/** Codigos de permissao usados pela interface. Espelham PermissaoCatalogo no backend. */
export const PERMISSOES = {
  perfilGerenciar: "Perfil.Gerenciar",
  usuarioGerenciar: "Usuario.Gerenciar",
  filialGerenciar: "Filial.Gerenciar",
  centroCustoGerenciar: "CentroCusto.Gerenciar",
  unidadeAlocacaoGerenciar: "UnidadeAlocacao.Gerenciar",
  unidadeNegocioGerenciar: "UnidadeNegocio.Gerenciar",
  configuracaoErpGerenciar: "ConfiguracaoErp.Gerenciar",
  sistemaGerenciar: "Sistema.Gerenciar",
  workflowGerenciar: "Workflow.Gerenciar",
  alcadaGerenciar: "Alcada.Gerenciar",
  orcamentoGerenciar: "Orcamento.Gerenciar",
  fornecedorCriar: "Fornecedor.Criar",
  fornecedorEditar: "Fornecedor.Editar"
} as const;
