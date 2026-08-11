export type IdentityProvider = {
  id: string;
  unidadeNegocioId: string;
  tipo: string;
  dominiosAutorizados: string[];
  parametrosConfigurados: boolean;
  status: "Ativo" | "Inativo";
};

export type IdentityProviderInput = {
  tipo: string;
  dominiosAutorizados: string[];
  /** Nunca preenchido com o segredo real na edicao — vazio preserva o que ja esta salvo. */
  parametros?: string;
};
