import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ParametrosRoutes } from "../routes/ParametrosRoutes";

type ParametroDto = { id: string; chave: string; valor: string; descricao: string; unidadeNegocioId: string | null };

const BASE = "/api/administracao/parametros";
let parametros: ParametroDto[];

beforeEach(() => {
  parametros = [{ id: "p1", chave: "TIMEOUT_ERP", valor: "30", descricao: "Timeout em segundos", unidadeNegocioId: null }];

  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url.startsWith(BASE)) {
      return { ok: true, status: 200, json: async () => parametros } as Response;
    }
    if (method === "POST" && url === BASE) {
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      if (!body.chave) return { ok: false, status: 400, json: async () => ({ code: "chave_obrigatoria", message: "Chave e obrigatoria." }) } as Response;
      const duplicado = parametros.some((p) => p.chave === body.chave);
      if (duplicado) return { ok: false, status: 409, json: async () => ({ code: "parametro_duplicado", message: "Parametro ja existe." }) } as Response;
      const criado: ParametroDto = { id: "p2", chave: body.chave, valor: body.valor, descricao: body.descricao, unidadeNegocioId: body.unidadeNegocioId ?? null };
      parametros = [...parametros, criado];
      return { ok: true, status: 201, json: async () => criado } as Response;
    }
    if (method === "DELETE" && url.startsWith(`${BASE}/`)) {
      const id = url.split(`${BASE}/`)[1];
      parametros = parametros.filter((p) => p.id !== id);
      return { ok: true, status: 204, json: async () => ({}) } as Response;
    }
    return { ok: false, status: 404, json: async () => ({}) } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderParametros(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <ParametrosRoutes />
    </MemoryRouter>
  );
}

describe("ParametrosPage", () => {
  it("lista os parametros cadastrados", async () => {
    renderParametros();
    expect(await screen.findByText("TIMEOUT_ERP")).toBeInTheDocument();
  });

  it("cria um novo parametro global", async () => {
    renderParametros();
    await screen.findByText("TIMEOUT_ERP");

    await userEvent.click(screen.getByRole("button", { name: /Novo parâmetro/i }));
    await userEvent.type(screen.getByLabelText(/Chave/i), "NOVA_CHAVE");
    await userEvent.type(screen.getByLabelText(/Valor/i), "valor-x");
    await userEvent.type(screen.getByLabelText(/Descricao/i), "descricao-x");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByText("NOVA_CHAVE")).toBeInTheDocument());
  });

  it("rejeita chave duplicada ao criar", async () => {
    renderParametros();
    await screen.findByText("TIMEOUT_ERP");

    await userEvent.click(screen.getByRole("button", { name: /Novo parâmetro/i }));
    await userEvent.type(screen.getByLabelText(/Chave/i), "TIMEOUT_ERP");
    await userEvent.type(screen.getByLabelText(/Valor/i), "60");
    await userEvent.type(screen.getByLabelText(/Descricao/i), "outra descricao");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    expect(await screen.findByText(/Parametro ja existe/i)).toBeInTheDocument();
  });

  it("exclui um parametro apos confirmacao no modal da aplicacao (nunca window.confirm)", async () => {
    renderParametros();
    await screen.findByText("TIMEOUT_ERP");

    await userEvent.click(screen.getByRole("button", { name: "Excluir" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText(/Excluir o parâmetro/)).toBeInTheDocument();
    await userEvent.click(within(dialog).getByRole("button", { name: "Excluir" }));

    await waitFor(() => expect(screen.queryByText("TIMEOUT_ERP")).not.toBeInTheDocument());
  });
});
