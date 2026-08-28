import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthContext } from "../../../auth/context/AuthContext";
import { FornecedoresPage } from "../pages/FornecedoresPage";
import { FornecedorDetalhePage } from "../pages/FornecedorDetalhePage";
import type { Fornecedor } from "../types/linxSupplierContract";

const usuarioTeste = {
  id: "u1",
  email: "ana@somagrupo.com.br",
  nome: "Ana Souza",
  unidadeNegocioId: "un1",
  permissoes: [],
  escopoAdministrativo: "Produto" as const
};

function fornecedor(over: Partial<Fornecedor> = {}): Fornecedor {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    razaoSocial: "ABC Comercio LTDA",
    nomeFantasia: "ABC",
    cnpj_Cpf: "12345678000195",
    tipoPessoa: "PJ",
    status: "Ativo",
    email: "contato@abc.example",
    telefone: "11999999999",
    cidade: "São Paulo",
    estado: "SP",
    ...over
  };
}

type Rota = { status: number; body?: unknown };

let rotas: Map<string, Rota>;
let chamadas: Array<{ url: string; method: string; body?: unknown }>;

function responder(url: string, method: string): Rota {
  const semQuery = url.split("?")[0];
  return rotas.get(`${method} ${url}`) ?? rotas.get(`${method} ${semQuery}`) ?? { status: 404, body: {} };
}

