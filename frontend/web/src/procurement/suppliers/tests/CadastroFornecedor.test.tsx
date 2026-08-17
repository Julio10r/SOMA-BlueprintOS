import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CadastroFornecedor } from "../components/CadastroFornecedor";
import { AuthContext } from "../../../auth/context/AuthContext";

const usuarioTeste = {
  id: "u1",
  email: "ana@somagrupo.com.br",
  nome: "Ana Souza",
  unidadeNegocioId: "un1",
  permissoes: [],
  escopoAdministrativo: "Produto" as const
};

function renderCadastroFornecedor() {
  return render(
    <AuthContext.Provider
      value={{ usuario: usuarioTeste, carregando: false, refresh: vi.fn(), setUsuario: vi.fn(), logout: vi.fn() }}
    >
      <CadastroFornecedor />
    </AuthContext.Provider>
  );
}

const consulta = {
  cnpj_Cpf: "12345678000195",
  razaoSocial: "ABC Comercio LTDA",
  nomeFantasia: "FORNECEDOR ERP",
  tipoPessoa: "PJ",
  situacaoCadastral: "Ativa",
  dataSituacaoCadastral: "2026-07-31",
  dataAbertura: "2020-01-01",
  cep: "01001000",
  logradouro: "Praca da Se",
  numero: "100",
  complemento: "Sala 1",
  bairro: "Se",
  cidade: "Sao Paulo",
  estado: "SP",
  pais: "BR",
  email: "novo@example.invalid",
  telefone: "11999999999",
  naturezaJuridica: "Sociedade Empresaria",
  porteEmpresa: "ME",
  cnaePrincipalCodigo: "6201501",
  cnaePrincipalDescricao: "Desenvolvimento de programas de computador sob encomenda",
  fonteConsulta: "BrasilAPI",
  dataConsulta: "2026-08-01T12:00:00Z",
  statusConsulta: "Sucesso",
  mensagemErro: null as string | null,
  sucesso: true
};

const supplier = {
  id: "4f2f4ae2-54cc-42f5-a1c0-9e1329b6c927",
  razaoSocial: "ABC LTDA",
  nomeFantasia: "ERP Atual",
  cnpj_Cpf: "12345678000195",
  tipoPessoa: "PJ",
  email: "antigo@example.invalid",
  telefone: "11000000000",
  cidade: "Sao Paulo",
  estado: "SP"
};

const analise = {
  fornecedorId: supplier.id,
  cnpj_Cpf: supplier.cnpj_Cpf,
  consultaId: null,
  fonteConsulta: "BrasilAPI",
  correlationId: "b224-test",
  alertas: [],
  divergencias: [
    { campo: "RazaoSocial", valorAtual: "ABC LTDA", valorSugerido: "ABC Comercio LTDA", origem: "ConsultaCnpj", statusDecisao: "Pendente" },
    { campo: "NomeFantasia", valorAtual: "ERP Atual", valorSugerido: "FORNECEDOR ERP", origem: "ConsultaCnpj", statusDecisao: "Pendente" },
    { campo: "Email", valorAtual: "antigo@example.invalid", valorSugerido: "novo@example.invalid", origem: "ConsultaCnpj", statusDecisao: "Pendente" }
  ]
};

