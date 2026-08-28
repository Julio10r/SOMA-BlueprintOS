import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LoginPage } from "../pages/LoginPage";
import { AuthProvider } from "../context/AuthContext";

function renderLogin() {
  return render(
    <MemoryRouter initialEntries={["/login"]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<div>Área autenticada</div>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe("LoginPage", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === "string" ? input : input.toString();

        if (url === "/auth/me") {
          return new Response(null, { status: 401 });
        }
        if (url === "/auth/otp/request") {
          return new Response(JSON.stringify({ message: "ok" }), { status: 200 });
        }
        if (url === "/auth/otp/verify") {
          const body = JSON.parse(String(init?.body));
          if (body.codigo === "123456") {
            return new Response(
              JSON.stringify({ usuario: { id: "u1", email: body.email, nome: "Ana Souza", unidadeNegocioId: "un1" } }),
              { status: 200 }
            );
          }
          return new Response(JSON.stringify({ code: "otp_invalido", message: "Código inválido ou expirado." }), {
            status: 400
          });
        }

        return new Response(null, { status: 404 });
      })
    );
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it("solicita o OTP e avança para a etapa de código", async () => {
    renderLogin();

    await userEvent.type(await screen.findByLabelText("E-mail corporativo"), "ana@somagrupo.com.br");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));

    expect(await screen.findByLabelText("Código de verificação")).toBeInTheDocument();
  });

  it("exibe erro genérico para código inválido, sem revelar o motivo específico", async () => {
    renderLogin();

    await userEvent.type(await screen.findByLabelText("E-mail corporativo"), "ana@somagrupo.com.br");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));
    await userEvent.type(await screen.findByLabelText("Código de verificação"), "000000");
    await userEvent.click(screen.getByRole("button", { name: "Entrar" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Código inválido ou expirado.");
  });

  it("navega para a área autenticada após validar o código corretamente", async () => {
    renderLogin();

    await userEvent.type(await screen.findByLabelText("E-mail corporativo"), "ana@somagrupo.com.br");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));
    await userEvent.type(await screen.findByLabelText("Código de verificação"), "123456");
    await userEvent.click(screen.getByRole("button", { name: "Entrar" }));

    await waitFor(() => expect(screen.getByText("Área autenticada")).toBeInTheDocument());
  });

  it("nunca grava o código OTP em localStorage ou sessionStorage", async () => {
    renderLogin();

    await userEvent.type(await screen.findByLabelText("E-mail corporativo"), "ana@somagrupo.com.br");
    await userEvent.click(screen.getByRole("button", { name: "Continuar" }));
    await userEvent.type(await screen.findByLabelText("Código de verificação"), "123456");
    await userEvent.click(screen.getByRole("button", { name: "Entrar" }));

    await waitFor(() => expect(screen.getByText("Área autenticada")).toBeInTheDocument());
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });
});
