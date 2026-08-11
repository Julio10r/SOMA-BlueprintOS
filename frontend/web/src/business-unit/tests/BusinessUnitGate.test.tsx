import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { BusinessUnitGate } from "../components/BusinessUnitGate";

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe("BusinessUnitGate", () => {
  it("segue direto para o conteudo quando ha apenas uma Unidade de Negocio (caso unico hoje em producao)", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({
      ok: true,
      status: 200,
      json: async () => [{ id: "1", nome: "AZZAS Corporativo", slug: "azzas-corporativo", ativa: true }]
    }) as Response));

    render(
      <BusinessUnitGate>
        <div>Dashboard</div>
      </BusinessUnitGate>
    );

    expect(await screen.findByText("Dashboard")).toBeInTheDocument();
  });

  it("exibe a tela de selecao quando ha mais de uma Unidade de Negocio disponivel", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({
      ok: true,
      status: 200,
      json: async () => [
        { id: "1", nome: "AZZAS Corporativo", slug: "azzas-corporativo", ativa: true },
        { id: "2", nome: "Farm", slug: "farm", ativa: true }
      ]
    }) as Response));

    render(
      <BusinessUnitGate>
        <div>Dashboard</div>
      </BusinessUnitGate>
    );

    expect(await screen.findByRole("heading", { name: "Selecione a Unidade de Negocio" })).toBeInTheDocument();
    expect(screen.queryByText("Dashboard")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /AZZAS Corporativo/i }));

    await waitFor(() => expect(screen.getByText("Dashboard")).toBeInTheDocument());
  });
});
