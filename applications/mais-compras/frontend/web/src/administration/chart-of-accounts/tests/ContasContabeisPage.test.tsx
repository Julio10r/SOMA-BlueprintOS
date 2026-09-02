import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ContasContabeisRoutes } from "../routes/ContasContabeisRoutes";

/**
 * B3 - Bloco 1: a Gestao de Contas Contabeis consome a API real (`administracao/contas-contabeis`).
 * Mesmo padrao de integracao HTTP de `administration/branches/tests/FiliaisPage.test.tsx`.
 */
type ContaContabilApiDto = {
  codigoErp: string;
  descricaoErp: string;
  inativaNoErp: boolean;
  descricaoMaisCompras?: string | null;
  ativoNoMaisCompras: boolean;
  ativoEfetivo: boolean;
  temMetadadoLocal: boolean;
  atualizadoEm?: string | null;
};

function contaDto(over: Partial<ContaContabilApiDto> = {}): ContaContabilApiDto {
  return {
    codigoErp: "1.1.01",
    descricaoErp: "CAIXA",
    inativaNoErp: false,
    descricaoMaisCompras: null,
    ativoNoMaisCompras: true,
    ativoEfetivo: true,
    temMetadadoLocal: false,
    atualizadoEm: "2026-07-01T09:00:00Z",
    ...over
  };
}

type Rota = { status: number; body?: unknown };

let rotas: Map<string, Rota>;
let contas: ContaContabilApiDto[];

function chave(url: string, method: string): string {
  const semQuery = url.split("?")[0];
  const codigo = semQuery.startsWith("/api/administracao/contas-contabeis/") ? "/api/administracao/contas-contabeis/:codigo" : semQuery;
  return `${method} ${codigo}`;
}

beforeEach(() => {
  contas = [
    contaDto(),
    contaDto({ codigoErp: "1.1.02", descricaoErp: "BANCOS", descricaoMaisCompras: "Conta corrente principal", temMetadadoLocal: true }),
    contaDto({ codigoErp: "2.9.99", descricaoErp: "CONTA ENCERRADA", inativaNoErp: true, ativoEfetivo: false }),
    contaDto({ codigoErp: "3.1.01", descricaoErp: "DESPESAS DIVERSAS", ativoNoMaisCompras: false, ativoEfetivo: false, temMetadadoLocal: true })
  ];
  rotas = new Map<string, Rota>();
  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url === "/api/administracao/contas-contabeis") {
      return { ok: true, status: 200, json: async () => contas } as Response;
    }

    if (method === "PUT" && url.startsWith("/api/administracao/contas-contabeis/")) {
      const codigo = decodeURIComponent(url.split("/api/administracao/contas-contabeis/")[1]);
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const existente = contas.find((c) => c.codigoErp === codigo);
      if (!existente) return { ok: false, status: 404, json: async () => ({ code: "conta_contabil_nao_encontrada" }) } as Response;
      const atualizado: ContaContabilApiDto = {
        ...existente,
        descricaoMaisCompras: body.descricaoMaisCompras ?? null,
        ativoNoMaisCompras: body.ativoNoMaisCompras,
        ativoEfetivo: !existente.inativaNoErp && body.ativoNoMaisCompras,
        temMetadadoLocal: true,
        atualizadoEm: new Date().toISOString()
      };
      contas = contas.map((c) => (c.codigoErp === codigo ? atualizado : c));
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

function renderContasContabeis(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <ContasContabeisRoutes />
    </MemoryRouter>
  );
}

describe("ContasContabeisPage", () => {
  it("lista as contas contábeis vindas da API com Código, Descrição ERP e Descrição +Compras", async () => {
    renderContasContabeis();
    expect(await screen.findByRole("heading", { name: "Contas contábeis integradas do ERP" })).toBeInTheDocument();
    expect(await screen.findByText("1.1.01")).toBeInTheDocument();
    expect(await screen.findByText("CAIXA")).toBeInTheDocument();
    expect(await screen.findByText("Conta corrente principal")).toBeInTheDocument();
  });

  it("nao exibe nenhum botao de criar ou excluir conta contábil", async () => {
    renderContasContabeis();
    await screen.findByText("1.1.01");
    expect(screen.queryByRole("button", { name: /nova conta/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /criar/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /excluir/i })).not.toBeInTheDocument();
  });

  it("mostra status Linx e status +Compras separadamente, respeitando ADR-0024", async () => {
    renderContasContabeis();
    const row = (await screen.findByText("CONTA ENCERRADA")).closest("tr")!;
    // Duas colunas de status: Status Linx e Status +Compras — ambas "Inativo" para esta conta.
    expect(within(row).getAllByText("Inativo").length).toBeGreaterThanOrEqual(1);
  });

  it("exibe aviso ao editar uma conta inativa no Linx", async () => {
    renderContasContabeis();
    const row = (await screen.findByText("CONTA ENCERRADA")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    expect(await screen.findByText(/esta conta está inativa no Linx/i)).toBeInTheDocument();
  });

  it("permite editar a Descrição +Compras sem alterar a Descrição ERP", async () => {
    renderContasContabeis();
    const row = (await screen.findByText("CAIXA")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Editar conta contábil", level: 1 });
    await userEvent.type(await screen.findByLabelText("Descrição +Compras"), "Caixa da matriz");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Detalhes da conta contábil" })).toBeInTheDocument());
    expect(await screen.findByText("Caixa da matriz")).toBeInTheDocument();
    expect(screen.getAllByText("CAIXA").length).toBeGreaterThan(0);
  });

  it("filtra por status efetivo no +Compras", async () => {
    renderContasContabeis();
    await screen.findByText("CAIXA");

    await userEvent.selectOptions(screen.getByLabelText("Status no +Compras"), "Inativo");

    expect(screen.queryByText("CAIXA")).not.toBeInTheDocument();
    expect(screen.getByText("DESPESAS DIVERSAS")).toBeInTheDocument();
  });

  it("pesquisa por código", async () => {
    renderContasContabeis();
    await screen.findByText("CAIXA");

    await userEvent.type(screen.getByLabelText("Pesquisar"), "1.1.02");

    expect(screen.getByText("BANCOS")).toBeInTheDocument();
    expect(screen.queryByText("CAIXA")).not.toBeInTheDocument();
  });

  it("mostra acesso negado quando a API responde 403", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status: 403, json: async () => ({}) }) as Response));

    renderContasContabeis();

    expect(await screen.findByText(/não tem permissão para acessar a Gestão de Contas Contábeis/i)).toBeInTheDocument();
  });
});