describe("CadastroFornecedor", () => {
  beforeEach(() => {
    vi.stubGlobal("crypto", { randomUUID: () => "test" });
  });

  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it("renderiza a tela inicial", () => {
    renderCadastroFornecedor();
    expect(screen.getByRole("heading", { name: /cadastro com enriquecimento cnpj/i })).toBeInTheDocument();
    expect(screen.getByLabelText("CNPJ/CPF")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /consultar cnpj/i })).toBeInTheDocument();
  });

  it("consulta CNPJ e exibe dados retornados e divergencias", async () => {
    mockFetch();
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12.345.678/0001-95");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    expect(await screen.findByRole("heading", { name: "Divergencias encontradas" })).toBeInTheDocument();
    expect(screen.getAllByText("ABC Comercio LTDA").length).toBeGreaterThan(0);
    expect(screen.getByText("BrasilAPI")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Divergencias encontradas" })).toBeInTheDocument();
    expect(screen.getByText("antigo@example.invalid")).toBeInTheDocument();
    expect(screen.getAllByText("novo@example.invalid").length).toBeGreaterThan(0);
    expect(screen.getByText("ERP")).toBeInTheDocument();
    expect(screen.getByLabelText("Selecionar NomeFantasia")).toBeDisabled();
  });

  it("exibe o CNAE principal (codigo mascarado + descricao) retornado pela consulta", async () => {
    mockFetch();
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    expect(await screen.findByText("6201-5/01")).toBeInTheDocument();
    expect(screen.getByText("Desenvolvimento de programas de computador sob encomenda")).toBeInTheDocument();
  });

  it("ausencia de CNAE principal na consulta nao quebra a Review (exibe 'Nao informado')", async () => {
    mockFetch({ ...consulta, cnaePrincipalCodigo: null as unknown as string, cnaePrincipalDescricao: null as unknown as string }, []);
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    await screen.findByText("CNAE principal");
    expect(screen.getAllByText("Nao informado").length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: /cadastrar fornecedor/i })).toBeInTheDocument();
  });

  it("envia o CNAE principal no POST de cadastro apenas apos a confirmacao explicita (consultar != persistir)", async () => {
    const fetchMock = mockFetch(consulta, []);
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));
    const cadastrarButton = await screen.findByRole("button", { name: /cadastrar fornecedor/i });

    expect(fetchMock.mock.calls.some(([input, init]) => String(input) === "/fornecedores" && (init as RequestInit)?.method === "POST")).toBe(false);

    await userEvent.click(cadastrarButton);

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      "/fornecedores",
      expect.objectContaining({
        method: "POST",
        body: expect.stringContaining("\"cnaePrincipalCodigo\":\"6201501\"")
      })
    ));
  });

  it("exibe situacao cadastral Desconhecida sem crash quando o backend retorna esse estado", async () => {
    mockFetch({ ...consulta, situacaoCadastral: "Desconhecida" });
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    expect(await screen.findByRole("heading", { name: "Divergencias encontradas" })).toBeInTheDocument();
    expect(screen.getAllByText("Desconhecida").length).toBeGreaterThan(0);
  });

  it("aprova somente campos selecionados e preserva NomeFantasia fora da decisao", async () => {
    const fetchMock = mockFetch();
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));
    await screen.findByRole("heading", { name: "Divergencias encontradas" });
    await userEvent.click(screen.getByRole("button", { name: "Aceitar" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      `/fornecedores/${supplier.id}/enriquecimento-cnpj/aprovar`,
      expect.objectContaining({
        method: "POST",
        body: expect.stringContaining("\"campos\":[\"RazaoSocial\",\"Email\"]")
      })
    ));
  });

  it("rejeita divergencias selecionadas", async () => {
    const fetchMock = mockFetch();
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));
    await screen.findByRole("heading", { name: "Divergencias encontradas" });
    const row = screen.getAllByText("Email").find((node) => node.closest("tr"))!.closest("tr")!;
    await userEvent.click(within(row).getByLabelText("Selecionar Email"));
    await userEvent.click(screen.getByRole("button", { name: "Rejeitar" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      `/fornecedores/${supplier.id}/enriquecimento-cnpj/rejeitar`,
      expect.objectContaining({
        method: "POST",
        body: expect.stringContaining("\"campos\":[\"RazaoSocial\"]")
      })
    ));
  });

  it("BUG-1: consultar CNPJ sem fornecedor existente NUNCA cria o fornecedor automaticamente (Review sem escrita)", async () => {
    const fetchMock = mockFetch(consulta, []);
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    expect(await screen.findByRole("heading", { name: /nenhum fornecedor cadastrado/i })).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([input, init]) => String(input) === "/fornecedores" && (init as RequestInit)?.method === "POST")).toBe(false);
    expect(screen.getByRole("button", { name: /cadastrar fornecedor/i })).toBeInTheDocument();
  });

  it("so persiste o novo fornecedor apos confirmacao explicita no botao Cadastrar fornecedor", async () => {
    const fetchMock = mockFetch(consulta, []);
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));
    await screen.findByRole("button", { name: /cadastrar fornecedor/i });

    expect(fetchMock.mock.calls.some(([input, init]) => String(input) === "/fornecedores" && (init as RequestInit)?.method === "POST")).toBe(false);

    await userEvent.click(screen.getByRole("button", { name: /cadastrar fornecedor/i }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      "/fornecedores",
      expect.objectContaining({ method: "POST" })
    ));
  });

  it("bloqueia o cadastro de novo fornecedor com situacao Suspensa sem a confirmacao explicita (consistente com Baixada/Inapta)", async () => {
    mockFetch({ ...consulta, situacaoCadastral: "Suspensa" }, []);
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));
    await screen.findByRole("button", { name: /cadastrar fornecedor/i });

    expect(screen.getByRole("button", { name: /cadastrar fornecedor/i })).toBeDisabled();

    await userEvent.click(screen.getByRole("checkbox", { name: /confirmar continuidade/i }));
    expect(screen.getByRole("button", { name: /cadastrar fornecedor/i })).not.toBeDisabled();
  });

  it("BUG-5: reconsultar um CNPJ ja cadastrado NUNCA cai em ErrorConsulta, mesmo se a fonte externa falhar (reconhece fornecedor existente e permite revisao/edicao)", async () => {
    const consultaComFalha = {
      ...consulta,
      sucesso: false,
      statusConsulta: "Falha",
      tipoErro: "LimiteDeConsultas",
      mensagemErro: "Limite de consultas atingido na fonte externa."
    };
    mockFetch(consultaComFalha, [supplier]);
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    // Nunca deve exibir o estado de erro generico quando ha um Fornecedor ja cadastrado para o
    // mesmo documento: reconsultar deve reconhecer o fornecedor existente e permitir revisao,
    // nunca falhar com "ja existe" nem com erro de consulta.
    await waitFor(() => expect(screen.queryByText(/consulta interrompida/i)).not.toBeInTheDocument());
    expect(screen.getByText(/fornecedor ja cadastrado/i)).toBeInTheDocument();
    expect(screen.queryByText("ErrorConsulta")).not.toBeInTheDocument();
  });

  it("DR-10: fornecedor existente + reconsulta externa com falha exibe os dados ja cadastrados (nao fica vazio)", async () => {
    const consultaComFalha = {
      ...consulta,
      sucesso: false,
      statusConsulta: "Falha",
      tipoErro: "LimiteDeConsultas",
      mensagemErro: "Limite de consultas atingido na fonte externa."
    };
    mockFetch(consultaComFalha, [supplier]);
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    // Antes da correcao do DR-10, SupplierComparison so renderizava quando
    // consulta.sucesso e verdadeiro, deixando a tela sem nenhum dado do
    // fornecedor quando a reconsulta externa falhava. Agora os dados ja
    // cadastrados localmente (objeto Fornecedor) devem aparecer.
    expect(await screen.findByRole("heading", { name: "Dados atuais no +Compras" })).toBeInTheDocument();
    expect(screen.getByText("ABC LTDA")).toBeInTheDocument();
    expect(screen.getByText("ERP Atual")).toBeInTheDocument();
    expect(screen.getByText("antigo@example.invalid")).toBeInTheDocument();

    // Decisao continua bloqueada: nenhuma divergencia foi calculada.
    expect(screen.getByRole("button", { name: "Aceitar" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Rejeitar" })).toBeDisabled();
  });

  it("protege contra submissao duplicada: dois cliques rapidos em Cadastrar fornecedor geram apenas um POST", async () => {
    const fetchMock = mockFetch(consulta, []);
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));
    const cadastrarButton = await screen.findByRole("button", { name: /cadastrar fornecedor/i });

    await userEvent.click(cadastrarButton);
    await userEvent.click(cadastrarButton);

    await waitFor(() => expect(screen.getByText(/fornecedor cadastrado no \+compras/i)).toBeInTheDocument());
    const createCalls = fetchMock.mock.calls.filter(([input, init]) => String(input) === "/fornecedores" && (init as RequestInit)?.method === "POST");
    expect(createCalls.length).toBe(1);
  });

  it("Review de fornecedor novo permite editar RazaoSocial, NomeFantasia, endereco, email e telefone, e persiste os valores revisados (nao os originais da consulta)", async () => {
    const fetchMock = mockFetch(consulta, []);
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));
    await screen.findByRole("button", { name: /cadastrar fornecedor/i });

    async function replace(label: string, value: string) {
      const input = screen.getByRole("textbox", { name: label });
      await userEvent.clear(input);
      await userEvent.type(input, value);
    }

    await replace("RazaoSocial", "Razao Social Revisada Ltda");
    await replace("NomeFantasia", "Fantasia Revisada");
    await replace("Email", "revisado@example.invalid");
    await replace("Telefone (DDD+numero)", "11888887777");
    await replace("CEP", "04567000");
    await replace("Logradouro", "Rua Revisada");
    await replace("Numero", "999");
    await replace("Complemento", "Bloco B");
    await replace("Bairro", "Bairro Revisado");
    await replace("Cidade", "Campinas");
    await replace("UF", "MG");

    await userEvent.click(screen.getByRole("button", { name: /cadastrar fornecedor/i }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      "/fornecedores",
      expect.objectContaining({ method: "POST" })
    ));

    const [, init] = fetchMock.mock.calls.find(([input, callInit]) => String(input) === "/fornecedores" && (callInit as RequestInit)?.method === "POST")!;
    const body = JSON.parse((init as RequestInit).body as string);

    // Os valores enviados sao os revisados pelo usuario, nunca os originais retornados pela consulta.
    expect(body.razaoSocial).toBe("Razao Social Revisada Ltda");
    expect(body.nomeFantasia).toBe("Fantasia Revisada");
    expect(body.email).toBe("revisado@example.invalid");
    expect(body.telefone).toBe("11888887777");
    expect(body.cep).toBe("04567000");
    expect(body.logradouro).toBe("Rua Revisada");
    expect(body.numero).toBe("999");
    expect(body.complemento).toBe("Bloco B");
    expect(body.bairro).toBe("Bairro Revisado");
    expect(body.cidade).toBe("Campinas");
    expect(body.estado).toBe("MG");
    expect(body.razaoSocial).not.toBe(consulta.razaoSocial);
    expect(body.cep).not.toBe(consulta.cep);
  });

  it("CNAE principal permanece somente leitura na Review de fornecedor novo (nenhum campo editavel para CNAE)", async () => {
    mockFetch(consulta, []);
    renderCadastroFornecedor();

    await userEvent.type(screen.getByLabelText("CNPJ/CPF"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));
    await screen.findByRole("button", { name: /cadastrar fornecedor/i });

    expect(screen.getByText("6201-5/01")).toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: /cnae/i })).not.toBeInTheDocument();
  });
});

