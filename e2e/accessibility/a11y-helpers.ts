import fs from 'node:fs';
import path from 'node:path';
import AxeBuilder from '@axe-core/playwright';
import { expect, type Page, type TestInfo } from '@playwright/test';
import { A11Y_API_URL } from '../playwright.accessibility.config';

/** Every Level A and AA tag up to WCAG 2.2, the current W3C Recommendation. */
export const WCAG_TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22a', 'wcag22aa'];

const FINDINGS_DIR = path.join(__dirname, '..', 'test-results-accessibility', '_findings');

/** Saves raw results next to the run so the written report can be built from real data. */
export function record(testInfo: TestInfo, name: string, data: unknown): void {
  fs.mkdirSync(FINDINGS_DIR, { recursive: true });
  const slug = name.replace(/[^a-z0-9]+/gi, '-').toLowerCase();
  fs.writeFileSync(path.join(FINDINGS_DIR, `${slug}.json`), JSON.stringify(data, null, 2));
  testInfo.attachments.push({ name, contentType: 'application/json', body: Buffer.from(JSON.stringify(data, null, 2)) });
}

export type AxeSummary = { id: string; impact: string | null | undefined; help: string; tags: string[]; nodes: { target: string; html: string; summary: string }[] };

/** Runs axe for WCAG 2.2 A/AA plus best practices; returns the WCAG violations and best-practice notes. */
export async function scan(page: Page, testInfo: TestInfo, label: string) {
  const results = await new AxeBuilder({ page }).withTags([...WCAG_TAGS, 'best-practice']).analyze();
  const summarize = (v: (typeof results.violations)[number]): AxeSummary => ({
    id: v.id,
    impact: v.impact,
    help: v.help,
    tags: v.tags.filter(t => /^wcag\d/.test(t) || t === 'best-practice'),
    nodes: v.nodes.slice(0, 8).map(n => ({ target: n.target.join(' '), html: n.html.slice(0, 200), summary: (n.failureSummary ?? '').slice(0, 400) })),
  });
  const isWcag = (v: (typeof results.violations)[number]) => v.tags.some(t => WCAG_TAGS.includes(t));
  const wcag = results.violations.filter(isWcag).map(summarize);
  const bestPractice = results.violations.filter(v => !isWcag(v)).map(summarize);
  const incomplete = results.incomplete.filter(isWcag).map(summarize);
  record(testInfo, `axe ${label}`, { label, url: page.url(), wcag, bestPractice, incomplete });
  for (const v of wcag) testInfo.annotations.push({ type: `WCAG ${v.tags.join(',')}`, description: `${v.id}: ${v.help} (${v.nodes.length} nodes) e.g. ${v.nodes[0]?.target}` });
  for (const v of bestPractice) testInfo.annotations.push({ type: 'best-practice', description: `${v.id}: ${v.help} e.g. ${v.nodes[0]?.target}` });
  return { wcag, bestPractice, incomplete };
}

/** Blazor Server renders before the circuit connects; wait until the page is interactive. */
export async function openMenu(page: Page): Promise<void> {
  await page.goto('/menu');
  await expect(page.getByRole('button', { name: 'Start Building' })).toBeVisible();
}

export async function setDarkMode(page: Page, dark: boolean): Promise<void> {
  const isDark = await page.evaluate(() => {
    const bg = getComputedStyle(document.body).backgroundColor.match(/\d+/g)!.map(Number);
    return bg[0] + bg[1] + bg[2] < 384;
  });
  if (isDark !== dark) {
    await page.getByRole('button', { name: 'Toggle dark mode' }).click();
    await page.waitForTimeout(400);
  }
}

/** Adds the first preset pizza to the cart and waits for the header badge to count it. */
export async function addPreset(page: Page, times = 1): Promise<void> {
  for (let i = 0; i < times; i++) {
    await page.getByRole('button', { name: 'Add', exact: true }).first().click();
    await expect(page.getByRole('link', { name: /Review Order \(\d+\)/ })).toContainText(`(${i + 1})`);
  }
}

const pizza = (over: Record<string, unknown> = {}) => ({ presetPizzaId: null, sizeId: 1, toppingIds: [], quantity: 1, ...over });

/** Creates an order through the API and returns it; optionally marks it Ready. */
export async function seedOrder(request: import('@playwright/test').APIRequestContext, name: string, ready = false) {
  const response = await request.post(`${A11Y_API_URL}/api/v1/orders`, { data: { customerName: name, pizzas: [pizza({ toppingIds: [1, 2] }), pizza({ sizeId: 2, quantity: 2 })] } });
  expect(response.ok()).toBeTruthy();
  const order = await response.json();
  if (ready) expect((await request.patch(`${A11Y_API_URL}/api/v1/kitchen/orders/${order.id}/ready`)).ok()).toBeTruthy();
  return order as { id: string; orderNumber: number };
}

/** A short human label for whatever has focus right now. */
export async function describeFocus(page: Page): Promise<string> {
  return page.evaluate(() => {
    const el = document.activeElement as HTMLElement | null;
    if (!el || el === document.body) return '(body)';
    const name = el.getAttribute('aria-label') || el.textContent?.trim().replace(/\s+/g, ' ').slice(0, 40) || '';
    return `${el.tagName.toLowerCase()}${el.getAttribute('role') ? `[role=${el.getAttribute('role')}]` : ''} "${name}"`;
  });
}
