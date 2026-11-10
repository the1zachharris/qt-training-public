# Neighborhood Pizza: Playwright Test Report

**Date:** 2026-11-10 · **Branch:** `ZH-pass1` · **Browser:** Microsoft Edge (Playwright `msedge` channel), headless
**Status:** DRAFT for review. Findings and severities are recommendations. The team decides what to fix and when.

No application code was changed during this testing.

---

## 1. Summary

| Suite | Tests | Passed | Failed |
|---|---:|---:|---:|
| Acceptance suite (`e2e/tests`, run with `npm test`) | 12 | 12 | 0 |
| Exploratory suite (`e2e/exploratory`, new) | 69 | 60 | 9 |

**Every acceptance criterion in `requirements.md` passes** (AC-1.1 through AC-4.3) on a desktop-size screen.

The exploratory suite goes beyond the acceptance criteria: edge cases, API validation, phone-size
screens, accessibility, and API outages. It found **4 medium** and **5 low** issues, plus
**1 intermittent problem I could not reproduce on demand** and several product decisions to confirm before launch.
All 9 failing exploratory tests were checked by hand (screenshots and follow-up probes) and are real app behavior, not test mistakes.

---

## 2. Findings

Severity guide: **Medium** = a customer or kitchen worker can be blocked or misled in a realistic case. **Low** = friction or polish.

### Medium

#### M1. On a phone, the cart hides each line's price and the Change/Remove buttons
- **Where:** `/cart` at 375px wide (a typical phone).
- **Seen:** The table is wider than the screen and does not scroll sideways. The **Price** column header ends past the right edge of the screen, and the **Remove** button starts at x≈437px on a 375px screen. The customer cannot see line prices or remove/change a pizza on a phone. AC-2.2 and AC-2.3 are met on desktop but not on a phone.
- **Also:** In the header, the dark-mode button is partly off-screen (x 350–398px) and the app title wraps to two lines.
- **Test:** `ui.spec.ts › mobile width (375px): cart Price column and Remove button are on screen`
- **Evidence:** `mobile-cart.png` in the test output for `mobile width (375px): no sideways scrolling…`

#### M2. Long names without spaces break the confirmation page and the kitchen card
- **Seen:** A 100-character name with no spaces (the most the form allows) makes the confirmation page scroll sideways (by 204px). On the kitchen card the name runs off the edge (by 220px), and it **pushes the New/Ready status chip and "Placed" time off the card**, so staff lose the status at a glance. On a phone-width kitchen screen, the whole board scrolls sideways.
- **Tests:** `ui.spec.ts › a long name without spaces stays inside its card`; `mobile width (375px): no sideways scrolling…` (`/kitchen`)
- **Evidence:** `kitchen-long-name.png`

#### M3. A mistyped or unknown address shows the browser's error page, not the app's
- **Seen:** `/does-not-exist`, `/orders/123`, and `/orders/` return a bare HTTP 404 with no page body, so Edge shows its own "page isn't working" error. The friendly "That page does not exist. Back to the menu" message in `Routes.razor` is never reached. This affects any customer with a broken or truncated confirmation link.
- **Note:** A well-formed but unknown order id (`/orders/00000000-…`) is handled well: "We could not find that order." with a link back to the menu.
- **Test:** `ui.spec.ts › malformed order id and unknown page show the not-found page`

#### M4. No page has a main heading (h1), so focus is not moved on navigation
- **Seen:** `/menu`, `/cart`, `/checkout`, and `/kitchen` each have 0 `<h1>` elements (page titles render as `<h5>`). `Routes.razor` uses `<FocusOnNavigate Selector="h1" />`, so after each page change, keyboard and screen-reader users are not moved to the new page's content.
- **Good:** Every page has its own `<title>`, every button and link has an accessible name, and focus moves into the pizza builder dialog when it opens.
- **Test:** `ui.spec.ts › every page has a unique title and exactly one h1`

### Low

#### L1. The "20 different pizzas" limit warning shows up about 19 seconds late
- **Seen:** The 21st different pizza is correctly refused (the cart stays at 20). But the warning toast waits in line behind the 20 "added to your order" toasts, which show 5 at a time for 4 seconds each. In testing it appeared **19.5s** after the click. Until then, the customer sees no response to the click.
- **Test:** `ui.spec.ts › the 21st different pizza is refused with a message`

#### L2. The confirmation page does not update when the kitchen marks the order Ready
- **Seen:** After the kitchen marks the order Ready, the customer's open confirmation page still says "We are making your order now" (checked after 12 seconds). It shows "ready for pickup" only after a manual reload. The requirements do not ask for this, but the kitchen board already refreshes itself, so customers may expect the same.
- **Test:** `ui.spec.ts › confirmation page shows status updates without a manual reload`

#### L3. Pressing Enter in the name field does not place the order
- **Seen:** The customer must click **Place Order**. Enter does nothing.
- **Test:** `ui.spec.ts › pressing Enter in the name field places the order`

