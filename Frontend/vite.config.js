import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// The dev server proxies /api to the backend, so the browser only ever
// talks to its own origin (no CORS needed in development).
// VITE_API_TARGET: http://localhost:8080 for docker compose,
//                  http://localhost:5203 for `dotnet run`.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  return {
    plugins: [react()],
    server: {
      proxy: {
        '/api': {
          target: env.VITE_API_TARGET || 'http://localhost:8080',
          changeOrigin: true,
        },
      },
    },
  }
})
