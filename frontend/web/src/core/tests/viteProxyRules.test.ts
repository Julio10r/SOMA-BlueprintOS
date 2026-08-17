import { describe, expect, it } from "vitest";
import { shouldBypassFornecedoresProxy } from "../viteProxyRules";

describe("shouldBypassFornecedoresProxy", () => {
  it("bypassa o proxy para navegacao de pagina (GET + Accept: text/html), devolvendo a rota ao SPA", () => {
    expect(shouldBypassFornecedoresProxy("GET", "text/html,application/xhtml+xml")).toBe(true);
  });

  it("nao bypassa chamadas fetch/XHR da propria aplicacao (Accept: application/json)", () => {
    expect(shouldBypassFornecedoresProxy("GET", "application/json")).toBe(false);
  });

  it("nao bypassa POST mesmo com Accept: text/html (nao e navegacao de pagina)", () => {
    expect(shouldBypassFornecedoresProxy("POST", "text/html")).toBe(false);
  });

  it("nao bypassa quando o Accept header esta ausente", () => {
    expect(shouldBypassFornecedoresProxy("GET", undefined)).toBe(false);
  });
});
