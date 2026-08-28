import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { BootstrapPage } from "../pages/BootstrapPage";

function renderBootstrap() {
  return render(
    <MemoryRouter initialEntries={["/bootstrap"]}>
      <Routes>
        <Route path="/bootstrap" element={<BootstrapPage />} />
        <Route path="/login" element={<div>Tela de login</div>} />
      </Routes>
    </MemoryRouter>
  );
}

async function preencherAcesso(secret = "chave-correta") {
  await userEvent.type(await screen.findByLabelText("E-mail autorizado"), "ana@somagrupo.com.br");
  await userEvent.type(screen.getByLabelText("Chave de configuração inicial"), secret);
  await userEvent.click(screen.getByRole("button", { name: "Continuar" }));
}

async function avancarParaConfirmacao(codigo = "123456", administradorNome = "Ana Souza") {
  await preencherAcesso();
  await userEvent.type(await screen.findByLabelText("Código de verificação"), codigo);
  await userEvent.click(screen.getByRole("button", { name: "Continuar" }));

  await userEvent.type(await screen.findByLabelText("Nome da Unidade de Negócio"), "Soma Grupo");
  await userEvent.click(screen.getByRole("button", { name: "Continuar" }));

  await userEvent.type(await screen.findByLabelText("Nome do Administrador Sênior"), administradorNome);
  await userEvent.click(screen.getByRole("button", { name: "Continuar" }));

  await userEvent.click(await screen.findByRole("checkbox"));
}

