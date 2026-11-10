import { expect, test } from '@playwright/test';
import {
  addBuiltPizza,
  builderPrice,
  checkout,
  chooseSize,
  openMenu,
  startBuilding,
  toggleTopping,
  uniqueName,
} from './helpers';

test.describe('1. Ordering a pizza', () => {
  test('AC-1.1/1.2/1.3: price shows before adding and follows size and toppings', async ({ page }) => {
    await openMenu(page);
    const dialog = await startBuilding(page);

    await chooseSize(dialog, 'Small');
    await expect(builderPrice(dialog)).toHaveText('$10.00');

    await chooseSize(dialog, 'Large');
    await expect(builderPrice(dialog)).toHaveText('$14.00');

    await toggleTopping(dialog, 'Pepperoni');
    await expect(builderPrice(dialog)).toHaveText('$16.00');

    await toggleTopping(dialog, 'Mushrooms');
    await toggleTopping(dialog, 'Onions');
    await toggleTopping(dialog, 'Green peppers');
    await expect(builderPrice(dialog)).toHaveText('$22.00');

    // Removing a topping takes its $2.00 back off.
    await toggleTopping(dialog, 'Onions');
    await expect(builderPrice(dialog)).toHaveText('$20.00');

    await chooseSize(dialog, 'Small');
    await expect(builderPrice(dialog)).toHaveText('$16.00');
  });

  test('menu lists every size and topping with its price', async ({ page }) => {
    await openMenu(page);
    await expect(page.getByText('Small (10") $10.00 · Large (14") $14.00')).toBeVisible();
    await expect(page.getByText('Pepperoni, Mushrooms, Onions, Green peppers +$2.00 each')).toBeVisible();
  });

  test('a preset card shows its price for the chosen size', async ({ page }) => {
    await openMenu(page);
    const works = page.locator('.preset-card').filter({ hasText: 'The Works' });
    await expect(works.locator('h6').last()).toHaveText('$18.00');
    await works.getByText('Large (14")').click();
    await expect(works.locator('h6').last()).toHaveText('$22.00');
  });
});

test.describe('2. Reviewing an order', () => {
  test('AC-2.1/2.2: several different pizzas show with details, prices, and total', async ({ page }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await addBuiltPizza(page, 'Large', ['Pepperoni', 'Mushrooms']);

    // MudButton with an Href renders as a link.
    await page.getByRole('link', { name: 'Review Order (2)' }).click();
    await expect(page).toHaveURL(/\/cart$/);

    const rows = page.locator('tbody tr');
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(0)).toContainText('Small (10") · Plain cheese');
    await expect(rows.nth(0)).toContainText('$10.00');
    await expect(rows.nth(1)).toContainText('Large (14") · Pepperoni, Mushrooms');
    await expect(rows.nth(1)).toContainText('$18.00');
    await expect(page.getByText('Total $28.00')).toBeVisible();
  });

  test('AC-2.3: changing quantity and removing pizzas updates the total', async ({ page }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Small', ['Onions']); // $12.00
    await addBuiltPizza(page, 'Large', []); // $14.00
    await page.goto('/cart');
    await expect(page.getByText('Total $26.00')).toBeVisible();

    const small = page.locator('tbody tr').filter({ hasText: 'Small (10")' });
    await small.getByRole('button', { name: 'One more' }).click();
    await expect(small.locator('.cart-quantity')).toHaveText('2');
    await expect(page.getByText('Total $38.00')).toBeVisible();

    await small.getByRole('button', { name: 'One fewer' }).click();
    await expect(page.getByText('Total $26.00')).toBeVisible();

    await page.locator('tbody tr').filter({ hasText: 'Large (14")' }).getByRole('button', { name: 'Remove' }).click();
    await expect(page.locator('tbody tr')).toHaveCount(1);
    await expect(page.getByText('Total $12.00')).toBeVisible();

    await small.getByRole('button', { name: 'Remove' }).click();
    await expect(page.getByText('You have not added any pizzas yet.')).toBeVisible();
  });
});

test.describe('3. Placing an order', () => {
  test('AC-3.1: cannot submit an empty order', async ({ page }) => {
    await page.goto('/checkout');
    await expect(page.getByText('There is nothing to order yet.')).toBeVisible();
    await expect(page.getByRole('button', { name: /^Place Order/ })).toHaveCount(0);
  });

  test('AC-3.1: cannot submit without a name', async ({ page }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await page.goto('/checkout');

    const placeOrder = page.getByRole('button', { name: /^Place Order/ });
    await expect(placeOrder).toBeDisabled();

    const name = page.getByLabel('Name for the order');
    await name.fill('   ');
    await expect(placeOrder).toBeDisabled();

    await name.fill('Pat');
    await expect(placeOrder).toBeEnabled();
  });

  test('AC-3.2: confirmation shows order number, name, items, and total', async ({ page }) => {
    const customer = uniqueName('Confirm');
    await openMenu(page);
    await addBuiltPizza(page, 'Large', ['Green peppers']); // $16.00
    await addBuiltPizza(page, 'Small', []); // $10.00

    const orderNumber = await checkout(page, customer);

    expect(orderNumber).toMatch(/^#\d+$/);
    await expect(page.getByText(`Thanks, ${customer}! Your order is in.`)).toBeVisible();
    await expect(page.getByText('Large (14") · Green peppers')).toBeVisible();
    await expect(page.getByText('Small (10") · Plain cheese')).toBeVisible();
    await expect(page.locator('.money').last()).toHaveText('$26.00');
    await expect(page.getByText('Pay at the counter', { exact: false })).toBeVisible();

    // The cart is emptied once the order is placed.
    await page.goto('/cart');
    await expect(page.getByText('You have not added any pizzas yet.')).toBeVisible();
  });
});
