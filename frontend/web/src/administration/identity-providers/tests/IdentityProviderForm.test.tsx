import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { IdentityProviderForm } from "../components/IdentityProviderForm";
import type { IdentityProvider } from "../types/identityProviderTypes";

/**
 * O1.11 — garante que o segredo (`parametros`) do Identity Provider nunca aparece pre-preenchido no
 * formulario de edicao: a API nunca devolve o valor real (apenas `parametrosConfigurados: boolean`), e
 * o componente deve refletir isso deixando o campo sempre vazio, mesmo quando o provider ja possui
 * parametros configurados.
 */
afterEach(() => cleanup());

const provider: IdentityProvider = {
  id: "aaaaaaaa-0000-0000-0000-000000000001",
  unidadeNegocioId: "11111111-1111-1111-1111-111111111111",
  tipo: "MicrosoftEntraId",
  dominiosAutorizados: ["soma.com.br"],
  parametrosConfigurados: true,
  status: "Ativo"
};

describe("IdentityProviderForm", () => {
  it("nao pre-preenche o campo de parametros sensiveis ao editar um provider ja configurado", () => {
    render(
      <IdentityProviderForm provider={provider} error={null} loading={false} onSubmit={vi.fn()} onCancel={vi.fn()} />
    );

    const campoParametros = screen.getByLabelText(/Parametros de configuracao/i) as HTMLInputElement;
    expect(campoParametros.value).toBe("");
    expect(campoParametros.type).toBe("password");
    expect(screen.getByText("Ja configurado")).toBeInTheDocument();
  });

  it("nao exibe indicador 'Ja configurado' para um provider novo", () => {
    render(<IdentityProviderForm error={null} loading={false} onSubmit={vi.fn()} onCancel={vi.fn()} />);
    expect(screen.queryByText("Ja configurado")).not.toBeInTheDocument();
  });
});
