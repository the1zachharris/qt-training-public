import { expect, type Locator, type Page } from '@playwright/test';

/** A name no other test will use, so kitchen assertions find exactly this test's order. */
export function uniqueName(prefix: string): string {
  return `${prefix} ${Date.now().toString(36)}${Math.floor(Math.random() * 1000)}`;
}

/**
 * Blazor Server pages render first without interactivity; clicks before the circuit connects are
 * lost. The menu shows its "Start Building" button only after the menu loads over the circuit.
 */
export async function openMenu(page: Page): Promise<void> {
  await page.goto('/menu');
  await expect(page.getByRole('button', { name: 'Start Building' })).toBeVisible();
}

export function builderDialog(page: Page): Locator {
  return page.getByRole('dialog');
}

/** Opens Build Your Own and returns the dialog. */
export async function startBuilding(page: Page): Promise<Locator> {
  await page.getByRole('button', { name: 'Start Building' }).click();
  const dialog = builderDialog(page);
  await expect(dialog).toBeVisible();
  return dialog;
}

export async function chooseSize(dialog: Locator, size: 'Small' | 'Large'): Promise<void> {
  await dialog.getByText(new RegExp(`^${size} \\(`)).click();
}

export async function toggleTopping(dialog: Locator, topping: string): Promise<void> {
  await dialog.getByText(`${topping} +$2.00`, { exact: true }).click();
}

/** The live price shown in the builder's footer. */
export function builderPrice(dialog: Locator): Locator {
  return dialog.locator('.mud-dialog-actions .builder-price');
}

/** Builds a pizza in the dialog and adds it to the order. */
export async function addBuiltPizza(
  page: Page,
  size: 'Small' | 'Large',
  toppings: string[],
): Promise<void> {
  const dialog = await startBuilding(page);
  await chooseSize(dialog, size);
  for (const topping of toppings) await toggleTopping(dialog, topping);
  await dialog.getByRole('button', { name: 'Add to Order' }).click();
  await expect(dialog).toBeHidden();
}

/** Goes from the cart through checkout and returns the confirmation page's order number. */
export async function checkout(page: Page, customerName: string): Promise<string> {
  await page.goto('/checkout');
  await page.getByLabel('Name for the order').fill(customerName);
  await page.getByRole('button', { name: /^Place Order/ }).click();
  await expect(page).toHaveURL(/\/orders\/[0-9a-f-]{36}$/);
  const heading = page.getByText(/^Order #\d+$/);
  await expect(heading).toBeVisible();
  return (await heading.textContent())!.replace('Order ', '').trim();
}
