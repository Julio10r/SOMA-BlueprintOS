import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UnidadesNegocioRoutes } from "../routes/UnidadesNegocioRoutes";

/**
 * O1.11 — Cadastro de Unidades de Negocio (CRUD real, recurso corporativo protegido por
 * `UnidadeNegocio.Gerenciar`). Mesmo padrao de integracao HTTP de
 * `administration/allocation-units/tests/UnidadesAlocacaoPage.test.tsx`.
 */
type UnidadeNegocioApiDto = { id: string; nome: string; slug: string; ativa: boolean };

const BASE = "/api/administracao/unidades-negocio";

function unidadeNegocioDto(over: Partial<UnidadeNegocioApiDto> = {}): UnidadeNegocioApiDto {
  return { id: "bbbbbbbb-0000-0000-0000-000000000001", nome: "SOMA", slug: "soma", ativa: true, ...over };
}

let unidadesNegocio: UnidadeNegocioApiDto[];

beforeEach(() => {
  unidadesNegocio = [unidadeNegocioDto()];

  vi.stubGlobal(
    "fetch",
    vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";

      if (method === "GET" && url === BASE) {
        return { ok: true, status: 200, json: async () => unidadesNegocio } as Response;
      }

      if (method === "POST" && url === BASE) {
        const body = init?.body ? JSON.parse(String(init.body)) : {};
        const duplicado = unidadesNegocio.some((u) => u.slug === body.slug);
        if (duplicado) {
          return { ok: false, status: 409, json: async () => ({ code: "slug_duplicado", message: "Slug ja utilizado." }) } as Response;
        }
        const criada = unidadeNegocioDto({
          id: `bbbbbbbb-0000-0000-0000-${String(unidadesNegocio.length + 1).padStart(12, "0")}`,
          nome: body.nome,
          slug: body.slug
        });
        unidadesNegocio = [...unidadesNegocio, criada];
        return { ok: true, status: 201, json: async () => criada } as Response;
      }

      if (method === "PATCH" && url.endsWith("/status")) {
        const id = decodeURIComponent(url.split(`${BASE}/`)[1].replace("/status", ""));
        const existente = unidadesNegocio.find((u) => u.id === id);
        if (!existente) return { ok: false, status: 404, json: async () => ({ code: "unidade_negocio_nao_encontrada" }) } as Response;
        const body = init?.body ? JSON.parse(String(init.body)) : {};
        const atualizada = { ...existente, ativa: body.ativa };
        unidadesNegocio = unidadesNegocio.map((u) => (u.id === id ? atualizada : u));
        return { ok: true, status: 200, json: async () => atualizada } as Response;
      }

      return { ok: false, status: 404, json: async () => ({}) } as Response;
    })
  );
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderUnidadesNegocio(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <UnidadesNegocioRoutes />
    </MemoryRouter>
  );
}

describe("UnidadesNegocioPage", () => {
  it("lista as Unidades de Negocio vindas da API", async () => {
    renderUnidadesNegocio();
    expect(await screen.findByRole("heading", { name: "Unidades de Negócio cadastradas" })).toBeInTheDocument();
    expect(await screen.findByText("SOMA")).toBeInTheDocument();
  });

  it("cria uma nova Unidade de Negocio", async () => {
    const user = userEvent.setup();
    renderUnidadesNegocio();
    await screen.findByText("SOMA");

    await user.click(screen.getByRole("button", { name: /Nova unidade de negócio/i }));
    await user.type(screen.getByLabelText(/Nome/i), "Reserva");
    await user.type(screen.getByLabelText(/Slug/i), "reserva");
    await user.click(screen.getByRole("button", { name: /Salvar/i }));

    expect(await screen.findByText("Reserva")).toBeInTheDocument();
  });

  it("alterna o status (Ativar/Inativar) de uma Unidade de Negocio", async () => {
    const user = userEvent.setup();
    renderUnidadesNegocio();
    await screen.findByText("SOMA");

    const botaoStatus = screen.getByRole("button", { name: /Inativar/i });
    await user.click(botaoStatus);

    expect(await screen.findByRole("button", { name: /Ativar/i })).toBeInTheDocument();
  });
});
