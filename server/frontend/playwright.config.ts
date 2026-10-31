import { defineConfig, devices } from '@playwright/test'
import { e2ePort, serverEnvironment } from './e2e/support/env'

const baseURL = `http://localhost:${e2ePort}`
const viewport = { width: 1440, height: 900 }

export default defineConfig({
  testDir: './e2e/tests',
  globalSetup: './e2e/support/global-setup.ts',
  globalTeardown: './e2e/support/global-teardown.ts',
  outputDir: './e2e/test-results',
  fullyParallel: true,
  workers: 4,
  retries: 0,
  timeout: 30_000,
  expect: { timeout: 8_000 },
  reporter: [['list'], ['html', { outputFolder: 'e2e/report', open: 'never' }]],
  use: {
    baseURL,
    locale: 'ru-RU',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'], viewport } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'], viewport } },
    { name: 'edge', use: { ...devices['Desktop Edge'], channel: 'msedge', viewport } },
    { name: 'mobile', use: { ...devices['Pixel 7'] } },
  ],
  webServer: {
    command: `dotnet run --no-launch-profile --project ../ServerAutomatedStorage.WebAPI --urls ${baseURL}`,
    url: `${baseURL}/health`,
    reuseExistingServer: false,
    timeout: 180_000,
    env: serverEnvironment(),
  },
})
