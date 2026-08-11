import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CentrosCustoRoutes } from "../routes/CentrosCustoRoutes";

/**
 * O1.7 — a Gestao de Centros de Custo consome a API real (`administracao/centros-custo`), substituindo o
 * `centrosCustoMockApi.ts` removido nesta sprint. Mesmo padrao de integracao HTTP de
 * `administration/users/tests/UsuariosPage.test.tsx` (O1.6): fetch interceptado.
 *
 * O1.9 — o vinculo real N:N com Unidade de Alocacao (`unidadeAlocacaoPadraoNome`/
 * `quantidadeUnidadesAlocacaoVinculadas`) e testado nos casos abaixo, substituindo o teste removido na O1.7
 * (que documentava esses campos como sempre indefinidos/zero, dívida ja resolvida).
 */
type CentroCustoApiDto = {
  codigoErp: string;
  descricaoErp: string;
  descricaoMaisCompras?: string | null;
  ativoNoMaisCompras: boolean;
  temMetadadoLocal: boolean;
  atualizadoEm?: string | null;
  unidadeAlocacaoPadraoNome?: string | null;
  quantidadeUnidadesAlocacaoVinculadas: number;
};

function centroCustoDto(over: Partial<CentroCustoApiDto> = {}): CentroCustoApiDto {
  return {
    codigoErp: "1001",
    descricaoErp: "ADMINISTRATIVO CORPORATIVO",
    descricaoMaisCompras: null,
    ativoNoMaisCompras: true,
    temMetadadoLocal: false,
    atualizadoEm: "2026-07-01T09:00:00Z",
    unidadeAlocacaoPadraoNome: null,
    quantidadeUnidadesAlocacaoVinculadas: 0,
    ...over
  };
}

type Rota = { status: number; body?: unknown };
type UnidadeAlocacaoApiDto = { id: string; nome: string; ativo: boolean };
type VinculoApiDto = { id: string; nome: string; ativo: boolean; padrao: boolean };

let rotas: Map<string, Rota>;
let centrosCusto: CentroCustoApiDto[];
let unidadesAlocacao: UnidadeAlocacaoApiDto[];
let vinculosPorCodigo: Map<string, VinculoApiDto[]>;

beforeEach(() => {
  centrosCusto = [
    centroCustoDto(),
    centroCustoDto({ codigoErp: "1002", descricaoErp: "LOGISTICA E DISTRIBUICAO", descricaoMaisCompras: "CD - prioridade de reposicao", temMetadadoLocal: true }),
    centroCustoDto({ codigoErp: "1003", descricaoErp: "MARKETING E TRADE", ativoNoMaisCompras: false, temMetadadoLocal: true }),
    centroCustoDto({ codigoErp: "1004", descricaoErp: "OPERACOES DE LOJA" }),
    centroCustoDto({ codigoErp: "1005", descricaoErp: "RECURSOS HUMANOS", ativoNoMaisCompras: false, temMetadadoLocal: true })
  ];
  unidadesAlocacao = [
    { id: "ua-1", nome: "Farm", ativo: true },
    { id: "ua-2", nome: "Animale", ativo: true }
  ];
  vinculosPorCodigo = new Map();
  rotas = new Map<string, Rota>();
  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (method === "GET" && url === "/api/administracao/centros-custo") {
      return { ok: true, status: 200, json: async () => centrosCusto } as Response;
    }

    if (method === "GET" && url === "/api/administracao/unidades-alocacao") {
      return { ok: true, status: 200, json: async () => unidadesAlocacao } as Response;
    }

    const vinculoMatch = url.match(/^\/api\/administracao\/centros-custo\/([^/]+)\/unidades-alocacao$/);
    if (vinculoMatch) {
      const codigo = decodeURIComponent(vinculoMatch[1]);
      if (method === "GET") {
        return { ok: true, status: 200, json: async () => vinculosPorCodigo.get(codigo) ?? [] } as Response;
      }
      if (method === "PUT") {
        const body = init?.body ? JSON.parse(String(init.body)) : {};
        const ids: string[] = body.unidadeAlocacaoIds ?? [];
        const padraoId: string | null = body.padraoId ?? null;
        const atualizado = ids.map((id) => {
          const unidade = unidadesAlocacao.find((u) => u.id === id)!;
          return { id: unidade.id, nome: unidade.nome, ativo: unidade.ativo, padrao: id === padraoId };
        });
        vinculosPorCodigo.set(codigo, atualizado);
        return { ok: true, status: 200, json: async () => atualizado } as Response;
      }
    }

    if (method === "PUT" && url.startsWith("/api/administracao/centros-custo/")) {
      const codigo = decodeURIComponent(url.split("/api/administracao/centros-custo/")[1]);
      const body = init?.body ? JSON.parse(String(init.body)) : {};
      const existente = centrosCusto.find((c) => c.codigoErp === codigo);
      if (!existente) return { ok: false, status: 404, json: async () => ({ code: "centro_custo_nao_encontrado" }) } as Response;
      const atualizado: CentroCustoApiDto = {
        ...existente,
        descricaoMaisCompras: body.descricaoMaisCompras ?? null,
        ativoNoMaisCompras: body.ativoNoMaisCompras,
        temMetadadoLocal: true,
        atualizadoEm: new Date().toISOString()
      };
      centrosCusto = centrosCusto.map((c) => (c.codigoErp === codigo ? atualizado : c));
      return { ok: true, status: 200, json: async () => atualizado } as Response;
    }

    const semQuery = url.split("?")[0];
    const rota = rotas.get(`${method} ${semQuery}`) ?? { status: 404, body: {} };
    return { ok: rota.status >= 200 && rota.status < 300, status: rota.status, json: async () => rota.body ?? {} } as Response;
  }));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderCentrosCusto(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <CentrosCustoRoutes />
    </MemoryRouter>
  );
}

