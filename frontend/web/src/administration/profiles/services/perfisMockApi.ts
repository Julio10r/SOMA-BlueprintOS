import type { Perfil, PerfilInput } from "../types/perfilTypes";

/**
 * Fundacao visual do modulo Gestao de Perfis (Sprint O1.2.2, ADR-0020).
 * Dados mockados em memoria: nenhuma chamada de API, nenhuma persistencia
 * definitiva. Simula latencia de rede para exercitar os estados de
 * carregamento da interface.
 */
let perfis: Perfil[] = [
  {
    id: "perfil-admin-senior",
    nome: "Administrador Senior",
    descricao: "Acesso total a administracao e operacao do +Compras. Perfil exigido pelo Bootstrap Mode.",
    status: "Ativo",
    unidadeNegocio: "SOMA",
    permissoes: ["fornecedores.criar", "fornecedores.editar", "fornecedores.aprovar", "pedidos.criar", "pedidos.aprovar", "pedidos.cancelar", "perfis.gerenciar", "usuarios.gerenciar", "centros-custo.gerenciar"],
    usuariosVinculados: 2,
    criadoEm: "2026-07-15T09:00:00Z",
    atualizadoEm: "2026-08-01T10:00:00Z"
  },
  {
    id: "perfil-analista",
    nome: "Analista",
    descricao: "Cria, aprova e cancela pedidos de compra do dia a dia.",
    status: "Ativo",
    unidadeNegocio: "SOMA",
    permissoes: ["pedidos.criar", "pedidos.aprovar", "pedidos.cancelar", "fornecedores.criar"],
    usuariosVinculados: 5,
    criadoEm: "2026-07-20T09:00:00Z",
    atualizadoEm: "2026-07-20T09:00:00Z"
  },
  {
    id: "perfil-analista-jr",
    nome: "Analista Jr",
    descricao: "Somente cria pedidos de compra, sem alcada de aprovacao.",
    status: "Ativo",
    unidadeNegocio: "SOMA",
    permissoes: ["pedidos.criar"],
    usuariosVinculados: 8,
    criadoEm: "2026-07-22T09:00:00Z",
    atualizadoEm: "2026-07-22T09:00:00Z"
  },
  {
    id: "perfil-auditoria",
    nome: "Auditoria",
    descricao: "Perfil somente leitura para acompanhamento de conformidade.",
    status: "Inativo",
    unidadeNegocio: "SOMA",
    permissoes: [],
    usuariosVinculados: 0,
    criadoEm: "2026-06-10T09:00:00Z",
    atualizadoEm: "2026-06-10T09:00:00Z"
  }
];

const LATENCY_MS = 250;

function delay<T>(value: T): Promise<T> {
  return new Promise((resolve) => setTimeout(() => resolve(value), LATENCY_MS));
}

export async function listPerfis(): Promise<Perfil[]> {
  return delay([...perfis]);
}

export async function getPerfil(id: string): Promise<Perfil | null> {
  return delay(perfis.find((perfil) => perfil.id === id) ?? null);
}

export async function createPerfil(input: PerfilInput): Promise<Perfil> {
  if (perfis.some((perfil) => perfil.nome.toLowerCase() === input.nome.toLowerCase())) {
    throw new Error("Ja existe um perfil com este nome.");
  }
  const now = new Date().toISOString();
  const created: Perfil = {
    id: `perfil-${crypto.randomUUID()}`,
    ...input,
    usuariosVinculados: 0,
    criadoEm: now,
    atualizadoEm: now
  };
  perfis = [...perfis, created];
  return delay(created);
}

export async function updatePerfil(id: string, input: PerfilInput): Promise<Perfil> {
  const existing = perfis.find((perfil) => perfil.id === id);
  if (!existing) throw new Error("Perfil nao encontrado.");
  if (perfis.some((perfil) => perfil.id !== id && perfil.nome.toLowerCase() === input.nome.toLowerCase())) {
    throw new Error("Ja existe um perfil com este nome.");
  }
  const updated: Perfil = { ...existing, ...input, atualizadoEm: new Date().toISOString() };
  perfis = perfis.map((perfil) => (perfil.id === id ? updated : perfil));
  return delay(updated);
}

export async function deletePerfil(id: string): Promise<void> {
  const existing = perfis.find((perfil) => perfil.id === id);
  if (!existing) throw new Error("Perfil nao encontrado.");
  if (existing.usuariosVinculados > 0) {
    throw new Error("Nao e possivel excluir um perfil com usuarios vinculados.");
  }
  perfis = perfis.filter((perfil) => perfil.id !== id);
  return delay(undefined);
}
