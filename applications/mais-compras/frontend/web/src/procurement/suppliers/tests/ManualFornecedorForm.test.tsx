import { useState } from "react";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ManualFornecedorForm, manualFornecedorDraftInicial } from "../components/ManualFornecedorForm";
import type { ManualFornecedorDraft } from "../types/linxSupplierContract";

/**
 * Gate de homologação de Fornecedores (2026-09-01), item 6: consulta automática de CEP (via
 * backend, ViaCEP) e regra de "não sobrescrever silenciosamente" (achado 3 de
 * docs/audits/Discovery-Fornecedor-Tela-001016G1.md).
 */

function Harness({ draftInicial }: { draftInicial?: Partial<ManualFornecedorDraft> }) {
  const [draft, setDraft] = useState<ManualFornecedorDraft>({ ...manualFornecedorDraftInicial, ...draftInicial });
  return <ManualFornecedorForm draft={draft} onDraftChange={setDraft} onSubmit={vi.fn()} onCancel={vi.fn()} loading={false} />;
}

beforeEach(() => {
  vi.stubGlobal(
    "fetch",
    vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url === "/fornecedores/consulta-cep" && init?.method === "POST") {
        const body = JSON.parse(String(init.body));
        if (body.cep === "01001000") {
          return {
            ok: true,
            status: 200,
            json: async () => ({
              cep: "01001000",
              logradouro: "Praca da Se",
              bairro: "Se",
              complemento: "",
              cidade: "São Paulo",
              estado: "SP",
              fonteConsulta: "ViaCEP",
              dataConsulta: new Date().toISOString(),
              statusConsulta: "Sucesso",
              mensagemErro: null,
              sucesso: true
            })
          } as Response;
        }
        return {
          ok: true,
          status: 200,
          json: async () => ({
            cep: body.cep,
            fonteConsulta: "ViaCEP",
            dataConsulta: new Date().toISOString(),
            statusConsulta: "Falha",
            mensagemErro: "CEP não encontrado.",
            sucesso: false,
            tipoErro: "NaoEncontrado"
          })
        } as Response;
      }
      if (url === "/fornecedores/categorias") {
        return {
          ok: true,
          status: 200,
          json: async () => [
            { codigo: "EMBALAGEM", descricao: "Embalagem" },
            { codigo: "OUTROS", descricao: "Outros" }
          ]
        } as Response;
      }
      if (url === "/fornecedores/consulta-cnpj" && init?.method === "POST") {
        return {
          ok: true,
          status: 200,
          json: async () => ({
            cnpj_Cpf: "11222333000181",
            razaoSocial: "Fornecedor Consultado LTDA",
            nomeFantasia: "Fornecedor Consultado",
            email: "contato@consultado.example",
            telefone: "1133334444",
            cep: "01001000",
            logradouro: "Praca da Se",
            numero: "1",
            complemento: "",
            bairro: "Se",
            cidade: "São Paulo",
            estado: "SP",
            pais: "Brasil",
            cnaePrincipalCodigo: "6201501",
            cnaePrincipalDescricao: "Desenvolvimento de programas",
            fonteConsulta: "BrasilAPI",
            dataConsulta: new Date().toISOString(),
            statusConsulta: "Sucesso",
            mensagemErro: null,
            sucesso: true,
            permiteRetry: false
          })
        } as Response;
      }
      throw new Error(`Unexpected fetch ${url}`);
    })
  );
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe("ManualFornecedorForm — consulta de CEP", () => {
  it("preenche Logradouro/Bairro/Cidade/UF automaticamente ao sair do campo CEP", async () => {
    render(<Harness />);

    await userEvent.type(screen.getByLabelText(/CEP/), "01001000");
    await userEvent.tab();

    await waitFor(() => {
      expect(screen.getByLabelText(/Logradouro/)).toHaveValue("Praca da Se");
    });
    expect(screen.getByLabelText(/Bairro/)).toHaveValue("Se");
    expect(screen.getByLabelText(/^Cidade/)).toHaveValue("São Paulo");
    expect(screen.getByLabelText(/^UF/)).toHaveValue("SP");
  });

  it("mostra aviso quando o CEP não é encontrado, sem preencher nada", async () => {
    render(<Harness />);

    await userEvent.type(screen.getByLabelText(/CEP/), "00000000");
    await userEvent.tab();

    expect(await screen.findByText("CEP não encontrado.")).toBeInTheDocument();
    expect(screen.getByLabelText(/Logradouro/)).toHaveValue("");
  });

  it("atualiza os campos de endereço para o novo CEP mesmo que já estivessem preenchidos por uma consulta anterior", async () => {
    // Gate de homologação (2026-09-01): diferente do CNPJ, o CEP é a fonte de verdade do
    // endereço — trocar o CEP precisa atualizar Logradouro/Bairro/Cidade/UF para o novo endereço,
    // nunca manter os dados do CEP anterior.
    render(<Harness draftInicial={{ bairro: "Bairro De Outro Endereco" }} />);

    await userEvent.type(screen.getByLabelText(/CEP/), "01001000");
    await userEvent.tab();

    await waitFor(() => {
      expect(screen.getByLabelText(/Logradouro/)).toHaveValue("Praca da Se");
    });
    expect(screen.getByLabelText(/Bairro/)).toHaveValue("Se");
  });

  it("não consulta o backend para CEP com menos de 8 dígitos", async () => {
    render(<Harness />);

    await userEvent.type(screen.getByLabelText(/CEP/), "123");
    await userEvent.tab();

    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(fetch).not.toHaveBeenCalledWith("/fornecedores/consulta-cep", expect.anything());
  });
});

