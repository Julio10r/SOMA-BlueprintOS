import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ConfiguracaoNotificacaoRoutes } from "../routes/ConfiguracaoNotificacaoRoutes";

const UN_ID = "11111111-1111-1111-1111-111111111111";
const UN_SEM_CONFIG_ID = "22222222-2222-2222-2222-222222222222";

type ConfigDto = { id: string; unidadeNegocioId: string; emailAtivado: boolean; emailRemetente: string | null; nomeRemetente: string | null };

let configuracao: ConfigDto | null;

beforeEach(() => {
  configuracao = {
    id: "dddddddd-0000-0000-0000-000000000001",
    unidadeNegocioId: UN_ID,
    emailAtivado: true,
    emailRemetente: "notificacoes@azzas.com.br",
    nomeRemetente: "AZZAS"
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

    if (method === "GET" && url === `/api/administracao/unidades-negocio/${UN_ID}/configuracao-notificacao`) {
      return { ok: true, status: 200, json: async () => configuracao } as Response;
    }
    if (method === "GET" && url === `/api/administracao/unidades-negocio/${UN_SEM_CONFIG_ID}/configuracao-notificacao`) {
      return { ok: false, status: 404, json: async () => ({ code: "configuracao_notificacao_nao_encontrada" }) } as Response;
    }
    if (method === "PUT" && url === `/api/administracao/unidades-negocio/${UN_ID}/configuracao-notificacao`) {
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      configuracao = {
        ...configuracao!,
        emailAtivado: body.emailAtivado,
        emailRemetente: body.emailRemetente ?? configuracao!.emailRemetente,
        nomeRemetente: body.nomeRemetente ?? configuracao!.nomeRemetente
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

function renderConfiguracaoNotificacao() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <ConfiguracaoNotificacaoRoutes />
    </MemoryRouter>
  );
}

describe("ConfiguracaoNotificacaoPage", () => {
  it("exibe a Configuração de Notificações existente ao selecionar a Unidade de Negócio", async () => {
    renderConfiguracaoNotificacao();
    await userEvent.selectOptions(await screen.findByLabelText("Unidade de Negócio"), UN_ID);
    expect(await screen.findByDisplayValue("notificacoes@azzas.com.br")).toBeInTheDocument();
    expect(screen.getByDisplayValue("AZZAS")).toBeInTheDocument();
  });

  it("trata 404 (nao configurado) como estado vazio, nao como erro", async () => {
    renderConfiguracaoNotificacao();
    await userEvent.selectOptions(await screen.findByLabelText("Unidade de Negócio"), UN_SEM_CONFIG_ID);
    expect(await screen.findByText(/Nenhuma Configuração de Notificações cadastrada/i)).toBeInTheDocument();
    expect(screen.queryByText(/configuracao_notificacao_nao_encontrada/i)).not.toBeInTheDocument();
  });

  it("permite ativar/inativar as notificacoes por e-mail e salvar", async () => {
    renderConfiguracaoNotificacao();
    await userEvent.selectOptions(await screen.findByLabelText("Unidade de Negócio"), UN_ID);
    await screen.findByDisplayValue("notificacoes@azzas.com.br");

    const checkbox = screen.getByRole("checkbox", { name: /Notificações por e-mail ativadas/i });
    expect(checkbox).toBeChecked();
    await userEvent.click(checkbox);
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(configuracao?.emailAtivado).toBe(false));
  });

  it("nao exibe catalogo de eventos ficticio, apenas uma indicacao textual de disponibilidade futura", async () => {
    renderConfiguracaoNotificacao();
    await userEvent.selectOptions(await screen.findByLabelText("Unidade de Negócio"), UN_ID);
    await screen.findByDisplayValue("notificacoes@azzas.com.br");

    expect(screen.queryAllByRole("checkbox").length).toBe(1);
    expect(screen.getByText(/catálogo de eventos configuráveis/i)).toBeInTheDocument();
  });
});