beforeEach(() => {
  rotas = new Map<string, Rota>();
  chamadas = [];
  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";
    const body = init?.body ? JSON.parse(String(init.body)) : undefined;
    chamadas.push({ url, method, body });
    const rota = responder(url, method);
    return {
      ok: rota.status >= 200 && rota.status < 300,
      status: rota.status,
      json: async () => rota.body ?? {}
    } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function paginado(items: Fornecedor[], overrides: Partial<{ totalCount: number; page: number; pageSize: number }> = {}) {
  return { items, totalCount: overrides.totalCount ?? items.length, page: overrides.page ?? 1, pageSize: overrides.pageSize ?? 20 };
}

function renderApp(initialPath = "/fornecedores") {
  return render(
    <AuthContext.Provider value={{ usuario: usuarioTeste, carregando: false, refresh: vi.fn(), setUsuario: vi.fn(), logout: vi.fn() }}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/fornecedores" element={<FornecedoresPage />} />
          <Route path="/fornecedores/:id" element={<FornecedorDetalhePage />} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>
  );
}

describe("FornecedoresPage — listagem", () => {
  it("lista fornecedores paginados vindos da API", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([fornecedor(), fornecedor({ id: "2", razaoSocial: "Beta LTDA", cnpj_Cpf: "98765432000100" })]) });

    renderApp();

    expect(await screen.findByText("ABC Comercio LTDA")).toBeInTheDocument();
    expect(await screen.findByText("Beta LTDA")).toBeInTheDocument();
    expect(await screen.findByText(/Exibindo 1–2 de 2 fornecedores/)).toBeInTheDocument();
  });

  it("mostra o estado vazio quando nao ha resultados", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([]) });

    renderApp();

    expect(await screen.findByText("Nenhum fornecedor encontrado.")).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "+ Novo fornecedor" }).length).toBeGreaterThan(0);
  });

  it("mostra erro quando a API falha", async () => {
    rotas.set("GET /fornecedores", { status: 500, body: { message: "Falha inesperada." } });

    renderApp();

    expect(await screen.findByText("Falha inesperada.")).toBeInTheDocument();
  });

  it("atualiza a busca na URL e refaz a pesquisa", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([fornecedor()]) });

    renderApp();
    await screen.findByText("ABC Comercio LTDA");

    await userEvent.type(screen.getByPlaceholderText("Buscar por CNPJ ou nome..."), "ABC");

    await waitFor(() => {
      expect(chamadas.some((c) => c.method === "GET" && c.url.includes("q=ABC"))).toBe(true);
    });
  });

  it("aplica o filtro de status", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([fornecedor()]) });

    renderApp();
    await screen.findByText("ABC Comercio LTDA");

    await userEvent.selectOptions(screen.getByLabelText("Status"), "Inativo");

    await waitFor(() => {
      expect(chamadas.some((c) => c.method === "GET" && c.url.includes("status=Inativo"))).toBe(true);
    });
  });

  it("inativa um fornecedor via PATCH, nunca DELETE", async () => {
    const alvo = fornecedor();
    rotas.set("GET /fornecedores", { status: 200, body: paginado([alvo]) });
    rotas.set(`PATCH /fornecedores/${alvo.id}/status`, { status: 200, body: { ...alvo, status: "Inativo" } });

    renderApp();
    const row = (await screen.findByText("ABC Comercio LTDA")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Inativar" }));

    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText("Inativar fornecedor?")).toBeInTheDocument();
    await userEvent.click(within(dialog).getByRole("button", { name: "Inativar fornecedor" }));

    await waitFor(() => {
      const patch = chamadas.find((c) => c.method === "PATCH");
      expect(patch).toBeDefined();
      expect(patch!.body).toEqual({ ativo: false });
      expect(patch!.url).not.toContain("DELETE");
    });
  });

  it("valida o formulario de cadastro manual (campos obrigatorios e CNPJ invalido)", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([]) });

    renderApp();
    await screen.findByText("Nenhum fornecedor encontrado.");

    await userEvent.click(screen.getAllByRole("button", { name: "+ Novo fornecedor" })[0]);
    await userEvent.click(screen.getByRole("button", { name: "Preencher manualmente" }));

    await userEvent.type(screen.getByLabelText("CNPJ *"), "11111111111111");
    await userEvent.click(screen.getByRole("button", { name: "Cadastrar fornecedor" }));

    expect(await screen.findByText("Informe a razão social.")).toBeInTheDocument();
    expect(await screen.findByText("CNPJ inválido.")).toBeInTheDocument();
    expect(chamadas.some((c) => c.method === "POST" && c.url === "/fornecedores")).toBe(false);
  });

  it("cadastra um fornecedor manualmente com dados validos", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([]) });
    rotas.set("POST /fornecedores", { status: 201, body: fornecedor({ razaoSocial: "Nova Fornecedora" }) });

    renderApp();
    await screen.findByText("Nenhum fornecedor encontrado.");

    await userEvent.click(screen.getAllByRole("button", { name: "+ Novo fornecedor" })[0]);
    await userEvent.click(screen.getByRole("button", { name: "Preencher manualmente" }));

    await userEvent.type(screen.getByLabelText("Razão Social *"), "Nova Fornecedora");
    await userEvent.type(screen.getByLabelText("Nome Fantasia *"), "Nova Fantasia");
    await userEvent.type(screen.getByLabelText("CNPJ *"), "11.222.333/0001-81");
    await userEvent.click(screen.getByRole("button", { name: "Cadastrar fornecedor" }));

    await waitFor(() => {
      expect(chamadas.some((c) => c.method === "POST" && c.url === "/fornecedores")).toBe(true);
    });
  });

  it("bloqueia o cadastro manual e exibe erro quando o Nome Fantasia nao e informado", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([]) });

    renderApp();
    await screen.findByText("Nenhum fornecedor encontrado.");

    await userEvent.click(screen.getAllByRole("button", { name: "+ Novo fornecedor" })[0]);
    await userEvent.click(screen.getByRole("button", { name: "Preencher manualmente" }));

    await userEvent.type(screen.getByLabelText("Razão Social *"), "Nova Fornecedora");
    await userEvent.type(screen.getByLabelText("CNPJ *"), "11.222.333/0001-81");
    await userEvent.click(screen.getByRole("button", { name: "Cadastrar fornecedor" }));

    expect(await screen.findByText("Informe o nome fantasia.")).toBeInTheDocument();
    expect(chamadas.some((c) => c.method === "POST" && c.url === "/fornecedores")).toBe(false);
  });
});

