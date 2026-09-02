import type { UnidadeMedida, UnidadeMedidaUpdateInput } from "../types/unidadeMedidaTypes";

/**
 * Cliente HTTP real da Gestao de Unidades de Medida (B3 - Bloco 2, Discovery homologado). Mesmo padrao de
 * `administration/chart-of-accounts/services/contasContabeisApi.ts`: sessao via cookie HttpOnly
 * (`credentials: "include"`), cabecalho CSRF nas escritas, nenhuma decisao de autorizacao acontece aqui -
 * o backend exige a permissao `UnidadeMedida.Gerenciar` em todos estes endpoints.
 *
 * Unidade de Medida e cadastro de apoio originado do ERP: nao existe endpoint de criacao nem de exclusao -
 * apenas leitura (combinada com metadados locais) e atualizacao dos metadados locais permitidos.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

/** Espelha `PerfisController.BaseRoute`/`UnidadesMedidaController` no backend. */
const BASE = "/api/administracao";

type ApiErrorBody = { code?: string; message?: string };

type UnidadeMedidaApiDto = {
  codigoErp: string;
  descricaoErp: string;
  descricaoMaisCompras?: string | null;
  ativoNoMaisCompras: boolean;
  temMetadadoLocal: boolean;
  atualizadoEm?: string | null;
};

export class UnidadeMedidaApiError extends Error {
  readonly code?: string;

  constructor(message: string, code?: string) {
    super(message);
    this.name = "UnidadeMedidaApiError";
    this.code = code;
  }
}

/** 403 - sessao valida, porem sem a permissao `UnidadeMedida.Gerenciar`. */
export class UnidadeMedidaAcessoNegadoError extends UnidadeMedidaApiError {
  constructor(message = "Você não tem permissão para acessar a Gestão de Unidades de Medida.") {
    super(message, "acesso_negado");
    this.name = "UnidadeMedidaAcessoNegadoError";
  }
}

/** 401 - sem sessao autenticada. */
export class UnidadeMedidaNaoAutenticadoError extends UnidadeMedidaApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "UnidadeMedidaNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new UnidadeMedidaNaoAutenticadoError();
  if (response.status === 403) throw new UnidadeMedidaAcessoNegadoError();

  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON - mensagem generica mantida.
  }
  throw new UnidadeMedidaApiError(message, code);
}

function paraUnidadeMedida(dto: UnidadeMedidaApiDto): UnidadeMedida {
  const atualizadoEm = dto.atualizadoEm ?? new Date().toISOString();
  return {
    id: dto.codigoErp,
    codigoErp: dto.codigoErp,
    descricaoErp: dto.descricaoErp,
    descricaoMaisCompras: dto.descricaoMaisCompras ?? undefined,
    ativoNoMaisCompras: dto.ativoNoMaisCompras,
    temMetadadoLocal: dto.temMetadadoLocal,
    atualizadoEm
  };
}

export async function listUnidadesMedida(): Promise<UnidadeMedida[]> {
  const response = await fetch(`${BASE}/unidades-medida`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar unidades de medida.");
  const data = (await response.json()) as UnidadeMedidaApiDto[];
  return data.map(paraUnidadeMedida);
}

export async function getUnidadeMedida(id: string): Promise<UnidadeMedida | null> {
  const todas = await listUnidadesMedida();
  return todas.find((unidade) => unidade.id === id) ?? null;
}

/**
 * Atualiza exclusivamente os metadados locais do +Compras (DescricaoMaisCompras, AtivoNoMaisCompras).
 * CodigoErp e DescricaoErp nunca sao alterados por esta funcao.
 */
export async function updateUnidadeMedida(id: string, input: UnidadeMedidaUpdateInput): Promise<UnidadeMedida> {
  const response = await fetch(`${BASE}/unidades-medida/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({
      descricaoMaisCompras: input.descricaoMaisCompras ?? null,
      ativoNoMaisCompras: input.ativoNoMaisCompras
    })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar unidade de medida.");
  return paraUnidadeMedida((await response.json()) as UnidadeMedidaApiDto);
}
