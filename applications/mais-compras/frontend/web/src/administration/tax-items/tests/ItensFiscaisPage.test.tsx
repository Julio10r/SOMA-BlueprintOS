import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ItensFiscaisRoutes } from "../routes/ItensFiscaisRoutes";

/**
 * B3 - Bloco 3: o cadastro de Item Fiscal consome a API real (`administracao/itens-fiscais`), mais os
 * clientes de Conta Contábil/Unidade de Medida (Blocos 1/2) para as opções do formulário. Mesmo padrão de
 * integração HTTP dos demais módulos de administração.
 */
type ItemFiscalApiDto = {
  id: string;
  codigo: string;
  descricao: string;
  unidadeMedidaCodigoErp: string;
  unidadeMedidaDescricao?: string | null;
  contaContabilCodigoErp: string;
  contaContabilDescricao?: string | null;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

function itemDto(over: Partial<ItemFiscalApiDto> = {}): ItemFiscalApiDto {
  return {
    id: "id-001",
    codigo: "001",
    descricao: "Notebook",
    unidadeMedidaCodigoErp: "UN",
    unidadeMedidaDescricao: "UNIDADE",
    contaContabilCodigoErp: "1.1.01",
    contaContabilDescricao: "CAIXA",
    ativo: true,
    criadoEm: "2026-09-01T09:00:00Z",
    atualizadoEm: "2026-09-01T09:00:00Z",
    ...over
  };
}

const contasContabeisDto = [
  { codigoErp: "1.1.01", descricaoErp: "CAIXA", inativaNoErp: false, descricaoMaisCompras: null, ativoNoMaisCompras: true, ativoEfetivo: true, temMetadadoLocal: false, atualizadoEm: null }
];
const unidadesMedidaDto = [
  { codigoErp: "UN", descricaoErp: "UNIDADE", descricaoMaisCompras: null, ativoNoMaisCompras: true, temMetadadoLocal: false, atualizadoEm: null }
];

/** B3 - Bloco 4: fornecedores ativos existentes no cadastro, para o seletor de Referências por Fornecedor. */
const fornecedoresDto = [
  { id: "fornecedor-amazon", razaoSocial: "Amazon Servicos Brasil Ltda", nomeFantasia: "Amazon", cnpj_Cpf: "12345678000100", status: "Ativo" },
  { id: "fornecedor-apple", razaoSocial: "Apple Computer Brasil Ltda", nomeFantasia: "Apple", cnpj_Cpf: "98765432000100", status: "Ativo" }
];

type ItemFiscalReferenciaFornecedorApiDto = {
  id: string;
  itemFiscalId: string;
  fornecedorId: string;
  fornecedorNome: string;
  codigoItemFornecedor: string;
  criadoEm: string;
  atualizadoEm: string;
};

type Rota = { status: number; body?: unknown };

let rotas: Map<string, Rota>;
let itens: ItemFiscalApiDto[];
let referencias: ItemFiscalReferenciaFornecedorApiDto[];

function referenciaDto(over: Partial<ItemFiscalReferenciaFornecedorApiDto> = {}): ItemFiscalReferenciaFornecedorApiDto {
  return {
    id: "ref-001",
    itemFiscalId: "id-001",
    fornecedorId: "fornecedor-amazon",
    fornecedorNome: "Amazon",
    codigoItemFornecedor: "hduahd78",
    criadoEm: "2026-09-02T09:00:00Z",
    atualizadoEm: "2026-09-02T09:00:00Z",
    ...over
  };
}

async function handleReferenciasFornecedor(url: string, method: string, init?: RequestInit): Promise<Response> {
  const semReferencias = url.split("/referencias-fornecedor")[0];
  const itemFiscalId = decodeURIComponent(semReferencias.split("/api/administracao/itens-fiscais/")[1]);
  const resto = url.split("/referencias-fornecedor")[1] ?? "";
  const referenciaId = resto.startsWith("/") ? decodeURIComponent(resto.slice(1)) : null;

  if (method === "GET") {
    return { ok: true, status: 200, json: async () => referencias.filter((r) => r.itemFiscalId === itemFiscalId) } as Response;
  }
  if (method === "POST") {
    const body = init?.body ? JSON.parse(String(init.body)) : {};
    const fornecedor = fornecedoresDto.find((f) => f.id === body.fornecedorId);
    const nova = referenciaDto({
      id: `ref-${referencias.length + 1}`,
      itemFiscalId,
      fornecedorId: body.fornecedorId,
      fornecedorNome: fornecedor?.nomeFantasia ?? "Desconhecido",
      codigoItemFornecedor: body.codigoItemFornecedor
    });
    referencias = [...referencias, nova];
    return { ok: true, status: 201, json: async () => nova } as Response;
  }
  if (method === "PUT" && referenciaId) {
    const body = init?.body ? JSON.parse(String(init.body)) : {};
    const existente = referencias.find((r) => r.id === referenciaId);
    if (!existente) return { ok: false, status: 404, json: async () => ({}) } as Response;
    const atualizada = { ...existente, codigoItemFornecedor: body.codigoItemFornecedor };
    referencias = referencias.map((r) => (r.id === referenciaId ? atualizada : r));
    return { ok: true, status: 200, json: async () => atualizada } as Response;
  }
  if (method === "DELETE" && referenciaId) {
    referencias = referencias.filter((r) => r.id !== referenciaId);
    return { ok: true, status: 204, json: async () => ({}) } as Response;
  }
  return { ok: false, status: 404, json: async () => ({}) } as Response;
}

function chave(url: string, method: string): string {
  const semQuery = url.split("?")[0];
  const codigo = semQuery.startsWith("/api/administracao/itens-fiscais/") ? "/api/administracao/itens-fiscais/:id" : semQuery;
  return `${method} ${codigo}`;
}

beforeEach(() => {
  itens = [
    itemDto(),
    itemDto({ id: "id-002", codigo: "002", descricao: "MacBook Pro 14", ativo: false })
  ];
  referencias = [];
  rotas = new Map<string, Rota>();
  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url === "/api/administracao/itens-fiscais") {
      return { ok: true, status: 200, json: async () => itens } as Response;
    }
    if (url.includes("/referencias-fornecedor")) {
      return handleReferenciasFornecedor(url, method, init);
    }
    if (method === "GET" && url.startsWith("/api/administracao/itens-fiscais/")) {
      const id = decodeURIComponent(url.split("/api/administracao/itens-fiscais/")[1]);
      const encontrado = itens.find((i) => i.id === id);
      if (!encontrado) return { ok: false, status: 404, json: async () => ({}) } as Response;
      return { ok: true, status: 200, json: async () => encontrado } as Response;
    }
    if (method === "GET" && url === "/api/administracao/contas-contabeis") {
      return { ok: true, status: 200, json: async () => contasContabeisDto } as Response;
    }
    if (method === "GET" && url === "/api/administracao/unidades-medida") {
      return { ok: true, status: 200, json: async () => unidadesMedidaDto } as Response;
    }
    if (method === "GET" && url.startsWith("/fornecedores?q=")) {
      return { ok: true, status: 200, json: async () => ({ items: fornecedoresDto, totalCount: fornecedoresDto.length, page: 1, pageSize: 20 }) } as Response;
    }

    if (method === "POST" && url === "/api/administracao/itens-fiscais") {
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const novo: ItemFiscalApiDto = itemDto({
        id: "id-novo",
        codigo: body.codigo,
        descricao: body.descricao,
        unidadeMedidaCodigoErp: body.unidadeMedidaCodigoErp,
        contaContabilCodigoErp: body.contaContabilCodigoErp
      });
      itens = [...itens, novo];
      return { ok: true, status: 201, json: async () => novo } as Response;
    }

    if (method === "PUT" && url.startsWith("/api/administracao/itens-fiscais/")) {
      const id = decodeURIComponent(url.split("/api/administracao/itens-fiscais/")[1]);
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const existente = itens.find((i) => i.id === id);
      if (!existente) return { ok: false, status: 404, json: async () => ({ code: "item_fiscal_nao_encontrado" }) } as Response;
      const atualizado: ItemFiscalApiDto = { ...existente, descricao: body.descricao };
      itens = itens.map((i) => (i.id === id ? atualizado : i));
      return { ok: true, status: 200, json: async () => atualizado } as Response;
    }

    if (method === "PATCH" && url.endsWith("/status")) {
      const id = decodeURIComponent(url.split("/api/administracao/itens-fiscais/")[1].split("/status")[0]);
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const existente = itens.find((i) => i.id === id);
      if (!existente) return { ok: false, status: 404, json: async () => ({}) } as Response;
      const atualizado: ItemFiscalApiDto = { ...existente, ativo: body.ativo };
      itens = itens.map((i) => (i.id === id ? atualizado : i));
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

function renderItensFiscais(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <ItensFiscaisRoutes />
    </MemoryRouter>
  );
}

describe("ItensFiscaisPage", () => {
  it("lista os itens fiscais vindos da API, com granularidade genérica e específica lado a lado", async () => {
    renderItensFiscais();
    expect(await screen.findByRole("heading", { name: "Itens fiscais cadastrados" })).toBeInTheDocument();
    expect(await screen.findByText("Notebook")).toBeInTheDocument();
    expect(await screen.findByText("MacBook Pro 14")).toBeInTheDocument();
  });

  it("exibe o botão de criação (Item Fiscal é cadastro primário, diferente de Conta Contábil/Unidade)", async () => {
    renderItensFiscais();
    await screen.findByText("Notebook");
    expect(screen.getByRole("button", { name: "Novo item fiscal" })).toBeInTheDocument();
  });

  it("cadastra um novo item fiscal exigindo Unidade e Conta Contábil", async () => {
    renderItensFiscais("/novo");

    await screen.findByRole("heading", { name: "Novo item fiscal", level: 1 });
    await userEvent.type(screen.getByLabelText("Código"), "003");
    await userEvent.type(screen.getByLabelText("Descrição"), "Mouse sem fio");
    await userEvent.selectOptions(await screen.findByLabelText("Unidade"), "UN");
    await userEvent.selectOptions(await screen.findByLabelText("Conta Contábil"), "1.1.01");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Itens fiscais cadastrados" })).toBeInTheDocument());
    expect(await screen.findByText("Mouse sem fio")).toBeInTheDocument();
  });

  it("não permite editar o Código (imutável após a criação)", async () => {
    renderItensFiscais();
    const row = (await screen.findByText("Notebook")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Editar item fiscal", level: 1 });
    expect(screen.queryByLabelText("Código")).not.toBeInTheDocument();
    expect(screen.getByText("001")).toBeInTheDocument();
  });

  it("permite editar a Descrição de um item fiscal existente", async () => {
    renderItensFiscais();
    const row = (await screen.findByText("Notebook")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Editar item fiscal", level: 1 });
    const campoDescricao = await screen.findByLabelText("Descrição");
    await userEvent.clear(campoDescricao);
    await userEvent.type(campoDescricao, "Notebook Dell");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Detalhes do item fiscal" })).toBeInTheDocument());
    expect(await screen.findByRole("heading", { name: "Notebook Dell", level: 2 })).toBeInTheDocument();
  });

  it("ativa e inativa o item fiscal localmente pela listagem", async () => {
    renderItensFiscais();
    const row = (await screen.findByText("MacBook Pro 14")).closest("tr")!;
    expect(within(row).getByText("Inativo")).toBeInTheDocument();

    await userEvent.click(within(row).getByRole("button", { name: "Ativar" }));

    await waitFor(() => {
      const updatedRow = screen.getByText("MacBook Pro 14").closest("tr")!;
      expect(within(updatedRow).getByText("Ativo")).toBeInTheDocument();
    });
  });

  it("filtra por status", async () => {
    renderItensFiscais();
    await screen.findByText("Notebook");

    await userEvent.selectOptions(screen.getByLabelText("Status"), "Inativo");

    expect(screen.queryByText("Notebook")).not.toBeInTheDocument();
    expect(screen.getByText("MacBook Pro 14")).toBeInTheDocument();
  });

  it("mostra acesso negado quando a API responde 403", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status: 403, json: async () => ({}) }) as Response));

    renderItensFiscais();

    expect(await screen.findByText(/não tem permissão para executar esta ação/i)).toBeInTheDocument();
  });
});