describe("FornecedoresPage — StatusSincronizacao real (nao inferido)", () => {
  it("renderiza o StatusSincronizacao vindo do backend, mesmo sem erpFornecedorId", async () => {
    const alvo = fornecedor({ statusSincronizacao: "Sincronizado" });
    rotas.set("GET /fornecedores", { status: 200, body: paginado([alvo]) });

    renderApp();

    const row = (await screen.findByText("ABC Comercio LTDA")).closest("tr")!;
    expect(within(row).getByText("Sincronizado")).toBeInTheDocument();
  });

  it("traduz StatusSincronizacao=Falhou para 'Erro de sincronização', sem inferir do erpFornecedorId", async () => {
    const alvo = fornecedor({ statusSincronizacao: "Falhou", erpFornecedorId: "ERP-123" });
    rotas.set("GET /fornecedores", { status: 200, body: paginado([alvo]) });

    renderApp();

    const row = (await screen.findByText("ABC Comercio LTDA")).closest("tr")!;
    expect(within(row).getByText("Erro de sincronização")).toBeInTheDocument();
    expect(within(row).queryByText("Sincronizado")).not.toBeInTheDocument();
  });

  it("mostra 'Pendente' quando statusSincronizacao esta ausente", async () => {
    const alvo = fornecedor();
    rotas.set("GET /fornecedores", { status: 200, body: paginado([alvo]) });

    renderApp();

    const row = (await screen.findByText("ABC Comercio LTDA")).closest("tr")!;
    expect(within(row).getByText("Pendente")).toBeInTheDocument();
  });
});

describe("FornecedoresPage — debounce e requisicoes obsoletas", () => {
  it("nao dispara uma requisicao por tecla digitada (debounce)", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([fornecedor()]) });

    renderApp();
    await screen.findByText("ABC Comercio LTDA");
    chamadas = [];

    await userEvent.type(screen.getByPlaceholderText("Buscar por CNPJ ou nome..."), "amazon");

    // Enquanto o debounce ainda esta pendente, nenhuma nova requisicao GET deve ter sido feita.
    expect(chamadas.filter((c) => c.method === "GET").length).toBe(0);

    await waitFor(() => {
      expect(chamadas.some((c) => c.method === "GET" && c.url.includes("q=amazon"))).toBe(true);
    });

    // Apenas uma unica requisicao para o termo final, nao uma por tecla.
    expect(chamadas.filter((c) => c.method === "GET").length).toBe(1);
  });

  it("nao deixa uma resposta atrasada de um termo antigo sobrescrever o resultado do termo atual", async () => {
    const resolverAmHolder: { current: (() => void) | null } = { current: null };
    const respostaLenta = new Promise<void>((resolve) => { resolverAmHolder.current = resolve; });

    vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";
      chamadas.push({ url, method });
      if (url.includes("q=am") && !url.includes("q=amazon")) {
        await respostaLenta;
        return { ok: true, status: 200, json: async () => paginado([fornecedor({ razaoSocial: "Resultado Antigo AM" })]) } as Response;
      }
      if (url.includes("q=amazon")) {
        return { ok: true, status: 200, json: async () => paginado([fornecedor({ razaoSocial: "Resultado Correto Amazon" })]) } as Response;
      }
      return { ok: true, status: 200, json: async () => paginado([]) } as Response;
    }));

    renderApp();
    await waitFor(() => expect(chamadas.length).toBeGreaterThan(0));

    const input = screen.getByPlaceholderText("Buscar por CNPJ ou nome...");
    await userEvent.type(input, "am");
    await waitFor(() => expect(chamadas.some((c) => c.url.includes("q=am") && !c.url.includes("q=amazon"))).toBe(true));

    await userEvent.type(input, "azon");
    await waitFor(() => expect(chamadas.some((c) => c.url.includes("q=amazon"))).toBe(true));

    expect(await screen.findByText("Resultado Correto Amazon")).toBeInTheDocument();

    resolverAmHolder.current?.();
    await new Promise((resolve) => setTimeout(resolve, 20));

    expect(screen.queryByText("Resultado Antigo AM")).not.toBeInTheDocument();
    expect(screen.getByText("Resultado Correto Amazon")).toBeInTheDocument();
  });
});

