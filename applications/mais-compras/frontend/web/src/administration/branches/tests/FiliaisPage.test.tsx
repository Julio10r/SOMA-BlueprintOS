import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { FiliaisRoutes } from "../routes/FiliaisRoutes";

/**
 * O1.7 — a Gestão de Filiais consome a API real (`administracao/filiais`), substituindo o
 * `filiaisMockApi.ts` removido nesta sprint. Mesmo padrao de integracao HTTP de
 * `administration/users/tests/UsuariosPage.test.tsx` (O1.6): fetch interceptado.
 */
type FilialApiDto = {
  codigoCliFor: string;
  nomeCliFor: string;
  unidadeNegocioErpId?: string | null;
  descricaoMaisCompras?: string | null;
  ativoNoMaisCompras: boolean;
  temMetadadoLocal: boolean;
  atualizadoEm?: string | null;
};

function filialDto(over: Partial<FilialApiDto> = {}): FilialApiDto {
  return {
    codigoCliFor: "0101",
    nomeCliFor: "SOMA MATRIZ SAO PAULO",
    unidadeNegocioErpId: "SOMA",
    descricaoMaisCompras: null,
    ativoNoMaisCompras: true,
    temMetadadoLocal: false,
    atualizadoEm: "2026-07-01T09:00:00Z",
    ...over
  };
}

type Rota = { status: number; body?: unknown };

let rotas: Map<string, Rota>;
let filiais: FilialApiDto[];

function chave(url: string, method: string): string {
  const semQuery = url.split("?")[0];
  const codigo = semQuery.startsWith("/api/administracao/filiais/") ? "/api/administracao/filiais/:codigo" : semQuery;
  return `${method} ${codigo}`;
}

beforeEach(() => {
  filiais = [
    filialDto(),
    filialDto({ codigoCliFor: "0102", nomeCliFor: "ANIMALE LOJA JARDINS", unidadeNegocioErpId: "ANIMALE", descricaoMaisCompras: "Loja conceito - prioridade de atendimento", temMetadadoLocal: true }),
    filialDto({ codigoCliFor: "0103", nomeCliFor: "FARM CD GUARULHOS", unidadeNegocioErpId: "FARM", ativoNoMaisCompras: false, temMetadadoLocal: true }),
    filialDto({ codigoCliFor: "0104", nomeCliFor: "FABULA LOJA VILLAGE MALL", unidadeNegocioErpId: "FABULA" }),
    filialDto({ codigoCliFor: "0106", nomeCliFor: "ANIMALE CD EXTREMA", unidadeNegocioErpId: "ANIMALE", ativoNoMaisCompras: false, temMetadadoLocal: true })
  ];
  rotas = new Map<string, Rota>();
  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url === "/api/administracao/filiais") {
      return { ok: true, status: 200, json: async () => filiais } as Response;
    }

    if (method === "PUT" && url.startsWith("/api/administracao/filiais/")) {
      const codigo = decodeURIComponent(url.split("/api/administracao/filiais/")[1]);
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const existente = filiais.find((f) => f.codigoCliFor === codigo);
      if (!existente) return { ok: false, status: 404, json: async () => ({ code: "filial_nao_encontrada" }) } as Response;
      const atualizado: FilialApiDto = {
        ...existente,
        descricaoMaisCompras: body.descricaoMaisCompras ?? null,
        ativoNoMaisCompras: body.ativoNoMaisCompras,
        temMetadadoLocal: true,
        atualizadoEm: new Date().toISOString()
      };
      filiais = filiais.map((f) => (f.codigoCliFor === codigo ? atualizado : f));
      return { ok: true, status: 200, json: async () => atualizado } as Response;
    }

    const rota = rotas.get(chave(url, method)) ?? { status: 404, body: {} };
    return { ok: rota.status >= 200 && rota.status < 300, status: rota.status, json: async () => rota.body ?? {} } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderFiliais(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <FiliaisRoutes />
    </MemoryRouter>
  );
}

