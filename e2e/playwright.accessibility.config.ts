import os from 'node:os';
import path from 'node:path';
import { defineConfig } from '@playwright/test';
import base from './playwright.config';

// WCAG 2.2 Level A/AA audit. Kept apart from `npm test`: a failure here is an accessibility finding
// to review, not a regression in the acceptance criteria. Run with:
//   npx playwright test -c playwright.accessibility.config.ts
// It runs its own API and UI on ports 5319 / 5376, so it does not clash with `npm test` (5219 / 5276)
// or a copy you already have running (5119 / 5176).
// Set A11Y_API_PORT / A11Y_UI_PORT to run two copies side by side.
export const A11Y_API_URL = `http://localhost:${process.env.A11Y_API_PORT ?? 5319}`;
export const A11Y_UI_URL = `http://localhost:${process.env.A11Y_UI_PORT ?? 5376}`;

process.env.E2E_A11Y_DATA_DIR ??= path.join(os.tmpdir(), `neighborhood-pizza-a11y-${Date.now()}`);

const repoRoot = path.resolve(__dirname, '..');
const devEnv = { ASPNETCORE_ENVIRONMENT: 'Development', DOTNET_ENVIRONMENT: 'Development' };

export default defineConfig({
  ...base,
  testDir: './accessibility',
  outputDir: './test-results-accessibility',
  timeout: 60_000,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report-accessibility' }]],
  use: { ...base.use, baseURL: A11Y_UI_URL, viewport: { width: 1280, height: 800 } },
  webServer: [
    {
      command: `dotnet build QT.PizzaOrdering.slnx -c Release -v q -nologo && dotnet run -c Release --no-build --no-launch-profile --project QT.PizzaOrdering.API --urls ${A11Y_API_URL}`,
      cwd: repoRoot,
      url: `${A11Y_API_URL}/`,
      env: { ...devEnv, DataDirectory: process.env.E2E_A11Y_DATA_DIR },
      timeout: 180_000,
      reuseExistingServer: false,
      stdout: 'ignore',
      stderr: 'pipe',
    },
    {
      command: `dotnet run -c Release --no-build --no-launch-profile --project QT.PizzaOrdering.UI --urls ${A11Y_UI_URL}`,
      cwd: repoRoot,
      url: A11Y_UI_URL,
      env: { ...devEnv, BaseAddresses__PizzaOrderingApi: `${A11Y_API_URL}/` },
      timeout: 120_000,
      reuseExistingServer: false,
      stdout: 'ignore',
      stderr: 'pipe',
    },
  ],
});
