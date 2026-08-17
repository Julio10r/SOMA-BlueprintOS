import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PerfisRoutes } from "../routes/PerfisRoutes";
import type { Perfil, Permissao } from "../types/perfilTypes";

/**
 * O1.5 — a Gestao de Perfis consome a API real. Estes testes exercitam a integracao HTTP
 * do slice com `fetch` interceptado, cobrindo os estados reais que a tela precisa tratar:
 * sucesso, vazio, erro, acesso negado (403) e ativacao/inativacao.
 *
 * Nenhum mock de dados de dominio permanece no codigo de producao — o `perfisMockApi.ts`
 * foi removido nesta sprint; o duplo abaixo existe apenas dentro do teste.
 */
const CATALOGO: Permissao[] = [
  { codigo: "Perfil.Gerenciar", recurso: "Perfil", acao: "Gerenciar", descricao: "Criar, editar e ativar/inativar Perfis e suas permissoes" },
  { codigo: "Pedido.Criar", recurso: "Pedido", acao: "Criar", descricao: "Criar pedido de compra" },
  { codigo: "Pedido.Aprovar", recurso: "Pedido", acao: "Aprovar", descricao: "Aprovar pedido de compra" }
];

function perfil(over: Partial<Perfil> = {}): Perfil {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    nome: "Administrador",
    descricao: "Acesso administrativo.",
    unidadeNegocioId: "99999999-9999-9999-9999-999999999999",
    ativo: true,
    permissoes: ["Perfil.Gerenciar"],
    usuariosVinculados: 2,
    criadoEm: "2026-08-01T09:00:00Z",
    atualizadoEm: "2026-08-10T09:00:00Z",
    ...over
  };
}

type Rota = { status: number; body?: unknown };

let rotas: Map<string, Rota>;
let chamadas: Array<{ url: string; method: string; body?: unknown }>;

function responder(url: string, method: string): Rota {
  return rotas.get(`${method} ${url}`) ?? rotas.get(`${method} ${url.split("?")[0]}`) ?? { status: 404, body: {} };
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

function renderPerfis(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <PerfisRoutes />
    </MemoryRouter>
  );
}

