import type {
  CentroCusto,
  CentroCustoUpdateInput,
  UnidadeAlocacaoParaVinculo,
  UnidadeAlocacaoVinculoResumo
} from "../types/centroCustoTypes";

/**
 * Cliente HTTP real da Gestao de Centros de Custo (O1.7 — Filiais e Centros de Custo Integrados ao ERP;
 * O1.9 — vinculo real N:N com Unidade de Alocacao). Substitui integralmente o antigo
 * `centrosCustoMockApi.ts` (dados em memoria), removido na O1.7.
 *
 * Mesmo padrao de `administration/users/services/usuariosApi.ts` (O1.6): sessao via cookie HttpOnly
 * (`credentials: "include"`), cabecalho CSRF nas escritas, e nenhuma decisao de autorizacao acontece aqui —
 * o backend exige a permissao `CentroCusto.Gerenciar` em todos estes endpoints e responde 401/403
 * independentemente do que a interface faca.
 *
 * Centro de Custo e dado mestre integrado do ERP (ADR-0020, item 3): nao existe endpoint de criacao nem de
 * exclusao — apenas leitura (combinada com metadados locais) e atualizacao dos metadados locais permitidos.
 * O relacionamento N:N com Unidade de Alocacao (ADR-0020, item 6) agora e real (O1.9): as funcoes de
 * vinculo abaixo consomem `GET/PUT /centros-custo/{codigoErp}/unidades-alocacao`. O catalogo de Unidades de
 * Alocacao disponiveis para vinculo e lido diretamente do endpoint real de Unidades de Alocacao (O1.8) —
 * nunca um catalogo mockado local.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

/** Espelha `PerfisController.BaseRoute`/`CentrosCustoController` no backend. */
const BASE = "/api/administracao";

type ApiErrorBody = { code?: string; message?: string };

type CentroCustoApiDto = {
  codigoErp: string;
  descricaoErp: string;
  descricaoMaisCompras?: string | null;
  ativoNoMaisCompras: boolean;
  temMetadadoLocal: boolean;
  atualizadoEm?: string | null;
  unidadeAlocacaoPadraoNome?: string | null;
  quantidadeUnidadesAlocacaoVinculadas: number;
  centroCustoMetadadoId?: string | null;
};

type UnidadeAlocacaoApiDto = {
  id: string;
  nome: string;
  ativo: boolean;
};

type UnidadeAlocacaoVinculoApiDto = {
  id: string;
  nome: string;
  ativo: boolean;
  padrao: boolean;
};

export class CentroCustoApiError extends Error {
  readonly code?: string;

  constructor(message: string, code?: string) {
    super(message);
    this.name = "CentroCustoApiError";
    this.code = code;
  }
}

/** 403 — sessao valida, porem sem a permissao `CentroCusto.Gerenciar`. */
export class CentroCustoAcessoNegadoError extends CentroCustoApiError {
  constructor(message = "Voce nao tem permissao para acessar a Gestao de Centros de Custo.") {
    super(message, "acesso_negado");
    this.name = "CentroCustoAcessoNegadoError";
  }
}

/** 401 — sem sessao autenticada. */
export class CentroCustoNaoAutenticadoError extends CentroCustoApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "CentroCustoNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new CentroCustoNaoAutenticadoError();
  if (response.status === 403) throw new CentroCustoAcessoNegadoError();

  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new CentroCustoApiError(message, code);
}

function paraCentroCusto(dto: CentroCustoApiDto): CentroCusto {
  const atualizadoEm = dto.atualizadoEm ?? new Date().toISOString();
  return {
    id: dto.codigoErp,
    codigoErp: dto.codigoErp,
    descricaoErp: dto.descricaoErp,
    descricaoMaisCompras: dto.descricaoMaisCompras ?? undefined,
    ativoNoMaisCompras: dto.ativoNoMaisCompras,
    unidadeNegocioId: "",
    temMetadadoLocal: dto.temMetadadoLocal,
    centroCustoMetadadoId: dto.centroCustoMetadadoId ?? undefined,
    unidadeAlocacaoPadraoNome: dto.unidadeAlocacaoPadraoNome ?? undefined,
    quantidadeUnidadesAlocacaoVinculadas: dto.quantidadeUnidadesAlocacaoVinculadas,
    criadoEm: atualizadoEm,
    atualizadoEm
  };
}

