import { defineConfig, devices } from "@playwright/test";

// E2E smoke configuration for the operator console.
//
// The stack is brought up out-of-band (locally via the dev stack, in CI via
// `docker compose` with EMBEDDING_BACKEND=hash). The web app is reached over
// PLAYWRIGHT_BASE_URL, defaulting to the published host port the CI compose
// override binds the `web` service to.
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:3002";

export default defineConfig({
  testDir: "./e2e",
  // Fail fast in CI if a test or describe block is accidentally left `.only`.
  forbidOnly: !!process.env.CI,
  // Retry once in CI to absorb cold-start flakiness; the trace is captured on
  // that first retry so the failure is debuggable.
  retries: process.env.CI ? 1 : 0,
  // Deterministic, serial execution against a single shared stack.
  workers: 1,
  reporter: process.env.CI ? "github" : "list",
  use: {
    baseURL,
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