describe("PerfisPage — integracao com a API real", () => {
  it("lista os perfis vindos da API", async () => {
    rotas.set("GET /api/administracao/perfis", {
      status: 200,
      body: [perfil(), perfil({ id: "22222222-2222-2222-2222-222222222222", nome: "Analista", permissoes: ["Pedido.Criar"] })]
    });
    rotas.set("GET /api/administracao/permissoes", { status: 200, body: CATALOGO });

    renderPerfis();

    expect(await screen.findByRole("heading", { name: "Perfis cadastrados" })).toBeInTheDocument();
    expect(await screen.findByText("Administrador")).toBeInTheDocument();
    expect(await screen.findByText("Analista")).toBeInTheDocument();
    expect(chamadas.some((c) => c.url === "/api/administracao/perfis" && c.method === "GET")).toBe(true);
  });

  it("mostra o estado vazio quando a API nao retorna perfis", async () => {
    rotas.set("GET /api/administracao/perfis", { status: 200, body: [] });

    renderPerfis();

    expect(await screen.findByText("Nenhum perfil cadastrado.")).toBeInTheDocument();
  });

  /** 403 real do backend: sessao valida, sem a permissao Perfil.Gerenciar. */
  it("mostra acesso negado quando a API responde 403", async () => {
    rotas.set("GET /api/administracao/perfis", { status: 403, body: {} });

    renderPerfis();

    expect(await screen.findByText(/não tem permissão para acessar a Gestão de Perfis/i)).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Perfis cadastrados" })).not.toBeInTheDocument();
  });

  it("mostra erro quando a API falha por outro motivo", async () => {
    rotas.set("GET /api/administracao/perfis", { status: 500, body: { message: "Falha inesperada no servidor." } });

    renderPerfis();

    expect(await screen.findByText("Falha inesperada no servidor.")).toBeInTheDocument();
  });

  it("cria um perfil enviando apenas nome, descricao e permissoes", async () => {
    rotas.set("GET /api/administracao/perfis", { status: 200, body: [] });
    rotas.set("GET /api/administracao/permissoes", { status: 200, body: CATALOGO });
    rotas.set("POST /api/administracao/perfis", { status: 201, body: perfil({ nome: "Comprador Regional" }) });

    renderPerfis();
    await screen.findByText("Nenhum perfil cadastrado.");

    await userEvent.click(screen.getByRole("button", { name: "Novo perfil" }));
    expect(await screen.findAllByRole("heading", { name: "Novo perfil" })).toHaveLength(2);

    await userEvent.type(screen.getByLabelText("Nome"), "Comprador Regional");
    await userEvent.type(screen.getByLabelText("Descricao"), "Compra insumos regionais.");
    await userEvent.click(screen.getByRole("checkbox", { name: /Criar pedido de compra/ }));
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => {
      const post = chamadas.find((c) => c.method === "POST" && c.url === "/api/administracao/perfis");
      expect(post).toBeDefined();
      expect(post!.body).toEqual({
        nome: "Comprador Regional",
        descricao: "Compra insumos regionais.",
        permissoes: ["Pedido.Criar"]
      });
    });
  });

  /** Seguranca: a interface nao envia unidadeNegocioId — o backend usa a da sessao. */
  it("nunca envia unidadeNegocioId no payload de criacao", async () => {
    rotas.set("GET /api/administracao/perfis", { status: 200, body: [] });
    rotas.set("GET /api/administracao/permissoes", { status: 200, body: CATALOGO });
    rotas.set("POST /api/administracao/perfis", { status: 201, body: perfil() });

    renderPerfis("/novo");
    await userEvent.type(await screen.findByLabelText("Nome"), "Qualquer");
    await userEvent.type(screen.getByLabelText("Descricao"), "Qualquer");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => {
      const post = chamadas.find((c) => c.method === "POST");
      expect(post).toBeDefined();
      expect(Object.keys(post!.body as object)).not.toContain("unidadeNegocioId");
    });
  });

  it("mostra a mensagem de conflito devolvida pela API ao salvar nome duplicado", async () => {
    rotas.set("GET /api/administracao/perfis", { status: 200, body: [] });
    rotas.set("GET /api/administracao/permissoes", { status: 200, body: CATALOGO });
    rotas.set("POST /api/administracao/perfis", {
      status: 409,
      body: { code: "nome_duplicado", message: "Ja existe um Perfil com este nome nesta Unidade de Negocio." }
    });

    renderPerfis("/novo");
    await userEvent.type(await screen.findByLabelText("Nome"), "Administrador");
    await userEvent.type(screen.getByLabelText("Descricao"), "x");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    expect(await screen.findByText(/Ja existe um Perfil com este nome/i)).toBeInTheDocument();
  });

  it("visualiza as permissoes de um perfil usando o catalogo da API", async () => {
    const analista = perfil({
      id: "22222222-2222-2222-2222-222222222222",
      nome: "Analista",
      permissoes: ["Pedido.Criar"]
    });
    rotas.set("GET /api/administracao/perfis", { status: 200, body: [analista] });
    rotas.set("GET /api/administracao/permissoes", { status: 200, body: CATALOGO });
    rotas.set(`GET /api/administracao/perfis/${analista.id}`, { status: 200, body: analista });

    renderPerfis();
    const row = (await screen.findByText("Analista")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Visualizar" }));

    expect(await screen.findByRole("heading", { name: "Analista" })).toBeInTheDocument();
    expect(await screen.findByText("Criar pedido de compra")).toBeInTheDocument();
  });

  /** Inativacao substitui a exclusao removida nesta sprint (ComprasFuncional.md). */
  it("inativa um perfil via PATCH de status, avisando sobre os usuarios impactados", async () => {
    const ativo = perfil({ usuariosVinculados: 3 });
    rotas.set("GET /api/administracao/perfis", { status: 200, body: [ativo] });
    rotas.set(`PATCH /api/administracao/perfis/${ativo.id}/status`, { status: 200, body: { ...ativo, ativo: false } });

    renderPerfis();
    const row = (await screen.findByText("Administrador")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Inativar" }));

    expect(await screen.findByText(/3 usuario\(s\) vinculado\(s\) perderao as permissoes/i)).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Inativar perfil" }));

    await waitFor(() => {
      const patch = chamadas.find((c) => c.method === "PATCH");
      expect(patch).toBeDefined();
      expect(patch!.body).toEqual({ ativo: false });
    });
  });

  it("mostra o erro do backend quando a inativacao e recusada pela invariante administrativa", async () => {
    const ativo = perfil();
    rotas.set("GET /api/administracao/perfis", { status: 200, body: [ativo] });
    rotas.set(`PATCH /api/administracao/perfis/${ativo.id}/status`, {
      status: 409,
      body: {
        code: "ultimo_perfil_administrativo",
        message: "Esta operacao deixaria a Unidade de Negocio sem nenhum Perfil ativo com a permissao Perfil.Gerenciar."
      }
    });

    renderPerfis();
    const row = (await screen.findByText("Administrador")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Inativar" }));
    await userEvent.click(screen.getByRole("button", { name: "Inativar perfil" }));

    expect(await screen.findByText(/sem nenhum Perfil ativo com a permissao/i)).toBeInTheDocument();
  });

  it("oferece Ativar para um perfil inativo", async () => {
    rotas.set("GET /api/administracao/perfis", { status: 200, body: [perfil({ ativo: false, usuariosVinculados: 0 })] });

    renderPerfis();
    const row = (await screen.findByText("Administrador")).closest("tr")!;

    expect(within(row).getByRole("button", { name: "Ativar" })).toBeInTheDocument();
    expect(within(row).queryByRole("button", { name: "Excluir" })).not.toBeInTheDocument();
  });
});
