/**
 * O1.11, item #24 — Configuracao de Notificacoes por Unidade de Negocio. ESCOPO MINIMO DE FUNDACAO
 * (decisao formal do Product Owner): apenas o canal e-mail (ativado/inativado, remetente, nome do
 * remetente). Sem catalogo de eventos configuraveis nesta sprint — nao ha documentacao formal aprovada
 * com o conjunto de eventos; sera endereçado quando os workflows operacionais correspondentes existirem.
 */
export type ConfiguracaoNotificacao = {
  id: string;
  unidadeNegocioId: string;
  emailAtivado: boolean;
  emailRemetente: string | null;
  nomeRemetente: string | null;
};

export type ConfiguracaoNotificacaoInput = {
  emailAtivado: boolean;
  emailRemetente?: string;
  nomeRemetente?: string;
};
