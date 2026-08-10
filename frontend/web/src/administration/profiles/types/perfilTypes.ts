export type StatusPerfil = "Ativo" | "Inativo";

export type Permissao = {
  id: string;
  recurso: string;
  acao: string;
  descricao: string;
};

export type Perfil = {
  id: string;
  nome: string;
  descricao: string;
  status: StatusPerfil;
  unidadeNegocio: string;
  permissoes: string[];
  usuariosVinculados: number;
  criadoEm: string;
  atualizadoEm: string;
};

export type PerfilInput = {
  nome: string;
  descricao: string;
  status: StatusPerfil;
  unidadeNegocio: string;
  permissoes: string[];
};
