import { defineConfig } from '@playwright/test'

// Uses the system-installed Google Chrome (channel: 'chrome') so there's no ~130MB browser
// download. The Vite dev server is started automatically; the .NET API must already be running
// on :5200 in Development (see e2e/README notes). Run with:  npm run e2e
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  fullyParallel: false,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:5173',
    channel: 'chrome',
    headless: true,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: true,
    timeout: 60_000,
  },
})
