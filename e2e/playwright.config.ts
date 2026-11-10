import os from 'node:os';
import path from 'node:path';
import { defineConfig } from '@playwright/test';

// The suite runs its own API and UI on ports that do not clash with a copy you already have
// running (5119 / 5176), against a throwaway database folder, so it never touches your real orders.
export const API_PORT = 5219;
export const UI_PORT = 5276;
export const API_URL = `http://localhost:${API_PORT}`;
export const UI_URL = `http://localhost:${UI_PORT}`;

// Set once in the main process; workers inherit it, so every process agrees on the folder.
process.env.E2E_DATA_DIR ??= path.join(os.tmpdir(), `neighborhood-pizza-e2e-${Date.now()}`);

const repoRoot = path.resolve(__dirname, '..');
const devEnv = { ASPNETCORE_ENVIRONMENT: 'Development', DOTNET_ENVIRONMENT: 'Development' };

export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  workers: 2,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: UI_URL,
    // Uses the Edge already installed on Windows, so no browser download is needed.
    // Set PW_CHANNEL=chromium (after `npx playwright install chromium`) to use Playwright's own build.
    channel: process.env.PW_CHANNEL ?? 'msedge',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  webServer: [
    {
      // Build the whole solution once, then run without rebuilding so the two servers do not
      // fight over the shared Core project's build output. Release keeps the build out of
      // bin/Debug, which is locked while you have the app running from Visual Studio.
      command: `dotnet build QT.PizzaOrdering.slnx -c Release -v q -nologo && dotnet run -c Release --no-build --no-launch-profile --project QT.PizzaOrdering.API --urls ${API_URL}`,
      cwd: repoRoot,
      url: `${API_URL}/`,
      env: { ...devEnv, DataDirectory: process.env.E2E_DATA_DIR },
      timeout: 180_000,
      reuseExistingServer: false,
      stdout: 'ignore',
      stderr: 'pipe',
    },
    {
      command: `dotnet run -c Release --no-build --no-launch-profile --project QT.PizzaOrdering.UI --urls ${UI_URL}`,
      cwd: repoRoot,
      url: UI_URL,
      env: { ...devEnv, BaseAddresses__PizzaOrderingApi: `${API_URL}/` },
      timeout: 120_000,
      reuseExistingServer: false,
      stdout: 'ignore',
      stderr: 'pipe',
    },
  ],
});
