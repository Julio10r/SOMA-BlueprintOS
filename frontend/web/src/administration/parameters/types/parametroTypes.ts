export type Parametro = {
  id: string;
  chave: string;
  valor: string;
  descricao: string;
  unidadeNegocioId: string | null;
};

export type ParametroCriarInput = {
  chave: string;
  valor: string;
  descricao: string;
  unidadeNegocioId?: string;
};

export type ParametroAtualizarInput = {
  valor: string;
  descricao: string;
};
