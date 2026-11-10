import { expect, test, type APIRequestContext, type Page } from '@playwright/test';
import { addPreset, describeFocus, openMenu, record, scan, seedOrder, setDarkMode } from './a11y-helpers';

// WCAG 2.2 Level A and AA audit of every customer and kitchen screen.
// Automated rules come from axe-core; the rest are targeted checks for criteria axe cannot judge
// (keyboard operation, visible focus, focus obscured, reflow, text spacing, status messages,
// non-text contrast, dialogs, tooltips). Each test records its raw results under
// test-results-accessibility/_findings/ for the written report.

// Dark mode is out of scope for this pass (user request); add 'dark' back here to cover it.
const THEMES = ['light'] as const;

type State = { name: string; go: (page: Page, request: APIRequestContext) => Promise<void> };

const STATES: State[] = [
  { name: 'menu', go: async () => {} },
  { name: 'navigation drawer open', go: async page => {
    await page.getByRole('button', { name: 'Navigation menu' }).click();
    await page.waitForTimeout(400);
  } },
  { name: 'pizza builder dialog', go: async page => {
    await page.getByRole('button', { name: 'Start Building' }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await page.waitForTimeout(300);
  } },
  { name: 'added-to-order toast', go: async page => {
    await addPreset(page);
    await expect(page.getByText(/added to your order/)).toBeVisible();
  } },
  { name: 'cart empty', go: async page => {
    await page.goto('/cart');
    await expect(page.getByText('You have not added any pizzas yet.')).toBeVisible();
  } },
  { name: 'cart with pizzas', go: async page => {
    await addPreset(page, 2);
    await page.getByRole('link', { name: /Review Order/ }).click();
    await expect(page.getByRole('link', { name: 'Continue' })).toBeVisible();
  } },
  { name: 'checkout', go: async page => {
    await addPreset(page);
    await page.goto('/checkout');
    await expect(page.getByLabel('Name for the order')).toBeVisible();
  } },
  { name: 'checkout name error', go: async page => {
    await addPreset(page);
    await page.goto('/checkout');
    const name = page.getByLabel('Name for the order');
    await name.fill('x');
    await name.fill('');
    await name.blur();
    await page.waitForTimeout(300);
  } },
  { name: 'order confirmation', go: async (page, request) => {
    const order = await seedOrder(request, 'Confirm Scan');
    await page.goto(`/orders/${order.id}`);
    await expect(page.getByText(/Your order is in/)).toBeVisible();
  } },
  { name: 'order not found', go: async page => {
    await page.goto('/orders/00000000-0000-0000-0000-000000000000');
    await expect(page.getByText('We could not find that order.')).toBeVisible();
  } },
  { name: 'kitchen', go: async (page, request) => {
    await seedOrder(request, 'Kitchen New');
    await seedOrder(request, 'Kitchen Ready', true);
    await page.goto('/kitchen');
    await expect(page.getByRole('button', { name: 'Mark Ready' }).first()).toBeVisible();
  } },
  { name: 'page not found', go: async page => {
    await page.goto('/does-not-exist');
    await expect(page.getByText('That page does not exist.')).toBeVisible();
  } },
];

test.describe('axe-core WCAG 2.2 A/AA scan', () => {
  for (const theme of THEMES) {
    for (const state of STATES) {
      test(`${state.name} (${theme})`, async ({ page, request }, testInfo) => {
        await openMenu(page);
        await setDarkMode(page, theme === 'dark');
        await state.go(page, request);
        const { wcag } = await scan(page, testInfo, `${state.name} ${theme}`);
        expect.soft(wcag.map(v => `${v.id} (${v.nodes.length}): ${v.nodes.map(n => n.target).join(' | ')}`)).toEqual([]);
      });
    }
  }
});

/** Presses Tab until the focused element matches, collecting every stop. */
async function tabTo(page: Page, match: RegExp, max = 60, key = 'Tab'): Promise<{ found: boolean; stops: string[] }> {
  const stops: string[] = [];
  for (let i = 0; i < max; i++) {
    await page.keyboard.press(key);
    await page.waitForTimeout(60);
    const focus = await describeFocus(page);
    stops.push(focus);
    if (match.test(focus)) return { found: true, stops };
  }
  return { found: false, stops };
}

test.describe('2.1.1 Keyboard, 2.4.3 Focus Order', () => {
  test('a customer can build, review, and place an order with the keyboard only', async ({ page }, testInfo) => {
    const log: Record<string, unknown> = {};
    await openMenu(page);
    await page.mouse.move(0, 0);

    const toStart = await tabTo(page, /Start Building/);
    log.tabStopsToStartBuilding = toStart.stops;
    expect(toStart.found, 'reach Start Building with Tab').toBe(true);
    await page.keyboard.press('Enter');
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    log.focusAfterDialogOpens = await describeFocus(page);

    const toRadio = await tabTo(page, /input\[role=radio\]|input "/, 15);
    log.tabStopsToSizeRadio = toRadio.stops;
    await page.keyboard.press('ArrowDown');
    await page.waitForTimeout(200);
    log.largeCheckedByArrowKey = await dialog.getByRole('radio', { name: /Large/ }).isChecked().catch(() => false);

    const toChip = await tabTo(page, /Pepperoni/, 15);
    log.tabStopsToPepperoniChip = toChip.stops;
    log.pepperoniChipReachable = toChip.found;
    if (toChip.found) {
      await page.keyboard.press('Space');
      await page.waitForTimeout(200);
    }
    log.priceAfterKeyboardChoices = await dialog.locator('.mud-dialog-actions .builder-price').textContent();

    const toAdd = await tabTo(page, /Add to Order/, 20);
    log.tabStopsToAddToOrder = toAdd.stops;
    expect(toAdd.found, 'reach Add to Order with Tab').toBe(true);
    await page.keyboard.press('Enter');
    await expect(dialog).toBeHidden();
    log.focusAfterDialogCloses = await describeFocus(page);

    const toReview = await tabTo(page, /Review Order/, 40);
    expect(toReview.found, 'reach Review Order with Tab').toBe(true);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/cart$/);
    await expect(page.getByRole('link', { name: 'Continue' })).toBeVisible();
    log.focusAfterNavigatingToCart = await describeFocus(page);

    const toContinue = await tabTo(page, /Continue/, 40);
    log.tabStopsInCart = toContinue.stops;
    expect(toContinue.found, 'reach Continue with Tab').toBe(true);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/checkout$/);
    await expect(page.getByLabel('Name for the order')).toBeVisible();

    const toName = await tabTo(page, /input/, 30);
    expect(toName.found, 'reach the name field with Tab').toBe(true);
    await page.keyboard.type('Keyboard Only');
    const toPlace = await tabTo(page, /Place Order/, 10);
    expect(toPlace.found, 'reach Place Order with Tab').toBe(true);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/orders\//);
    log.result = 'order placed with keyboard only';
    record(testInfo, 'keyboard order', log);
    expect.soft(log.largeCheckedByArrowKey, 'arrow key selects the Large size').toBe(true);
    expect.soft(log.pepperoniChipReachable, 'topping chip reachable with Tab').toBe(true);
    expect.soft(log.priceAfterKeyboardChoices).toBe('$16.00');
  });

  test('the kitchen can mark an order Ready with the keyboard, and focus is not lost', async ({ page, request }, testInfo) => {
    await seedOrder(request, 'Keyboard Kitchen');
    await openMenu(page);
    await page.goto('/kitchen');
    await expect(page.getByRole('button', { name: 'Mark Ready' }).first()).toBeVisible();
    const toMark = await tabTo(page, /Mark Ready/, 40);
    expect(toMark.found).toBe(true);
    await page.keyboard.press('Enter');
    await page.waitForTimeout(1500);
    const after = await describeFocus(page);
    record(testInfo, 'kitchen keyboard', { tabStops: toMark.stops, focusAfterMarkReady: after });
    expect.soft(after, 'focus should land somewhere meaningful, not the page body').not.toBe('(body)');
  });

  test('removing a cart line keeps keyboard focus in the cart', async ({ page }, testInfo) => {
    await openMenu(page);
    await addPreset(page, 2);
    await page.getByRole('link', { name: /Review Order/ }).click();
    await expect(page.getByRole('link', { name: 'Continue' })).toBeVisible();
    const toRemove = await tabTo(page, /"Remove"/, 40);
    expect(toRemove.found).toBe(true);
    await page.keyboard.press('Enter');
    await page.waitForTimeout(800);
    const after = await describeFocus(page);
    record(testInfo, 'cart remove focus', { focusAfterRemove: after });
    expect.soft(after, 'focus after removing a line').not.toBe('(body)');
  });

  test('the header logo is not a mouse-only control', async ({ page }, testInfo) => {
    await openMenu(page);
    const logo = await page.evaluate(() => {
      const svg = document.querySelector('.mud-appbar .cursor-pointer') as HTMLElement | null;
      if (!svg) return null;
      return { tag: svg.tagName, tabindex: svg.getAttribute('tabindex'), role: svg.getAttribute('role'), label: svg.getAttribute('aria-label'), ariaHidden: svg.getAttribute('aria-hidden') };
    });
    record(testInfo, 'header logo', { logo });
    // A clickable element must be reachable and named, or not clickable at all.
    expect.soft(logo === null || (logo.tabindex !== null && !!logo.label), `logo: ${JSON.stringify(logo)}`).toBe(true);
  });
});

