/** Espelha SincronizacaoFornecedorResumoDto do backend (O1.13, Procurement.Suppliers.Models). */
export type SincronizacaoFornecedorResumo = {
  id: string;
  sistemaOrigem: string;
  businessUnit: string;
  dataInicio: string;
  dataFim: string | null;
  status: "Sucesso" | "Parcial" | "Erro";
  totalConsultado: number;
  totalIncluido: number;
  totalAtualizado: number;
  totalSemAlteracao: number;
  totalErro: number;
  tempoExecucaoMs: number;
};

/** Espelha ErroSincronizacaoFornecedorDto. StackTrace nunca chega ao frontend — apenas Mensagem sanitizada. */
export type ErroSincronizacaoFornecedor = {
  id: string;
  fornecedorIdentificacao: string | null;
  mensagem: string;
  dataHora: string;
};

/** Espelha SincronizacaoFornecedorDetalheDto. */
export type SincronizacaoFornecedorDetalhe = SincronizacaoFornecedorResumo & {
  erros: ErroSincronizacaoFornecedor[];
};

export type ListarSincronizacoesFiltro = {
  status?: string;
  businessUnit?: string;
  pagina?: number;
  tamanhoPagina?: number;
};

export type ListarSincronizacoesResultado = {
  itens: SincronizacaoFornecedorResumo[];
  totalRegistros: number;
  pagina: number;
  tamanhoPagina: number;
};

/** Espelha o histórico por fornecedor já exposto por FornecedorSyncController (B2.1.3), reaproveitado
 * aqui para a Auditoria por Fornecedor (#32). */
export type FornecedorSincronizacaoHistorico = {
  id: string;
  businessUnit: string;
  erpSistema: string;
  erpFornecedorId: string;
  fornecedorId: string | null;
  direcao: string;
  status: string;
  correlationId: string;
  executadaEm: string;
  mensagemErro: string | null;
  decisao: string;
  camposAlterados: string | null;
  tentativa: number;
  duracaoMs: number;
};

/** Espelha SincronizacaoFornecedoresErpResumo (retorno de GET /api/fornecedores/sincronizar-erp). */
export type DispararSincronizacaoErpResultado = {
  execucaoId: string;
  status: string;
  inicio: string;
  fim: string;
  consultados: number;
  incluidos: number;
  atualizados: number;
  semAlteracao: number;
  erros: number;
  duracaoMs: number;
  businessUnit: string;
  erpSistema: string;
  correlationId: string;
};
