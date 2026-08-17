import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { IdentityProvidersRoutes } from "../routes/IdentityProvidersRoutes";

const UN_ID = "11111111-1111-1111-1111-111111111111";

type ProviderDto = {
  id: string;
  unidadeNegocioId: string;
  tipo: string;
  dominiosAutorizados: string[];
  parametrosConfigurados: boolean;
  ativo: boolean;
};

let providers: ProviderDto[];

beforeEach(() => {
  providers = [
    {
      id: "aaaaaaaa-0000-0000-0000-000000000001",
      unidadeNegocioId: UN_ID,
      tipo: "MicrosoftEntraId",
      dominiosAutorizados: ["azzas2154.com.br"],
      parametrosConfigurados: true,
      ativo: true
    }
  ];

  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url === "/api/administracao/unidades-negocio") {
      return { ok: true, status: 200, json: async () => [{ id: UN_ID, nome: "AZZAS Corporativo", slug: "azzas-corporativo", ativa: true }] } as Response;
    }

    const base = `/api/administracao/unidades-negocio/${UN_ID}/identity-providers`;
    if (method === "GET" && url === base) {
      return { ok: true, status: 200, json: async () => providers } as Response;
    }
    if (method === "POST" && url === base) {
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const criado: ProviderDto = {
        id: "bbbbbbbb-0000-0000-0000-000000000002",
        unidadeNegocioId: UN_ID,
        tipo: body.tipo,
        dominiosAutorizados: body.dominiosAutorizados,
        parametrosConfigurados: Boolean(body.parametros),
        ativo: true
      };
      providers = [...providers, criado];
      return { ok: true, status: 201, json: async () => criado } as Response;
    }
    if (method === "PUT" && url.startsWith(`${base}/`)) {
      const id = url.split(`${base}/`)[1];
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const existente = providers.find((p) => p.id === id)!;
      const atualizado = {
        ...existente,
        tipo: body.tipo,
        dominiosAutorizados: body.dominiosAutorizados,
        parametrosConfigurados: body.parametros ? true : existente.parametrosConfigurados
      };
      providers = providers.map((p) => (p.id === id ? atualizado : p));
      return { ok: true, status: 200, json: async () => atualizado } as Response;
    }
    return { ok: false, status: 404, json: async () => ({}) } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderIdentityProviders() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <IdentityProvidersRoutes />
    </MemoryRouter>
  );
}

async function selecionarUnidadeNegocio() {
  const seletor = await screen.findByLabelText("Unidade de Negócio");
  await userEvent.selectOptions(seletor, UN_ID);
}

describe("IdentityProvidersPage", () => {
  it("lista os Identity Providers da Unidade de Negócio selecionada", async () => {
    renderIdentityProviders();
    await selecionarUnidadeNegocio();
    expect(await screen.findByText("MicrosoftEntraId")).toBeInTheDocument();
    expect(screen.getByText("azzas2154.com.br")).toBeInTheDocument();
    expect(screen.getByText("Já configurado")).toBeInTheDocument();
  });

  it("NUNCA pre-preenche o campo de parametros de configuracao ao editar um provider existente", async () => {
    renderIdentityProviders();
    await selecionarUnidadeNegocio();
    await screen.findByText("MicrosoftEntraId");

    const row = screen.getByText("MicrosoftEntraId").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    const campoParametros = await screen.findByLabelText(/Parâmetros de configuração/i);
    expect(campoParametros).toHaveValue("");
    expect(screen.getAllByText("Já configurado").length).toBeGreaterThan(0);
  });

  it("cria um novo Identity Provider com dominios autorizados", async () => {
    renderIdentityProviders();
    await selecionarUnidadeNegocio();
    await screen.findByText("MicrosoftEntraId");

    await userEvent.click(screen.getByRole("button", { name: "Novo Identity Provider" }));
    await userEvent.type(screen.getByPlaceholderText(/pressione Enter/i), "novodominio.com.br{enter}");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByText("novodominio.com.br")).toBeInTheDocument());
  });
});
