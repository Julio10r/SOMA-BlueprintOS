import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { MonitorIntegracoesPage } from "../pages/MonitorIntegracoesPage";

type ExecucaoDto = {
  id: string;
  sistemaOrigem: string;
  businessUnit: string;
  dataInicio: string;
  dataFim: string | null;
  status: string;
  totalConsultado: number;
  totalIncluido: number;
  totalAtualizado: number;
  totalSemAlteracao: number;
  totalErro: number;
  tempoExecucaoMs: number;
};

const BASE = "/api/administracao/monitoramento/sincronizacoes-fornecedores";

let execucoes: ExecucaoDto[];
let forcarForbidden: boolean;

function novaExecucao(overrides: Partial<ExecucaoDto> = {}): ExecucaoDto {
  return {
    id: "aaaaaaaa-0000-0000-0000-000000000001",
    sistemaOrigem: "SOMA_DESENV",
    businessUnit: "DEFAULT",
    dataInicio: "2026-08-10T10:00:00Z",
    dataFim: "2026-08-10T10:01:00Z",
    status: "Sucesso",
    totalConsultado: 10,
    totalIncluido: 2,
    totalAtualizado: 3,
    totalSemAlteracao: 5,
    totalErro: 0,
    tempoExecucaoMs: 1234,
    ...overrides
  };
}

beforeEach(() => {
  execucoes = [novaExecucao()];
  forcarForbidden = false;

  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (forcarForbidden && url.startsWith(BASE)) {
      return { ok: false, status: 403, json: async () => ({ code: "acesso_negado", message: "Sem permissao." }) } as Response;
    }

    if (method === "GET" && url.startsWith(BASE)) {
      const parsed = new URL(url, "http://localhost");
      const status = parsed.searchParams.get("status");
      const businessUnit = parsed.searchParams.get("businessUnit");
      const filtrados = execucoes.filter(
        (e) => (!status || e.status === status) && (!businessUnit || e.businessUnit === businessUnit)
      );
      return { ok: true, status: 200, json: async () => ({ itens: filtrados, totalRegistros: filtrados.length, pagina: 1, tamanhoPagina: 20 }) } as Response;
    }

    if (method === "GET" && url.startsWith("/api/fornecedores/sincronizar-erp")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          execucaoId: "bbbbbbbb-0000-0000-0000-000000000002",
          status: "Sucesso",
          inicio: "2026-08-11T10:00:00Z",
          fim: "2026-08-11T10:00:05Z",
          consultados: 1,
          incluidos: 1,
          atualizados: 0,
          semAlteracao: 0,
          erros: 0,
          duracaoMs: 500,
          businessUnit: "DEFAULT",
          erpSistema: "SOMA_DESENV",
          correlationId: "corr-1"
        })
      } as Response;
    }

    return { ok: false, status: 404, json: async () => ({}) } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <MonitorIntegracoesPage />
    </MemoryRouter>
  );
}

describe("MonitorIntegracoesPage", () => {
  it("lista as execucoes de sincronizacao de fornecedores", async () => {
    renderPage();
    expect(await screen.findByText("SOMA_DESENV")).toBeInTheDocument();
    expect(screen.getByText("DEFAULT")).toBeInTheDocument();
    expect(screen.getByText("Sucesso", { selector: "span" })).toBeInTheDocument();
  });

  it("exibe estado vazio quando nao ha execucoes", async () => {
    execucoes = [];
    renderPage();
    expect(await screen.findByText("Nenhuma execução de sincronização de fornecedores encontrada.")).toBeInTheDocument();
  });

  it("exibe mensagem de erro quando a chamada falha", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status: 500, json: async () => ({ message: "Erro interno" }) } as Response)));
    renderPage();
    expect(await screen.findByText("Erro interno")).toBeInTheDocument();
  });

  it("exibe mensagem clara quando o acesso e negado (403)", async () => {
    forcarForbidden = true;
    renderPage();
    expect(await screen.findByText(/permissão para acessar o Monitoramento Operacional/i)).toBeInTheDocument();
  });

  it("filtra por status", async () => {
    execucoes = [novaExecucao({ id: "1", status: "Sucesso" }), novaExecucao({ id: "2", status: "Erro", businessUnit: "OUTRA" })];
    renderPage();
    await screen.findAllByText("SOMA_DESENV");

    await userEvent.selectOptions(screen.getByLabelText("Status"), "Erro");

    await waitFor(() => expect(screen.getByText("OUTRA")).toBeInTheDocument());
    expect(screen.queryByText("DEFAULT")).not.toBeInTheDocument();
  });

  it("dispara reprocessamento ao clicar em Reprocessar sincronização", async () => {
    renderPage();
    await screen.findByText("SOMA_DESENV");

    await userEvent.type(screen.getAllByLabelText("Unidade de Negócio (BusinessUnit)")[0], "DEFAULT");
    await userEvent.click(screen.getByRole("button", { name: "Reprocessar sincronização" }));

    await waitFor(() => expect(screen.getByText(/Sincronização disparada\. Status: Sucesso/)).toBeInTheDocument());
  });
});
