import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AlcadasAprovacaoRoutes } from "../routes/AlcadasAprovacaoRoutes";

const UN_ID = "11111111-1111-1111-1111-111111111111";
const USUARIO_ID = "22222222-2222-2222-2222-222222222222";
const PERFIL_ID = "33333333-3333-3333-3333-333333333333";

type AlcadaDto = {
  id: string;
  unidadeNegocioId: string;
  nome: string;
  criterio: number;
  valorMinimo: number | null;
  valorMaximo: number | null;
  centroCustoMetadadoId: string | null;
  nivel: number;
  aprovadorUsuarioId: string | null;
  aprovadorPerfilId: string | null;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
};

let alcadas: AlcadaDto[];

beforeEach(() => {
  alcadas = [
    {
      id: "aaaaaaaa-0000-0000-0000-000000000001",
      unidadeNegocioId: UN_ID,
      nome: "Alcada Nivel 1",
      criterio: 0,
      valorMinimo: 0,
      valorMaximo: 1000,
      centroCustoMetadadoId: null,
      nivel: 1,
      aprovadorUsuarioId: USUARIO_ID,
      aprovadorPerfilId: null,
      ativo: true,
      criadoEm: "2026-01-01T00:00:00Z",
      atualizadoEm: "2026-01-01T00:00:00Z"
    }
  ];

  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url === "/api/administracao/unidades-negocio") {
      return { ok: true, status: 200, json: async () => [{ id: UN_ID, nome: "AZZAS Corporativo", slug: "azzas-corporativo", ativa: true }] } as Response;
    }
    if (method === "GET" && url === "/api/administracao/usuarios") {
      return { ok: true, status: 200, json: async () => [{ id: USUARIO_ID, nome: "Fulano de Tal", email: "fulano@azzas.com.br", unidadeNegocioId: UN_ID, ativo: true, perfis: [], centrosCusto: [], todosCentrosCusto: true, criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z" }] } as Response;
    }
    if (method === "GET" && url === "/api/administracao/perfis") {
      return { ok: true, status: 200, json: async () => [{ id: PERFIL_ID, nome: "Gestor de Compras", descricao: "", unidadeNegocioId: UN_ID, ativo: true, permissoes: [], usuariosVinculados: 0, criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z" }] } as Response;
    }
    if (method === "GET" && url === "/api/administracao/centros-custo") {
      return { ok: true, status: 200, json: async () => [{ codigoErp: "CC001", descricaoErp: "Centro Um", descricaoMaisCompras: null, ativoNoMaisCompras: true, temMetadadoLocal: true, atualizadoEm: "2026-01-01T00:00:00Z", unidadeAlocacaoPadraoNome: null, quantidadeUnidadesAlocacaoVinculadas: 0, centroCustoMetadadoId: "44444444-4444-4444-4444-444444444444" }] } as Response;
    }

    const base = `/api/administracao/unidades-negocio/${UN_ID}/alcadas-aprovacao`;
    if (method === "GET" && url === base) {
      return { ok: true, status: 200, json: async () => alcadas } as Response;
    }
    if (method === "POST" && url === base) {
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const criada: AlcadaDto = {
        id: "bbbbbbbb-0000-0000-0000-000000000002",
        unidadeNegocioId: UN_ID,
        nome: body.nome,
        criterio: body.criterio,
        valorMinimo: body.valorMinimo,
        valorMaximo: body.valorMaximo,
        centroCustoMetadadoId: body.centroCustoMetadadoId,
        nivel: body.nivel,
        aprovadorUsuarioId: body.aprovadorUsuarioId,
        aprovadorPerfilId: body.aprovadorPerfilId,
        ativo: true,
        criadoEm: "2026-01-02T00:00:00Z",
        atualizadoEm: "2026-01-02T00:00:00Z"
      };
      alcadas = [...alcadas, criada];
      return { ok: true, status: 201, json: async () => criada } as Response;
    }
    if (method === "PATCH" && url.endsWith("/status")) {
      const id = url.split(`${base}/`)[1].replace("/status", "");
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      alcadas = alcadas.map((a) => (a.id === id ? { ...a, ativo: body.ativo } : a));
      return { ok: true, status: 200, json: async () => alcadas.find((a) => a.id === id) } as Response;
    }
    return { ok: false, status: 404, json: async () => ({}) } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderAlcadas() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <AlcadasAprovacaoRoutes />
    </MemoryRouter>
  );
}

async function selecionarUnidadeNegocio() {
  const seletor = await screen.findByLabelText("Unidade de Negocio");
  await userEvent.selectOptions(seletor, UN_ID);
}

describe("AlcadasAprovacaoPage", () => {
  it("lista as Alcadas de Aprovacao da Unidade de Negocio selecionada", async () => {
    renderAlcadas();
    await selecionarUnidadeNegocio();
    expect(await screen.findByText("Alcada Nivel 1")).toBeInTheDocument();
    expect(screen.getByText("Valor")).toBeInTheDocument();
    expect(screen.getByText("Usuario")).toBeInTheDocument();
  });

  it("cria uma nova Alcada de Aprovacao com aprovador Usuario", async () => {
    renderAlcadas();
    await selecionarUnidadeNegocio();
    await screen.findByText("Alcada Nivel 1");

    await userEvent.click(screen.getByRole("button", { name: "Nova Alcada de Aprovacao" }));
    await userEvent.type(screen.getByLabelText("Nome"), "Alcada Nivel 2");
    const nivelInput = screen.getByLabelText("Nivel");
    await userEvent.clear(nivelInput);
    await userEvent.type(nivelInput, "2");
    await userEvent.selectOptions(screen.getByLabelText("Usuario aprovador"), USUARIO_ID);
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByText("Alcada Nivel 2")).toBeInTheDocument());
  });

  it("permite selecionar aprovador do tipo Perfil, ocultando o seletor de Usuario", async () => {
    renderAlcadas();
    await selecionarUnidadeNegocio();
    await screen.findByText("Alcada Nivel 1");

    await userEvent.click(screen.getByRole("button", { name: "Nova Alcada de Aprovacao" }));
    await userEvent.selectOptions(screen.getByLabelText("Tipo de aprovador"), "Perfil");

    expect(screen.queryByLabelText("Usuario aprovador")).not.toBeInTheDocument();
    await userEvent.selectOptions(screen.getByLabelText("Perfil aprovador"), PERFIL_ID);
  });

  it("ativa/inativa uma Alcada de Aprovacao", async () => {
    renderAlcadas();
    await selecionarUnidadeNegocio();
    await screen.findByText("Alcada Nivel 1");

    const row = screen.getByText("Alcada Nivel 1").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Inativar" }));

    await waitFor(() => expect(within(row).getByText("Inativo")).toBeInTheDocument());
  });
});
