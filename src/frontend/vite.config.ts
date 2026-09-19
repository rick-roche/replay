import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import path from 'path'
import packageJson from '../../package.json'

const buildIdentifier = process.env.GIT_SHA?.slice(0, 7) || 'development'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  define: {
    __APP_VERSION__: JSON.stringify(`${packageJson.version}+${buildIdentifier}`),
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
  },
  server: {
    proxy: {
      // Proxy API calls to the app service
      '/api': {
        target: process.env.SERVER_HTTPS || process.env.SERVER_HTTP,
        changeOrigin: true,
        // Preserve cookies through proxy: don't rewrite cookie domain/path
        cookieDomainRewrite: '',
        // Forward credentials-enabled requests properly
        ws: true
      }
    }
  }
})
