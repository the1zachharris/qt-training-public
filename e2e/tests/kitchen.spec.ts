import { spawn, type ChildProcess } from 'node:child_process';
import path from 'node:path';
import { expect, test, type Page } from '@playwright/test';
import { addBuiltPizza, checkout, openMenu, uniqueName } from './helpers';

async function placeOrder(page: Page, customer: string): Promise<string> {
  await openMenu(page);
  await addBuiltPizza(page, 'Large', ['Pepperoni', 'Onions']); // $18.00
  await addBuiltPizza(page, 'Small', []); // $10.00
  return checkout(page, customer);
}

function kitchenCard(page: Page, customer: string) {
  return page.locator('.kitchen-card').filter({ hasText: customer });
}

test.describe('4. Managing kitchen orders', () => {
  test('AC-3.3/4.1: a placed order shows in the kitchen with its details', async ({ page, browser }) => {
    const customer = uniqueName('Kitchen');
    const orderNumber = await placeOrder(page, customer);

    // Kitchen staff are on a different screen, not the customer's browser session.
    const kitchen = await (await browser.newContext()).newPage();
    await kitchen.goto('/kitchen');
    const card = kitchenCard(kitchen, customer);
    await expect(card).toBeVisible();
    await expect(card).toContainText(orderNumber);
    await expect(card).toContainText('New');
    await expect(card).toContainText('1 × Large (14") · Pepperoni, Onions');
    await expect(card).toContainText('1 × Small (10") · Plain cheese');
    await expect(card).toContainText('Total $28.00');
  });

  test('AC-4.2: a New order can be marked Ready, and the customer sees it', async ({ page, browser }) => {
    const customer = uniqueName('Ready');
    await placeOrder(page, customer);
    const confirmationUrl = page.url();

    const kitchen = await (await browser.newContext()).newPage();
    await kitchen.goto('/kitchen');
    const card = kitchenCard(kitchen, customer);
    await expect(card).toContainText('New');

    await card.getByRole('button', { name: 'Mark Ready' }).click();
    await expect(card).toContainText('Ready');
    await expect(card.getByRole('button', { name: 'Mark Ready' })).toHaveCount(0);
    await expect(card).toHaveClass(/kitchen-card-ready/);

    // Still Ready after a reload: the status was saved, not just changed on screen.
    await kitchen.reload();
    await expect(kitchenCard(kitchen, customer)).toHaveClass(/kitchen-card-ready/);

    await page.goto(confirmationUrl);
    await expect(page.getByText('Your order is ready for pickup.', { exact: false })).toBeVisible();
  });

  test('AC-4.3: orders and statuses survive the API being restarted', async ({ page, browser }) => {
    test.setTimeout(120_000);
    const newCustomer = uniqueName('Restart New');
    const readyCustomer = uniqueName('Restart Ready');
    await placeOrder(page, newCustomer);
    await placeOrder(page, readyCustomer);

    const kitchen = await (await browser.newContext()).newPage();
    await kitchen.goto('/kitchen');
    await kitchenCard(kitchen, readyCustomer).getByRole('button', { name: 'Mark Ready' }).click();
    await expect(kitchenCard(kitchen, readyCustomer)).toHaveClass(/kitchen-card-ready/);

    // A second, freshly started API process on the same database folder stands in for
    // "the application was closed and reopened".
    const port = 5229 + test.info().workerIndex;
    const restarted = await startApi(port);
    try {
      const response = await page.request.get(`http://localhost:${port}/api/v1/kitchen/orders`);
      expect(response.ok()).toBeTruthy();
      const body = await response.json();
      const names = (list: { customerName: string }[]) => list.map(order => order.customerName);
      expect(names(body.new)).toContain(newCustomer);
      expect(names(body.ready)).toContain(readyCustomer);
    } finally {
      restarted.kill();
    }
  });
});

async function startApi(port: number): Promise<ChildProcess> {
  const child = spawn(
    'dotnet',
    ['run', '-c', 'Release', '--no-build', '--no-launch-profile', '--project', 'QT.PizzaOrdering.API', '--urls', `http://localhost:${port}`],
    {
      cwd: path.resolve(__dirname, '..', '..'),
      env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development', DataDirectory: process.env.E2E_DATA_DIR },
      stdio: 'ignore',
    },
  );
  const deadline = Date.now() + 60_000;
  while (Date.now() < deadline) {
    try {
      if ((await fetch(`http://localhost:${port}/`)).ok) return child;
    } catch {
      // Not listening yet.
    }
    await new Promise(resolve => setTimeout(resolve, 500));
  }
  child.kill();
  throw new Error(`Restarted API did not start on port ${port}`);
}
