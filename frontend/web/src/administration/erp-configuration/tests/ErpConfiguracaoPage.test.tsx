import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ErpConfiguracaoRoutes } from "../routes/ErpConfiguracaoRoutes";

const UN_ID = "11111111-1111-1111-1111-111111111111";
const UN_SEM_CONFIG_ID = "22222222-2222-2222-2222-222222222222";

type ConfigDto = { id: string; unidadeNegocioId: string; sistemaErp: string; parametrosConfigurados: boolean; ativo: boolean };

let configuracao: ConfigDto | null;

beforeEach(() => {
  configuracao = {
    id: "cccccccc-0000-0000-0000-000000000001",
    unidadeNegocioId: UN_ID,
    sistemaErp: "SAP",
    parametrosConfigurados: true,
    ativo: true
  };

  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url === "/api/administracao/unidades-negocio") {
      return {
        ok: true,
        status: 200,
        json: async () => [
          { id: UN_ID, nome: "AZZAS Corporativo", slug: "azzas-corporativo", ativa: true },
          { id: UN_SEM_CONFIG_ID, nome: "Farm", slug: "farm", ativa: true }
        ]
      } as Response;
    }

    if (method === "GET" && url === `/api/administracao/unidades-negocio/${UN_ID}/configuracao-erp`) {
      return { ok: true, status: 200, json: async () => configuracao } as Response;
    }
    if (method === "GET" && url === `/api/administracao/unidades-negocio/${UN_SEM_CONFIG_ID}/configuracao-erp`) {
      return { ok: false, status: 404, json: async () => ({ code: "configuracao_erp_nao_encontrada" }) } as Response;
    }
    if (method === "PUT" && url === `/api/administracao/unidades-negocio/${UN_ID}/configuracao-erp`) {
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      configuracao = {
        ...configuracao!,
        sistemaErp: body.sistemaErp,
        parametrosConfigurados: body.parametrosConexao ? true : configuracao!.parametrosConfigurados
      };
      return { ok: true, status: 200, json: async () => configuracao } as Response;
    }
    return { ok: false, status: 404, json: async () => ({}) } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderErpConfiguracao() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <ErpConfiguracaoRoutes />
    </MemoryRouter>
  );
}

describe("ErpConfiguracaoPage", () => {
  it("exibe a Configuração de ERP existente ao selecionar a Unidade de Negócio", async () => {
    renderErpConfiguracao();
    await userEvent.selectOptions(await screen.findByLabelText("Unidade de Negócio"), UN_ID);
    expect(await screen.findByRole("heading", { name: "SAP" })).toBeInTheDocument();
  });

  it("trata 404 (nao configurado) como estado vazio, nao como erro", async () => {
    renderErpConfiguracao();
    await userEvent.selectOptions(await screen.findByLabelText("Unidade de Negócio"), UN_SEM_CONFIG_ID);
    expect(await screen.findByText(/Nenhuma Configuração de ERP cadastrada/i)).toBeInTheDocument();
    expect(screen.queryByText(/configuracao_erp_nao_encontrada/i)).not.toBeInTheDocument();
  });

  it("NUNCA pre-preenche o campo de parametros de conexao ao editar uma configuracao ja existente", async () => {
    renderErpConfiguracao();
    await userEvent.selectOptions(await screen.findByLabelText("Unidade de Negócio"), UN_ID);
    await screen.findByRole("heading", { name: "SAP" });

    const campoParametros = await screen.findByLabelText(/Parametros de conexao/i);
    expect(campoParametros).toHaveValue("");
    expect(screen.getByText("Já configurado")).toBeInTheDocument();
  });

  it("salva a Configuração de ERP mantendo o segredo quando o campo e deixado vazio", async () => {
    renderErpConfiguracao();
    await userEvent.selectOptions(await screen.findByLabelText("Unidade de Negócio"), UN_ID);
    await screen.findByRole("heading", { name: "SAP" });

    const campoSistema = screen.getByLabelText("Sistema ERP");
    await userEvent.clear(campoSistema);
    await userEvent.type(campoSistema, "TOTVS");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "TOTVS" })).toBeInTheDocument());
  });
});
