import { defineConfig } from "vitest/config";
import path from 'path'

export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test/setup.ts',
    coverage: {
      provider: "v8",
      reporter: ["text", "lcov", "json-summary", "cobertura"],
      reportsDirectory: "./coverage",
      // Optional: enforce minimums in CI
      thresholds: {
        lines: 70,
        functions: 70,
        branches: 60,
        statements: 70,
      },
    },
  },
});
