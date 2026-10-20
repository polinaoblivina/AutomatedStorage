import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: '../ServerAutomatedStorage.WebAPI/wwwroot',
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5037',
      '/hubs': { target: 'http://localhost:5037', ws: true },
    },
  },
})