#### L4. Escape does not close the pizza builder dialog
- **Seen:** This is set on purpose (`CloseOnEscapeKey="false"` in `MainLayout.razor`). Keyboard users expect Escape to cancel a dialog. The Cancel button works.
- **Test:** `ui.spec.ts › Escape key closes the builder dialog`

#### L5. Outage messages show technical details to customers
- **Seen:** With the API down, the menu shows: *"The menu could not be loaded. No connection could be made because the target machine actively refused it. (localhost:5249)"*. The server host and port do not mean anything to a customer.
- **Good:** The checkout message during an outage is careful and correct: *"We could not confirm whether your order went through. Please call the shop before ordering again…"* The cart is kept, and the kitchen board recovers by itself when the API comes back.

### Intermittent and not confirmed

#### U1. The cart may lose pizzas after very fast clicks followed by an immediate page load
- **Seen:** Twice in about 8 attempts, clicking a preset's **Add** several times quickly and then loading `/cart` straight away showed fewer cart lines than clicks (once 1 of 2, once 0 of 4). Three focused reruns of the same steps kept all 4 of 4 lines, so **I could not reproduce it on demand**.
- **Suggested next step:** Look into whether the cart's save to browser session storage can still be in progress when the page is reloaded. Treat this as a lead to check, not a confirmed bug.

---

## 3. Product decisions to confirm before launch

These are working as built, but someone at the shop needs to decide on them.

| # | Topic | What was observed |
|---|---|---|
| D1 | **Kitchen screen is public** | The customer navigation drawer links to **Kitchen**, and `GET /api/v1/kitchen/orders` / `PATCH …/ready` answer with no sign-in. Anyone could see every customer's name or mark orders Ready. `AppNavigation.cs` already notes this was kept for testing and should be put behind staff sign-in before real customers use the site. |
| D2 | Swagger / OpenAPI pages | `/swagger` and `/openapi/v1.json` are reachable. They are turned on only for the Development environment. Confirm production does not run as Development. |
| D3 | No upper limit on order size | One order of 20 lines × 20 Large "The Works" (400 pizzas, **$8,800.00**) was accepted. Decide whether large online orders need a cap or a phone call. |
| D4 | Identical pizzas are not combined | Adding the same pizza twice makes two cart lines instead of one line with a quantity of 2. Each line counts toward the 20-line limit. |
| D5 | Cart is per browser tab | Opening the site in a second tab starts an empty cart (the cart is stored in that tab's session storage). |
| D6 | Quantity field corrects itself | Typing 0 or -1 changes to 1, and 21 changes to 20, without any message. Prices stay right. |

---

## 4. What passed

**Ordering (Section 1):** Price updates live for size and toppings and shows before adding to the order. Preset cards show the correct price per size. **Customize** on a preset shows the preset's price and names a changed pizza "Custom Veggie" etc. Cancel adds nothing. The header badge counts every pizza, including quantity.

**Reviewing (Section 2):** Multiple different pizzas are listed with details, unit price, line price, and total. The quantity stepper stops at 1 and 20. **Change this pizza** updates the line and total. **Start Over** empties the cart, and the cart survives a page reload.

**Placing an order (Section 3):** An empty order and a blank or whitespace name are blocked. The name is trimmed and limited to 100 characters. HTML in a name shows as plain text and never runs (checked on the confirmation page and the kitchen screen). Double-clicking **Place Order** creates only one order. The browser Back button after ordering cannot resubmit. The confirmation shows the order number, name, items, total, and pay-at-counter note.

**Kitchen (Section 4):** New orders appear on an open kitchen screen by themselves (10-second refresh). The New column is oldest first. Mark Ready works, saves, and survives a reload and an API restart. Two kitchen screens marking the same order Ready at once both end up showing Ready with no error.

**API:** The menu matches `requirements.md`. All 20 malformed-request cases (blank, null, or 101-character name; empty, null, or 21-line orders; quantity 0, 21, -5, or text; unknown size, topping, or preset; duplicate topping; bad JSON; no body) return **400**, never 500, with clear field-level messages. Prices sent by the client are ignored and the server prices the order. Ten orders placed at once get ten unique, consecutive numbers. Unknown orders return 404, and marking Ready twice is safe.

**Outages:** With the API stopped, checkout gives a careful "could not confirm" message and keeps the cart. The menu and kitchen show errors instead of blank pages, and the kitchen recovers by itself.

**Other:** Dark mode survives a reload and page changes. No browser console errors during a full order.

---

## 5. How to reproduce

```bash
cd e2e
npm install
npm test                                              # acceptance suite (12 tests)
npx playwright test -c playwright.exploratory.config.ts   # exploratory suite (69 tests; 9 expected failures = findings above)
npx playwright show-report playwright-report-exploratory
```

The exploratory suite starts its own API and UI on separate ports with a throwaway database, the same way the acceptance suite does. `outage.spec.ts` also starts and stops a private API and UI pair (ports 5249 / 5296) to test outages. Screenshots of each test, including the phone-size pages, are saved under `e2e/test-results-exploratory/` (not committed).