describe("BootstrapPage", () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === "string" ? input : input.toString();

      if (url === "/bootstrap/iniciar") {
        const body = JSON.parse(String(init?.body));
        if (body.secret === "indisponivel") {
          return new Response(null, { status: 404 });
        }
        return new Response(JSON.stringify({ message: "Se as informações fornecidas forem válidas, um código foi enviado." }), {
          status: 200
        });
      }

      if (url === "/bootstrap/otp/verificar") {
        const body = JSON.parse(String(init?.body));
        if (body.codigo === "000000") {
          return new Response(
            JSON.stringify({ code: "otp_invalido", message: "Código inválido ou expirado." }),
            { status: 400 }
          );
        }
        return new Response(null, { status: 204 });
      }

      if (url === "/bootstrap/concluir") {
        const body = JSON.parse(String(init?.body));
        if (body.administrador?.nome === "Conflito") {
          return new Response(
            JSON.stringify({ code: "bootstrap_nao_concluido", message: "A configuração inicial já foi concluída." }),
            { status: 400 }
          );
        }
        if (body.administrador?.nome === "SessaoExpirada") {
          return new Response(null, { status: 401 });
        }
        if (body.administrador?.nome === "SemPermissao") {
          return new Response(null, { status: 403 });
        }
        if (body.administrador?.nome === "Indisponivel") {
          return new Response(null, { status: 404 });
        }
        if (body.administrador?.nome === "Instavel") {
          return new Response(JSON.stringify({ message: "Erro inesperado." }), { status: 500 });
        }
        return new Response(
          JSON.stringify({
            usuario: { id: "u1", email: "ana@somagrupo.com.br", nome: body.administrador?.nome },
            unidadeNegocioId: "un1"
          }),
          { status: 200 }
        );
      }

      return new Response(null, { status: 404 });
    });

    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it("renderiza o passo inicial de acesso (e-mail + chave)", async () => {
    renderBootstrap();

    expect(await screen.findByLabelText("E-mail autorizado")).toBeInTheDocument();
    expect(screen.getByLabelText("Chave de configuração inicial")).toBeInTheDocument();
    expect(screen.queryByLabelText(/administrador/i)).not.toBeInTheDocument();
  });

  it("não permite avançar sem preencher e-mail e chave (botão desabilitado)", async () => {
    renderBootstrap();

    const botao = await screen.findByRole("button", { name: "Continuar" });
    expect(botao).toBeDisabled();

    await userEvent.type(screen.getByLabelText("E-mail autorizado"), "ana@somagrupo.com.br");
    expect(botao).toBeDisabled();

    await userEvent.type(screen.getByLabelText("Chave de configuração inicial"), "chave-correta");
    expect(botao).toBeEnabled();
  });

  it("avança até o passo de OTP após iniciar com sucesso", async () => {
    renderBootstrap();
    await preencherAcesso();

    expect(await screen.findByLabelText("Código de verificação")).toBeInTheDocument();
    const chamada = fetchMock.mock.calls.find((c) => c[0] === "/bootstrap/iniciar");
    expect(chamada).toBeDefined();
    expect(JSON.parse(String(chamada![1].body))).toEqual({
      email: "ana@somagrupo.com.br",
      secret: "chave-correta"
    });
  });

  it("exibe estado 'não disponível' quando /bootstrap/iniciar responde 404 (já concluído)", async () => {
    renderBootstrap();
    await preencherAcesso("indisponivel");

    expect(await screen.findByText(/não está mais disponível/i)).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Ir para o login" }));
    expect(await screen.findByText("Tela de login")).toBeInTheDocument();
  });

  it("exibe erro genérico de OTP inválido, sem revelar o motivo técnico", async () => {
    renderBootstrap();
    await preencherAcesso();
    await userEvent.type(await screen.findByLabelText("Código de verificação"), "000000");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Código inválido ou expirado.");
  });

  it("conclui o wizard com sucesso e envia o payload de conclusão sem nenhum campo de e-mail", async () => {
    renderBootstrap();
    await avancarParaConfirmacao();

    await userEvent.click(screen.getByRole("button", { name: "Concluir configuração inicial" }));

    await waitFor(() => expect(screen.getByText(/Configuração inicial concluída/)).toBeInTheDocument());

    const chamada = fetchMock.mock.calls.find((c) => c[0] === "/bootstrap/concluir");
    expect(chamada).toBeDefined();
    const corpo = JSON.parse(String(chamada![1].body));

    expect(corpo).toEqual({
      unidadeNegocio: { nome: "Soma Grupo", slug: "soma-grupo" },
      administrador: { nome: "Ana Souza" }
    });
    expect(corpo).not.toHaveProperty("email");
    expect(corpo.unidadeNegocio).not.toHaveProperty("email");
    expect(corpo.administrador).not.toHaveProperty("email");
    expect(Object.keys(corpo.administrador)).toEqual(["nome"]);
  });

  it("não expõe nenhum campo de e-mail nos passos de Administrador Sênior e confirmação", async () => {
    renderBootstrap();
    await preencherAcesso();
    await userEvent.type(await screen.findByLabelText("Código de verificação"), "123456");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));
    await userEvent.type(await screen.findByLabelText("Nome da Unidade de Negócio"), "Soma Grupo");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));

    // Passo do Administrador Sênior: nenhum <input type="email"> ou campo rotulado "e-mail".
    expect(screen.queryByLabelText(/e-mail/i)).not.toBeInTheDocument();
    const inputsEmail = document.querySelectorAll('input[type="email"]');
    expect(inputsEmail.length).toBe(0);
  });

  it("mostra mensagem de sessão expirada e volta ao passo de acesso em erro 401 na conclusão", async () => {
    renderBootstrap();
    await avancarParaConfirmacao("123456", "SessaoExpirada");

    await userEvent.click(screen.getByRole("button", { name: "Concluir configuração inicial" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(/sessão de configuração inicial expirou/i);
    expect(await screen.findByLabelText("E-mail autorizado")).toBeInTheDocument();
  });

  it("trata 403 na conclusão como bootstrap indisponível, nunca como sessão expirada", async () => {
    renderBootstrap();
    await avancarParaConfirmacao("123456", "SemPermissao");

    await userEvent.click(screen.getByRole("button", { name: "Concluir configuração inicial" }));

    // 403 = BootstrapNaoConcluidoRequirement negou porque o Bootstrap já foi concluído (ou o
    // estado está ausente) — a sessão em si autenticou com sucesso. Nunca deve orientar o
    // usuário a "reiniciar o processo" (Bootstrap não reabre), e nunca deve expor texto técnico.
    const alerta = await screen.findByText(/não está mais disponível/i);
    expect(alerta).toBeInTheDocument();
    expect(screen.queryByText(/sessão de configuração inicial expirou/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/reinicie o processo/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/403|forbidden|unauthorized/i)).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Ir para o login" })).toBeInTheDocument();
  });

  it("trata 404 na conclusão como bootstrap indisponível (concluído por outra sessão)", async () => {
    renderBootstrap();
    await avancarParaConfirmacao("123456", "Indisponivel");

    await userEvent.click(screen.getByRole("button", { name: "Concluir configuração inicial" }));

    expect(await screen.findByText(/não está mais disponível/i)).toBeInTheDocument();
  });

  it("exibe mensagem genérica de conflito/concorrência sem detalhe técnico", async () => {
    renderBootstrap();
    await avancarParaConfirmacao("123456", "Conflito");

    await userEvent.click(screen.getByRole("button", { name: "Concluir configuração inicial" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("A configuração inicial já foi concluída.");
  });

  it("exibe mensagem genérica quando o backend responde erro inesperado (5xx)", async () => {
    renderBootstrap();
    await avancarParaConfirmacao("123456", "Instavel");

    await userEvent.click(screen.getByRole("button", { name: "Concluir configuração inicial" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Erro inesperado.");
  });

  it("exibe mensagem genérica quando o backend está indisponível (falha de rede)", async () => {
    fetchMock.mockImplementationOnce(async () => {
      throw new Error("network down");
    });
    renderBootstrap();

    await userEvent.type(await screen.findByLabelText("E-mail autorizado"), "ana@somagrupo.com.br");
    await userEvent.type(screen.getByLabelText("Chave de configuração inicial"), "chave-correta");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Não foi possível iniciar a configuração inicial.");
  });

  it("desabilita o botão de concluir até o checkbox de confirmação ser marcado", async () => {
    renderBootstrap();
    await preencherAcesso();
    await userEvent.type(await screen.findByLabelText("Código de verificação"), "123456");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));
    await userEvent.type(await screen.findByLabelText("Nome da Unidade de Negócio"), "Soma Grupo");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));
    await userEvent.type(await screen.findByLabelText("Nome do Administrador Sênior"), "Ana Souza");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));

    expect(screen.getByRole("button", { name: "Concluir configuração inicial" })).toBeDisabled();
    await userEvent.click(screen.getByRole("checkbox"));
    expect(screen.getByRole("button", { name: "Concluir configuração inicial" })).toBeEnabled();
  });

  it("nunca grava secret, código OTP ou dados do wizard em localStorage/sessionStorage", async () => {
    renderBootstrap();
    await avancarParaConfirmacao();
    await userEvent.click(screen.getByRole("button", { name: "Concluir configuração inicial" }));

    await waitFor(() => expect(screen.getByText(/Configuração inicial concluída/)).toBeInTheDocument());
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });
});
