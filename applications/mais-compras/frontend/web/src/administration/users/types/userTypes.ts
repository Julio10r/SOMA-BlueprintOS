export type StatusUsuario = "Ativo" | "Inativo";

/** Um Perfil vinculado ao Usuário, como devolvido por GET /administracao/usuarios (O1.6). */
export type UsuarioPerfilResumo = {
  id: string;
  nome: string;
  ativo: boolean;
};

/** Espelha UsuarioDto do backend (BlueprintOS.Application.Identity.Models). */
export type Usuario = {
  id: string;
  nome: string;
  email: string;
  unidadeNegocioId: string;
  ativo: boolean;
  perfis: UsuarioPerfilResumo[];
  centrosCusto: string[];
  todosCentrosCusto: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

/**
 * Entrada de criacao/edicao. Sem `unidadeNegocioId` de proposito (mesmo cuidado de
 * `PerfilInput`, O1.5): o backend usa sempre a Unidade de Negocio da sessao autenticada.
 * `perfis` e uma lista de Ids de Perfil — o vinculo em si (Nome/Ativo) e responsabilidade
 * exclusiva do backend.
 */
export type UsuarioInput = {
  nome: string;
  email: string;
  perfis: string[];
  centrosCusto: string[];
  todosCentrosCusto: boolean;
};

export function statusDoUsuario(usuario: Usuario): StatusUsuario {
  return usuario.ativo ? "Ativo" : "Inativo";
}