test.describe('2.4.7 Focus Visible, 2.4.11 Focus Not Obscured', () => {
  const pages: { name: string; height?: number; go: (page: Page, request: APIRequestContext) => Promise<void> }[] = [
    { name: 'menu', go: async () => {} },
    { name: 'pizza builder dialog', go: async page => {
      await page.getByRole('button', { name: 'Start Building' }).click();
      await expect(page.getByRole('dialog')).toBeVisible();
      await page.waitForTimeout(300);
      await page.evaluate(() => (document.activeElement as HTMLElement | null)?.blur());
    } },
    { name: 'cart', height: 450, go: async page => {
      await addPreset(page, 5);
      await page.getByRole('link', { name: /Review Order/ }).click();
      await expect(page.getByRole('link', { name: 'Continue' })).toBeVisible();
    } },
    { name: 'checkout', go: async page => {
      await addPreset(page);
      await page.goto('/checkout');
      await expect(page.getByLabel('Name for the order')).toBeVisible();
    } },
    { name: 'kitchen', height: 500, go: async (page, request) => {
      for (let i = 0; i < 3; i++) await seedOrder(request, `Focus ${i}`);
      await page.goto('/kitchen');
      await expect(page.getByRole('button', { name: 'Mark Ready' }).first()).toBeVisible();
    } },
  ];

  for (const theme of THEMES) {
    for (const p of pages) {
      test(`${p.name} (${theme}): every Tab stop shows focus and is not hidden behind fixed content`, async ({ page, request }, testInfo) => {
        await page.setViewportSize({ width: 1280, height: p.height ?? 800 });
        await openMenu(page);
        await setDarkMode(page, theme === 'dark');
        await p.go(page, request);
        await page.mouse.move(640, (p.height ?? 800) - 2);
        await page.evaluate(() => window.scrollTo(0, 0));

        const stops: { focus: string; visible: boolean; obscured: boolean }[] = [];
        const seen = new Set<string>();
        for (const key of ['Tab', 'Shift+Tab']) {
          for (let i = 0; i < 45; i++) {
            await page.keyboard.press(key);
            await page.waitForTimeout(350);
            const info = await page.evaluate(() => {
              const el = document.activeElement as HTMLElement | null;
              if (!el || el === document.body) return null;
              const r = el.getBoundingClientRect();
              // Native radios are hidden inputs; their visible box is the surrounding button.
              const box = (r.width < 2 || r.height < 2 ? el.closest('.mud-button-root, label') as HTMLElement ?? el : el).getBoundingClientRect();
              const points = [[box.left + box.width / 2, box.top + box.height / 2], [box.left + 2, box.top + 2], [box.right - 2, box.top + 2], [box.left + 2, box.bottom - 2], [box.right - 2, box.bottom - 2]];
              const owner = (n: Element | null) => !!n && (el.contains(n) || n.contains(el) || (el.closest('.mud-button-root, label')?.contains(n) ?? false));
              const hits = points.map(([x, y]) => document.elementFromPoint(x, y));
              const onScreen = box.bottom > 0 && box.top < innerHeight;
              const obscured = onScreen && hits.every(h => !owner(h));
              return { box: { x: box.left, y: box.top, width: box.width, height: box.height }, obscured, coveredBy: obscured ? (hits[0]?.closest('.mud-appbar, .mud-snackbar, .mud-overlay, header')?.className ?? hits[0]?.tagName) : null };
            });
            if (!info) break;
            const focus = await describeFocus(page);
            const pad = 6;
            const vp = page.viewportSize()!;
            const clip = {
              x: Math.max(0, info.box.x - pad), y: Math.max(0, info.box.y - pad),
              width: Math.min(vp.width, info.box.x + info.box.width + pad) - Math.max(0, info.box.x - pad),
              height: Math.min(vp.height, info.box.y + info.box.height + pad) - Math.max(0, info.box.y - pad),
            };
            let visible = true;
            if (clip.width > 2 && clip.height > 2 && !info.obscured) {
              const focused = await page.screenshot({ clip, animations: 'disabled' });
              // Blur keeps the browser's sequential-focus starting point, so the next Tab continues from here.
              await page.evaluate(() => (document.activeElement as HTMLElement | null)?.blur());
              await page.waitForTimeout(350);
              const unfocused = await page.screenshot({ clip, animations: 'disabled' });
              visible = !focused.equals(unfocused);
            }
            const keyName = `${key} ${focus} @${Math.round(info.box.x)},${Math.round(info.box.y)}`;
            if (seen.has(keyName)) break;
            seen.add(keyName);
            stops.push({ focus: `${key}: ${focus}${info.coveredBy ? ` (covered by ${info.coveredBy})` : ''}`, visible, obscured: info.obscured });
          }
        }
        record(testInfo, `focus ${p.name} ${theme}`, stops);
        const invisible = stops.filter(s => !s.visible).map(s => s.focus);
        const obscured = stops.filter(s => s.obscured).map(s => s.focus);
        testInfo.annotations.push({ type: 'stops', description: `${stops.length} focus stops checked` });
        expect.soft(invisible, '2.4.7 no visible focus indicator').toEqual([]);
        expect.soft(obscured, '2.4.11 focused element entirely hidden').toEqual([]);
      });
    }
  }
});

