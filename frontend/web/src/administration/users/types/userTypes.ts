export type StatusUsuario = "Ativo" | "Inativo";

export type Usuario = {
  id: string;
  nome: string;
  email: string;
  status: StatusUsuario;
  perfis: string[];
  centrosCusto: string[];
  todosCentrosCusto: boolean;
  filiais: string[];
  unidadeNegocio: string;
  criadoEm: string;
  atualizadoEm: string;
};

export type UsuarioInput = {
  nome: string;
  email: string;
  status: StatusUsuario;
  perfis: string[];
  centrosCusto: string[];
  todosCentrosCusto: boolean;
  filiais: string[];
  unidadeNegocio: string;
};
