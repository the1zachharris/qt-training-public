# End-to-end tests (Playwright)

Browser tests that drive the real Blazor UI and API against the acceptance criteria in
[`requirements.md`](../requirements.md).

## Run

```bash
cd e2e
npm install
npm test            # headless
npm run test:headed # watch it in a browser window
npm run report      # open the HTML report from the last run
```

Playwright starts its own API (port 5219) and UI (port 5276) from a Release build, so it does not
clash with a copy you already have running on 5119 / 5176. Each run uses a new, empty database
folder under your temp directory, so your real orders are never touched.

Tests run in the Microsoft Edge already installed on Windows. To use Playwright's bundled Chromium
instead, run `npx playwright install chromium` and set `PW_CHANNEL=chromium`.

## Coverage

| Spec | Acceptance criteria |
|---|---|
| `tests/ordering.spec.ts` | AC-1.1, 1.2, 1.3 (live pricing), AC-2.1, 2.2, 2.3 (cart), AC-3.1, 3.2 (checkout and confirmation) |
| `tests/kitchen.spec.ts` | AC-3.3, 4.1 (order reaches the kitchen), AC-4.2 (Mark Ready), AC-4.3 (orders survive an API restart) |

## Exploratory suite

`exploratory/` holds a wider pass beyond the acceptance criteria (edge cases, API validation, phone
width, accessibility, API outages). It is not part of `npm test`; failures there are findings, written
up in [`docs/test-reports/2026-11-10-playwright-test-report.md`](../docs/test-reports/2026-11-10-playwright-test-report.md).

```bash
npx playwright test -c playwright.exploratory.config.ts
```

## Accessibility suite (WCAG 2.2 A/AA)

`accessibility/` checks every screen against WCAG 2.2 Level A and AA. It runs axe-core (`@axe-core/playwright`) plus targeted checks for keyboard use, visible focus, reflow, text spacing, contrast, and status messages. It runs on ports 5319 / 5376; set `A11Y_API_PORT` and `A11Y_UI_PORT` to change them. Findings are in [`docs/test-reports/2026-11-10-wcag-2.2-accessibility-report.md`](../docs/test-reports/2026-11-10-wcag-2.2-accessibility-report.md).

```bash
npx playwright test -c playwright.accessibility.config.ts
```
