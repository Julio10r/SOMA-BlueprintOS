import type {
  BootstrapAdministradorPayload,
  BootstrapConcluirResponse,
  BootstrapEstado,
  BootstrapUnidadeNegocioPayload
} from "../types/bootstrapTypes";

/**
 * Cliente HTTP do fluxo de Bootstrap (O1.4.3.3). A sessão de Bootstrap viaja
 * exclusivamente pelo cookie HttpOnly `mc_bootstrap_sid` (credentials:
 * "include") — este módulo nunca lê, grava ou repassa o Bootstrap Secret, o
 * código OTP ou qualquer identificador de sessão; nada relacionado a este
 * fluxo é persistido em localStorage/sessionStorage.
 *
 * O e-mail do Administrador Sênior nunca é enviado em `concluir` — o backend
 * obtém o e-mail já validado por OTP a partir da própria `BootstrapSessao`
 * (Work Order O1.4.3, seção 13, item 3). Este módulo não expõe nenhum
 * parâmetro de e-mail para essa chamada, por desenho.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

type ApiErrorBody = { code?: string; message?: string };

export class BootstrapApiError extends Error {
  readonly status?: number;
  readonly code?: string;

  constructor(message: string, status?: number, code?: string) {
    super(message);
    this.name = "BootstrapApiError";
    this.status = status;
    this.code = code;
  }
}

async function postJson<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(path, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      [CSRF_HEADER]: "1"
    },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    let message = "Não foi possível concluir a operação.";
    let code: string | undefined;
    try {
      const data = (await response.json()) as ApiErrorBody;
      if (data.message) message = data.message;
      code = data.code;
    } catch {
      // resposta sem corpo JSON (ex.: 404, 429) — mensagem genérica mantida.
    }
    throw new BootstrapApiError(message, response.status, code);
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export async function consultarEstado(): Promise<BootstrapEstado> {
  try {
    const response = await fetch("/bootstrap/estado", { credentials: "include" });
    if (!response.ok) return { disponivel: false };
    return (await response.json()) as BootstrapEstado;
  } catch {
    // Backend indisponível — trata como Bootstrap não disponível (fail-closed
    // na decisão de roteamento; não é uma autorização de negócio).
    return { disponivel: false };
  }
}

export function iniciar(email: string, secret: string): Promise<{ message: string }> {
  return postJson("/bootstrap/iniciar", { email, secret });
}

export function verificarOtp(email: string, codigo: string): Promise<void> {
  return postJson("/bootstrap/otp/verificar", { email, codigo });
}

export function concluir(
  unidadeNegocio: BootstrapUnidadeNegocioPayload,
  administrador: BootstrapAdministradorPayload
): Promise<BootstrapConcluirResponse> {
  return postJson("/bootstrap/concluir", { unidadeNegocio, administrador });
}