function mockFetch(consultaOverride: typeof consulta = consulta, suppliersOverride: typeof supplier[] = [supplier]) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    if (url.startsWith("/fornecedores?q=")) return json(suppliersOverride);
    if (url === "/fornecedores" && init?.method === "POST") {
      return json({ id: "novo-fornecedor-id", cnpj_Cpf: consultaOverride.cnpj_Cpf, razaoSocial: consultaOverride.razaoSocial ?? "" });
    }
    if (url === "/fornecedores/consulta-cnpj") return json(consultaOverride);
    if (url.endsWith("/enriquecimento-cnpj")) return json(analise);
    if (url.endsWith("/aprovar")) return json({
      ...analise,
      divergencias: analise.divergencias.map((item) => ({ ...item, statusDecisao: item.campo === "NomeFantasia" ? "Pendente" : "Aceito" }))
    });
    if (url.endsWith("/rejeitar")) return json({
      ...analise,
      divergencias: analise.divergencias.map((item) => ({ ...item, statusDecisao: item.campo === "RazaoSocial" ? "Rejeitado" : item.statusDecisao }))
    });
    throw new Error(`Unexpected fetch ${url}`);
  });
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

function json(body: unknown) {
  return Promise.resolve({
    ok: true,
    json: () => Promise.resolve(body)
  } as Response);
}