test.describe('1.4.10 Reflow, 1.4.4 Resize Text, 1.4.12 Text Spacing', () => {
  const pages: State[] = STATES.filter(s => ['menu', 'pizza builder dialog', 'cart with pizzas', 'checkout name error', 'order confirmation', 'kitchen', 'page not found', 'navigation drawer open'].includes(s.name));

  const overflowProbe = () => {
    const vw = document.documentElement.clientWidth;
    const offenders: string[] = [];
    for (const el of Array.from(document.querySelectorAll<HTMLElement>('body *'))) {
      const style = getComputedStyle(el);
      if (style.visibility === 'hidden' || style.display === 'none') continue;
      const r = el.getBoundingClientRect();
      if (r.width === 0 || r.height === 0) continue;
      // Closed MudBlazor popovers park at x=-9999 and ripples are click animations; neither is content.
      if (el.closest('.mud-popover:not(.mud-popover-open), .mud-ripple-effect')) continue;
      // Content inside its own horizontal scroller is allowed (e.g. a data table).
      let scroller = el.parentElement, inScroller = false;
      while (scroller) { const o = getComputedStyle(scroller).overflowX; if (o === 'auto' || o === 'scroll') { inScroller = true; break; } scroller = scroller.parentElement; }
      if (!inScroller && (r.right > vw + 1 || r.left < -1) && el.children.length === 0) {
        offenders.push(`${el.tagName.toLowerCase()}.${(el.className?.toString() ?? '').split(' ').slice(0, 2).join('.')} "${el.textContent?.trim().slice(0, 30)}" x=${Math.round(r.left)}..${Math.round(r.right)}`);
      }
    }
    return { pageScrollsSideways: document.documentElement.scrollWidth > vw + 1, scrollWidth: document.documentElement.scrollWidth, viewport: vw, offenders: offenders.slice(0, 12) };
  };

  const clipProbe = () => {
    const clipped: string[] = [];
    // A click leaves a MudBlazor ripple (a large circle meant to be clipped by its button) for about
    // a second; it is an animation, not content, so take it out before measuring (as overflowProbe does).
    for (const ripple of Array.from(document.querySelectorAll<HTMLElement>('.mud-ripple-effect'))) ripple.style.display = 'none';
    for (const el of Array.from(document.querySelectorAll<HTMLElement>('body *'))) {
      const style = getComputedStyle(el);
      if (style.display === 'none' || !el.textContent?.trim()) continue;
      const hides = ['hidden', 'clip'];
      const r = el.getBoundingClientRect();
      if (r.width === 0) continue;
      if ((hides.includes(style.overflowY) && el.scrollHeight > el.clientHeight + 2) || (hides.includes(style.overflowX) && el.scrollWidth > el.clientWidth + 2)) {
        clipped.push(`${el.tagName.toLowerCase()}.${(el.className?.toString() ?? '').split(' ').slice(0, 2).join('.')} "${el.textContent.trim().slice(0, 40)}" (${el.scrollWidth}x${el.scrollHeight} in ${el.clientWidth}x${el.clientHeight})`);
      }
    }
    return Array.from(new Set(clipped)).slice(0, 12);
  };

  for (const width of [320, 640]) {
    for (const state of pages) {
      test(`${state.name} at ${width}px wide has no sideways scrolling`, async ({ page, request }, testInfo) => {
        await page.setViewportSize({ width, height: 800 });
        await openMenu(page);
        await state.go(page, request);
        await page.waitForTimeout(300);
        const result = await page.evaluate(overflowProbe);
        record(testInfo, `reflow ${state.name} ${width}`, result);
        await testInfo.attach(`${state.name}-${width}.png`, { body: await page.screenshot({ fullPage: true }), contentType: 'image/png' });
        expect.soft(result.pageScrollsSideways, `page scrollWidth ${result.scrollWidth} > ${result.viewport}`).toBe(false);
        expect.soft(result.offenders).toEqual([]);
      });
    }
  }

  for (const state of pages) {
    test(`${state.name} with WCAG text spacing and 200% text loses no content`, async ({ page, request }, testInfo) => {
      await openMenu(page);
      await state.go(page, request);
      await page.addStyleTag({ content: `
        * { line-height: 1.5 !important; letter-spacing: 0.12em !important; word-spacing: 0.16em !important; }
        p { margin-bottom: 2em !important; }` });
      await page.waitForTimeout(300);
      const spacing = await page.evaluate(clipProbe);
      await page.addStyleTag({ content: 'html { font-size: 200% !important; }' });
      await page.waitForTimeout(300);
      const bigText = await page.evaluate(clipProbe);
      record(testInfo, `text spacing ${state.name}`, { spacing, bigText });
      await testInfo.attach(`${state.name}-text.png`, { body: await page.screenshot({ fullPage: true }), contentType: 'image/png' });
      expect.soft(spacing, '1.4.12 clipped with text spacing').toEqual([]);
      expect.soft(bigText, '1.4.4 clipped at 200% text').toEqual([]);
    });
  }
});

