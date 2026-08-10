import type { UsuarioAutenticado } from "../types/authTypes";

/**
 * Cliente HTTP do fluxo de autenticação. A sessão viaja exclusivamente por
 * cookie HttpOnly (credentials: "include"); nenhum token/OTP/identificador
 * de sessão é lido, gravado ou repassado por este módulo — o navegador nunca
 * tem acesso ao valor do cookie via JavaScript.
 */
const CSRF_HEADER = "X-MaisCompras-Csrf";

type ApiErrorBody = { code?: string; message?: string };

export class AuthApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "AuthApiError";
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
    try {
      const data = (await response.json()) as ApiErrorBody;
      if (data.message) message = data.message;
    } catch {
      // resposta sem corpo JSON (ex.: 429 Too Many Requests) — mensagem genérica mantida.
    }
    throw new AuthApiError(message);
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export function requestOtp(email: string): Promise<{ message: string }> {
  return postJson("/auth/otp/request", { email });
}

export function verifyOtp(email: string, codigo: string): Promise<{ usuario: UsuarioAutenticado }> {
  return postJson("/auth/otp/verify", { email, codigo });
}

export async function logout(): Promise<void> {
  await postJson<void>("/auth/logout", {});
}

export async function fetchCurrentUser(): Promise<UsuarioAutenticado | null> {
  const response = await fetch("/auth/me", { credentials: "include" });
  if (response.status === 401) return null;
  if (!response.ok) return null;

  const data = (await response.json()) as { usuario: UsuarioAutenticado };
  return data.usuario;
}
