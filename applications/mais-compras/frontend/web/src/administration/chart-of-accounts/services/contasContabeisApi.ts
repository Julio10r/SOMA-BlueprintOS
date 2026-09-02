import type { ContaContabil, ContaContabilUpdateInput } from "../types/contaContabilTypes";

/**
 * Cliente HTTP real da Gestao de Contas Contabeis (B3 - Bloco 1, Discovery homologado). Mesmo padrao de
 * `administration/branches/services/filiaisApi.ts`: sessao via cookie HttpOnly (`credentials: "include"`),
 * cabecalho CSRF nas escritas, nenhuma decisao de autorizacao acontece aqui - o backend exige a permissao
 * `ContaContabil.Gerenciar` em todos estes endpoints e responde 401/403 independentemente do que a
 * interface faca.
 *
 * Conta Contabil e cadastro de apoio originado do ERP: nao existe endpoint de criacao nem de exclusao -
 * apenas leitura (combinada com metadados locais) e atualizacao dos metadados locais permitidos.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

/** Espelha `PerfisController.BaseRoute`/`ContasContabeisController` no backend. */
const BASE = "/api/administracao";

type ApiErrorBody = { code?: string; message?: string };

type ContaContabilApiDto = {
  codigoErp: string;
  descricaoErp: string;
  inativaNoErp: boolean;
  descricaoMaisCompras?: string | null;
  ativoNoMaisCompras: boolean;
  ativoEfetivo: boolean;
  temMetadadoLocal: boolean;
  atualizadoEm?: string | null;
};

export class ContaContabilApiError extends Error {
  readonly code?: string;

  constructor(message: string, code?: string) {
    super(message);
    this.name = "ContaContabilApiError";
    this.code = code;
  }
}

/** 403 - sessao valida, porem sem a permissao `ContaContabil.Gerenciar`. */
export class ContaContabilAcessoNegadoError extends ContaContabilApiError {
  constructor(message = "Você não tem permissão para acessar a Gestão de Contas Contábeis.") {
    super(message, "acesso_negado");
    this.name = "ContaContabilAcessoNegadoError";
  }
}

/** 401 - sem sessao autenticada. */
export class ContaContabilNaoAutenticadoError extends ContaContabilApiError {
  constructor(message = "Sua sessao expirou. Entre novamente para continuar.") {
    super(message, "nao_autenticado");
    this.name = "ContaContabilNaoAutenticadoError";
  }
}

async function lerErro(response: Response, fallback: string): Promise<never> {
  if (response.status === 401) throw new ContaContabilNaoAutenticadoError();
  if (response.status === 403) throw new ContaContabilAcessoNegadoError();

  let message = fallback;
  let code: string | undefined;
  try {
    const data = (await response.json()) as ApiErrorBody;
    if (data.message) message = data.message;
    code = data.code;
  } catch {
    // resposta sem corpo JSON - mensagem generica mantida.
  }
  throw new ContaContabilApiError(message, code);
}

function paraContaContabil(dto: ContaContabilApiDto): ContaContabil {
  const atualizadoEm = dto.atualizadoEm ?? new Date().toISOString();
  return {
    id: dto.codigoErp,
    codigoErp: dto.codigoErp,
    descricaoErp: dto.descricaoErp,
    inativaNoErp: dto.inativaNoErp,
    descricaoMaisCompras: dto.descricaoMaisCompras ?? undefined,
    ativoNoMaisCompras: dto.ativoNoMaisCompras,
    ativoEfetivo: dto.ativoEfetivo,
    temMetadadoLocal: dto.temMetadadoLocal,
    atualizadoEm
  };
}

export async function listContasContabeis(): Promise<ContaContabil[]> {
  const response = await fetch(`${BASE}/contas-contabeis`, { credentials: "include" });
  if (!response.ok) await lerErro(response, "Falha ao carregar contas contábeis.");
  const data = (await response.json()) as ContaContabilApiDto[];
  return data.map(paraContaContabil);
}

export async function getContaContabil(id: string): Promise<ContaContabil | null> {
  const todas = await listContasContabeis();
  return todas.find((conta) => conta.id === id) ?? null;
}

/**
 * Atualiza exclusivamente os metadados locais do +Compras (DescricaoMaisCompras, AtivoNoMaisCompras).
 * CodigoErp, DescricaoErp e InativaNoErp nunca sao alterados por esta funcao.
 */
export async function updateContaContabil(id: string, input: ContaContabilUpdateInput): Promise<ContaContabil> {
  const response = await fetch(`${BASE}/contas-contabeis/${encodeURIComponent(id)}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", [CSRF_HEADER]: "1" },
    body: JSON.stringify({
      descricaoMaisCompras: input.descricaoMaisCompras ?? null,
      ativoNoMaisCompras: input.ativoNoMaisCompras
    })
  });
  if (!response.ok) await lerErro(response, "Falha ao salvar conta contábil.");
  return paraContaContabil((await response.json()) as ContaContabilApiDto);
}
