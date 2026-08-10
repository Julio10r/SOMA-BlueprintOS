import type { Usuario, UsuarioInput } from "../types/userTypes";

/**
 * Fundacao visual do modulo Gestao de Usuarios (Sprint O1.3.2, ADR-0020).
 * Dados mockados em memoria: nenhuma chamada de API, nenhuma persistencia
 * definitiva. Simula latencia de rede para exercitar os estados de
 * carregamento da interface, seguindo o mesmo padrao de administration/profiles.
 *
 * Usuarios nunca sao excluidos fisicamente: permanecem auditaveis e apenas
 * transitam entre Ativo/Inativo (mesmo padrao de Filiais, Centros de Custo
 * e Unidades de Alocacao).
 */
let usuarios: Usuario[] = [
  {
    id: "usuario-ana-souza",
    nome: "Ana Souza",
    email: "ana.souza@somagrupo.com.br",
    status: "Ativo",
    perfis: ["perfil-admin-senior"],
    centrosCusto: [],
    todosCentrosCusto: true,
    filiais: [],
    unidadeNegocio: "SOMA",
    criadoEm: "2026-07-15T09:00:00Z",
    atualizadoEm: "2026-08-01T10:00:00Z"
  },
  {
    id: "usuario-bruno-lima",
    nome: "Bruno Lima",
    email: "bruno.lima@somagrupo.com.br",
    status: "Ativo",
    perfis: ["perfil-analista"],
    centrosCusto: ["cc-001", "cc-002"],
    todosCentrosCusto: false,
    filiais: [],
    unidadeNegocio: "SOMA",
    criadoEm: "2026-07-20T09:00:00Z",
    atualizadoEm: "2026-07-20T09:00:00Z"
  },
  {
    id: "usuario-carla-mendes",
    nome: "Carla Mendes",
    email: "carla.mendes@somagrupo.com.br",
    status: "Ativo",
    perfis: ["perfil-analista-jr"],
    centrosCusto: ["cc-003"],
    todosCentrosCusto: false,
    filiais: [],
    unidadeNegocio: "SOMA",
    criadoEm: "2026-07-22T09:00:00Z",
    atualizadoEm: "2026-07-22T09:00:00Z"
  },
  {
    id: "usuario-diego-alves",
    nome: "Diego Alves",
    email: "diego.alves@somagrupo.com.br",
    status: "Inativo",
    perfis: ["perfil-auditoria"],
    centrosCusto: [],
    todosCentrosCusto: false,
    filiais: [],
    unidadeNegocio: "SOMA",
    criadoEm: "2026-06-10T09:00:00Z",
    atualizadoEm: "2026-06-10T09:00:00Z"
  }
];

const LATENCY_MS = 250;

function delay<T>(value: T): Promise<T> {
  return new Promise((resolve) => setTimeout(() => resolve(value), LATENCY_MS));
}

export async function listUsuarios(): Promise<Usuario[]> {
  return delay([...usuarios]);
}

export async function getUsuario(id: string): Promise<Usuario | null> {
  return delay(usuarios.find((usuario) => usuario.id === id) ?? null);
}

export async function createUsuario(input: UsuarioInput): Promise<Usuario> {
  if (usuarios.some((usuario) => usuario.email.toLowerCase() === input.email.toLowerCase())) {
    throw new Error("Ja existe um usuario com este e-mail.");
  }
  const now = new Date().toISOString();
  const created: Usuario = {
    id: `usuario-${crypto.randomUUID()}`,
    ...input,
    criadoEm: now,
    atualizadoEm: now
  };
  usuarios = [...usuarios, created];
  return delay(created);
}

export async function updateUsuario(id: string, input: UsuarioInput): Promise<Usuario> {
  const existing = usuarios.find((usuario) => usuario.id === id);
  if (!existing) throw new Error("Usuario nao encontrado.");
  if (usuarios.some((usuario) => usuario.id !== id && usuario.email.toLowerCase() === input.email.toLowerCase())) {
    throw new Error("Ja existe um usuario com este e-mail.");
  }
  const updated: Usuario = { ...existing, ...input, atualizadoEm: new Date().toISOString() };
  usuarios = usuarios.map((usuario) => (usuario.id === id ? updated : usuario));
  return delay(updated);
}

export async function setStatusUsuario(id: string, status: Usuario["status"]): Promise<Usuario> {
  const existing = usuarios.find((usuario) => usuario.id === id);
  if (!existing) throw new Error("Usuario nao encontrado.");
  const updated: Usuario = { ...existing, status, atualizadoEm: new Date().toISOString() };
  usuarios = usuarios.map((usuario) => (usuario.id === id ? updated : usuario));
  return delay(updated);
}
