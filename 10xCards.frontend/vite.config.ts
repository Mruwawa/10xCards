import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Helper to build proxy target config with disabled TLS verification (self-signed dev cert)
const apiTarget = {
  target: 'https://localhost:7268',
  changeOrigin: true,
  secure: false, // allow self-signed
};

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/auth': apiTarget,
      '/flashcards': apiTarget,
      '/study': apiTarget,
      '/stats': apiTarget,
      '/account': apiTarget,
    }
  }
});
