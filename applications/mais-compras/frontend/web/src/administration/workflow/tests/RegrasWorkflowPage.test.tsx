import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RegrasWorkflowRoutes } from "../routes/RegrasWorkflowRoutes";

const UN_ID = "11111111-1111-1111-1111-111111111111";

type RegraDto = {
  id: string;
  unidadeNegocioId: string;
  nome: string;
  tipoProcesso: string;
  ordem: number;
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
      nome: "Aprovacao de Pedido",
      tipoProcesso: "PedidoCompra",
      ordem: 1,
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

    const base = `/api/administracao/unidades-negocio/${UN_ID}/regras-workflow`;
    if (method === "GET" && url === base) {
      return { ok: true, status: 200, json: async () => regras } as Response;
    }
    if (method === "POST" && url === base) {
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const criada: RegraDto = {
        id: "bbbbbbbb-0000-0000-0000-000000000002",
        unidadeNegocioId: UN_ID,
        nome: body.nome,
        tipoProcesso: body.tipoProcesso,
        ordem: body.ordem,
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
      regras = regras.map((r) => (r.id === id ? { ...r, nome: body.nome, tipoProcesso: body.tipoProcesso, ordem: body.ordem } : r));
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

function renderRegrasWorkflow() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <RegrasWorkflowRoutes />
    </MemoryRouter>
  );
}

async function selecionarUnidadeNegocio() {
  const seletor = await screen.findByLabelText("Unidade de Negócio");
  await userEvent.selectOptions(seletor, UN_ID);
}

describe("RegrasWorkflowPage", () => {
  it("lista as Regras de Workflow da Unidade de Negócio selecionada", async () => {
    renderRegrasWorkflow();
    await selecionarUnidadeNegocio();
    expect(await screen.findByText("Aprovacao de Pedido")).toBeInTheDocument();
    expect(screen.getByText("PedidoCompra")).toBeInTheDocument();
    expect(screen.getByText("Ativo")).toBeInTheDocument();
  });

  it("cria uma nova Regra de Workflow", async () => {
    renderRegrasWorkflow();
    await selecionarUnidadeNegocio();
    await screen.findByText("Aprovacao de Pedido");

    await userEvent.click(screen.getByRole("button", { name: "Nova Regra de Workflow" }));
    await userEvent.type(screen.getByLabelText("Nome"), "Aprovacao de Contrato");
    await userEvent.type(screen.getByLabelText("Tipo de processo"), "Contrato");
    const ordemInput = screen.getByLabelText("Ordem");
    await userEvent.clear(ordemInput);
    await userEvent.type(ordemInput, "2");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByText("Aprovacao de Contrato")).toBeInTheDocument());
  });

  it("edita uma Regra de Workflow existente", async () => {
    renderRegrasWorkflow();
    await selecionarUnidadeNegocio();
    await screen.findByText("Aprovacao de Pedido");

    const row = screen.getByText("Aprovacao de Pedido").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    const nomeInput = screen.getByLabelText("Nome");
    await userEvent.clear(nomeInput);
    await userEvent.type(nomeInput, "Aprovacao de Pedido Revisada");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByText("Aprovacao de Pedido Revisada")).toBeInTheDocument());
  });

  it("ativa/inativa uma Regra de Workflow", async () => {
    renderRegrasWorkflow();
    await selecionarUnidadeNegocio();
    await screen.findByText("Aprovacao de Pedido");

    const row = screen.getByText("Aprovacao de Pedido").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Inativar" }));

    await waitFor(() => expect(within(row).getByText("Inativo")).toBeInTheDocument());
  });
});
