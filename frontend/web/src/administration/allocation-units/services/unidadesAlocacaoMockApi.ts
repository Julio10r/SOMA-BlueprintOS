import type { UnidadeAlocacao, UnidadeAlocacaoInput } from "../types/unidadeAlocacaoTypes";

/**
 * Fundacao visual do modulo Gestao de Unidades de Alocacao (Sprint
 * O1.3.5, ADR-0020 item 4/5). Dados mockados em memoria: nenhuma chamada
 * de API, nenhuma persistencia definitiva, nenhuma integracao com o ERP —
 * Unidade de Alocacao pertence exclusivamente ao +Compras. Simula
 * latencia de rede, seguindo o mesmo padrao dos demais modulos de
 * Administracao.
 */
let unidadesAlocacao: UnidadeAlocacao[] = [
  {
    id: "ua-soma-corporativo",
    nome: "SOMA Corporativo",
    descricao: "Agrupamento administrativo das areas corporativas do grupo SOMA.",
    unidadeNegocio: "SOMA",
    status: "Ativo",
    criadoEm: "2026-07-10T09:00:00Z",
    atualizadoEm: "2026-07-10T09:00:00Z"
  },
  {
    id: "ua-farm",
    nome: "Farm",
    descricao: "Agrupamento orcamentario e de relatorios da marca Farm.",
    unidadeNegocio: "FARM",
    status: "Ativo",
    criadoEm: "2026-07-10T09:00:00Z",
    atualizadoEm: "2026-07-20T14:00:00Z"
  },
  {
    id: "ua-animale",
    nome: "Animale",
    descricao: "Agrupamento orcamentario e de relatorios da marca Animale.",
    unidadeNegocio: "ANIMALE",
    status: "Ativo",
    criadoEm: "2026-07-11T09:00:00Z",
    atualizadoEm: "2026-07-11T09:00:00Z"
  },
  {
    id: "ua-fabula",
    nome: "Fabula",
    descricao: "Agrupamento orcamentario e de relatorios da marca Fabula.",
    unidadeNegocio: "FABULA",
    status: "Ativo",
    criadoEm: "2026-07-11T09:00:00Z",
    atualizadoEm: "2026-07-11T09:00:00Z"
  },
  {
    id: "ua-projetos-especiais",
    nome: "Projetos Especiais",
    descricao: "Agrupamento temporario para iniciativas fora da estrutura recorrente de centros de custo.",
    unidadeNegocio: "SOMA",
    status: "Inativo",
    criadoEm: "2026-06-01T09:00:00Z",
    atualizadoEm: "2026-07-28T16:00:00Z"
  }
];

const LATENCY_MS = 250;

function delay<T>(value: T): Promise<T> {
  return new Promise((resolve) => setTimeout(() => resolve(value), LATENCY_MS));
}

export async function listUnidadesAlocacao(): Promise<UnidadeAlocacao[]> {
  return delay([...unidadesAlocacao]);
}

export async function getUnidadeAlocacao(id: string): Promise<UnidadeAlocacao | null> {
  return delay(unidadesAlocacao.find((unidade) => unidade.id === id) ?? null);
}

export async function createUnidadeAlocacao(input: UnidadeAlocacaoInput): Promise<UnidadeAlocacao> {
  if (unidadesAlocacao.some((unidade) => unidade.nome.toLowerCase() === input.nome.toLowerCase())) {
    throw new Error("Ja existe uma unidade de alocacao com este nome.");
  }
  const now = new Date().toISOString();
  const created: UnidadeAlocacao = {
    id: `ua-${crypto.randomUUID()}`,
    ...input,
    criadoEm: now,
    atualizadoEm: now
  };
  unidadesAlocacao = [...unidadesAlocacao, created];
  return delay(created);
}

export async function updateUnidadeAlocacao(id: string, input: UnidadeAlocacaoInput): Promise<UnidadeAlocacao> {
  const existing = unidadesAlocacao.find((unidade) => unidade.id === id);
  if (!existing) throw new Error("Unidade de alocacao nao encontrada.");
  if (unidadesAlocacao.some((unidade) => unidade.id !== id && unidade.nome.toLowerCase() === input.nome.toLowerCase())) {
    throw new Error("Ja existe uma unidade de alocacao com este nome.");
  }
  const updated: UnidadeAlocacao = { ...existing, ...input, atualizadoEm: new Date().toISOString() };
  unidadesAlocacao = unidadesAlocacao.map((unidade) => (unidade.id === id ? updated : unidade));
  return delay(updated);
}

/**
 * Ativa/inativa a unidade de alocacao. Nao existe exclusao fisica —
 * apenas inativacao, seguindo o mesmo principio ja aplicado a Filiais e
 * Centros de Custo, mesmo sem regra de ERP envolvida aqui.
 */
export async function toggleStatusUnidadeAlocacao(id: string): Promise<UnidadeAlocacao> {
  const existing = unidadesAlocacao.find((unidade) => unidade.id === id);
  if (!existing) throw new Error("Unidade de alocacao nao encontrada.");
  const updated: UnidadeAlocacao = {
    ...existing,
    status: existing.status === "Ativo" ? "Inativo" : "Ativo",
    atualizadoEm: new Date().toISOString()
  };
  unidadesAlocacao = unidadesAlocacao.map((unidade) => (unidade.id === id ? updated : unidade));
  return delay(updated);
}
