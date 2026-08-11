import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { FeatureFlagsRoutes } from "../routes/FeatureFlagsRoutes";

type FlagDto = { id: string; nome: string; descricao: string; status: { unidadeNegocioId: string; unidadeNegocioNome: string; ativa: boolean }[] };

const UN_BASE = "/api/administracao/unidades-negocio";
const FF_BASE = "/api/administracao/feature-flags";
const UN_ID = "11111111-1111-1111-1111-111111111111";

let flags: FlagDto[];

beforeEach(() => {
  flags = [];

  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url === UN_BASE) {
      return { ok: true, status: 200, json: async () => [{ id: UN_ID, nome: "AZZAS Corporativo", slug: "azzas-corporativo", ativa: true }] } as Response;
    }
    if (method === "GET" && url === FF_BASE) {
      return { ok: true, status: 200, json: async () => flags } as Response;
    }
    if (method === "POST" && url === FF_BASE) {
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const criada: FlagDto = { id: "f1", nome: body.nome, descricao: body.descricao, status: [{ unidadeNegocioId: UN_ID, unidadeNegocioNome: "AZZAS Corporativo", ativa: false }] };
      flags = [...flags, criada];
      return { ok: true, status: 201, json: async () => criada } as Response;
    }
    if (method === "PATCH" && url.endsWith("/status")) {
      const [, id] = url.match(new RegExp(`${FF_BASE}/([^/]+)/unidades-negocio/`)) ?? [];
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      flags = flags.map((f) => (f.id === id ? { ...f, status: f.status.map((s) => (s.unidadeNegocioId === UN_ID ? { ...s, ativa: body.ativa } : s)) } : f));
      return { ok: true, status: 200, json: async () => flags.find((f) => f.id === id) } as Response;
    }
    return { ok: false, status: 404, json: async () => ({}) } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderFeatureFlags() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <FeatureFlagsRoutes />
    </MemoryRouter>
  );
}

describe("FeatureFlagsPage", () => {
  it("exibe o estado vazio honesto quando nao ha feature flags cadastradas", async () => {
    renderFeatureFlags();
    expect(await screen.findByText("Nenhuma feature flag cadastrada.")).toBeInTheDocument();
  });

  it("cria uma nova feature flag", async () => {
    renderFeatureFlags();
    await screen.findByText("Nenhuma feature flag cadastrada.");

    await userEvent.click(screen.getByRole("button", { name: "Nova Feature Flag" }));
    await userEvent.type(screen.getByLabelText(/Nome da flag/i), "novo-checkout");
    await userEvent.type(screen.getByLabelText(/Descricao/i), "Habilita o novo fluxo de checkout.");
    await userEvent.click(screen.getByRole("button", { name: "Criar" }));

    await waitFor(() => expect(screen.getByText("novo-checkout")).toBeInTheDocument());
  });
});
