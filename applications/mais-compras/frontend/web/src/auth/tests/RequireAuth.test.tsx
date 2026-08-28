import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "../context/AuthContext";
import { RequireAuth } from "../components/RequireAuth";

function renderApp(authenticated: boolean) {
  vi.stubGlobal(
    "fetch",
    vi.fn(async () =>
      authenticated
        ? new Response(JSON.stringify({ usuario: { id: "u1", email: "ana@somagrupo.com.br", nome: "Ana", unidadeNegocioId: "un1" } }), { status: 200 })
        : new Response(null, { status: 401 })
    )
  );

  return render(
    <MemoryRouter initialEntries={["/"]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<div>Página de Login</div>} />
          <Route
            path="/"
            element={
              <RequireAuth>
                <div>Conteúdo protegido</div>
              </RequireAuth>
            }
          />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe("RequireAuth", () => {
  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it("redireciona para o Login quando não há sessão", async () => {
    renderApp(false);
    expect(await screen.findByText("Página de Login")).toBeInTheDocument();
  });

  it("renderiza o conteúdo protegido quando há sessão válida", async () => {
    renderApp(true);
    expect(await screen.findByText("Conteúdo protegido")).toBeInTheDocument();
  });
});
