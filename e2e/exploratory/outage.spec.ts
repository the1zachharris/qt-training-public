import { spawn, type ChildProcess } from 'node:child_process';
import os from 'node:os';
import path from 'node:path';
import { expect, test } from '@playwright/test';
import { addBuiltPizza } from '../tests/helpers';

// A private API + UI pair, so the API can be stopped without breaking the other tests.
const API_PORT = 5249;
const UI_PORT = 5296;
const UI = `http://localhost:${UI_PORT}`;
const repoRoot = path.resolve(__dirname, '..', '..');
const dataDir = path.join(os.tmpdir(), `neighborhood-pizza-outage-${Date.now()}`);

function run(project: string, port: number, env: Record<string, string>): ChildProcess {
  return spawn('dotnet', ['run', '-c', 'Release', '--no-build', '--no-launch-profile', '--project', project, '--urls', `http://localhost:${port}`], {
    cwd: repoRoot,
    env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development', DOTNET_ENVIRONMENT: 'Development', ...env },
    stdio: 'ignore',
  });
}

async function waitFor(url: string): Promise<void> {
  const deadline = Date.now() + 60_000;
  while (Date.now() < deadline) {
    try { if ((await fetch(url)).ok) return; } catch { /* not up yet */ }
    await new Promise(r => setTimeout(r, 500));
  }
  throw new Error(`${url} did not start`);
}

async function startApi(): Promise<ChildProcess> {
  const api = run('QT.PizzaOrdering.API', API_PORT, { DataDirectory: dataDir });
  await waitFor(`http://localhost:${API_PORT}/`);
  return api;
}

test.describe.configure({ mode: 'serial' });

test.describe('When the API is down', () => {
  let api: ChildProcess | undefined;
  let ui: ChildProcess | undefined;

  test.beforeAll(async () => {
    test.setTimeout(120_000);
    api = await startApi();
    ui = run('QT.PizzaOrdering.UI', UI_PORT, { BaseAddresses__PizzaOrderingApi: `http://localhost:${API_PORT}/`, KitchenConfig__RefreshSeconds: '2' });
    await waitFor(UI);
  });

  test.afterAll(() => {
    api?.kill();
    ui?.kill();
  });

  test('checkout tells the customer the order did not go through', async ({ page }, info) => {
    test.setTimeout(90_000);
    await page.goto(`${UI}/menu`);
    await expect(page.getByRole('button', { name: 'Start Building' })).toBeVisible();
    await addBuiltPizza(page, 'Small', []);
    await page.goto(`${UI}/checkout`);
    await page.getByLabel('Name for the order').fill('Outage Pat');

    api!.kill();
    await new Promise(r => setTimeout(r, 1500));

    await page.getByRole('button', { name: /^Place Order/ }).click();
    const alert = page.locator('.mud-alert-text-error, .mud-alert-filled-error, .mud-alert').filter({ hasText: /order/i }).first();
    await expect(alert).toBeVisible({ timeout: 30_000 });
    info.annotations.push({ type: 'observation', description: `checkout message: "${(await alert.textContent())?.trim()}"` });
    await page.screenshot({ path: info.outputPath('checkout-api-down.png'), fullPage: true });
    // The cart must not be lost.
    await page.goto(`${UI}/cart`);
    await expect(page.locator('tbody tr')).toHaveCount(1);
  });

  test('menu shows an error instead of a blank page', async ({ page }, info) => {
    test.setTimeout(60_000);
    await page.goto(`${UI}/menu`);
    const alert = page.getByText('The menu could not be loaded.', { exact: false });
    await expect(alert).toBeVisible({ timeout: 30_000 });
    info.annotations.push({ type: 'observation', description: `menu message: "${(await alert.textContent())?.trim()}"` });
    // Customers get plain wording; the host, port, and socket error belong in the log.
    await expect(alert).not.toContainText(`${API_PORT}`);
    await expect(alert).not.toContainText(/localhost|refused|socket/i);
    await page.screenshot({ path: info.outputPath('menu-api-down.png'), fullPage: true });
  });

  test('kitchen shows an error, then recovers by itself when the API comes back', async ({ page }, info) => {
    test.setTimeout(120_000);
    await page.goto(`${UI}/kitchen`);
    await expect(page.getByText('Orders could not be loaded.', { exact: false })).toBeVisible({ timeout: 30_000 });
    await page.screenshot({ path: info.outputPath('kitchen-api-down.png'), fullPage: true });

    api = await startApi();
    // The order from the first test was never sent, so the board should be empty but working.
    await expect(page.getByText('Orders could not be loaded.', { exact: false })).toBeHidden({ timeout: 20_000 });
    await expect(page.getByText('No orders waiting.')).toBeVisible();
  });
});
