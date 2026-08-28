export type BootstrapEstado = {
  disponivel: boolean;
};

export type BootstrapUnidadeNegocioPayload = {
  id?: string;
  nome?: string;
  slug?: string;
};

export type BootstrapAdministradorPayload = {
  nome: string;
};

export type BootstrapConcluirResponse = {
  usuario: {
    id: string;
    email: string;
    nome: string;
  };
  unidadeNegocioId: string;
};
