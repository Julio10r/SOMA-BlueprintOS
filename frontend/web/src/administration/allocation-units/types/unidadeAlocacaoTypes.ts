export type StatusUnidadeAlocacao = "Ativo" | "Inativo";

/**
 * Unidade de Alocacao (Gestao de Unidades de Alocacao, ADR-0020 item 4/5).
 * Pertence exclusivamente ao +Compras: nunca e integrada do ERP, ao
 * contrario de Filial e Centro de Custo. Pode ser usada por diversos
 * Centros de Custo (relacionamento N:N previsto pela ADR-0020, item 5;
 * ainda nao implementado nesta etapa (O1.9), apenas o cadastro real da
 * Unidade de Alocacao em si, com persistencia real (O1.8)).
 *
 * `unidadeNegocioId` e sempre resolvido pelo backend a partir da sessao
 * autenticada — nunca enviado pelo cliente em Create/Update (mesmo cuidado
 * de Usuario/Perfil).
 */
export type UnidadeAlocacao = {
  id: string;
  nome: string;
  descricao: string;
  unidadeNegocioId: string;
  status: StatusUnidadeAlocacao;
  criadoEm: string;
  atualizadoEm: string;
};

export type UnidadeAlocacaoInput = {
  nome: string;
  descricao: string;
};
