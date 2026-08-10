import type { Filial, FilialUpdateInput } from "../types/filialTypes";

/**
 * Fundacao visual do modulo Gestao de Filiais (Sprint O1.3.3, ADR-0020
 * item 3). Dados mockados em memoria: nenhuma chamada de API, nenhuma
 * persistencia definitiva, nenhuma escrita no ERP. Simula latencia de
 * rede para exercitar os estados de carregamento da interface, seguindo o
 * mesmo padrao de administration/profiles e administration/users.
 *
 * Filiais sao dados mestres do ERP (fonte canonica). O +Compras nunca cria
 * uma filial nem altera CodigoCliFor/NomeCliFor/UnidadeNegocioId — o mock
 * abaixo representa o resultado de uma sincronizacao ja concluida do ERP,
 * apenas para exercitar a gestao dos metadados locais permitidos.
 */
let filiais: Filial[] = [
  {
    id: "filial-0101",
    codigoCliFor: "0101",
    nomeCliFor: "SOMA MATRIZ SAO PAULO",
    descricaoMaisCompras: undefined,
    ativoNoMaisCompras: true,
    unidadeNegocioId: "SOMA",
    criadoEm: "2026-07-01T09:00:00Z",
    atualizadoEm: "2026-07-01T09:00:00Z"
  },
  {
    id: "filial-0102",
    codigoCliFor: "0102",
    nomeCliFor: "ANIMALE LOJA JARDINS",
    descricaoMaisCompras: "Loja conceito - prioridade de atendimento",
    ativoNoMaisCompras: true,
    unidadeNegocioId: "ANIMALE",
    criadoEm: "2026-07-01T09:00:00Z",
    atualizadoEm: "2026-07-18T14:30:00Z"
  },
  {
    id: "filial-0103",
    codigoCliFor: "0103",
    nomeCliFor: "FARM CD GUARULHOS",
    descricaoMaisCompras: undefined,
    ativoNoMaisCompras: false,
    unidadeNegocioId: "FARM",
    criadoEm: "2026-07-01T09:00:00Z",
    atualizadoEm: "2026-07-25T11:00:00Z"
  },
  {
    id: "filial-0104",
    codigoCliFor: "0104",
    nomeCliFor: "FABULA LOJA VILLAGE MALL",
    descricaoMaisCompras: "Ponto de venda de alto giro - RJ",
    ativoNoMaisCompras: true,
    unidadeNegocioId: "FABULA",
    criadoEm: "2026-07-02T09:00:00Z",
    atualizadoEm: "2026-07-02T09:00:00Z"
  },
  {
    id: "filial-0105",
    codigoCliFor: "0105",
    nomeCliFor: "SOMA CORPORATIVO JARDIM BOTANICO",
    descricaoMaisCompras: undefined,
    ativoNoMaisCompras: true,
    unidadeNegocioId: "SOMA",
    criadoEm: "2026-07-02T09:00:00Z",
    atualizadoEm: "2026-07-02T09:00:00Z"
  },
  {
    id: "filial-0106",
    codigoCliFor: "0106",
    nomeCliFor: "ANIMALE CD EXTREMA",
    descricaoMaisCompras: undefined,
    ativoNoMaisCompras: false,
    unidadeNegocioId: "ANIMALE",
    criadoEm: "2026-07-03T09:00:00Z",
    atualizadoEm: "2026-07-28T16:00:00Z"
  },
  {
    id: "filial-0107",
    codigoCliFor: "0107",
    nomeCliFor: "FARM LOJA OSCAR FREIRE",
    descricaoMaisCompras: "Flagship - acompanhamento comercial semanal",
    ativoNoMaisCompras: true,
    unidadeNegocioId: "FARM",
    criadoEm: "2026-07-03T09:00:00Z",
    atualizadoEm: "2026-08-01T10:00:00Z"
  },
  {
    id: "filial-0108",
    codigoCliFor: "0108",
    nomeCliFor: "FABULA CD ITAPEVI",
    descricaoMaisCompras: undefined,
    ativoNoMaisCompras: true,
    unidadeNegocioId: "FABULA",
    criadoEm: "2026-07-04T09:00:00Z",
    atualizadoEm: "2026-07-04T09:00:00Z"
  }
];

const LATENCY_MS = 250;

function delay<T>(value: T): Promise<T> {
  return new Promise((resolve) => setTimeout(() => resolve(value), LATENCY_MS));
}

export async function listFiliais(): Promise<Filial[]> {
  return delay([...filiais]);
}

export async function getFilial(id: string): Promise<Filial | null> {
  return delay(filiais.find((filial) => filial.id === id) ?? null);
}

/**
 * Atualiza exclusivamente os metadados locais do +Compras
 * (DescricaoMaisCompras, AtivoNoMaisCompras). CodigoCliFor, NomeCliFor e
 * UnidadeNegocioId nunca sao alterados por esta funcao — nao existe
 * parametro para isso, pois sao somente leitura, de origem ERP.
 */
export async function updateFilial(id: string, input: FilialUpdateInput): Promise<Filial> {
  const existing = filiais.find((filial) => filial.id === id);
  if (!existing) throw new Error("Filial nao encontrada.");
  const updated: Filial = { ...existing, ...input, atualizadoEm: new Date().toISOString() };
  filiais = filiais.map((filial) => (filial.id === id ? updated : filial));
  return delay(updated);
}
