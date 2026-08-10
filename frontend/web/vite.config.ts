import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

const backend = "http://127.0.0.1:5262";

export default defineConfig({
  plugins: [react()],
  test: {
    setupFiles: "./src/test/setup.ts"
  },
  server: {
    port: 5173,
    proxy: {
      "/fornecedores": backend,
      "/auth": {
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