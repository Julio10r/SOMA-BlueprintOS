import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CadastroFornecedor } from "../components/CadastroFornecedor";

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
  mensagemErro: null,
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
    render(<CadastroFornecedor />);
    expect(screen.getByRole("heading", { name: /cadastro com enriquecimento cnpj/i })).toBeInTheDocument();
    expect(screen.getByLabelText("Cnpj_Cpf")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /consultar cnpj/i })).toBeInTheDocument();
  });

  it("consulta CNPJ e exibe dados retornados e divergencias", async () => {
    mockFetch();
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12.345.678/0001-95");
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
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    expect(await screen.findByText("6201-5/01")).toBeInTheDocument();
    expect(screen.getByText("Desenvolvimento de programas de computador sob encomenda")).toBeInTheDocument();
  });

  it("ausencia de CNAE principal na consulta nao quebra a Review (exibe 'Nao informado')", async () => {
    mockFetch({ ...consulta, cnaePrincipalCodigo: null as unknown as string, cnaePrincipalDescricao: null as unknown as string }, []);
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    await screen.findByText("CNAE principal");
    expect(screen.getAllByText("Nao informado").length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: /cadastrar fornecedor/i })).toBeInTheDocument();
  });

  it("envia o CNAE principal no POST de cadastro apenas apos a confirmacao explicita (consultar != persistir)", async () => {
    const fetchMock = mockFetch(consulta, []);
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12345678000195");
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
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    expect(await screen.findByRole("heading", { name: "Divergencias encontradas" })).toBeInTheDocument();
    expect(screen.getAllByText("Desconhecida").length).toBeGreaterThan(0);
  });

  it("aprova somente campos selecionados e preserva NomeFantasia fora da decisao", async () => {
    const fetchMock = mockFetch();
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12345678000195");
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
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12345678000195");
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
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));

    expect(await screen.findByRole("heading", { name: /nenhum fornecedor cadastrado/i })).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([input, init]) => String(input) === "/fornecedores" && (init as RequestInit)?.method === "POST")).toBe(false);
    expect(screen.getByRole("button", { name: /cadastrar fornecedor/i })).toBeInTheDocument();
  });

  it("so persiste o novo fornecedor apos confirmacao explicita no botao Cadastrar fornecedor", async () => {
    const fetchMock = mockFetch(consulta, []);
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12345678000195");
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
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));
    await screen.findByRole("button", { name: /cadastrar fornecedor/i });

    expect(screen.getByRole("button", { name: /cadastrar fornecedor/i })).toBeDisabled();

    await userEvent.click(screen.getByRole("checkbox", { name: /confirmar continuidade/i }));
    expect(screen.getByRole("button", { name: /cadastrar fornecedor/i })).not.toBeDisabled();
  });

  it("protege contra submissao duplicada: dois cliques rapidos em Cadastrar fornecedor geram apenas um POST", async () => {
    const fetchMock = mockFetch(consulta, []);
    render(<CadastroFornecedor />);

    await userEvent.type(screen.getByLabelText("Cnpj_Cpf"), "12345678000195");
    await userEvent.click(screen.getByRole("button", { name: /consultar cnpj/i }));
    const cadastrarButton = await screen.findByRole("button", { name: /cadastrar fornecedor/i });

    await userEvent.click(cadastrarButton);
    await userEvent.click(cadastrarButton);

    await waitFor(() => expect(screen.getByText(/fornecedor cadastrado no \+compras/i)).toBeInTheDocument());
    const createCalls = fetchMock.mock.calls.filter(([input, init]) => String(input) === "/fornecedores" && (init as RequestInit)?.method === "POST");
    expect(createCalls.length).toBe(1);
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
