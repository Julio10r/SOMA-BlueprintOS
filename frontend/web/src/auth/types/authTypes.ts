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
};

/** Codigos de permissao usados pela interface. Espelham PermissaoCatalogo no backend. */
export const PERMISSOES = {
  perfilGerenciar: "Perfil.Gerenciar",
  usuarioGerenciar: "Usuario.Gerenciar",
  filialGerenciar: "Filial.Gerenciar",
  centroCustoGerenciar: "CentroCusto.Gerenciar",
  unidadeAlocacaoGerenciar: "UnidadeAlocacao.Gerenciar"
} as const;
