import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UnidadesAlocacaoRoutes } from "../routes/UnidadesAlocacaoRoutes";

/**
 * O1.8 — a Gestao de Unidades de Alocacao consome a API real (`administracao/unidades-alocacao`),
 * substituindo o `unidadesAlocacaoMockApi.ts` removido nesta sprint. Mesmo padrao de integracao HTTP de
 * `administration/cost-centers/tests/CentrosCustoPage.test.tsx` (O1.7): fetch interceptado.
 *
 * Sem vinculo com Centro de Custo (escopo da O1.9) e sem campo de Unidade de Negocio no formulario — ela
 * e sempre resolvida pelo backend a partir da sessao, nunca escolhida pelo cliente.
 */
type UnidadeAlocacaoApiDto = {
  id: string;
  nome: string;
  descricao: string;
  unidadeNegocioId: string;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

const BU = "11111111-1111-1111-1111-111111111111";

function unidadeAlocacaoDto(over: Partial<UnidadeAlocacaoApiDto> = {}): UnidadeAlocacaoApiDto {
  return {
    id: "aaaaaaaa-0000-0000-0000-000000000001",
    nome: "SOMA Corporativo",
    descricao: "Agrupamento administrativo das areas corporativas do grupo SOMA.",
    unidadeNegocioId: BU,
    ativo: true,
    criadoEm: "2026-07-10T09:00:00Z",
    atualizadoEm: "2026-07-10T09:00:00Z",
    ...over
  };
}

let unidadesAlocacao: UnidadeAlocacaoApiDto[];

beforeEach(() => {
  unidadesAlocacao = [
    unidadeAlocacaoDto(),
    unidadeAlocacaoDto({
      id: "aaaaaaaa-0000-0000-0000-000000000002",
      nome: "Farm",
      descricao: "Agrupamento orcamentario e de relatorios da marca Farm."
    }),
    unidadeAlocacaoDto({
      id: "aaaaaaaa-0000-0000-0000-000000000003",
      nome: "Animale",
      descricao: "Agrupamento orcamentario e de relatorios da marca Animale."
    }),
    unidadeAlocacaoDto({
      id: "aaaaaaaa-0000-0000-0000-000000000004",
      nome: "Projetos Especiais",
      descricao: "Agrupamento temporario para iniciativas fora da estrutura recorrente de centros de custo.",
      ativo: false
    })
  ];

  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";
    const base = "/api/administracao/unidades-alocacao";

    if (method === "GET" && url === base) {
      return { ok: true, status: 200, json: async () => unidadesAlocacao } as Response;
    }

    if (method === "GET" && url.startsWith(`${base}/`)) {
      const id = decodeURIComponent(url.split(`${base}/`)[1]);
      const encontrada = unidadesAlocacao.find((u) => u.id === id);
      if (!encontrada) return { ok: false, status: 404, json: async () => ({ code: "unidade_alocacao_nao_encontrada" }) } as Response;
      return { ok: true, status: 200, json: async () => encontrada } as Response;
    }

    if (method === "POST" && url === base) {
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const duplicado = unidadesAlocacao.some((u) => u.nome.toLowerCase() === String(body.nome).toLowerCase());
      if (duplicado) return { ok: false, status: 409, json: async () => ({ code: "nome_duplicado", message: "Ja existe uma Unidade de Alocacao com este nome." }) } as Response;
      const criada = unidadeAlocacaoDto({
        id: `aaaaaaaa-0000-0000-0000-${String(unidadesAlocacao.length + 1).padStart(12, "0")}`,
        nome: body.nome,
        descricao: body.descricao
      });
      unidadesAlocacao = [...unidadesAlocacao, criada];
      return { ok: true, status: 201, json: async () => criada } as Response;
    }

    if (method === "PUT" && url.startsWith(`${base}/`)) {
      const id = decodeURIComponent(url.split(`${base}/`)[1]);
      const existente = unidadesAlocacao.find((u) => u.id === id);
      if (!existente) return { ok: false, status: 404, json: async () => ({ code: "unidade_alocacao_nao_encontrada" }) } as Response;
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const atualizada: UnidadeAlocacaoApiDto = { ...existente, nome: body.nome, descricao: body.descricao, atualizadoEm: new Date().toISOString() };
      unidadesAlocacao = unidadesAlocacao.map((u) => (u.id === id ? atualizada : u));
      return { ok: true, status: 200, json: async () => atualizada } as Response;
    }

    if (method === "PATCH" && url.endsWith("/status")) {
      const id = decodeURIComponent(url.split(`${base}/`)[1].replace("/status", ""));
      const existente = unidadesAlocacao.find((u) => u.id === id);
      if (!existente) return { ok: false, status: 404, json: async () => ({ code: "unidade_alocacao_nao_encontrada" }) } as Response;
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const atualizada: UnidadeAlocacaoApiDto = { ...existente, ativo: body.ativo, atualizadoEm: new Date().toISOString() };
      unidadesAlocacao = unidadesAlocacao.map((u) => (u.id === id ? atualizada : u));
      return { ok: true, status: 200, json: async () => atualizada } as Response;
    }

    return { ok: false, status: 404, json: async () => ({}) } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderUnidadesAlocacao(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <UnidadesAlocacaoRoutes />
    </MemoryRouter>
  );
}

describe("UnidadesAlocacaoPage", () => {
  it("lista as unidades de alocacao vindas da API", async () => {
    renderUnidadesAlocacao();
    expect(await screen.findByRole("heading", { name: "Unidades de Alocacao cadastradas" })).toBeInTheDocument();
    expect(await screen.findByText("SOMA Corporativo")).toBeInTheDocument();
    expect(await screen.findByText("Farm")).toBeInTheDocument();
  });

  it("abre o formulario de nova unidade de alocacao e volta para a lista apos salvar", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("SOMA Corporativo");

    await userEvent.click(screen.getByRole("button", { name: "Nova unidade de alocacao" }));
    expect(await screen.findAllByRole("heading", { name: "Nova unidade de alocacao" })).toHaveLength(2);

    await userEvent.type(screen.getByLabelText("Nome"), "Fabula Outlet");
    await userEvent.type(screen.getByLabelText("Descricao"), "Agrupamento das lojas de outlet da Fabula.");

    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Unidades de Alocacao cadastradas" })).toBeInTheDocument());
    expect(await screen.findByText("Fabula Outlet")).toBeInTheDocument();
  });

  it("rejeita nome duplicado ao criar", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("SOMA Corporativo");

    await userEvent.click(screen.getByRole("button", { name: "Nova unidade de alocacao" }));
    await userEvent.type(screen.getByLabelText("Nome"), "Farm");
    await userEvent.type(screen.getByLabelText("Descricao"), "Duplicada.");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    expect(await screen.findByText(/Ja existe uma Unidade de Alocacao com este nome/i)).toBeInTheDocument();
  });

  it("visualiza uma unidade de alocacao existente", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("SOMA Corporativo");

    const row = screen.getByText("Farm").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Visualizar" }));

    expect(await screen.findByRole("heading", { name: "Farm" })).toBeInTheDocument();
    expect(screen.getByText("Agrupamento orcamentario e de relatorios da marca Farm.")).toBeInTheDocument();
  });

  it("edita uma unidade de alocacao existente", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("SOMA Corporativo");

    const row = screen.getByText("Animale").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    expect(await screen.findByRole("heading", { name: "Editar unidade de alocacao", level: 1 })).toBeInTheDocument();
    const descricaoInput = await screen.findByLabelText("Descricao");
    await userEvent.clear(descricaoInput);
    await userEvent.type(descricaoInput, "Descricao atualizada da unidade Animale.");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Animale" })).toBeInTheDocument());
    expect(await screen.findByText("Descricao atualizada da unidade Animale.")).toBeInTheDocument();
  });

  it("ativa e inativa uma unidade de alocacao pela listagem", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("Projetos Especiais");

    const row = screen.getByText("Projetos Especiais").closest("tr")!;
    expect(within(row).getByText("Inativo")).toBeInTheDocument();

    await userEvent.click(within(row).getByRole("button", { name: "Ativar" }));

    await waitFor(() => expect(within(screen.getByText("Projetos Especiais").closest("tr")!).getByText("Ativo")).toBeInTheDocument());
  });

  it("nao possui acao de exclusao fisica em nenhuma linha", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("SOMA Corporativo");

    expect(screen.queryByRole("button", { name: "Excluir" })).not.toBeInTheDocument();
  });

  it("nao exibe campo de Unidade de Negocio no formulario", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("SOMA Corporativo");

    await userEvent.click(screen.getByRole("button", { name: "Nova unidade de alocacao" }));
    await screen.findAllByRole("heading", { name: "Nova unidade de alocacao" });

    expect(screen.queryByLabelText("Unidade de Negocio")).not.toBeInTheDocument();
  });

  it("mostra acesso negado quando a API responde 403", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status: 403, json: async () => ({}) }) as Response));

    renderUnidadesAlocacao();

    expect(await screen.findByText(/nao tem permissao para acessar a Gestao de Unidades de Alocacao/i)).toBeInTheDocument();
  });

  it("mostra sessao expirada quando a API responde 401", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status: 401, json: async () => ({}) }) as Response));

    renderUnidadesAlocacao();

    expect(await screen.findByText(/Sua sessao expirou/i)).toBeInTheDocument();
  });
});
