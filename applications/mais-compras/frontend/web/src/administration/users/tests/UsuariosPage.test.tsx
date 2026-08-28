import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UsuariosRoutes } from "../routes/UsuariosRoutes";
import type { Usuario } from "../types/userTypes";

/**
 * O1.6 — a Gestao de Usuarios consome a API real (`administracao/usuarios`), substituindo
 * o `usuariosMockApi.ts` removido nesta sprint. Mesmo padrao de integracao HTTP de
 * `administration/profiles/tests/PerfisPage.test.tsx` (O1.5): fetch interceptado, cobrindo
 * sucesso, vazio, erro, acesso negado (403) e ativacao/inativacao.
 */
function usuario(over: Partial<Usuario> = {}): Usuario {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    nome: "Ana Souza",
    email: "ana.souza@somagrupo.com.br",
    unidadeNegocioId: "99999999-9999-9999-9999-999999999999",
    ativo: true,
    perfis: [{ id: "perfil-admin", nome: "Administrador Sênior", ativo: true }],
    centrosCusto: [],
    todosCentrosCusto: true,
    criadoEm: "2026-07-15T09:00:00Z",
    atualizadoEm: "2026-08-01T10:00:00Z",
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
  rotas.set("GET /api/administracao/perfis", { status: 200, body: [] });
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

function renderUsuarios(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <UsuariosRoutes />
    </MemoryRouter>
  );
}

