import type { Permissao } from "../types/perfilTypes";

/**
 * Agrupamento de apresentacao do catalogo de permissoes.
 *
 * O catalogo em si NAO vive mais aqui: ate a O1.4.x este arquivo mantinha uma lista
 * estatica de permissoes no frontend, que era a unica fonte da tela. A partir da O1.5
 * (RBAC Real) o catalogo vem de `GET /administracao/permissoes`, alimentado pela tabela
 * `Permissoes` — a mesma fonte que as policies de autorizacao do backend consultam.
 * Manter uma segunda lista aqui recriaria a duplicacao de "nomes magicos" que a
 * ADR-0020 (item 8) e a Work Order O1.5 proibem.
 */
export function groupPermissionsByRecurso(permissoes: Permissao[]): Array<[string, Permissao[]]> {
  const groups = new Map<string, Permissao[]>();
  for (const permissao of permissoes) {
    const group = groups.get(permissao.recurso) ?? [];
    group.push(permissao);
    groups.set(permissao.recurso, group);
  }
  return Array.from(groups.entries());
}
