import type { CentroCusto, CentroCustoUpdateInput } from "../types/centroCustoTypes";

/**
 * Fundacao visual do modulo Gestao de Centros de Custo (Sprint O1.3.4,
 * ADR-0020 item 3/5). Dados mockados em memoria: nenhuma chamada de API,
 * nenhuma persistencia definitiva, nenhuma escrita no ERP. Simula latencia
 * de rede para exercitar os estados de carregamento da interface, seguindo
 * o mesmo padrao de administration/branches.
 *
 * Centros de Custo sao dados mestres do ERP (fonte canonica). O +Compras
 * nunca cria um centro de custo nem altera CodigoErp/DescricaoErp/
 * UnidadeNegocioId — o mock abaixo representa o resultado de uma
 * sincronizacao ja concluida do ERP. unidadeAlocacaoPadraoNome e
 * quantidadeUnidadesAlocacaoVinculadas sao tambem mockados: preparam o
 * relacionamento com Unidade de Alocacao, ainda nao implementado.
 */
let centrosCusto: CentroCusto[] = [
  {
    id: "cc-1001",
    codigoErp: "1001",
    descricaoErp: "ADMINISTRATIVO CORPORATIVO",
    descricaoMaisCompras: undefined,
    ativoNoMaisCompras: true,
    unidadeNegocioId: "SOMA",
    unidadeAlocacaoPadraoNome: "SOMA Corporativo",
    quantidadeUnidadesAlocacaoVinculadas: 1,
    criadoEm: "2026-07-01T09:00:00Z",
    atualizadoEm: "2026-07-01T09:00:00Z"
  },
  {
    id: "cc-1002",
    codigoErp: "1002",
    descricaoErp: "LOGISTICA E DISTRIBUICAO",
    descricaoMaisCompras: "CD - prioridade de reposicao",
    ativoNoMaisCompras: true,
    unidadeNegocioId: "FARM",
    unidadeAlocacaoPadraoNome: "Farm",
    quantidadeUnidadesAlocacaoVinculadas: 2,
    criadoEm: "2026-07-01T09:00:00Z",
    atualizadoEm: "2026-07-18T14:30:00Z"
  },
  {
    id: "cc-1003",
    codigoErp: "1003",
    descricaoErp: "MARKETING E TRADE",
    descricaoMaisCompras: undefined,
    ativoNoMaisCompras: false,
    unidadeNegocioId: "ANIMALE",
    unidadeAlocacaoPadraoNome: undefined,
    quantidadeUnidadesAlocacaoVinculadas: 0,
    criadoEm: "2026-07-01T09:00:00Z",
    atualizadoEm: "2026-07-25T11:00:00Z"
  },
  {
    id: "cc-1004",
    codigoErp: "1004",
    descricaoErp: "OPERACOES DE LOJA",
    descricaoMaisCompras: "Acompanhamento comercial semanal",
    ativoNoMaisCompras: true,
    unidadeNegocioId: "FABULA",
    unidadeAlocacaoPadraoNome: "Fabula",
    quantidadeUnidadesAlocacaoVinculadas: 3,
    criadoEm: "2026-07-02T09:00:00Z",
    atualizadoEm: "2026-07-02T09:00:00Z"
  },
  {
    id: "cc-1005",
    codigoErp: "1005",
    descricaoErp: "TECNOLOGIA E SISTEMAS",
    descricaoMaisCompras: undefined,
    ativoNoMaisCompras: true,
    unidadeNegocioId: "SOMA",
    unidadeAlocacaoPadraoNome: undefined,
    quantidadeUnidadesAlocacaoVinculadas: 0,
    criadoEm: "2026-07-02T09:00:00Z",
    atualizadoEm: "2026-07-02T09:00:00Z"
  },
  {
    id: "cc-1006",
    codigoErp: "1006",
    descricaoErp: "RECURSOS HUMANOS",
    descricaoMaisCompras: undefined,
    ativoNoMaisCompras: false,
    unidadeNegocioId: "ANIMALE",
    unidadeAlocacaoPadraoNome: "Animale",
    quantidadeUnidadesAlocacaoVinculadas: 1,
    criadoEm: "2026-07-03T09:00:00Z",
    atualizadoEm: "2026-07-28T16:00:00Z"
  }
];

const LATENCY_MS = 250;

function delay<T>(value: T): Promise<T> {
  return new Promise((resolve) => setTimeout(() => resolve(value), LATENCY_MS));
}

export async function listCentrosCusto(): Promise<CentroCusto[]> {
  return delay([...centrosCusto]);
}

export async function getCentroCusto(id: string): Promise<CentroCusto | null> {
  return delay(centrosCusto.find((centroCusto) => centroCusto.id === id) ?? null);
}

/**
 * Atualiza exclusivamente os metadados locais do +Compras
 * (DescricaoMaisCompras, AtivoNoMaisCompras). CodigoErp, DescricaoErp e
 * UnidadeNegocioId nunca sao alterados por esta funcao — nao existe
 * parametro para isso, pois sao somente leitura, de origem ERP.
 */
export async function updateCentroCusto(id: string, input: CentroCustoUpdateInput): Promise<CentroCusto> {
  const existing = centrosCusto.find((centroCusto) => centroCusto.id === id);
  if (!existing) throw new Error("Centro de custo nao encontrado.");
  const updated: CentroCusto = { ...existing, ...input, atualizadoEm: new Date().toISOString() };
  centrosCusto = centrosCusto.map((centroCusto) => (centroCusto.id === id ? updated : centroCusto));
  return delay(updated);
}
