import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      // Forward API calls to the .NET backend during local development so the
      // frontend never needs CORS or an absolute API URL.
      '/api': {
        target: 'http://localhost:5054',
        changeOrigin: true,
      },
    },
  },
})
