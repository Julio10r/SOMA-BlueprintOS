import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";
import { viteSingleFile } from "vite-plugin-singlefile";

/**
 * O plugin vite-plugin-singlefile so e ativado no modo "build" (npm run build).
 * Ele inlina JS/CSS gerados em um unico index.html, formato exigido para
 * publicacao via webhook do n8n (que serve HTML como string unica, sem
 * suporte a servir uma pasta dist/ com multiplos assets versionados).
 * Nao afeta "npm run dev" nem "npm run test".
 */
export default defineConfig(({ command }) => ({
  plugins: [react(), ...(command === "build" ? [viteSingleFile()] : [])],
  test: {
    setupFiles: "./src/test/setup.ts"
  },
  server: {
    port: 5173,
    proxy: {
      "/fornecedores": "http://127.0.0.1:5188"
    }
  },
  build: {
    target: "esnext",
    assetsInlineLimit: 100000000,
    cssCodeSplit: false,
    chunkSizeWarningLimit: 100000000,
    modulePreload: { polyfill: false }
  }
}));
