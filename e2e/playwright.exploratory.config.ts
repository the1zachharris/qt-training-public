import { defineConfig } from '@playwright/test';
import base from './playwright.config';

// Exploratory pass that goes beyond the acceptance criteria (edge cases, API validation, mobile,
// accessibility, outages). Kept apart from `npm test`: a failure here is a finding to review,
// not a regression. Run with: npx playwright test -c playwright.exploratory.config.ts
export default defineConfig({
  ...base,
  testDir: './exploratory',
  outputDir: './test-results-exploratory',
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report-exploratory' }]],
  use: { ...base.use, screenshot: 'on' },
});