describe("UsuariosPage — integracao com a API real", () => {
  it("lista os usuarios vindos da API", async () => {
    rotas.set("GET /api/administracao/usuarios", {
      status: 200,
      body: [usuario(), usuario({ id: "22222222-2222-2222-2222-222222222222", nome: "Bruno Lima" })]
    });

    renderUsuarios();

    expect(await screen.findByRole("heading", { name: "Usuários cadastrados" })).toBeInTheDocument();
    expect(await screen.findByText("Ana Souza")).toBeInTheDocument();
    expect(await screen.findByText("Bruno Lima")).toBeInTheDocument();
    expect(chamadas.some((c) => c.url === "/api/administracao/usuarios" && c.method === "GET")).toBe(true);
  });

  it("mostra o estado vazio quando a API nao retorna usuarios", async () => {
    rotas.set("GET /api/administracao/usuarios", { status: 200, body: [] });

    renderUsuarios();

    expect(await screen.findByText("Nenhum usuário cadastrado.")).toBeInTheDocument();
  });

  /** 403 real do backend: sessao valida, sem a permissao Usuario.Gerenciar. */
  it("mostra acesso negado quando a API responde 403", async () => {
    rotas.set("GET /api/administracao/usuarios", { status: 403, body: {} });

    renderUsuarios();

    expect(await screen.findByText(/não tem permissão para acessar a Gestão de Usuários/i)).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Usuários cadastrados" })).not.toBeInTheDocument();
  });

  it("mostra erro quando a API falha por outro motivo", async () => {
    rotas.set("GET /api/administracao/usuarios", { status: 500, body: { message: "Falha inesperada no servidor." } });

    renderUsuarios();

    expect(await screen.findByText("Falha inesperada no servidor.")).toBeInTheDocument();
  });

  it("cria um usuario enviando nome, e-mail, perfis e centros de custo", async () => {
    rotas.set("GET /api/administracao/usuarios", { status: 200, body: [] });
    rotas.set("POST /api/administracao/usuarios", { status: 201, body: usuario({ nome: "Elisa Prado" }) });

    renderUsuarios();
    await screen.findByText("Nenhum usuário cadastrado.");

    await userEvent.click(screen.getByRole("button", { name: "Novo usuário" }));
    await waitFor(() => expect(screen.getAllByRole("heading", { name: "Novo usuário" })).toHaveLength(2));

    await userEvent.type(screen.getByLabelText("Nome"), "Elisa Prado");
    await userEvent.type(screen.getByLabelText("E-mail"), "elisa.prado@somagrupo.com.br");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => {
      const post = chamadas.find((c) => c.method === "POST" && c.url === "/api/administracao/usuarios");
      expect(post).toBeDefined();
      expect(post!.body).toEqual({
        nome: "Elisa Prado",
        email: "elisa.prado@somagrupo.com.br",
        perfis: [],
        centrosCusto: [],
        todosCentrosCusto: false
      });
    });

    await waitFor(() => expect(screen.getByRole("heading", { name: "Usuários cadastrados" })).toBeInTheDocument());
  });

  /** Seguranca: a interface nunca envia unidadeNegocioId — o backend usa a da sessao. */
  it("nunca envia unidadeNegocioId no payload de criacao", async () => {
    rotas.set("GET /api/administracao/usuarios", { status: 200, body: [] });
    rotas.set("POST /api/administracao/usuarios", { status: 201, body: usuario() });

    renderUsuarios("/novo");
    await userEvent.type(await screen.findByLabelText("Nome"), "Qualquer");
    await userEvent.type(screen.getByLabelText("E-mail"), "qualquer@somagrupo.com.br");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => {
      const post = chamadas.find((c) => c.method === "POST");
      expect(post).toBeDefined();
      expect(Object.keys(post!.body as object)).not.toContain("unidadeNegocioId");
    });
  });

  it("mostra a mensagem de conflito devolvida pela API ao salvar e-mail duplicado", async () => {
    rotas.set("GET /api/administracao/usuarios", { status: 200, body: [] });
    rotas.set("POST /api/administracao/usuarios", {
      status: 409,
      body: { code: "email_duplicado", message: "Ja existe um usuario com este e-mail." }
    });

    renderUsuarios("/novo");
    await userEvent.type(await screen.findByLabelText("Nome"), "Duplicado");
    await userEvent.type(screen.getByLabelText("E-mail"), "duplicado@somagrupo.com.br");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    expect(await screen.findByText(/Ja existe um usuario com este e-mail/i)).toBeInTheDocument();
  });

  it("visualiza os perfis e centros de custo de um usuario existente", async () => {
    const alvo = usuario({
      id: "33333333-3333-3333-3333-333333333333",
      nome: "Bruno Lima",
      todosCentrosCusto: false,
      centrosCusto: ["CC-001"],
      perfis: []
    });
    rotas.set("GET /api/administracao/usuarios", { status: 200, body: [alvo] });
    rotas.set(`GET /api/administracao/usuarios/${alvo.id}`, { status: 200, body: alvo });
    rotas.set("GET /api/administracao/centros-custo", {
      status: 200,
      body: [{
        id: "CC-001",
        codigoErp: "CC-001",
        descricaoErp: "Compras Corporativo",
        ativoNoMaisCompras: true,
        unidadeNegocioId: alvo.unidadeNegocioId,
        temMetadadoLocal: false,
        quantidadeUnidadesAlocacaoVinculadas: 0,
        criadoEm: alvo.criadoEm,
        atualizadoEm: alvo.atualizadoEm
      }]
    });

    renderUsuarios();
    const row = (await screen.findByText("Bruno Lima")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Visualizar" }));

    expect(await screen.findByRole("heading", { name: "Bruno Lima" })).toBeInTheDocument();
    expect(await screen.findByText("CC-001")).toBeInTheDocument();
    expect(screen.getByText("Nenhum perfil vinculado a este usuário.")).toBeInTheDocument();
  });

  it("inativa um usuario ativo em vez de excluir", async () => {
    const alvo = usuario({ id: "44444444-4444-4444-4444-444444444444", nome: "Bruno Lima" });
    rotas.set("GET /api/administracao/usuarios", { status: 200, body: [alvo] });
    rotas.set(`PATCH /api/administracao/usuarios/${alvo.id}/status`, { status: 200, body: { ...alvo, ativo: false } });

    renderUsuarios();
    const row = (await screen.findByText("Bruno Lima")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Inativar" }));

    const dialog = await screen.findByRole("dialog");
    await userEvent.click(within(dialog).getByRole("button", { name: "Inativar" }));

    await waitFor(() => {
      const patch = chamadas.find((c) => c.method === "PATCH");
      expect(patch).toBeDefined();
      expect(patch!.body).toEqual({ ativo: false });
    });
  });

  it("mostra o erro do backend quando a inativacao e recusada pela regra do Administrador Sênior", async () => {
    const alvo = usuario({ id: "55555555-5555-5555-5555-555555555555", nome: "Admin Único" });
    rotas.set("GET /api/administracao/usuarios", { status: 200, body: [alvo] });
    rotas.set(`PATCH /api/administracao/usuarios/${alvo.id}/status`, {
      status: 409,
      body: {
        code: "ultimo_administrador_senior_ativo",
        message: "A operação deixaria a Unidade de Negócio sem nenhum Administrador Sênior ativo."
      }
    });

    renderUsuarios();
    const row = (await screen.findByText("Admin Único")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Inativar" }));
    const dialog = await screen.findByRole("dialog");
    await userEvent.click(within(dialog).getByRole("button", { name: "Inativar" }));

    expect(await screen.findByText(/sem nenhum Administrador Sênior ativo/i)).toBeInTheDocument();
  });
});
