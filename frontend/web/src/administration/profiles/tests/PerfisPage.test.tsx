import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { PerfisRoutes } from "../routes/PerfisRoutes";

function renderPerfis(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <PerfisRoutes />
    </MemoryRouter>
  );
}

describe("PerfisPage", () => {
  afterEach(() => {
    cleanup();
  });

  it("lista os perfis mockados", async () => {
    renderPerfis();
    expect(await screen.findByRole("heading", { name: "Perfis cadastrados" })).toBeInTheDocument();
    expect(await screen.findByText("Administrador Senior")).toBeInTheDocument();
    expect(await screen.findByText("Analista")).toBeInTheDocument();
  });

  it("abre o formulario de novo perfil e volta para a lista apos salvar", async () => {
    renderPerfis();
    await screen.findByText("Administrador Senior");

    await userEvent.click(screen.getByRole("button", { name: "Novo perfil" }));
    expect(await screen.findAllByRole("heading", { name: "Novo perfil" })).toHaveLength(2);

    await userEvent.type(screen.getByLabelText("Nome"), "Comprador Regional");
    await userEvent.type(screen.getByLabelText("Descricao"), "Compra insumos regionais.");

    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Perfis cadastrados" })).toBeInTheDocument());
    expect(await screen.findByText("Comprador Regional")).toBeInTheDocument();
  });

  it("visualiza as permissoes de um perfil existente", async () => {
    renderPerfis();
    await screen.findByText("Administrador Senior");

    const row = screen.getByText("Analista").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Visualizar" }));

    expect(await screen.findByRole("heading", { name: "Analista" })).toBeInTheDocument();
    expect(screen.getByText("Criar pedido de compra")).toBeInTheDocument();
  });
});