describe("ItemFiscalForm — Referências por Fornecedor (B3 - Bloco 4)", () => {
  it("desabilita a aba de referências ao criar um novo item fiscal (precisa existir antes)", async () => {
    renderItensFiscais("/novo");

    await screen.findByRole("heading", { name: "Novo item fiscal", level: 1 });
    expect(screen.getByRole("tab", { name: "Referências por fornecedor" })).toBeDisabled();
  });

  it("lista as referências existentes ao abrir a aba na edição", async () => {
    referencias = [referenciaDto()];
    renderItensFiscais();
    const row = (await screen.findByText("Notebook")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Editar item fiscal", level: 1 });
    await userEvent.click(screen.getByRole("tab", { name: "Referências por fornecedor" }));

    expect(await screen.findByText("Amazon")).toBeInTheDocument();
    expect(screen.getByText("hduahd78")).toBeInTheDocument();
  });

  it("inclui uma nova referência de fornecedor", async () => {
    renderItensFiscais();
    const row = (await screen.findByText("Notebook")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));
    await screen.findByRole("heading", { name: "Editar item fiscal", level: 1 });
    await userEvent.click(screen.getByRole("tab", { name: "Referências por fornecedor" }));

    await userEvent.selectOptions(await screen.findByLabelText("Fornecedor"), "fornecedor-apple");
    await userEvent.type(screen.getByLabelText("Código no fornecedor"), "jaidjabdjao");
    await userEvent.click(screen.getByRole("button", { name: "Incluir referência" }));

    expect(await screen.findByText("Apple")).toBeInTheDocument();
    expect(screen.getByText("jaidjabdjao")).toBeInTheDocument();
  });

  it("permite corrigir o código no fornecedor de uma referência existente (Fornecedor é imutável)", async () => {
    referencias = [referenciaDto()];
    renderItensFiscais();
    const row = (await screen.findByText("Notebook")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));
    await screen.findByRole("heading", { name: "Editar item fiscal", level: 1 });
    await userEvent.click(screen.getByRole("tab", { name: "Referências por fornecedor" }));

    await screen.findByText("Amazon");
    await userEvent.click(screen.getByRole("button", { name: "Editar" }));
    const campoCodigo = screen.getByLabelText("Código no fornecedor Amazon");
    await userEvent.clear(campoCodigo);
    await userEvent.type(campoCodigo, "hduahd78-v2");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    expect(await screen.findByText("hduahd78-v2")).toBeInTheDocument();
  });

  it("remove uma referência de fornecedor (remoção física, sem inativação)", async () => {
    referencias = [referenciaDto()];
    renderItensFiscais();
    const row = (await screen.findByText("Notebook")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));
    await screen.findByRole("heading", { name: "Editar item fiscal", level: 1 });
    await userEvent.click(screen.getByRole("tab", { name: "Referências por fornecedor" }));

    await screen.findByText("Amazon");
    await userEvent.click(screen.getByRole("button", { name: "Remover" }));

    await waitFor(() => expect(screen.queryByText("hduahd78")).not.toBeInTheDocument());
    expect(screen.getByText("Nenhuma referência de fornecedor cadastrada ainda.")).toBeInTheDocument();
  });
});
