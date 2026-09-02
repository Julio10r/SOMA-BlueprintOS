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
  // Fornecedor.Criar/Editar: gate de permissão (2026-09-01) para "+ Novo fornecedor"/"Editar
  // fornecedor"/"Ativar-Inativar" — usuário de teste padrão tem ambas para exercitar os fluxos.
  permissoes: ["Fornecedor.Criar", "Fornecedor.Editar"],
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
    cep: "01001000",
    logradouro: "Praca da Se",
    numero: "100",
    bairro: "Se",
    cidade: "São Paulo",
    estado: "SP",
    pais: "Brasil",
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
  // Categoria (Gate de homologação, 2026-09-01) é combobox do catálogo pré-cadastrado — registrado
  // por padrão em todo teste (ManualFornecedorForm busca essa rota ao montar).
  rotas.set("GET /fornecedores/categorias", {
    status: 200,
    body: [
      { codigo: "EMBALAGEM", descricao: "Embalagem" },
      { codigo: "OUTROS", descricao: "Outros" }
    ]
  });
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

  it("abre a listagem filtrando por Ativo por padrao, sem parametro status na URL (Gate homologacao Fornecedores)", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([fornecedor()]) });

    renderApp();
    await screen.findByText("ABC Comercio LTDA");

    await waitFor(() => {
      expect(chamadas.some((c) => c.method === "GET" && c.url.includes("status=Ativo"))).toBe(true);
    });
    expect(chamadas.some((c) => c.method === "GET" && c.url.includes("status=Todos"))).toBe(false);
    expect(await screen.findByLabelText("Status")).toHaveValue("Ativo");
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

  it("nao mostra 'Inativar' na listagem (só dentro da edição, item de feedback do homologador)", async () => {
    const alvo = fornecedor();
    rotas.set("GET /fornecedores", { status: 200, body: paginado([alvo]) });

    renderApp();
    const row = (await screen.findByText("ABC Comercio LTDA")).closest("tr")!;
    expect(within(row).queryByRole("button", { name: "Inativar" })).not.toBeInTheDocument();
    expect(within(row).getByRole("button", { name: "Ver" })).toBeInTheDocument();
  });

  it("valida o formulario de cadastro manual (campos obrigatorios e CNPJ invalido)", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([]) });

    renderApp();
    await screen.findByText("Nenhum fornecedor encontrado.");

    await userEvent.click(screen.getAllByRole("button", { name: "+ Novo fornecedor" })[0]);

    await userEvent.type(screen.getByLabelText(/^CNPJ\/CPF \*/), "11111111111111");
    await userEvent.click(screen.getByRole("button", { name: "Cadastrar fornecedor" }));

    expect(await screen.findByText("Informe a razão social.")).toBeInTheDocument();
    expect(await screen.findByText("CNPJ inválido.")).toBeInTheDocument();
    expect(chamadas.some((c) => c.method === "POST" && c.url === "/fornecedores")).toBe(false);
  });

  it("cadastra um fornecedor manualmente com dados validos", async () => {
    rotas.set("GET /fornecedores", { status: 200, body: paginado([]) });
    rotas.set("POST /fornecedores", { status: 201, body: fornecedor({ razaoSocial: "Nova Fornecedora" }) });
    // Cidade é combo dependente da UF (municípios reais via IBGE) — mock do backend.
    rotas.set("GET /fornecedores/municipios", { status: 200, body: ["São Paulo"] });

    renderApp();
    await screen.findByText("Nenhum fornecedor encontrado.");

    await userEvent.click(screen.getAllByRole("button", { name: "+ Novo fornecedor" })[0]);

    await userEvent.type(screen.getByLabelText("Razão Social *"), "Nova Fornecedora");
    await userEvent.type(screen.getByLabelText("Nome Fantasia *"), "Nova Fantasia");
    await userEvent.selectOptions(await screen.findByLabelText("Categoria *"), "Embalagem");
    await userEvent.type(screen.getByLabelText(/^CNPJ\/CPF \*/), "11.222.333/0001-81");
    await userEvent.tab();
    // CNPJ válido dispara o modal "Deseja consultar online...?" — recusa para seguir o
    // preenchimento manual determinístico deste teste (sem depender de rede/BrasilAPI).
    await userEvent.click(await screen.findByRole("button", { name: "Não, cadastrar manualmente" }));
    // Gate de homologação (2026-09-01), item 6: endereço completo e contato são obrigatórios.
    await userEvent.type(screen.getByLabelText("CEP *"), "01001000");
    await userEvent.type(screen.getByLabelText("Logradouro *"), "Praca da Se");
    await userEvent.type(screen.getByLabelText("Número *"), "100");
    await userEvent.type(screen.getByLabelText("Bairro *"), "Se");
    await userEvent.selectOptions(screen.getByLabelText("UF *"), "SP");
    await userEvent.selectOptions(await screen.findByLabelText(/^Cidade/), "São Paulo");
    await userEvent.type(screen.getByLabelText("E-mail *"), "contato@nova.example");
    await userEvent.type(screen.getByLabelText("Telefone *"), "11988887777");
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

    await userEvent.type(screen.getByLabelText("Razão Social *"), "Nova Fornecedora");
    await userEvent.type(screen.getByLabelText(/^CNPJ\/CPF \*/), "11.222.333/0001-81");
    await userEvent.click(screen.getByRole("button", { name: "Cadastrar fornecedor" }));

    expect(await screen.findByText("Informe o nome fantasia.")).toBeInTheDocument();
    expect(chamadas.some((c) => c.method === "POST" && c.url === "/fornecedores")).toBe(false);
  });

  it("nunca duplica: quando o CNPJ/CPF ja existe como Fornecedor no Linx, mostra aviso e abre o detalhe existente ao clicar OK (Gate homologacao, validacao de existencia no Linx)", async () => {
    const existente = fornecedor({ id: "existente-1", razaoSocial: "Fornecedora Ja Existente" });
    rotas.set("GET /fornecedores", { status: 200, body: paginado([]) });
    rotas.set("GET /fornecedores/municipios", { status: 200, body: ["São Paulo"] });
    rotas.set("POST /fornecedores", {
      status: 409,
      body: {
        code: "ja_existe_no_erp",
        fornecedorId: existente.id,
        message: "Este fornecedor já está cadastrado no Linx. Os dados existentes serão exibidos."
      }
    });
    rotas.set(`GET /fornecedores/${existente.id}`, { status: 200, body: existente });

    renderApp();
    await screen.findByText("Nenhum fornecedor encontrado.");

    await userEvent.click(screen.getAllByRole("button", { name: "+ Novo fornecedor" })[0]);

    await userEvent.type(screen.getByLabelText("Razão Social *"), "Nova Fornecedora");
    await userEvent.type(screen.getByLabelText("Nome Fantasia *"), "Nova Fantasia");
    await userEvent.selectOptions(await screen.findByLabelText("Categoria *"), "Embalagem");
    await userEvent.type(screen.getByLabelText(/^CNPJ\/CPF \*/), "11.222.333/0001-81");
    await userEvent.tab();
    await userEvent.click(await screen.findByRole("button", { name: "Não, cadastrar manualmente" }));
    await userEvent.type(screen.getByLabelText("CEP *"), "01001000");
    await userEvent.type(screen.getByLabelText("Logradouro *"), "Praca da Se");
    await userEvent.type(screen.getByLabelText("Número *"), "100");
    await userEvent.type(screen.getByLabelText("Bairro *"), "Se");
    await userEvent.selectOptions(screen.getByLabelText("UF *"), "SP");
    await userEvent.selectOptions(await screen.findByLabelText(/^Cidade/), "São Paulo");
    await userEvent.type(screen.getByLabelText("E-mail *"), "contato@nova.example");
    await userEvent.type(screen.getByLabelText("Telefone *"), "11988887777");
    await userEvent.click(screen.getByRole("button", { name: "Cadastrar fornecedor" }));

    // Nunca duplicar: o formulário de cadastro fecha e um aviso é exibido, sem criar um segundo
    // fornecedor com o mesmo CNPJ/CPF já existente como Fornecedor no Linx.
    expect(await screen.findByText("Fornecedor já cadastrado")).toBeInTheDocument();
    expect(screen.getByText("Este fornecedor já está cadastrado no Linx. Os dados existentes serão exibidos.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Cadastrar fornecedor" })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "OK" }));

    // "OK" navega direto para o detalhe do fornecedor já existente (importado do ERP pelo backend).
    // A razão social aparece tanto no título quanto no campo "Razão Social" do detalhe.
    expect(await screen.findByRole("heading", { name: "Fornecedora Ja Existente", level: 2 })).toBeInTheDocument();
    expect(screen.queryByText("Fornecedor já cadastrado")).not.toBeInTheDocument();
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
    const alvo = fornecedor({ categoria: "Embalagem" });
    rotas.set(`GET /fornecedores/${alvo.id}`, { status: 200, body: alvo });
    rotas.set(`PUT /fornecedores/${alvo.id}`, { status: 200, body: { ...alvo, razaoSocial: "ABC Atualizada" } });

    renderApp(`/fornecedores/${alvo.id}`);
    await screen.findByRole("heading", { name: "ABC Comercio LTDA" });

    await userEvent.click(screen.getByRole("button", { name: "Editar fornecedor" }));
    const razaoSocialInput = await screen.findByLabelText("Razão Social *");
    await userEvent.clear(razaoSocialInput);
    await userEvent.type(razaoSocialInput, "ABC Atualizada");
    await userEvent.click(screen.getByRole("button", { name: "Salvar alterações" }));

    // Gate de homologação (2026-09-01): salvar edição pede confirmação num modal da própria
    // aplicação (nunca window.confirm) antes de persistir.
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText("Deseja realmente salvar as alterações deste fornecedor?")).toBeInTheDocument();
    await userEvent.click(within(dialog).getByRole("button", { name: "Salvar alterações" }));

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

    // Ativar/Inativar só existe dentro da edição (item de feedback do homologador, 2026-09-01).
    await userEvent.click(screen.getByRole("button", { name: "Editar fornecedor" }));
    await userEvent.click(await screen.findByRole("button", { name: "Ativar fornecedor" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText("Ativar fornecedor?")).toBeInTheDocument();
    await userEvent.click(within(dialog).getByRole("button", { name: "Ativar fornecedor" }));

    await waitFor(() => {
      const patch = chamadas.find((c) => c.method === "PATCH");
      expect(patch!.body).toEqual({ ativo: true });
    });
  });

  it("'Enviar ao ERP' chama garantir-erp (+Compras -> ERP), nunca /sincronizar (Gate homologacao, item 2)", async () => {
    const alvo = fornecedor();
    rotas.set(`GET /fornecedores/${alvo.id}`, { status: 200, body: alvo });
    rotas.set(`POST /api/fornecedores/${alvo.id}/garantir-erp`, { status: 200, body: { status: "Sincronizado" } });

    renderApp(`/fornecedores/${alvo.id}`);
    await screen.findByRole("heading", { name: "ABC Comercio LTDA" });

    await userEvent.click(screen.getByRole("button", { name: "Enviar ao ERP" }));

    await waitFor(() => {
      expect(chamadas.some((c) => c.method === "POST" && c.url === `/api/fornecedores/${alvo.id}/garantir-erp`)).toBe(true);
    });
    expect(chamadas.some((c) => c.method === "POST" && c.url === "/api/fornecedores/sincronizar")).toBe(false);
    expect(await screen.findByText("Envio ao ERP concluído.")).toBeInTheDocument();
  });

  it("'Atualizar do ERP' chama /sincronizar com direcao ErpParaMaisCompras (Gate homologacao, item 2)", async () => {
    const alvo = fornecedor({ erpSistema: "SOMA_DESENV", erpFornecedorId: "ERP-42" });
    rotas.set(`GET /fornecedores/${alvo.id}`, { status: 200, body: alvo });
    rotas.set("POST /api/fornecedores/sincronizar", { status: 200, body: { status: "Sincronizado" } });

    renderApp(`/fornecedores/${alvo.id}`);
    await screen.findByRole("heading", { name: "ABC Comercio LTDA" });

    await userEvent.click(screen.getByRole("button", { name: "Atualizar do ERP" }));

    await waitFor(() => {
      const chamada = chamadas.find((c) => c.method === "POST" && c.url === "/api/fornecedores/sincronizar");
      expect(chamada).toBeDefined();
      expect(chamada!.body).toMatchObject({
        erpSistema: "SOMA_DESENV",
        erpFornecedorId: "ERP-42",
        fornecedorId: alvo.id,
        direcao: "ErpParaMaisCompras"
      });
    });
    expect(chamadas.some((c) => c.method === "POST" && c.url === `/api/fornecedores/${alvo.id}/garantir-erp`)).toBe(false);
    expect(await screen.findByText("Dados atualizados a partir do ERP.")).toBeInTheDocument();
  });

  it("mostra a mensagem de erro devolvida pelo backend quando 'Atualizar do ERP' encontra conflito nao resolvido", async () => {
    const alvo = fornecedor();
    rotas.set(`GET /fornecedores/${alvo.id}`, { status: 200, body: alvo });
    rotas.set("POST /api/fornecedores/sincronizar", {
      status: 400,
      body: { code: "adapter_error", message: "Conflito não resolvido automaticamente entre ERP e +Compras." }
    });

    renderApp(`/fornecedores/${alvo.id}`);
    await screen.findByRole("heading", { name: "ABC Comercio LTDA" });

    await userEvent.click(screen.getByRole("button", { name: "Atualizar do ERP" }));

    expect(await screen.findByText("Conflito não resolvido automaticamente entre ERP e +Compras.")).toBeInTheDocument();
  });
});