describe("FornecedoresPage — acessibilidade dos campos de filtro", () => {
  it("o campo de pesquisa e o filtro de status tem id/name associados ao label", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([fornecedor()]) });

    renderApp();
    await screen.findByText("ABC Comercio LTDA");

    const searchInput = screen.getByLabelText("Pesquisar") as HTMLInputElement;
    expect(searchInput.id).toBe("fornecedores-pesquisa");
    expect(searchInput.name).toBe("pesquisa");

    const statusSelect = screen.getByLabelText("Status") as HTMLSelectElement;
    expect(statusSelect.id).toBe("fornecedores-status");
    expect(statusSelect.name).toBe("status");
  });
});

describe("FornecedoresPage — navegacao para detalhe e estado de URL", () => {
  it("navega para o detalhe ao clicar na linha e preserva a busca ao voltar", async () => {
    const alvo = fornecedor();
    rotas.set("GET /fornecedores", { status: 200, body: paginado([alvo]) });
    rotas.set(`GET /fornecedores/${alvo.id}`, { status: 200, body: alvo });

    renderApp("/fornecedores?search=ABC&status=Ativo&page=1");
    const row = (await screen.findByText("ABC Comercio LTDA")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Ver" }));

    expect(await screen.findByRole("heading", { name: "ABC Comercio LTDA" })).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Voltar" }));

    await waitFor(() => {
      expect(chamadas.some((c) => c.method === "GET" && c.url.includes("q=ABC") && c.url.includes("status=Ativo"))).toBe(true);
    });
  });
});

describe("FornecedorDetalhePage — leitura, edicao e status", () => {
  it("mostra o fornecedor em modo leitura", async () => {
    const alvo = fornecedor();
    rotas.set(`GET /fornecedores/${alvo.id}`, { status: 200, body: alvo });

    renderApp(`/fornecedores/${alvo.id}`);

    expect(await screen.findByRole("heading", { name: "ABC Comercio LTDA" })).toBeInTheDocument();
    expect(screen.getByText("12345678000195")).toBeInTheDocument();
    expect(screen.queryByLabelText("Razão Social *")).not.toBeInTheDocument();
  });

  it("alterna para o modo de edicao e salva via PUT", async () => {
    const alvo = fornecedor();
    rotas.set(`GET /fornecedores/${alvo.id}`, { status: 200, body: alvo });
    rotas.set(`PUT /fornecedores/${alvo.id}`, { status: 200, body: { ...alvo, razaoSocial: "ABC Atualizada" } });

    renderApp(`/fornecedores/${alvo.id}`);
    await screen.findByRole("heading", { name: "ABC Comercio LTDA" });

    await userEvent.click(screen.getByRole("button", { name: "Editar fornecedor" }));
    const razaoSocialInput = await screen.findByLabelText("Razão Social *");
    await userEvent.clear(razaoSocialInput);
    await userEvent.type(razaoSocialInput, "ABC Atualizada");
    await userEvent.click(screen.getByRole("button", { name: "Salvar alterações" }));

    await waitFor(() => {
      const put = chamadas.find((c) => c.method === "PUT");
      expect(put).toBeDefined();
    });
    expect(await screen.findByRole("heading", { name: "ABC Atualizada" })).toBeInTheDocument();
  });

  it("reativa um fornecedor inativo via PATCH", async () => {
    const alvo = fornecedor({ status: "Inativo" });
    rotas.set(`GET /fornecedores/${alvo.id}`, { status: 200, body: alvo });
    rotas.set(`PATCH /fornecedores/${alvo.id}/status`, { status: 200, body: { ...alvo, status: "Ativo" } });

    renderApp(`/fornecedores/${alvo.id}`);
    await screen.findByRole("heading", { name: "ABC Comercio LTDA" });

    await userEvent.click(screen.getByRole("button", { name: "Ativar fornecedor" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText("Ativar fornecedor?")).toBeInTheDocument();
    await userEvent.click(within(dialog).getByRole("button", { name: "Ativar fornecedor" }));

    await waitFor(() => {
      const patch = chamadas.find((c) => c.method === "PATCH");
      expect(patch!.body).toEqual({ ativo: true });
    });
  });
});
