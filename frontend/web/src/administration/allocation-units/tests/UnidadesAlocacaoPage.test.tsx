import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { UnidadesAlocacaoRoutes } from "../routes/UnidadesAlocacaoRoutes";

function renderUnidadesAlocacao(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <UnidadesAlocacaoRoutes />
    </MemoryRouter>
  );
}

describe("UnidadesAlocacaoPage", () => {
  afterEach(() => {
    cleanup();
  });

  it("lista as unidades de alocacao mockadas", async () => {
    renderUnidadesAlocacao();
    expect(await screen.findByRole("heading", { name: "Unidades de Alocacao cadastradas" })).toBeInTheDocument();
    expect(await screen.findByText("SOMA Corporativo")).toBeInTheDocument();
    expect(await screen.findByText("Farm")).toBeInTheDocument();
  });

  it("abre o formulario de nova unidade de alocacao e volta para a lista apos salvar", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("SOMA Corporativo");

    await userEvent.click(screen.getByRole("button", { name: "Nova unidade de alocacao" }));
    expect(await screen.findAllByRole("heading", { name: "Nova unidade de alocacao" })).toHaveLength(2);

    await userEvent.type(screen.getByLabelText("Nome"), "Fabula Outlet");
    await userEvent.type(screen.getByLabelText("Descricao"), "Agrupamento das lojas de outlet da Fabula.");

    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Unidades de Alocacao cadastradas" })).toBeInTheDocument());
    expect(await screen.findByText("Fabula Outlet")).toBeInTheDocument();
  });

  it("visualiza uma unidade de alocacao existente", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("SOMA Corporativo");

    const row = screen.getByText("Farm").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Visualizar" }));

    expect(await screen.findByRole("heading", { name: "Farm" })).toBeInTheDocument();
    expect(screen.getByText("Agrupamento orcamentario e de relatorios da marca Farm.")).toBeInTheDocument();
  });

  it("edita uma unidade de alocacao existente", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("SOMA Corporativo");

    const row = screen.getByText("Animale").closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    expect(await screen.findByRole("heading", { name: "Editar unidade de alocacao", level: 1 })).toBeInTheDocument();
    const descricaoInput = await screen.findByLabelText("Descricao");
    await userEvent.clear(descricaoInput);
    await userEvent.type(descricaoInput, "Descricao atualizada da unidade Animale.");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Animale" })).toBeInTheDocument());
    expect(await screen.findByText("Descricao atualizada da unidade Animale.")).toBeInTheDocument();
  });

  it("ativa e inativa uma unidade de alocacao pela listagem", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("Projetos Especiais");

    const row = screen.getByText("Projetos Especiais").closest("tr")!;
    expect(within(row).getByText("Inativo")).toBeInTheDocument();

    await userEvent.click(within(row).getByRole("button", { name: "Ativar" }));

    await waitFor(() => expect(within(screen.getByText("Projetos Especiais").closest("tr")!).getByText("Ativo")).toBeInTheDocument());
  });

  it("nao possui acao de exclusao fisica em nenhuma linha", async () => {
    renderUnidadesAlocacao();
    await screen.findByText("SOMA Corporativo");

    expect(screen.queryByRole("button", { name: "Excluir" })).not.toBeInTheDocument();
  });
});
