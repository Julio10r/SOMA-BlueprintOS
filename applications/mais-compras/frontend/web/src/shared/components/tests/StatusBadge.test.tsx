import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { StatusBadge } from "../StatusBadge";

/**
 * Regressao: StatusBadge (tone="situacao") deriva a classe CSS de um valor
 * externo (ex.: status de Filial/Centro de Custo/Perfil/Usuario). O
 * componente nao deve quebrar nem gerar uma classe CSS invalida quando o
 * valor recebido e vazio, contem espacos ou nao e reconhecido.
 */
describe("StatusBadge", () => {
  afterEach(() => cleanup());

  it("renderiza normalmente para um valor conhecido (tone situacao)", () => {
    render(<StatusBadge value="Ativo" tone="situacao" />);
    const badge = screen.getByText("Ativo");
    expect(badge.className).toBe("status status-ativo");
  });

  it("nao quebra com string vazia", () => {
    render(<StatusBadge value="" tone="situacao" />);
    expect(document.querySelector(".status")).toBeInTheDocument();
    expect(document.querySelector(".status")!.className).toBe("status status-desconhecido");
  });

  it("nao gera classe CSS invalida quando o valor contem espacos", () => {
    render(<StatusBadge value="Em Analise" tone="situacao" />);
    const badge = screen.getByText("Em Analise");
    expect(badge.className).toBe("status status-em-analise");
  });

  it("nao quebra para um valor nao mapeado (tone decisao)", () => {
    render(<StatusBadge value="Outro" tone="decisao" />);
    const badge = screen.getByText("Outro");
    expect(badge.className).toBe("badge ");
  });
});