describe("ManualFornecedorForm — consulta de CNPJ (item 'novo fluxo unico', 2026-09-01)", () => {
  it("pergunta via modal proprio da aplicacao (nunca window.confirm) e, se confirmado, preenche os dados consultados", async () => {
    render(<Harness />);

    await userEvent.type(screen.getByLabelText(/^CNPJ\/CPF \*/), "11.222.333/0001-81");
    await userEvent.tab();

    expect(await screen.findByText("Deseja consultar online os dados cadastrais deste CNPJ?")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Sim, consultar" }));

    await waitFor(() => {
      expect(screen.getByLabelText("Razão Social *")).toHaveValue("Fornecedor Consultado LTDA");
    });
    expect(screen.getByLabelText("Nome Fantasia *")).toHaveValue("Fornecedor Consultado");
    expect(screen.getByLabelText(/^E-mail/)).toHaveValue("contato@consultado.example");
    expect(screen.getByLabelText(/^Cidade/)).toHaveValue("São Paulo");
  });

  it("nao consulta quando o usuario recusa a confirmacao no modal", async () => {
    render(<Harness />);

    await userEvent.type(screen.getByLabelText(/^CNPJ\/CPF \*/), "11.222.333/0001-81");
    await userEvent.tab();

    await userEvent.click(await screen.findByRole("button", { name: "Não, cadastrar manualmente" }));

    expect(screen.queryByText("Deseja consultar online os dados cadastrais deste CNPJ?")).not.toBeInTheDocument();
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(fetch).not.toHaveBeenCalledWith("/fornecedores/consulta-cnpj", expect.anything());
    expect(screen.getByLabelText("Razão Social *")).toHaveValue("");
  });

  it("nunca sobrescreve Razao Social ja preenchida manualmente", async () => {
    render(<Harness draftInicial={{ razaoSocial: "Razao Social Digitada Pelo Usuario" }} />);

    await userEvent.type(screen.getByLabelText(/^CNPJ\/CPF \*/), "11.222.333/0001-81");
    await userEvent.tab();
    await userEvent.click(await screen.findByRole("button", { name: "Sim, consultar" }));

    await waitFor(() => {
      expect(screen.getByLabelText("Nome Fantasia *")).toHaveValue("Fornecedor Consultado");
    });
    expect(screen.getByLabelText("Razão Social *")).toHaveValue("Razao Social Digitada Pelo Usuario");
  });

  it("nao pergunta para CNPJ invalido ou incompleto", async () => {
    render(<Harness />);

    await userEvent.type(screen.getByLabelText(/^CNPJ\/CPF \*/), "111111111111");
    await userEvent.tab();

    expect(screen.queryByText("Deseja consultar online os dados cadastrais deste CNPJ?")).not.toBeInTheDocument();
  });
});