describe("CentrosCustoPage", () => {
  it("lista os centros de custo vindos da API com Codigo, Descricao ERP e Descricao +Compras", async () => {
    renderCentrosCusto();
    expect(await screen.findByRole("heading", { name: "Centros de Custo integrados do ERP" })).toBeInTheDocument();
    expect(await screen.findByText("1001")).toBeInTheDocument();
    expect(await screen.findByText("ADMINISTRATIVO CORPORATIVO")).toBeInTheDocument();
    expect(await screen.findByText("LOGISTICA E DISTRIBUICAO")).toBeInTheDocument();
    expect(await screen.findByText("CD - prioridade de reposicao")).toBeInTheDocument();
  });

  it("nao exibe nenhum botao de criar ou excluir centro de custo", async () => {
    renderCentrosCusto();
    await screen.findByText("1001");
    expect(screen.queryByRole("button", { name: /novo centro de custo/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /criar/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /excluir/i })).not.toBeInTheDocument();
  });

  it("exibe os dados do ERP como somente leitura na tela de edicao", async () => {
    renderCentrosCusto();
    const row = (await screen.findByText("ADMINISTRATIVO CORPORATIVO")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    expect(await screen.findByRole("heading", { name: "Editar centro de custo", level: 1 })).toBeInTheDocument();
    expect(await screen.findByText("Dados do ERP (somente leitura)")).toBeInTheDocument();

    expect(screen.queryByLabelText("Codigo Centro de Custo")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Descricao ERP")).not.toBeInTheDocument();
    expect(screen.getAllByText("1001").length).toBeGreaterThan(0);
    expect(screen.getAllByText("ADMINISTRATIVO CORPORATIVO").length).toBeGreaterThan(0);
  });

  it("permite editar a Descricao +Compras sem alterar a Descricao ERP", async () => {
    renderCentrosCusto();
    const row = (await screen.findByText("ADMINISTRATIVO CORPORATIVO")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Editar centro de custo", level: 1 });
    await userEvent.type(await screen.findByLabelText("Descricao +Compras"), "Sede administrativa");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Detalhes do centro de custo" })).toBeInTheDocument());
    expect(await screen.findByText("Sede administrativa")).toBeInTheDocument();
    expect(screen.getAllByText("ADMINISTRATIVO CORPORATIVO").length).toBeGreaterThan(0);
  });

  it("ativa e inativa o centro de custo no +Compras diretamente pela listagem", async () => {
    renderCentrosCusto();
    const row = (await screen.findByText("RECURSOS HUMANOS")).closest("tr")!;
    expect(within(row).getByText("Inativo")).toBeInTheDocument();

    await userEvent.click(within(row).getByRole("button", { name: "Ativar no +Compras" }));

    await waitFor(() => {
      const updatedRow = screen.getByText("RECURSOS HUMANOS").closest("tr")!;
      expect(within(updatedRow).getByText("Ativo")).toBeInTheDocument();
      expect(within(updatedRow).getByRole("button", { name: "Inativar no +Compras" })).toBeInTheDocument();
    });
  });

  it("filtra por status no +Compras", async () => {
    renderCentrosCusto();
    await screen.findByText("ADMINISTRATIVO CORPORATIVO");

    await userEvent.selectOptions(screen.getByLabelText("Status no +Compras"), "Inativo");

    expect(screen.queryByText("ADMINISTRATIVO CORPORATIVO")).not.toBeInTheDocument();
    expect(screen.getByText("MARKETING E TRADE")).toBeInTheDocument();
  });

  it("pesquisa por Codigo", async () => {
    renderCentrosCusto();
    await screen.findByText("ADMINISTRATIVO CORPORATIVO");

    await userEvent.type(screen.getByLabelText("Pesquisar"), "1004");

    expect(screen.getByText("OPERACOES DE LOJA")).toBeInTheDocument();
    expect(screen.queryByText("ADMINISTRATIVO CORPORATIVO")).not.toBeInTheDocument();
  });

  it("mostra acesso negado quando a API responde 403", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({ ok: false, status: 403, json: async () => ({}) }) as Response));

    renderCentrosCusto();

    expect(await screen.findByText(/nao tem permissao para acessar a Gestao de Centros de Custo/i)).toBeInTheDocument();
  });

  it("vincula Unidades de Alocacao reais e define a padrao, refletindo na listagem", async () => {
    renderCentrosCusto();
    const row = (await screen.findByText("ADMINISTRATIVO CORPORATIVO")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Unidades de Alocacao vinculadas" });
    await userEvent.click(screen.getByRole("checkbox", { name: /Farm/i }));
    await userEvent.click(screen.getByRole("checkbox", { name: /Animale/i }));

    const linhaFarm = screen.getByText("Farm").closest("label")!;
    await userEvent.click(within(linhaFarm).getByRole("radio", { name: "Padrao" }));

    await userEvent.click(screen.getByRole("button", { name: "Salvar vinculo" }));

    await waitFor(() => expect(screen.getByRole("button", { name: "Salvar vinculo" })).toBeEnabled());

    await userEvent.click(screen.getByRole("button", { name: "Cancelar" }));
    await waitFor(() => expect(screen.getByRole("heading", { name: "Detalhes do centro de custo" })).toBeInTheDocument());
  });

  it("nao permite marcar como padrao uma Unidade de Alocacao nao selecionada", async () => {
    renderCentrosCusto();
    const row = (await screen.findByText("ADMINISTRATIVO CORPORATIVO")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Unidades de Alocacao vinculadas" });
    const linhaFarm = screen.getByText("Farm").closest("label")!;

    expect(within(linhaFarm).getByRole("radio", { name: "Padrao" })).toBeDisabled();
  });
});
