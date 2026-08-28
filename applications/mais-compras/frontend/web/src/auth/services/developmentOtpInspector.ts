/**
 * Cliente do mecanismo de diagnóstico exclusivo de Development
 * (security-design-auth-o1.4.md, §17.5). Só é chamado pela UI quando o
 * próprio build do Vite está em modo de desenvolvimento (`import.meta.env.DEV`)
 * — nunca em produção. O backend, por sua vez, só mapeia esta rota quando
 * `IHostEnvironment.IsDevelopment()`; fora disso a rota nem existe no servidor.
 */
export async function fetchDevelopmentOtp(email: string): Promise<string | null> {
  if (!import.meta.env.DEV) return null;

  const response = await fetch(`/dev/otp?email=${encodeURIComponent(email)}`, { credentials: "include" });
  if (!response.ok) return null;

  const data = (await response.json()) as { codigo: string };
  return data.codigo;
}
