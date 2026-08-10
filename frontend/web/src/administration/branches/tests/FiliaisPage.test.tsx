import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { FiliaisRoutes } from "../routes/FiliaisRoutes";

function renderFiliais(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <FiliaisRoutes />
    </MemoryRouter>
  );
}

describe("FiliaisPage", () => {
  afterEach(() => {
    cleanup();
  });

  it("lista as filiais mockadas com Codigo CliFor, Nome CliFor e Descricao +Compras", async () => {
    renderFiliais();
    expect(await screen.findByRole("heading", { name: "Filiais integradas do ERP" })).toBeInTheDocument();
    expect(await screen.findByText("0101")).toBeInTheDocument();
    expect(await screen.findByText("SOMA MATRIZ SAO PAULO")).toBeInTheDocument();
    expect(await screen.findByText("ANIMALE LOJA JARDINS")).toBeInTheDocument();
    expect(await screen.findByText("Loja conceito - prioridade de atendimento")).toBeInTheDocument();
  });

  it("nao exibe nenhum botao de criar ou excluir filial", async () => {
    renderFiliais();
    await screen.findByText("0101");
    expect(screen.queryByRole("button", { name: /nova filial/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /criar/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /excluir/i })).not.toBeInTheDocument();
  });

  it("exibe os dados do ERP como somente leitura na tela de edicao", async () => {
    renderFiliais();
    const row = (await screen.findByText("SOMA MATRIZ SAO PAULO")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    expect(await screen.findByRole("heading", { name: "Editar filial" })).toBeInTheDocument();
    expect(await screen.findByText("Dados do ERP (somente leitura)")).toBeInTheDocument();

    // Codigo CliFor e Nome CliFor aparecem como texto, nao como input editavel.
    expect(screen.queryByLabelText("Codigo CliFor")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Nome CliFor")).not.toBeInTheDocument();
    expect(screen.getAllByText("0101").length).toBeGreaterThan(0);
    expect(screen.getAllByText("SOMA MATRIZ SAO PAULO").length).toBeGreaterThan(0);
  });

  it("permite editar a Descricao +Compras sem alterar o Nome CliFor do ERP", async () => {
    renderFiliais();
    const row = (await screen.findByText("SOMA MATRIZ SAO PAULO")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Editar filial" });
    await userEvent.type(await screen.findByLabelText("Descricao +Compras"), "Sede administrativa");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Detalhes da filial" })).toBeInTheDocument());
    expect(await screen.findByText("Sede administrativa")).toBeInTheDocument();
    expect(screen.getAllByText("SOMA MATRIZ SAO PAULO").length).toBeGreaterThan(0);
  });

  it("ativa e inativa a filial no +Compras diretamente pela listagem", async () => {
    renderFiliais();
    const row = (await screen.findByText("ANIMALE CD EXTREMA")).closest("tr")!;
    expect(within(row).getByText("Inativo")).toBeInTheDocument();

    await userEvent.click(within(row).getByRole("button", { name: "Ativar no +Compras" }));

    await waitFor(() => {
      const updatedRow = screen.getByText("ANIMALE CD EXTREMA").closest("tr")!;
      expect(within(updatedRow).getByText("Ativo")).toBeInTheDocument();
      expect(within(updatedRow).getByRole("button", { name: "Inativar no +Compras" })).toBeInTheDocument();
    });
  });

  it("filtra por status no +Compras", async () => {
    renderFiliais();
    await screen.findByText("SOMA MATRIZ SAO PAULO");

    await userEvent.selectOptions(screen.getByLabelText("Status no +Compras"), "Inativo");

    expect(screen.queryByText("SOMA MATRIZ SAO PAULO")).not.toBeInTheDocument();
    expect(screen.getByText("FARM CD GUARULHOS")).toBeInTheDocument();
  });

  it("pesquisa por Codigo CliFor", async () => {
    renderFiliais();
    await screen.findByText("SOMA MATRIZ SAO PAULO");

    await userEvent.type(screen.getByLabelText("Pesquisar"), "0104");

    expect(screen.getByText("FABULA LOJA VILLAGE MALL")).toBeInTheDocument();
    expect(screen.queryByText("SOMA MATRIZ SAO PAULO")).not.toBeInTheDocument();
  });
});
