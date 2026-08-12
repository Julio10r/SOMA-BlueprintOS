import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { SituacaoCadastralBadge } from "../components/SituacaoCadastralBadge";
import type { SituacaoCadastralCnpj } from "../types/linxSupplierContract";

describe("SituacaoCadastralBadge", () => {
  afterEach(() => cleanup());

  const casos: Array<[SituacaoCadastralCnpj, string, string]> = [
    ["Ativa", "Ativa", "status-ativa"],
    ["Baixada", "Baixada", "status-baixada"],
    ["Suspensa", "Suspensa", "status-suspensa"],
    ["Inapta", "Inapta", "status-inapta"],
    ["Nula", "Nula", "status-nula"],
    ["Desconhecida", "Desconhecida", "status-desconhecida"]
  ];

  it.each(casos)("renderiza a situacao %s com o label e a classe corretos", (valor, labelEsperado, classeEsperada) => {
    render(<SituacaoCadastralBadge value={valor} />);
    const badge = screen.getByText(labelEsperado);
    expect(badge.className).toContain(classeEsperada);
  });

  it("renderiza fallback seguro (Desconhecida) quando o valor e null (consulta sem sucesso)", () => {
    render(<SituacaoCadastralBadge value={null} />);
    expect(screen.getByText("Desconhecida")).toBeInTheDocument();
  });

  it("renderiza fallback seguro (Desconhecida) quando o valor e undefined", () => {
    render(<SituacaoCadastralBadge value={undefined} />);
    expect(screen.getByText("Desconhecida")).toBeInTheDocument();
  });

  it("nunca lanca TypeError value.toLowerCase is not a function, mesmo com valor inesperado em runtime", () => {
    // Regressao do crash real registrado no Design Review: o backend antes desta
    // sprint podia enviar o codigo numerico bruto do enum. O componente nao pode
    // depender de `.toLowerCase()` sobre um valor cuja natureza nao esta garantida.
    const valorInesperado = 2 as unknown as SituacaoCadastralCnpj;
    expect(() => render(<SituacaoCadastralBadge value={valorInesperado} />)).not.toThrow();
    expect(screen.getByText("Desconhecida")).toBeInTheDocument();
  });
});
