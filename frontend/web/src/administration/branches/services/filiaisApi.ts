import type { Filial, FilialUpdateInput } from "../types/filialTypes";

/**
 * Cliente HTTP real da Gestao de Filiais (O1.7 — Filiais e Centros de Custo Integrados ao ERP). Substitui
 * integralmente o antigo `filiaisMockApi.ts` (dados em memoria), removido nesta sprint.
 *
 * Mesmo padrao de `administration/users/services/usuariosApi.ts` (O1.6): sessao via cookie HttpOnly
 * (`credentials: "include"`), cabecalho CSRF nas escritas, e nenhuma decisao de autorizacao acontece aqui —
 * o backend exige a permissao `Filial.Gerenciar` em todos estes endpoints e responde 401/403
 * independentemente do que a interface faca.
 *
 * Filial e dado mestre integrado do ERP (ADR-0020, item 3): nao existe endpoint de criacao nem de exclusao
 * — apenas leitura (combinada com metadados locais) e atualizacao dos metadados locais permitidos.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

/** Espelha `PerfisController.BaseRoute`/`FiliaisController` no backend. */
const BASE = "/api/administracao";

type ApiErrorBody = { code?: string; message?: string };

type FilialApiDto = {
  codigoCliFor: string;
  nomeCliFor: string;
  unidadeNegocioErpId?: string | null;
  descricaoMaisCompras?: string | null;
  ativoNoMaisCompras: boolean;
  temMetadadoLocal: boolean;
  atualizadoEm?: string | null;
};

export class FilialApiError extends Error {
  readonly code?: string;

  constructor(message: string, code?: string) {
    super(message);
    this.name = "FilialApiError";
    this.code = code;
  }
}

/** 403 — sessao valida, porem sem a permissao `Filial.Gerenciar`. */
export class FilialAcessoNegadoError extends FilialApiError {
  constructor(message = "Você não tem permissão para acessar a Gestão de Filiais.") {
    super(message, "acesso_negado");
    this.name = "FilialAcessoNegadoError";
  }
}

/** 401 — sem sessao autenticada. */
export class FilialNaoAutenticadoError extends FilialApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "FilialNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new FilialNaoAutenticadoError();
  if (response.status === 403) throw new FilialAcessoNegadoError();

  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON — mensagem generica mantida.
  }
  throw new FilialApiError(message, code);
}

function paraFilial(dto: FilialApiDto): Filial {
  const atualizadoEm = dto.atualizadoEm ?? new Date().toISOString();
  return {
    id: dto.codigoCliFor,
    codigoCliFor: dto.codigoCliFor,
    nomeCliFor: dto.nomeCliFor,
    descricaoMaisCompras: dto.descricaoMaisCompras ?? undefined,
    ativoNoMaisCompras: dto.ativoNoMaisCompras,
    unidadeNegocioId: dto.unidadeNegocioErpId ?? "",
    temMetadadoLocal: dto.temMetadadoLocal,
    criadoEm: atualizadoEm,
    atualizadoEm
  };
}

export async function listFiliais(): Promise<Filial[]> {
  const response = await fetch(`${BASE}/filiais`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar filiais.");
  const data = (await response.json()) as FilialApiDto[];
  return data.map(paraFilial);
}

export async function getFilial(id: string): Promise<Filial | null> {
  const todas = await listFiliais();
  return todas.find((filial) => filial.id === id) ?? null;
}

/**
 * Atualiza exclusivamente os metadados locais do +Compras (DescricaoMaisCompras, AtivoNoMaisCompras).
 * CodigoCliFor, NomeCliFor e UnidadeNegocioId nunca sao alterados por esta funcao — nao existe parametro
 * para isso, pois sao somente leitura, de origem ERP.
 */
export async function updateFilial(id: string, input: FilialUpdateInput): Promise<Filial> {
  const response = await fetch(`${BASE}/filiais/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({
      descricaoMaisCompras: input.descricaoMaisCompras ?? null,
      ativoNoMaisCompras: input.ativoNoMaisCompras
    })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar filial.");
  return paraFilial((await response.json()) as FilialApiDto);
}
