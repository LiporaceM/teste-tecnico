import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Em desenvolvimento, /api é encaminhado para a API (porta 5000 do docker-compose ou VITE_API_URL).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: { '/api': { target: process.env.VITE_API_URL ?? 'http://localhost:5000', changeOrigin: true } },
  },
});
