import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UnidadesMedidaRoutes } from "../routes/UnidadesMedidaRoutes";

/**
 * B3 - Bloco 2: a Gestao de Unidades de Medida consome a API real (`administracao/unidades-medida`).
 * Mesmo padrao de integracao HTTP de `administration/chart-of-accounts/tests/ContasContabeisPage.test.tsx`.
 */
type UnidadeMedidaApiDto = {
  codigoErp: string;
  descricaoErp: string;
  descricaoMaisCompras?: string | null;
  ativoNoMaisCompras: boolean;
  temMetadadoLocal: boolean;
  atualizadoEm?: string | null;
};

function unidadeDto(over: Partial<UnidadeMedidaApiDto> = {}): UnidadeMedidaApiDto {
  return {
    codigoErp: "UN",
    descricaoErp: "UNIDADE",
    descricaoMaisCompras: null,
    ativoNoMaisCompras: true,
    temMetadadoLocal: false,
    atualizadoEm: "2026-07-01T09:00:00Z",
    ...over
  };
}

type Rota = { status: number; body?: unknown };

let rotas: Map<string, Rota>;
let unidades: UnidadeMedidaApiDto[];

function chave(url: string, method: string): string {
  const semQuery = url.split("?")[0];
  const codigo = semQuery.startsWith("/api/administracao/unidades-medida/") ? "/api/administracao/unidades-medida/:codigo" : semQuery;
  return `${method} ${codigo}`;
}

beforeEach(() => {
  unidades = [
    unidadeDto(),
    unidadeDto({ codigoErp: "KG", descricaoErp: "QUILOGRAMA", descricaoMaisCompras: "Peso", temMetadadoLocal: true }),
    unidadeDto({ codigoErp: "CJ", descricaoErp: "CONJUNTO", ativoNoMaisCompras: false, temMetadadoLocal: true })
  ];
  rotas = new Map<string, Rota>();
  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url === "/api/administracao/unidades-medida") {
      return { ok: true, status: 200, json: async () => unidades } as Response;
    }

    if (method === "PUT" && url.startsWith("/api/administracao/unidades-medida/")) {
      const codigo = decodeURIComponent(url.split("/api/administracao/unidades-medida/")[1]);
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const existente = unidades.find((u) => u.codigoErp === codigo);
      if (!existente) return { ok: false, status: 404, json: async () => ({ code: "unidade_medida_nao_encontrada" }) } as Response;
      const atualizado: UnidadeMedidaApiDto = {
        ...existente,
        descricaoMaisCompras: body.descricaoMaisCompras ?? null,
        ativoNoMaisCompras: body.ativoNoMaisCompras,
        temMetadadoLocal: true,
        atualizadoEm: new Date().toISOString()
      };
      unidades = unidades.map((u) => (u.codigoErp === codigo ? atualizado : u));
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

function renderUnidadesMedida(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <UnidadesMedidaRoutes />
    </MemoryRouter>
  );
}

describe("UnidadesMedidaPage", () => {
  it("lista as unidades de medida vindas da API com Código, Descrição ERP e Descrição +Compras", async () => {
    renderUnidadesMedida();
    expect(await screen.findByRole("heading", { name: "Unidades de medida integradas do ERP" })).toBeInTheDocument();
    expect(await screen.findByText("UN")).toBeInTheDocument();
    expect(await screen.findByText("QUILOGRAMA")).toBeInTheDocument();
    expect(await screen.findByText("Peso")).toBeInTheDocument();
  });

  it("nao exibe nenhum botao de criar ou excluir unidade de medida", async () => {
    renderUnidadesMedida();
    await screen.findByText("UN");
    expect(screen.queryByRole("button", { name: /nova unidade/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /criar/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /excluir/i })).not.toBeInTheDocument();
  });

  it("permite editar a Descrição +Compras sem alterar a Descrição ERP", async () => {
    renderUnidadesMedida();
    const row = (await screen.findByText("UNIDADE")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Editar unidade de medida", level: 1 });
    await userEvent.type(await screen.findByLabelText("Descrição +Compras"), "Unidade avulsa");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Detalhes da unidade de medida" })).toBeInTheDocument());
    expect(await screen.findByText("Unidade avulsa")).toBeInTheDocument();
  });

  it("ativa e inativa a unidade de medida no +Compras diretamente pela listagem", async () => {
    renderUnidadesMedida();
    const row = (await screen.findByText("CONJUNTO")).closest("tr")!;
    expect(within(row).getByText("Inativo")).toBeInTheDocument();

    await userEvent.click(within(row).getByRole("button", { name: "Ativar no +Compras" }));

    await waitFor(() => {
      const updatedRow = screen.getByText("CONJUNTO").closest("tr")!;
      expect(within(updatedRow).getByText("Ativo")).toBeInTheDocument();
    });
  });

  it("filtra por status no +Compras", async () => {
    renderUnidadesMedida();
    await screen.findByText("UNIDADE");

    await userEvent.selectOptions(screen.getByLabelText("Status no +Compras"), "Inativo");

    expect(screen.queryByText("UNIDADE")).not.toBeInTheDocument();
    expect(screen.getByText("CONJUNTO")).toBeInTheDocument();
  });

  it("mostra acesso negado quando a API responde 403", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status: 403, json: async () => ({}) }) as Response));

    renderUnidadesMedida();

    expect(await screen.findByText(/não tem permissão para acessar a Gestão de Unidades de Medida/i)).toBeInTheDocument();
  });
});
