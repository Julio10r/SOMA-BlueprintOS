import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";
import { shouldBypassFornecedoresProxy } from "./src/core/viteProxyRules";

const backend = "http://127.0.0.1:5262";

export default defineConfig({
  plugins: [react()],
  test: {
    setupFiles: "./src/test/setup.ts"
  },
  server: {
    port: 5173,
    proxy: {
      // "/fornecedores" e simultaneamente prefixo de API (GET/POST base) e rota da SPA
      // (mesma colisao documentada para "/bootstrap" abaixo). Sem o bypass, uma navegacao
      // de pagina (F5/deep-link em /fornecedores) e encaminhada ao backend e retorna o JSON
      // da API em vez do shell React — bypass devolve o controle ao Vite (SPA fallback) para
      // requisicoes de navegacao (Accept: text/html), preservando o proxy para fetch/XHR da app.
      "/fornecedores": {
        target: backend,
        changeOrigin: true,
        bypass: (req) => (shouldBypassFornecedoresProxy(req.method ?? "", req.headers.accept) ? req.url : undefined)
      },
      // O1.5 — API real da Gestão de Perfis (RBAC). Prefixada com /api de proposito:
      // "/administracao" e espaco de rotas da SPA e nunca deve ser encaminhado ao backend.
      "/api": {
        target: backend,
        changeOrigin: true
      },
      "/auth": {
        target: backend,
        changeOrigin: true
      },
      // O1.11 — Seleção de Unidade de Negócio (GET /me/unidades-negocio) e identidade auxiliar.
      // "/me" não é rota da SPA (sem colisão, ao contrário de "/bootstrap"/"/administracao").
      "/me": {
        target: backend,
        changeOrigin: true
      },
      "/dev": {
        target: backend,
        changeOrigin: true
      },

      // Bootstrap API somente.
      // Não usar "/bootstrap" como prefixo geral,
      // pois /bootstrap também é rota da SPA.
      "/bootstrap/estado": {
        target: backend,
        changeOrigin: true
      },
      "/bootstrap/iniciar": {
        target: backend,
        changeOrigin: true
      },
      "/bootstrap/otp": {
        target: backend,
        changeOrigin: true
      },
      "/bootstrap/concluir": {
        target: backend,
        changeOrigin: true
      }
    }
  }
});