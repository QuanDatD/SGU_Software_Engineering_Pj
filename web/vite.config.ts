import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
	allowedHosts: [
        "positive-zealand-fish-pour.trycloudflare.com",
		],
    proxy: {
      "/api": {
        target: "http://localhost:5230",
        changeOrigin: true,
      },
      "/storage": {
        target: "http://localhost:5230",
        changeOrigin: true,
      },
    },
  },
});