export type UnidadeNegocio = {
  id: string;
  nome: string;
  slug: string;
  status: "Ativo" | "Inativo";
};

export type UnidadeNegocioCriarInput = {
  nome: string;
  slug: string;
};

export type UnidadeNegocioEditarInput = {
  nome: string;
};
