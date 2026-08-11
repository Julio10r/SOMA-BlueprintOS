import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RegrasOrcamentariasRoutes } from "../routes/RegrasOrcamentariasRoutes";

const UN_ID = "11111111-1111-1111-1111-111111111111";
const CC_ID = "44444444-4444-4444-4444-444444444444";

type RegraDto = {
  id: string;
  unidadeNegocioId: string;
  nome: string;
  centroCustoMetadadoId: string;
  valorLimite: number;
  periodo: number;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

let regras: RegraDto[];

beforeEach(() => {
  regras = [
    {
      id: "aaaaaaaa-0000-0000-0000-000000000001",
      unidadeNegocioId: UN_ID,
      nome: "Orcamento Marketing",
      centroCustoMetadadoId: CC_ID,
      valorLimite: 50000,
      periodo: 0,
      ativo: true,
      criadoEm: "2026-01-01T00:00:00Z",
      atualizadoEm: "2026-01-01T00:00:00Z"
    }
  ];

  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url === "/api/administracao/unidades-negocio") {
      return { ok: true, status: 200, json: async () => [{ id: UN_ID, nome: "AZZAS Corporativo", slug: "azzas-corporativo", ativa: true }] } as Response;
    }
    if (method === "GET" && url === "/api/administracao/centros-custo") {
      return { ok: true, status: 200, json: async () => [{ codigoErp: "CCMKT", descricaoErp: "Centro Marketing", descricaoMaisCompras: null, ativoNoMaisCompras: true, temMetadadoLocal: true, atualizadoEm: "2026-01-01T00:00:00Z", unidadeAlocacaoPadraoNome: null, quantidadeUnidadesAlocacaoVinculadas: 0, centroCustoMetadadoId: CC_ID }] } as Response;
    }

    const base = `/api/administracao/unidades-negocio/${UN_ID}/regras-orcamentarias`;
    if (method === "GET" && url === base) {
      return { ok: true, status: 200, json: async () => regras } as Response;
    }
    if (method === "POST" && url === base) {
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const criada: RegraDto = {
        id: "bbbbbbbb-0000-0000-0000-000000000002",
        unidadeNegocioId: UN_ID,
        nome: body.nome,
        centroCustoMetadadoId: body.centroCustoMetadadoId,
        valorLimite: body.valorLimite,
        periodo: body.periodo,
        ativo: true,
        criadoEm: "2026-01-02T00:00:00Z",
        atualizadoEm: "2026-01-02T00:00:00Z"
      };
      regras = [...regras, criada];
      return { ok: true, status: 201, json: async () => criada } as Response;
    }
    if (method === "PUT" && url.startsWith(`${base}/`)) {
      const id = url.split(`${base}/`)[1];
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      regras = regras.map((r) => (r.id === id ? { ...r, nome: body.nome, valorLimite: body.valorLimite, periodo: body.periodo, centroCustoMetadadoId: body.centroCustoMetadadoId } : r));
      return { ok: true, status: 200, json: async () => regras.find((r) => r.id === id) } as Response;
    }
    if (method === "PATCH" && url.endsWith("/status")) {
      const id = url.split(`${base}/`)[1].replace("/status", "");
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      regras = regras.map((r) => (r.id === id ? { ...r, ativo: body.ativo } : r));
      return { ok: true, status: 200, json: async () => regras.find((r) => r.id === id) } as Response;
    }
    return { ok: false, status: 404, json: async () => ({}) } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderRegrasOrcamentarias() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <RegrasOrcamentariasRoutes />
    </MemoryRouter>
  );
}

async function selecionarUnidadeNegocio() {
  const seletor = await screen.findByLabelText("Unidade de Negocio");
  await userEvent.selectOptions(seletor, UN_ID);
}

describe("RegrasOrcamentariasPage", () => {
  it("lista as Regras Orcamentarias da Unidade de Negocio selecionada", async () => {
    renderRegrasOrcamentarias();
    await selecionarUnidadeNegocio();
    expect(await screen.findByText("Orcamento Marketing")).toBeInTheDocument();
    expect(screen.getByText("Mensal")).toBeInTheDocument();
  });

  it("cria uma nova Regra Orcamentaria", async () => {
    renderRegrasOrcamentarias();
    await selecionarUnidadeNegocio();
    await screen.findByText("Orcamento Marketing");

    await userEvent.click(screen.getByRole("button", { name: "Nova Regra Orcamentaria" }));
    await userEvent.type(screen.getByLabelText("Nome"), "Orcamento TI");
    await userEvent.selectOptions(screen.getByLabelText("Centro de Custo"), CC_ID);
    await userEvent.type(screen.getByLabelText("Valor limite"), "20000");
    await userEvent.selectOptions(screen.getByLabelText("Periodo"), "1");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByText("Orcamento TI")).toBeInTheDocument());
  });

  it("edita uma Regra Orcamentaria existente", async () => {
    renderRegrasOrcamentarias();
    await selecionarUnidadeNegocio();
    await screen.findByText("Orcamento Marketing");

    const row = screen.getByText("Orcamento Marketing").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    const nomeInput = screen.getByLabelText("Nome");
    await userEvent.clear(nomeInput);
    await userEvent.type(nomeInput, "Orcamento Marketing Revisado");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByText("Orcamento Marketing Revisado")).toBeInTheDocument());
  });

  it("ativa/inativa uma Regra Orcamentaria", async () => {
    renderRegrasOrcamentarias();
    await selecionarUnidadeNegocio();
    await screen.findByText("Orcamento Marketing");

    const row = screen.getByText("Orcamento Marketing").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Inativar" }));

    await waitFor(() => expect(within(row).getByText("Inativo")).toBeInTheDocument());
  });
});
