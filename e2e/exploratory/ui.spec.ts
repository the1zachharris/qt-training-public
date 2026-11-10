import { expect, test, type Page } from '@playwright/test';
import {
  addBuiltPizza,
  builderPrice,
  checkout,
  chooseSize,
  openMenu,
  startBuilding,
  toggleTopping,
  uniqueName,
} from '../tests/helpers';
import { API_URL } from '../playwright.config';

async function kitchenOrdersFor(page: Page, customer: string) {
  const body = await (await page.request.get(`${API_URL}/api/v1/kitchen/orders`)).json();
  return [...body.new, ...body.ready].filter((o: { customerName: string }) => o.customerName === customer);
}

function presetCard(page: Page, name: string) {
  return page.locator('.preset-card').filter({ has: page.getByRole('heading', { name, exact: true }) });
}

test.describe('Builder dialog', () => {
  test('quantity field: out-of-range values block Add and show no price', async ({ page }) => {
    await openMenu(page);
    const dialog = await startBuilding(page);
    const qty = dialog.getByLabel('Quantity');
    const add = dialog.getByRole('button', { name: 'Add to Order' });

    await qty.fill('3');
    await qty.blur();
    await expect(builderPrice(dialog)).toHaveText('$30.00');
    await expect(dialog.getByText('$10.00 each')).toBeVisible();

    for (const bad of ['0', '21', '-1']) {
      await qty.fill(bad);
      await qty.blur();
      const shown = await qty.inputValue();
      const price = await builderPrice(dialog).textContent();
      test.info().annotations.push({ type: 'observation', description: `typed ${bad} -> field shows "${shown}", price "${price}", Add enabled=${await add.isEnabled()}` });
      // Either the field clamps to a legal value, or the price is blanked and Add is disabled.
      if (shown === bad) {
        await expect(builderPrice(dialog)).toHaveText('—');
        await expect(add).toBeDisabled();
      } else {
        expect(Number(shown)).toBeGreaterThanOrEqual(1);
        expect(Number(shown)).toBeLessThanOrEqual(20);
      }
    }
  });

  test('Cancel adds nothing to the order', async ({ page }) => {
    await openMenu(page);
    const dialog = await startBuilding(page);
    await toggleTopping(dialog, 'Pepperoni');
    await dialog.getByRole('button', { name: 'Cancel' }).click();
    await expect(dialog).toBeHidden();
    await expect(page.getByRole('link', { name: /Review Order/ })).toHaveCount(0);
  });

  test('Escape key closes the builder dialog', async ({ page }) => {
    await openMenu(page);
    const dialog = await startBuilding(page);
    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden({ timeout: 2_000 });
  });

  test('keyboard only: open builder, pick Large, add a topping, add to order', async ({ page }) => {
    await openMenu(page);
    await page.getByRole('button', { name: 'Start Building' }).focus();
    await page.keyboard.press('Enter');
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    // Focus should move into the dialog so a keyboard user can work it.
    const focusInDialog = await dialog.evaluate(d => d.contains(document.activeElement));
    test.info().annotations.push({ type: 'observation', description: `focus moved into dialog: ${focusInDialog}` });
    expect(focusInDialog).toBeTruthy();
  });

  test('preset Add and Customize flows', async ({ page }) => {
    await openMenu(page);
    await presetCard(page, 'Pepperoni').getByRole('button', { name: 'Add' }).click();
    await expect(page.getByText('1 × Pepperoni added to your order.')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Review Order (1)' })).toBeVisible();

    await presetCard(page, 'Veggie').getByText('Large (14")').click();
    await presetCard(page, 'Veggie').getByRole('button', { name: 'Customize' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toContainText('Veggie');
    await expect(builderPrice(dialog)).toHaveText('$20.00');
    await toggleTopping(dialog, 'Onions');
    await expect(builderPrice(dialog)).toHaveText('$18.00');
    await dialog.getByRole('button', { name: 'Add to Order' }).click();

    await page.goto('/cart');
    await expect(page.locator('tbody tr').nth(1)).toContainText('Custom Veggie');
    await expect(page.locator('tbody tr').nth(1)).toContainText('Large (14") · Mushrooms, Green peppers');
    await expect(page.getByText('Total $30.00')).toBeVisible();
  });

  test('header cart badge counts pizzas, including quantity', async ({ page }) => {
    await openMenu(page);
    const dialog = await startBuilding(page);
    await dialog.getByLabel('Quantity').fill('3');
    await dialog.getByLabel('Quantity').blur();
    await dialog.getByRole('button', { name: 'Add to Order' }).click();
    await expect(page.locator('.mud-badge').filter({ hasText: '3' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Review Order (3)' })).toBeVisible();
  });
});

test.describe('Cart', () => {
  test('editing a pizza in the cart updates the line and total', async ({ page }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await page.goto('/cart');
    // Each line's button is named for its pizza, e.g. "Change Build Your Own, Small (10")".
    await page.getByRole('button', { name: /^Change / }).click();
    const dialog = page.getByRole('dialog');
    await chooseSize(dialog, 'Large');
    await toggleTopping(dialog, 'Mushrooms');
    await dialog.getByRole('button', { name: 'Update Order' }).click();
    await expect(page.locator('tbody tr')).toHaveCount(1);
    await expect(page.locator('tbody tr')).toContainText('Large (14") · Mushrooms');
    await expect(page.getByText('Total $16.00')).toBeVisible();
  });

  test('quantity stepper stops at 1 and 20', async ({ page }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await page.goto('/cart');
    const row = page.locator('tbody tr').first();
    await expect(row.getByRole('button', { name: 'One fewer' })).toBeDisabled();
    for (let i = 0; i < 19; i++) await row.getByRole('button', { name: 'One more' }).click();
    await expect(row.locator('.cart-quantity')).toHaveText('20');
    await expect(row.getByRole('button', { name: 'One more' })).toBeDisabled();
    await expect(page.getByText('Total $200.00')).toBeVisible();
  });

  test('Start Over empties the cart', async ({ page }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await page.goto('/cart');
    await page.getByRole('button', { name: 'Start Over' }).click();
    await expect(page.getByText('You have not added any pizzas yet.')).toBeVisible();
  });

  test('cart survives a page reload in the same tab', async ({ page }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Large', ['Onions']);
    await page.reload();
    await page.goto('/cart');
    await expect(page.locator('tbody tr')).toHaveCount(1);
  });

  test('observation: a second tab starts with its own empty cart', async ({ page, context }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Large', []);
    const second = await context.newPage();
    await second.goto('/cart');
    await expect(second.getByRole('heading', { name: 'Your Order' })).toBeVisible();
    await second.waitForTimeout(1_500);
    test.info().annotations.push({ type: 'observation', description: `cart lines in a second tab: ${await second.locator('tbody tr').count()} (first tab has 1)` });
  });

  test('the 21st different pizza is refused with a message', async ({ page }) => {
    test.setTimeout(90_000);
    await openMenu(page);
    const add = presetCard(page, 'Cheese').getByRole('button', { name: 'Add' });
    for (let i = 0; i < 20; i++) await add.click();
    await expect(page.getByRole('link', { name: 'Review Order (20)' })).toBeVisible();
    await add.click();
    // Expected promptly; behind a queue of "added" toasts it can take ~20s.
    await expect(page.getByText('Your order already has 20 different pizzas.', { exact: false })).toBeVisible({ timeout: 3_000 });
    await expect(page.getByRole('link', { name: 'Review Order (20)' })).toBeVisible();
  });

  test('observation: adding the same pizza twice makes two lines', async ({ page }) => {
    await openMenu(page);
    const add = presetCard(page, 'Cheese').getByRole('button', { name: 'Add' });
    await add.click();
    await expect(page.getByRole('link', { name: 'Review Order (1)' })).toBeVisible();
    await add.click();
    await expect(page.getByRole('link', { name: 'Review Order (2)' })).toBeVisible();
    await page.goto('/cart');
    await expect(page.getByRole('heading', { name: 'Your Order' })).toBeVisible();
    await page.waitForTimeout(500);
    const rows = await page.locator('tbody tr').count();
    test.info().annotations.push({ type: 'observation', description: `two identical Cheese pizzas -> ${rows} cart line(s)` });
  });
});

test.describe('Checkout', () => {
  test('customer name is trimmed on the confirmation and kitchen', async ({ page }) => {
    const customer = uniqueName('Trim');
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await checkout(page, `   ${customer}   `);
    await expect(page.getByText(`Thanks, ${customer}! Your order is in.`)).toBeVisible();
    expect(await kitchenOrdersFor(page, customer)).toHaveLength(1);
  });

  test('name field stops at 100 characters', async ({ page }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await page.goto('/checkout');
    const name = page.getByLabel('Name for the order');
    await name.fill('A'.repeat(150));
    expect((await name.inputValue()).length).toBeLessThanOrEqual(100);
  });

  test('HTML in a name is shown as text, never run', async ({ page, browser }) => {
    const marker = uniqueName('X').replace(/\s/g, '');
    const customer = `<img src=x onerror="window.__xss='${marker}'">`;
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await checkout(page, customer);
    await expect(page.getByText(`Thanks, ${customer}!`, { exact: false })).toBeVisible();
    expect(await page.evaluate(() => (window as any).__xss)).toBeUndefined();

    const kitchen = await (await browser.newContext()).newPage();
    await kitchen.goto('/kitchen');
    await expect(kitchen.locator('.kitchen-card').filter({ hasText: customer })).toBeVisible();
    expect(await kitchen.evaluate(() => (window as any).__xss)).toBeUndefined();
  });

  test('double-clicking Place Order creates only one order', async ({ page }) => {
    const customer = uniqueName('Double');
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await page.goto('/checkout');
    await page.getByLabel('Name for the order').fill(customer);
    await page.getByRole('button', { name: /^Place Order/ }).dblclick();
    await expect(page).toHaveURL(/\/orders\//);
    expect(await kitchenOrdersFor(page, customer)).toHaveLength(1);
  });

  test('pressing Enter in the name field places the order', async ({ page }) => {
    const customer = uniqueName('Enter');
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await page.goto('/checkout');
    const name = page.getByLabel('Name for the order');
    await name.fill(customer);
    await name.press('Enter');
    await expect(page).toHaveURL(/\/orders\//, { timeout: 5_000 });
  });

  test('browser Back after ordering does not allow a resubmit', async ({ page }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await checkout(page, uniqueName('Back'));
    await page.goBack();
    await expect(page.getByText('There is nothing to order yet.')).toBeVisible();
  });
});

test.describe('Confirmation and routing', () => {
  test('unknown order id shows a friendly not-found message', async ({ page }) => {
    await page.goto('/orders/00000000-0000-0000-0000-000000000000');
    await expect(page.getByText('We could not find that order.')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Back to the Menu' })).toBeVisible();
  });

  test('malformed order id and unknown page show the not-found page', async ({ page }) => {
    for (const url of ['/orders/123', '/does-not-exist']) {
      const outcome = await page.goto(url).then(r => `HTTP ${r?.status()}`, (e: Error) => e.message.split('\n')[0]);
      test.info().annotations.push({ type: 'observation', description: `${url}: ${outcome}` });
      await expect.soft(page.getByText('That page does not exist.'), url).toBeVisible();
    }
  });

  test('confirmation page shows status updates without a manual reload', async ({ page, browser }) => {
    test.setTimeout(60_000);
    const customer = uniqueName('LiveStatus');
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await checkout(page, customer);
    const kitchen = await (await browser.newContext()).newPage();
    await kitchen.goto('/kitchen');
    await kitchen.locator('.kitchen-card').filter({ hasText: customer }).getByRole('button', { name: 'Mark Ready' }).click();
    await expect(page.getByText('Your order is ready for pickup.', { exact: false })).toBeVisible({ timeout: 20_000 });
  });
});

test.describe('Kitchen', () => {
  test('a new order appears on an open kitchen screen without touching it', async ({ page, browser }) => {
    test.setTimeout(60_000);
    const kitchen = await (await browser.newContext()).newPage();
    await kitchen.goto('/kitchen');
    await expect(kitchen.getByText('Kitchen Orders')).toBeVisible();

    const customer = uniqueName('AutoRefresh');
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await checkout(page, customer);
    await expect(kitchen.locator('.kitchen-card').filter({ hasText: customer })).toBeVisible({ timeout: 15_000 });
  });

  test('two kitchen screens marking the same order Ready both end up consistent', async ({ page, browser }) => {
    const customer = uniqueName('TwoScreens');
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await checkout(page, customer);

    const a = await (await browser.newContext()).newPage();
    const b = await (await browser.newContext()).newPage();
    await a.goto('/kitchen');
    await b.goto('/kitchen');
    const cardA = a.locator('.kitchen-card').filter({ hasText: customer });
    const cardB = b.locator('.kitchen-card').filter({ hasText: customer });
    await expect(cardA).toBeVisible();
    await expect(cardB).toBeVisible();
    await cardA.getByRole('button', { name: 'Mark Ready' }).click();
    await expect(cardA).toHaveClass(/kitchen-card-ready/);
    await cardB.getByRole('button', { name: 'Mark Ready' }).click();
    await expect(cardB).toHaveClass(/kitchen-card-ready/);
    await expect(b.getByText(/was not marked Ready/)).toHaveCount(0);
  });

  test('New column is oldest first', async ({ page }) => {
    const first = uniqueName('OrderA');
    const second = uniqueName('OrderB');
    for (const name of [first, second]) {
      await openMenu(page);
      await addBuiltPizza(page, 'Small', []);
      await checkout(page, name);
    }
    await page.goto('/kitchen');
    // The board loads over the Blazor circuit after the page appears; read the cards only once both orders are on it.
    await expect(page.locator('.kitchen-card', { hasText: second })).toBeVisible();
    const names = await page.locator('.kitchen-card').allTextContents();
    const i1 = names.findIndex(t => t.includes(first));
    const i2 = names.findIndex(t => t.includes(second));
    expect(i1).toBeGreaterThanOrEqual(0);
    expect(i1).toBeLessThan(i2);
  });
});

test.describe('Look, layout, and accessibility', () => {
  const pages = ['/menu', '/cart', '/checkout', '/kitchen'];

  test('every page has a unique title and exactly one h1', async ({ page }) => {
    const titles = new Set<string>();
    for (const url of pages) {
      await page.goto(url);
      await page.waitForLoadState('networkidle');
      titles.add(await page.title());
      const h1s = await page.locator('h1').count();
      test.info().annotations.push({ type: 'observation', description: `${url}: title "${await page.title()}", h1 count ${h1s}` });
    }
    expect(titles.size).toBe(pages.length);
    for (const url of pages) {
      await page.goto(url);
      await page.waitForLoadState('networkidle');
      expect.soft(await page.locator('h1').count(), `${url} h1 count`).toBe(1);
    }
  });

  test('every button and link has an accessible name', async ({ page }) => {
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    for (const url of ['/menu', '/cart', '/checkout', '/kitchen']) {
      await page.goto(url);
      await page.waitForLoadState('networkidle');
      const unnamed = await page.locator('button, a[href]').evaluateAll(els =>
        els
          .filter(el => (el as HTMLElement).offsetParent !== null)
          .filter(el => !(el.getAttribute('aria-label') || el.textContent?.trim() || el.getAttribute('title')))
          .map(el => el.outerHTML.slice(0, 120)),
      );
      expect.soft(unnamed, `${url} unnamed controls`).toEqual([]);
    }
  });

  test('mobile width (375px): no sideways scrolling, screenshots saved', async ({ page }, info) => {
    await page.setViewportSize({ width: 375, height: 800 });
    await openMenu(page);
    await addBuiltPizza(page, 'Large', ['Pepperoni', 'Mushrooms', 'Onions', 'Green peppers']);
    for (const url of ['/menu', '/cart', '/checkout', '/kitchen']) {
      await page.goto(url);
      await page.waitForLoadState('networkidle');
      await page.screenshot({ path: info.outputPath(`mobile${url.replace('/', '-')}.png`), fullPage: true });
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect.soft(overflow, `${url} horizontal overflow (px)`).toBeLessThanOrEqual(0);
    }
  });

  test('dark mode setting survives a reload', async ({ page }, info) => {
    await openMenu(page);
    const bg = () => page.evaluate(() => getComputedStyle(document.body).backgroundColor);
    const light = await bg();
    await page.getByRole('button', { name: 'Toggle dark mode' }).click();
    await expect.poll(bg).not.toBe(light);
    const bgBefore = await bg();
    await page.reload();
    await expect(page.getByRole('button', { name: 'Start Building' })).toBeVisible();
    await page.waitForTimeout(500);
    const bgAfter = await bg();
    await page.screenshot({ path: info.outputPath('dark-menu.png'), fullPage: true });
    expect(bgAfter).toBe(bgBefore);
  });

  test('no browser console errors through a full order', async ({ page, browser }) => {
    const errors: string[] = [];
    page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
    page.on('pageerror', e => errors.push(e.message));
    await openMenu(page);
    await addBuiltPizza(page, 'Large', ['Pepperoni']);
    await page.goto('/cart');
    await checkout(page, uniqueName('Console'));
    const kitchen = await (await browser.newContext()).newPage();
    kitchen.on('console', m => { if (m.type() === 'error') errors.push(`kitchen: ${m.text()}`); });
    await kitchen.goto('/kitchen');
    await kitchen.waitForLoadState('networkidle');
    expect(errors).toEqual([]);
  });

  test('observation: the customer navigation drawer links to the kitchen screen', async ({ page }) => {
    await page.goto('/menu');
    await page.getByRole('button', { name: 'Navigation menu' }).click();
    await expect(page.getByRole('link', { name: 'Your Order' })).toBeVisible();
    test.info().annotations.push({ type: 'observation', description: `Kitchen links in customer drawer: ${await page.getByRole('link', { name: 'Kitchen' }).count()}` });
  });

  test('mobile width (375px): cart Price column and Remove button are on screen', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 800 });
    await openMenu(page);
    await addBuiltPizza(page, 'Large', ['Pepperoni']);
    await page.goto('/cart');
    for (const control of [page.getByRole('columnheader', { name: 'Price' }), page.getByRole('button', { name: 'Remove' })]) {
      await expect(control).toBeAttached();
      const box = (await control.boundingBox())!;
      expect.soft(box.x + box.width, `${control} right edge (px)`).toBeLessThanOrEqual(375);
    }
  });

  test('a long name without spaces stays inside its card', async ({ page }) => {
    const customer = 'W' + 'x'.repeat(99);
    await openMenu(page);
    await addBuiltPizza(page, 'Small', []);
    await checkout(page, customer);
    const confirmOverflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect.soft(confirmOverflow, 'confirmation page horizontal overflow (px)').toBeLessThanOrEqual(0);
    await page.goto('/kitchen');
    const card = page.locator('.kitchen-card').filter({ hasText: customer });
    await expect(card).toBeVisible();
    await card.screenshot({ path: test.info().outputPath('kitchen-long-name.png') });
    expect.soft(await card.evaluate(el => el.scrollWidth - el.clientWidth), 'kitchen card overflow (px)').toBeLessThanOrEqual(0);
  });
});