describe("FiliaisPage", () => {
  it("lista as filiais vindas da API com Código CliFor, Nome CliFor e Descrição +Compras", async () => {
    renderFiliais();
    expect(await screen.findByRole("heading", { name: "Filiais integradas do ERP" })).toBeInTheDocument();
    expect(await screen.findByText("0101")).toBeInTheDocument();
    expect(await screen.findByText("SOMA MATRIZ SAO PAULO")).toBeInTheDocument();
    expect(await screen.findByText("ANIMALE LOJA JARDINS")).toBeInTheDocument();
    expect(await screen.findByText("Loja conceito - prioridade de atendimento")).toBeInTheDocument();
  });

  it("nao exibe nenhum botao de criar ou excluir filial", async () => {
    renderFiliais();
    await screen.findByText("0101");
    expect(screen.queryByRole("button", { name: /nova filial/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /criar/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /excluir/i })).not.toBeInTheDocument();
  });

  it("exibe os dados do ERP como somente leitura na tela de edicao", async () => {
    renderFiliais();
    const row = (await screen.findByText("SOMA MATRIZ SAO PAULO")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    expect(await screen.findByRole("heading", { name: "Editar filial", level: 1 })).toBeInTheDocument();
    expect(await screen.findByText("Dados do ERP (somente leitura)")).toBeInTheDocument();

    expect(screen.queryByLabelText("Código CliFor")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Nome CliFor")).not.toBeInTheDocument();
    expect(screen.getAllByText("0101").length).toBeGreaterThan(0);
    expect(screen.getAllByText("SOMA MATRIZ SAO PAULO").length).toBeGreaterThan(0);
  });

  it("permite editar a Descrição +Compras sem alterar o Nome CliFor do ERP", async () => {
    renderFiliais();
    const row = (await screen.findByText("SOMA MATRIZ SAO PAULO")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Editar filial", level: 1 });
    await userEvent.type(await screen.findByLabelText("Descrição +Compras"), "Sede administrativa");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Detalhes da filial" })).toBeInTheDocument());
    expect(await screen.findByText("Sede administrativa")).toBeInTheDocument();
    expect(screen.getAllByText("SOMA MATRIZ SAO PAULO").length).toBeGreaterThan(0);
  });

  it("ativa e inativa a filial no +Compras diretamente pela listagem", async () => {
    renderFiliais();
    const row = (await screen.findByText("ANIMALE CD EXTREMA")).closest("tr")!;
    expect(within(row).getByText("Inativo")).toBeInTheDocument();

    await userEvent.click(within(row).getByRole("button", { name: "Ativar no +Compras" }));

    await waitFor(() => {
      const updatedRow = screen.getByText("ANIMALE CD EXTREMA").closest("tr")!;
      expect(within(updatedRow).getByText("Ativo")).toBeInTheDocument();
      expect(within(updatedRow).getByRole("button", { name: "Inativar no +Compras" })).toBeInTheDocument();
    });
  });

  it("filtra por status no +Compras", async () => {
    renderFiliais();
    await screen.findByText("SOMA MATRIZ SAO PAULO");

    await userEvent.selectOptions(screen.getByLabelText("Status no +Compras"), "Inativo");

    expect(screen.queryByText("SOMA MATRIZ SAO PAULO")).not.toBeInTheDocument();
    expect(screen.getByText("FARM CD GUARULHOS")).toBeInTheDocument();
  });

  it("pesquisa por Código CliFor", async () => {
    renderFiliais();
    await screen.findByText("SOMA MATRIZ SAO PAULO");

    await userEvent.type(screen.getByLabelText("Pesquisar"), "0104");

    expect(screen.getByText("FABULA LOJA VILLAGE MALL")).toBeInTheDocument();
    expect(screen.queryByText("SOMA MATRIZ SAO PAULO")).not.toBeInTheDocument();
  });

  it("mostra acesso negado quando a API responde 403", async () => {
    rotas.set("GET /api/administracao/filiais", { status: 403, body: {} });
    vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status: 403, json: async () => ({}) }) as Response));

    renderFiliais();

    expect(await screen.findByText(/não tem permissão para acessar a Gestão de Filiais/i)).toBeInTheDocument();
  });
});
