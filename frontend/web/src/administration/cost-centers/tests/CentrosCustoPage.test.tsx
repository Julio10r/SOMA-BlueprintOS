import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { CentrosCustoRoutes } from "../routes/CentrosCustoRoutes";

function renderCentrosCusto(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <CentrosCustoRoutes />
    </MemoryRouter>
  );
}

describe("CentrosCustoPage", () => {
  afterEach(() => {
    cleanup();
  });

  it("lista os centros de custo mockados com Codigo, Descricao ERP e Descricao +Compras", async () => {
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

  it("exibe a Unidade de Alocacao padrao e a quantidade de vinculos na listagem", async () => {
    renderCentrosCusto();
    const row = (await screen.findByText("LOGISTICA E DISTRIBUICAO")).closest("tr")!;
    expect(within(row).getByText("Farm")).toBeInTheDocument();

    const rowSemPadrao = (await screen.findByText("MARKETING E TRADE")).closest("tr")!;
    expect(within(rowSemPadrao).getByText("Sem unidade padrao")).toBeInTheDocument();
  });

  it("exibe os dados do ERP como somente leitura na tela de edicao", async () => {
    renderCentrosCusto();
    const row = (await screen.findByText("ADMINISTRATIVO CORPORATIVO")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    expect(await screen.findByRole("heading", { name: "Editar centro de custo" })).toBeInTheDocument();
    expect(await screen.findByText("Dados do ERP (somente leitura)")).toBeInTheDocument();

    // Codigo e Descricao ERP aparecem como texto, nao como input editavel.
    expect(screen.queryByLabelText("Codigo Centro de Custo")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Descricao ERP")).not.toBeInTheDocument();
    expect(screen.getAllByText("1001").length).toBeGreaterThan(0);
    expect(screen.getAllByText("ADMINISTRATIVO CORPORATIVO").length).toBeGreaterThan(0);
  });

  it("permite editar a Descricao +Compras sem alterar a Descricao ERP", async () => {
    renderCentrosCusto();
    const row = (await screen.findByText("ADMINISTRATIVO CORPORATIVO")).closest("tr")!;
    await userEvent.click(within(row).getByRole("button", { name: "Editar" }));

    await screen.findByRole("heading", { name: "Editar centro de custo" });
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
});