test.describe('4.1.2 Name, Role, Value; 1.3.1 Info and Relationships; 3.3 Input Assistance', () => {
  test('pizza builder dialog exposes its controls correctly', async ({ page }, testInfo) => {
    await openMenu(page);
    await page.getByRole('button', { name: 'Start Building' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    const facts = await page.evaluate(() => {
      const d = document.querySelector('[role=dialog]')!;
      const labelledBy = d.getAttribute('aria-labelledby');
      const radios = Array.from(d.querySelectorAll('input[type=radio]'));
      const group = d.querySelector('[role=radiogroup]');
      const chips = Array.from(d.querySelectorAll('.mud-chip'));
      return {
        ariaModal: d.getAttribute('aria-modal'),
        labelledBy, labelText: labelledBy ? document.getElementById(labelledBy)?.textContent?.trim() : null,
        radiogroup: group ? { name: group.getAttribute('aria-label') ?? (group.getAttribute('aria-labelledby') ? document.getElementById(group.getAttribute('aria-labelledby')!)?.textContent : null) } : null,
        radioCount: radios.length,
        chips: chips.map(c => ({ tag: c.tagName, role: c.getAttribute('role'), tabindex: c.getAttribute('tabindex'), pressed: c.getAttribute('aria-pressed'), checked: c.getAttribute('aria-checked'), selected: c.getAttribute('aria-selected'), text: c.textContent?.trim() })),
      };
    });
    const snapshot = await dialog.ariaSnapshot();
    // Select one chip and see whether its state is announced.
    await dialog.getByText('Pepperoni +$2.00', { exact: true }).click();
    await page.waitForTimeout(200);
    const snapshotAfterChip = await dialog.ariaSnapshot();
    record(testInfo, 'builder dialog semantics', { facts, snapshot, snapshotAfterChip });
    expect.soft(facts.ariaModal, 'dialog aria-modal').toBe('true');
    expect.soft(facts.labelText ?? '', 'dialog named by its title').toMatch(/Build|Customize|Change/);
    expect.soft(facts.radiogroup?.name ?? null, 'size radio group has a name ("Size")').toBeTruthy();
    expect.soft(snapshotAfterChip, 'selected topping exposes pressed/checked state').toMatch(/Pepperoni[^\n]*\[(pressed|checked|selected)/);
  });

  test('preset cards: each size radio group is named for its pizza', async ({ page }, testInfo) => {
    await openMenu(page);
    const groups = await page.evaluate(() => Array.from(document.querySelectorAll('.preset-card')).map(card => {
      const g = card.querySelector('[role=radiogroup]');
      const radios = Array.from(card.querySelectorAll('input[type=radio]')).map(r => (r as HTMLInputElement).name);
      return { title: card.querySelector('h6, h2, h3')?.textContent?.trim(), group: g ? (g.getAttribute('aria-label') ?? g.getAttribute('aria-labelledby')) : null, radioNames: Array.from(new Set(radios)) };
    }));
    const buttons = await page.evaluate(() => Array.from(document.querySelectorAll('.preset-card button')).map(b => b.getAttribute('aria-label') ?? b.textContent?.trim()));
    record(testInfo, 'preset card semantics', { groups, buttons });
    expect.soft(groups.every(g => g.group), 'each preset size group is named').toBe(true);
    // Several "Add" and "Customize" buttons with identical names are ambiguous out of context (2.4.6 / 2.4.4 advisory).
    testInfo.annotations.push({ type: 'observation', description: `preset buttons: ${buttons.join(', ')}` });
  });

  test('checkout name field: label, required, autocomplete, and error are announced', async ({ page }, testInfo) => {
    await openMenu(page);
    await addPreset(page);
    await page.goto('/checkout');
    const name = page.getByLabel('Name for the order');
    await expect(name).toBeVisible();
    const before = await name.evaluate((el: HTMLInputElement) => ({ required: el.required, ariaRequired: el.getAttribute('aria-required'), autocomplete: el.getAttribute('autocomplete'), describedBy: el.getAttribute('aria-describedby') }));
    await name.fill('x');
    await name.fill('');
    await name.blur();
    await page.waitForTimeout(300);
    const after = await name.evaluate((el: HTMLInputElement) => {
      const ids = (el.getAttribute('aria-describedby') ?? '').split(/\s+/).filter(Boolean);
      // Prefer the helper-text line itself: a selector list matches in document order, and the outer
      // .mud-input-control also carries .mud-input-error (its text is label + input + error).
      const errorText = (document.querySelector('.mud-input-helper-text.mud-input-error') ?? document.querySelector('.mud-input-error'))?.textContent?.trim() ?? null;
      const errorEl = Array.from(document.querySelectorAll('.mud-input-helper-text')).find(e => e.textContent?.trim() === errorText) ?? null;
      const live = errorEl?.closest('[aria-live], [role=alert], [role=status]');
      return { ariaInvalid: el.getAttribute('aria-invalid'), describedByText: ids.map(id => document.getElementById(id)?.textContent?.trim()), errorText, errorInLiveRegion: !!live };
    });
    record(testInfo, 'checkout name semantics', { before, after });
    expect.soft(before.required || before.ariaRequired === 'true', '3.3.2 required is exposed').toBe(true);
    expect.soft(before.autocomplete, '1.3.5 autocomplete="name"').toMatch(/name/);
    expect.soft(after.ariaInvalid, '3.3.1 aria-invalid when in error').toBe('true');
    expect.soft(after.describedByText.join(' ') , '3.3.1 error text is tied to the field').toContain(after.errorText ?? '(no error text)');
  });

  test('header controls expose state: menu button, cart count, dark mode', async ({ page }, testInfo) => {
    await openMenu(page);
    await addPreset(page, 2);
    const menuButton = page.getByRole('button', { name: 'Navigation menu' });
    const expandedBefore = await menuButton.getAttribute('aria-expanded');
    await menuButton.click();
    await page.waitForTimeout(400);
    const expandedAfter = await menuButton.getAttribute('aria-expanded');
    const cart = await page.evaluate(() => {
      const a = document.querySelector('.mud-appbar a[href="/cart"], .mud-appbar [aria-label="Cart"]') as HTMLElement | null;
      return a ? { tag: a.tagName, label: a.getAttribute('aria-label'), badge: a.closest('.mud-badge-root')?.querySelector('.mud-badge')?.textContent?.trim() } : null;
    });
    const dark = await page.getByRole('button', { name: 'Toggle dark mode' }).getAttribute('aria-pressed');
    const header = await page.locator('header').first().ariaSnapshot();
    record(testInfo, 'header semantics', { expandedBefore, expandedAfter, cart, darkModeAriaPressed: dark, header });
    expect.soft(expandedAfter, 'menu button aria-expanded reflects the drawer').toBe('true');
    expect.soft(cart?.label ?? '', 'cart link name includes the pizza count shown in the badge').toContain(cart?.badge ?? '?');
    expect.soft(dark, 'dark mode toggle exposes aria-pressed').toMatch(/true|false/);
  });

  test('page structure: landmarks, skip link, and heading levels', async ({ page, request }, testInfo) => {
    const out: Record<string, unknown> = {};
    for (const state of STATES.filter(s => ['menu', 'cart with pizzas', 'checkout', 'order confirmation', 'kitchen', 'order not found', 'page not found'].includes(s.name))) {
      await openMenu(page);
      await state.go(page, request);
      out[state.name] = await page.evaluate(() => ({
        main: document.querySelectorAll('main, [role=main]').length,
        nav: document.querySelectorAll('nav, [role=navigation]').length,
        banner: document.querySelectorAll('header, [role=banner]').length,
        skipLink: !!Array.from(document.querySelectorAll('a[href^="#"]')).find(a => /skip|main/i.test(a.textContent ?? '')),
        headings: Array.from(document.querySelectorAll('h1,h2,h3,h4,h5,h6')).filter(h => (h as HTMLElement).offsetParent !== null).map(h => `${h.tagName} ${h.textContent?.trim().slice(0, 40)}`),
      }));
    }
    record(testInfo, 'page structure', out);
    for (const [name, s] of Object.entries(out) as [string, any][]) {
      expect.soft(s.main, `${name}: one main landmark`).toBe(1);
      expect.soft(s.headings.filter((h: string) => h.startsWith('H1')).length, `${name}: one h1`).toBe(1);
    }
  });
});

test.describe('4.1.3 Status Messages, 2.2.1 Timing, 1.4.13 Content on Hover or Focus', () => {
  test('status messages are announced without moving focus', async ({ page, request }, testInfo) => {
    await openMenu(page);
    await addPreset(page);
    const toast = await page.evaluate(() => {
      const msg = Array.from(document.querySelectorAll('.mud-snackbar')).find(s => /added to your order/.test(s.textContent ?? ''));
      const live = msg?.closest('[aria-live], [role=status], [role=alert]') ?? msg?.querySelector('[aria-live], [role=status], [role=alert]');
      return { found: !!msg, live: live ? { role: live.getAttribute('role'), ariaLive: live.getAttribute('aria-live') } : null };
    });
    await page.goto('/orders/00000000-0000-0000-0000-000000000000');
    await expect(page.getByText('We could not find that order.')).toBeVisible();
    const alert = await page.evaluate(() => {
      const a = Array.from(document.querySelectorAll('.mud-alert')).find(x => /could not find/.test(x.textContent ?? ''));
      return { role: a?.getAttribute('role') ?? null, ariaLive: a?.getAttribute('aria-live') ?? null, liveAncestor: !!a?.closest('[aria-live]') };
    });
    await seedOrder(request, 'Status Kitchen');
    await page.goto('/kitchen');
    await expect(page.getByRole('button', { name: 'Mark Ready' }).first()).toBeVisible();
    const kitchen = await page.evaluate(() => ({ liveRegions: Array.from(document.querySelectorAll('[aria-live], [role=status], [role=log]')).map(e => `${e.tagName} ${e.getAttribute('role')} ${e.getAttribute('aria-live')} "${e.textContent?.trim().slice(0, 40)}"`) }));
    record(testInfo, 'status messages', { toast, alert, kitchen });
    expect.soft(toast.live, 'added-to-order toast is in a live region').not.toBeNull();
    expect.soft(alert.role === 'alert' || alert.role === 'status' || alert.liveAncestor, 'page status alert is announced').toBe(true);
  });

  test('cart tooltips can be dismissed with Escape and hovered', async ({ page }, testInfo) => {
    await openMenu(page);
    await addPreset(page);
    await page.getByRole('link', { name: /Review Order/ }).click();
    const remove = page.getByRole('button', { name: 'Remove' }).first();
    await expect(remove).toBeVisible();
    await remove.hover();
    const tooltip = page.locator('.mud-tooltip').filter({ hasText: 'Remove' });
    await expect(tooltip).toBeVisible();
    await page.keyboard.press('Escape');
    await page.waitForTimeout(300);
    const dismissedByEscape = !(await tooltip.isVisible());
    await page.mouse.move(0, 0);
    await remove.hover();
    await expect(tooltip).toBeVisible();
    const tb = (await tooltip.boundingBox())!;
    await page.mouse.move(tb.x + tb.width / 2, tb.y + tb.height / 2, { steps: 5 });
    await page.waitForTimeout(300);
    const hoverable = await tooltip.isVisible();
    await remove.focus();
    await page.waitForTimeout(300);
    const showsOnFocus = await page.locator('.mud-tooltip').filter({ hasText: 'Remove' }).isVisible();
    record(testInfo, 'tooltips', { dismissedByEscape, hoverable, showsOnFocus });
    expect.soft(dismissedByEscape, '1.4.13 dismissible with Escape').toBe(true);
    expect.soft(hoverable, '1.4.13 hoverable').toBe(true);
  });

  test('toasts stay long enough to read', async ({ page }, testInfo) => {
    await openMenu(page);
    await addPreset(page);
    const toast = page.locator('.mud-snackbar').filter({ hasText: 'added to your order' });
    await expect(toast).toBeVisible();
    const start = Date.now();
    await page.mouse.move(0, 0);
    await expect(toast).toBeHidden({ timeout: 20_000 });
    const visibleMs = Date.now() - start;
    // Hovering should pause the timer so a slow reader can finish.
    await addPreset(page, 1).catch(() => {});
    await page.locator('.preset-card').first().getByRole('button', { name: /^Add / }).click();
    const toast2 = page.locator('.mud-snackbar').filter({ hasText: 'added to your order' }).first();
    await expect(toast2).toBeVisible();
    await toast2.hover();
    await page.waitForTimeout(6000);
    const pausedOnHover = await toast2.isVisible();
    record(testInfo, 'toast timing', { visibleMs, pausedOnHover });
    testInfo.annotations.push({ type: 'observation', description: `toast visible ~${visibleMs}ms; paused while hovered: ${pausedOnHover}` });
  });
});

test.describe('1.4.11 Non-text Contrast', () => {
  for (const theme of THEMES) {
    for (const state of STATES.filter(s => ['pizza builder dialog', 'menu', 'cart with pizzas', 'checkout', 'kitchen'].includes(s.name))) {
      test(`${state.name} (${theme}): control boundaries and icons reach 3:1`, async ({ page, request }, testInfo) => {
        await openMenu(page);
        await setDarkMode(page, theme === 'dark');
        await state.go(page, request);
        await page.waitForTimeout(300);
        const failures = await page.evaluate(() => {
          const parse = (c: string) => { const m = c.match(/[\d.]+/g)?.map(Number) ?? [0, 0, 0, 0]; return { r: m[0], g: m[1], b: m[2], a: m.length > 3 ? m[3] : 1 }; };
          const blend = (fg: ReturnType<typeof parse>, bg: ReturnType<typeof parse>) => ({ r: fg.r * fg.a + bg.r * (1 - fg.a), g: fg.g * fg.a + bg.g * (1 - fg.a), b: fg.b * fg.a + bg.b * (1 - fg.a), a: 1 });
          const bgOf = (el: Element | null): ReturnType<typeof parse> => {
            const layers: ReturnType<typeof parse>[] = [];
            while (el) { const c = parse(getComputedStyle(el).backgroundColor); if (c.a > 0) { layers.push(c); if (c.a >= 1) break; } el = el.parentElement; }
            let result = { r: 255, g: 255, b: 255, a: 1 };
            for (const layer of layers.reverse()) result = blend(layer, result);
            return result;
          };
          const lum = (c: ReturnType<typeof parse>) => { const f = (v: number) => { v /= 255; return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4; }; return 0.2126 * f(c.r) + 0.7152 * f(c.g) + 0.0722 * f(c.b); };
          const ratio = (a: ReturnType<typeof parse>, b: ReturnType<typeof parse>) => { const [x, y] = [lum(a), lum(b)].sort((p, q) => q - p); return (x + 0.05) / (y + 0.05); };
          const out: string[] = [];
          const visible = (el: Element) => { const r = el.getBoundingClientRect(); return r.width > 0 && r.height > 0 && getComputedStyle(el).visibility !== 'hidden'; };
          const name = (el: Element) => (el.getAttribute('aria-label') || el.textContent?.trim().slice(0, 30) || el.className.toString().split(' ')[0]);
          // Outlined text fields: the border is what shows where to type.
          for (const b of Array.from(document.querySelectorAll('.mud-input-outlined-border'))) {
            if (!visible(b)) continue;
            const field = b.closest('.mud-input-control');
            if (field?.querySelector('input:disabled')) continue;
            const c = blend(parse(getComputedStyle(b).borderTopColor), bgOf(b.parentElement));
            const r = ratio(c, bgOf(b.parentElement));
            if (r < 3) out.push(`text field border "${field?.querySelector('label')?.textContent?.trim()}" ${r.toFixed(2)}:1`);
          }
          // Unchecked radio circles and icon-only buttons: the icon is the control.
          for (const btn of Array.from(document.querySelectorAll('.mud-radio .mud-button-root, button.mud-icon-button, a.mud-icon-button'))) {
            if (!visible(btn) || (btn as HTMLButtonElement).disabled || btn.classList.contains('mud-disabled')) continue;
            const svg = btn.querySelector('svg');
            if (!svg) continue;
            const bg = bgOf(btn.parentElement);
            const c = blend(parse(getComputedStyle(svg).color || getComputedStyle(svg).fill), bg);
            const r = ratio(c, bg);
            if (r < 3) out.push(`icon "${name(btn.closest('label') ?? btn)}" ${r.toFixed(2)}:1`);
          }
          // Outlined chips (toppings): the outline marks each choice.
          for (const chip of Array.from(document.querySelectorAll('.mud-chip-outlined'))) {
            if (!visible(chip)) continue;
            const bg = bgOf(chip.parentElement);
            const border = blend(parse(getComputedStyle(chip).borderTopColor), bg);
            const r = ratio(border, bg);
            if (r < 3) out.push(`chip outline "${chip.textContent?.trim()}" ${r.toFixed(2)}:1`);
          }
          return Array.from(new Set(out));
        });
        record(testInfo, `non-text contrast ${state.name} ${theme}`, failures);
        expect.soft(failures).toEqual([]);
      });
    }
  }
});