function paraUnidadeAlocacaoParaVinculo(dto: UnidadeAlocacaoApiDto): UnidadeAlocacaoParaVinculo {
  return { id: dto.id, nome: dto.nome, ativo: dto.ativo };
}

function paraUnidadeAlocacaoVinculoResumo(dto: UnidadeAlocacaoVinculoApiDto): UnidadeAlocacaoVinculoResumo {
  return { id: dto.id, nome: dto.nome, ativo: dto.ativo, padrao: dto.padrao };
}

export async function listCentrosCusto(): Promise<CentroCusto[]> {
  const response = await fetch(`${BASE}/centros-custo`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar centros de custo.");
  const data = (await response.json()) as CentroCustoApiDto[];
  return data.map(paraCentroCusto);
}

export async function getCentroCusto(id: string): Promise<CentroCusto | null> {
  const todos = await listCentrosCusto();
  return todos.find((centroCusto) => centroCusto.id === id) ?? null;
}

/**
 * Atualiza exclusivamente os metadados locais do +Compras (DescricaoMaisCompras, AtivoNoMaisCompras).
 * CodigoErp e DescricaoErp nunca sao alterados por esta funcao — nao existe parametro para isso, pois sao
 * somente leitura, de origem ERP.
 */
export async function updateCentroCusto(id: string, input: CentroCustoUpdateInput): Promise<CentroCusto> {
  const response = await fetch(`${BASE}/centros-custo/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({
      descricaoMaisCompras: input.descricaoMaisCompras ?? null,
      ativoNoMaisCompras: input.ativoNoMaisCompras
    })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar centro de custo.");
  return paraCentroCusto((await response.json()) as CentroCustoApiDto);
}

/**
 * Catalogo real de Unidades de Alocacao disponiveis para vinculo (O1.8). Le diretamente o endpoint real
 * de Unidades de Alocacao — nunca um catalogo mockado local (mesmo cuidado ja resolvido para Centro de
 * Custo em `administration/users`, O1.7-L2).
 */
export async function listUnidadesAlocacaoParaVinculo(): Promise<UnidadeAlocacaoParaVinculo[]> {
  const response = await fetch("/api/administracao/unidades-alocacao", { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar unidades de alocacao.");
  const data = (await response.json()) as UnidadeAlocacaoApiDto[];
  return data.map(paraUnidadeAlocacaoParaVinculo);
}

/** Vinculo real N:N Centro de Custo × Unidade de Alocacao (O1.9, ADR-0020 item 6). */
export async function listVinculosUnidadeAlocacao(codigoErp: string): Promise<UnidadeAlocacaoVinculoResumo[]> {
  const response = await fetch(`${BASE}/centros-custo/${encodeURIComponent(codigoErp)}/unidades-alocacao`, {
    credentials: "include"
  });
  if (!response.ok) await lerErro(response, "Falha ao carregar unidades de alocacao vinculadas.");
  const data = (await response.json()) as UnidadeAlocacaoVinculoApiDto[];
  return data.map(paraUnidadeAlocacaoVinculoResumo);
}

/**
 * Substitui integralmente o conjunto de Unidades de Alocacao vinculadas a este Centro de Custo.
 * `padraoId`, quando informado, deve estar entre `unidadeAlocacaoIds` — o backend rejeita caso contrario.
 */
export async function substituirVinculosUnidadeAlocacao(
  codigoErp: string, unidadeAlocacaoIds: string[], padraoId: string | null
): Promise<UnidadeAlocacaoVinculoResumo[]> {
  const response = await fetch(`${BASE}/centros-custo/${encodeURIComponent(codigoErp)}/unidades-alocacao`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({ unidadeAlocacaoIds, padraoId })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar unidades de alocacao vinculadas.");
  const data = (await response.json()) as UnidadeAlocacaoVinculoApiDto[];
  return data.map(paraUnidadeAlocacaoVinculoResumo);
}
