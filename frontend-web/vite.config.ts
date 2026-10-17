import { fileURLToPath, URL } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

const strings = fileURLToPath(new URL("../backend/1.Platform/Localization/Strings", import.meta.url));

export default defineConfig({
  plugins: [react({ babel: { plugins: [["babel-plugin-react-compiler", {}]] } }), tailwindcss()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
      "@strings": strings,
    },
  },
  server: {
    port: 5173,
    fs: { allow: [".", strings] },
    proxy: { "/api": { target: "http://localhost:5080", changeOrigin: true }, "/hubs": { target: "http://localhost:5080", changeOrigin: true, ws: true } },
  },
  build: {
    target: "es2022",
    rollupOptions: {
      output: {
        manualChunks: {
          react: ["react", "react-dom"],
          tanstack: ["@tanstack/react-query", "@tanstack/react-router", "@tanstack/react-table", "@tanstack/react-virtual"],
          charts: ["echarts"],
          motion: ["motion"],
        },
      },
    },
  },
});
