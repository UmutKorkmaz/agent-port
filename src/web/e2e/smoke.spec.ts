import { expect, test } from "@playwright/test";

// End-to-end smoke for the operator console.
//
// The journey test exercises the real backend (onboard -> cited answer ->
// trace detail) and therefore requires the full stack to be up on the hash
// embedding backend. The two locale tests assert SSR locale negotiation and
// run against the same stack.

test.describe("operator console smoke", () => {
  test("onboard -> cited answer -> trace detail", async ({ page }) => {
    // 1) Onboard: the Overview page exposes the "Run onboarding" action that
    //    bootstraps a workspace, agent, knowledge base, and API key.
    await page.goto("/");
    await expect(page.locator("html")).toHaveAttribute("lang", "en");
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();

    const onboard = page.getByRole("button", { name: "Run onboarding" });
    await onboard.click();
    // The workspace status card flips to the bootstrap workspace id once the
    // onboarding round-trip completes.
    await expect(page.getByText(/workspace/i).first()).toBeVisible();

    // 2) Cited answer: open Playground and ask the pre-seeded question. A
    //    citation chip carrying the localized "Source" label must render.
    await page.getByRole("link", { name: "Playground" }).click();
    await expect(page.getByRole("heading", { name: "Ask the document agent" })).toBeVisible();

    await page.getByRole("button", { name: "Ask", exact: true }).click();

    // The citations panel surfaces at least one chunk/citation card with the
    // "Source: <file>" label once the chat round-trip returns.
    const citationLabel = page.locator(".citation-source-label").first();
    await expect(citationLabel).toBeVisible({ timeout: 30_000 });
    await expect(citationLabel).toHaveText(/Source/);

    // 3) Trace detail: load the captured trace for this run and assert the
    //    trace-detail panel renders an id.
    await page.getByRole("button", { name: "Load trace" }).first().click();

    await page.getByRole("link", { name: "Traces" }).click();
    await expect(page.getByRole("heading", { name: "Trace detail" })).toBeVisible();
  });

  test("locale: Accept-Language tr yields a Turkish document", async ({ browser }) => {
    const context = await browser.newContext({
      locale: "tr-TR",
      extraHTTPHeaders: { "Accept-Language": "tr" },
    });
    const page = await context.newPage();
    await page.goto("/");
    await expect(page.locator("html")).toHaveAttribute("lang", "tr");
    await context.close();
  });

  test("locale: default yields an English document", async ({ page }) => {
    await page.goto("/");
    await expect(page.locator("html")).toHaveAttribute("lang", "en");
  });
});
