import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthContext } from "../../auth/context/AuthContext";
import { AppShell } from "../AppShell";

const usuarioTeste = {
  id: "u1",
  email: "ana@somagrupo.com.br",
  nome: "Ana Souza",
  unidadeNegocioId: "un1",
  permissoes: [],
  escopoAdministrativo: "Produto" as const
};

function renderShell() {
  return render(
    <AuthContext.Provider value={{ usuario: usuarioTeste, carregando: false, refresh: vi.fn(), setUsuario: vi.fn(), logout: vi.fn() }}>
      <MemoryRouter initialEntries={["/"]}>
        <AppShell>
          <div>conteudo</div>
        </AppShell>
      </MemoryRouter>
    </AuthContext.Provider>
  );
}

beforeEach(() => {
  window.localStorage.clear();
});

afterEach(() => {
  cleanup();
});

describe("AppShell — recolher/expandir sidebar", () => {
  it("comeca expandida por padrao, com rotulos visiveis", () => {
    renderShell();

    expect(screen.getByRole("navigation", { name: "Navegação do portal +Compras" })).not.toHaveClass("app-sidebar-collapsed");
    expect(screen.getByRole("link", { name: /Fornecedores/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Recolher menu" })).toBeInTheDocument();
  });

  it("recolhe ao clicar no botao de alternancia, mantendo o link acessivel por titulo", async () => {
    renderShell();

    await userEvent.click(screen.getByRole("button", { name: "Recolher menu" }));

    expect(screen.getByRole("navigation", { name: "Navegação do portal +Compras" })).toHaveClass("app-sidebar-collapsed");
    const linkFornecedores = screen.getByRole("link", { name: /Fornecedores/ });
    expect(linkFornecedores).toHaveAttribute("title", "Fornecedores");
    expect(screen.getByRole("button", { name: "Expandir menu" })).toBeInTheDocument();
  });

  it("persiste a preferencia de sidebar recolhida entre montagens (localStorage)", async () => {
    const { unmount } = renderShell();
    await userEvent.click(screen.getByRole("button", { name: "Recolher menu" }));
    unmount();

    renderShell();

    expect(screen.getByRole("navigation", { name: "Navegação do portal +Compras" })).toHaveClass("app-sidebar-collapsed");
  });
});
