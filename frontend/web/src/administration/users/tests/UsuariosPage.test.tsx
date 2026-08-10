import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { UsuariosRoutes } from "../routes/UsuariosRoutes";

function renderUsuarios(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <UsuariosRoutes />
    </MemoryRouter>
  );
}

describe("UsuariosPage", () => {
  afterEach(() => {
    cleanup();
  });

  it("lista os usuarios mockados", async () => {
    renderUsuarios();
    expect(await screen.findByRole("heading", { name: "Usuarios cadastrados" })).toBeInTheDocument();
    expect(await screen.findByText("Ana Souza")).toBeInTheDocument();
    expect(await screen.findByText("Bruno Lima")).toBeInTheDocument();
  });

  it("abre o formulario de novo usuario e volta para a lista apos salvar", async () => {
    renderUsuarios();
    await screen.findByText("Ana Souza");

    await userEvent.click(screen.getByRole("button", { name: "Novo usuario" }));
    await waitFor(() => expect(screen.getAllByRole("heading", { name: "Novo usuario" })).toHaveLength(2));

    await userEvent.type(screen.getByLabelText("Nome"), "Elisa Prado");
    await userEvent.type(screen.getByLabelText("E-mail"), "elisa.prado@somagrupo.com.br");

    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Usuarios cadastrados" })).toBeInTheDocument());
    expect(await screen.findByText("Elisa Prado")).toBeInTheDocument();
  });

  it("visualiza os perfis e centros de custo de um usuario existente", async () => {
    renderUsuarios();
    await screen.findByText("Ana Souza");

    const row = screen.getByText("Bruno Lima").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Visualizar" }));

    expect(await screen.findByRole("heading", { name: "Bruno Lima" })).toBeInTheDocument();
    expect(screen.getByText("Analista")).toBeInTheDocument();
    expect(screen.getByText("CC-001")).toBeInTheDocument();
  });

  it("inativa um usuario ativo em vez de excluir", async () => {
    renderUsuarios();
    await screen.findByText("Ana Souza");

    const row = screen.getByText("Bruno Lima").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Inativar" }));

    const dialog = await screen.findByRole("dialog");
    await userEvent.click(within(dialog).getByRole("button", { name: "Inativar" }));

    await waitFor(() => {
      const updatedRow = screen.getByText("Bruno Lima").closest("tr")!;
      expect(within(updatedRow).getByText("Inativo")).toBeInTheDocument();
    });
    expect(screen.getByText("Bruno Lima")).toBeInTheDocument();
  });
});
