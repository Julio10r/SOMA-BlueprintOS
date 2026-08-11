export type ConfiguracaoErp = {
  id: string;
  unidadeNegocioId: string;
  sistemaErp: string;
  parametrosConfigurados: boolean;
  status: "Ativo" | "Inativo";
};

export type ConfiguracaoErpInput = {
  sistemaErp: string;
  /** Nunca preenchido com o segredo real na edicao — vazio preserva o que ja esta salvo. */
  parametrosConexao?: string;
};
