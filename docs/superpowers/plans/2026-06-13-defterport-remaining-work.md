# DefterPort Remaining Work Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Executed here as a dynamic Workflow: three parallel tracks (A Python / B .NET / C Web), sequential test-gated stages within each track, adversarial review on each hardening diff.**

**Goal:** Complete four deferred items — Python/.NET/Web monolith refactors with targeted hardening, the training-form correctness fix, .NET auth-floor integration tests, and full operator-console i18n — without changing the auth/embedding/sovereign/KVKK behavior shipped earlier on `feat/defterport-pilot-hardening`.

**Architecture:** File footprints partition into three disjoint trees (`src/ai-services`, `src/platform-api`, `src/web`). Tracks run in parallel; stages within a track run in sequence. Each refactor extraction is behavior-preserving and gated by the existing test suite (characterization); each hardening change is a small isolated diff gated by tests + an adversarial review agent. The web tree is internally sequenced (split → i18n → Playwright) because three workstreams touch it.

**Tech Stack:** Python 3.11 (FastAPI, psycopg, psycopg_pool, sentence-transformers, pytest), .NET 10 (minimal APIs, EF Core, xUnit, Testcontainers), Next.js 15 (App Router, next-intl, Playwright).

**Behavior-frozen contracts (must NOT change meaning):** e5 embedding (768-dim, `query:`/`passage:` prefixes, dim-mismatch 409), `DEFAULT_RAG_SCORE_THRESHOLD=0.25`, `AGENTPORT_SOVEREIGN` egress lock, `TRACE_REDACT_CONTENT`, `AI_SERVICES_INTERNAL_TOKEN` auth, API-key 401/403 + workspace scoping, `AGENTPORT_ALLOW_DEV_ENDPOINTS` gating, random bootstrap key, `vector(768)` column. The cross-runtime dim guard test (`test_pgvector_column_dim_matches_e5_embedding_dim`) and all existing tests must stay green throughout.

**Baseline gates (green now, keep green):** Python 26 pytest @ ≥45%; .NET 38 xUnit; prod + dev compose parse.

---

## Track A — Python: `main.py` refactor + targeted hardening

Establish a package `src/ai-services/agentport/` and move logic out of `main.py` incrementally. After EACH extraction, run the full suite with the hash backend so no model download is needed:
`cd src/ai-services && EMBEDDING_BACKEND=hash PYTHONPATH=. python3 -m pytest tests/ -q` → expect 26 passed.
Imports may be re-exported from `main.py` so `tests/test_contracts.py` (which imports from `main`) keeps working unchanged; do not edit tests except where a moved symbol's import path is part of the public contract being verified.

### Task A1: Create package + extract Pydantic models

**Files:**
- Create: `src/ai-services/agentport/__init__.py`, `src/ai-services/agentport/models.py`
- Modify: `src/ai-services/main.py` (remove model class bodies, `from agentport.models import *`)

- [ ] **Step 1:** Confirm baseline green: `cd src/ai-services && EMBEDDING_BACKEND=hash PYTHONPATH=. python3 -m pytest tests/ -q` → 26 passed.
- [ ] **Step 2:** Move all Pydantic request/response models (`ChatRequest`, `ChatResponse`, `Citation`, `RetrievedChunk`, `IngestResponse`, `TrainRequest`, `TrainResponse`, etc.) verbatim into `agentport/models.py`. Keep field definitions, validators, and bounds identical.
- [ ] **Step 3:** In `main.py`, replace the moved bodies with `from agentport.models import ( ... )` re-exporting every moved name (tests import these from `main`).
- [ ] **Step 4:** `python3 -m py_compile main.py agentport/models.py` then run the suite → 26 passed.
- [ ] **Step 5:** Commit: `git add -A src/ai-services && git commit -m "refactor(ai-services): extract Pydantic models to agentport/models.py"`

### Task A2: Extract embeddings module

**Files:**
- Create: `src/ai-services/agentport/embeddings.py`
- Modify: `src/ai-services/main.py`

- [ ] **Step 1:** Move `hash_embedding`, `sentence_transformer_embedding`, `embed_text`, `embed_query`, `embed_passage`, `e5_prefixed`, `use_hash_backend`, `active_embedding_dim`, and the `EMBEDDING_*` constants (`EMBEDDING_DIMENSIONS`, `EMBEDDING_DIM_E5`, `SENTENCE_TRANSFORMERS_MODEL`, `EMBEDDING_STRICT`) into `embeddings.py`, verbatim. Preserve lazy model loading + the prefixing + the strict/fallback semantics exactly.
- [ ] **Step 2:** Re-export all moved names from `main.py`.
- [ ] **Step 3:** Suite green (hash backend) → 26 passed; `py_compile` clean.
- [ ] **Step 4:** Commit: `refactor(ai-services): extract embeddings module`

### Task A3: Extract chunking module

**Files:** Create `src/ai-services/agentport/chunking.py`; modify `main.py`.

- [ ] **Step 1:** Move `chunk_text`, `content_hash`, `ingestion_idempotency_key`, `ingestion_metadata`, and chunk-span helpers verbatim.
- [ ] **Step 2:** Re-export from `main.py`; suite green; `py_compile` clean.
- [ ] **Step 3:** Commit: `refactor(ai-services): extract chunking module`

### Task A4: Extract persistence + add connection pool (HARDENING — adversarial review)

**Files:**
- Create: `src/ai-services/agentport/persistence/__init__.py`, `src/ai-services/agentport/persistence/db.py`
- Modify: `src/ai-services/main.py`, `src/ai-services/requirements.txt`

- [ ] **Step 1:** Move `db_connect` and all raw-SQL DB functions (`find_existing_document`, `archive_replaced_documents`, `write_document_chunks`, `retrieve_chunks`, `load_agent`, `save_run_trace`, delete/reingest helpers) into `persistence/db.py`. Pure relocation in this step; suite green; commit `refactor(ai-services): extract persistence/db`.
- [ ] **Step 2 (HARDENING):** Add `psycopg_pool>=3.2,<4` to `requirements.txt`. Introduce a module-level `ConnectionPool` created lazily in the FastAPI lifespan; replace per-call `db_connect()` with `with pool.connection() as conn:`. The pool size defaults to env `DB_POOL_MAX=10`. **Behavior must be identical** (same SQL, same transactions) — only connection acquisition changes.
- [ ] **Step 3:** Suite green (hash backend, which still uses a DB connection only in DB-touching tests — confirm the 26 still pass; DB-less tests unaffected). `py_compile` clean.
- [ ] **Step 4:** Commit: `perf(ai-services): pool Postgres connections instead of per-call connect`
- [ ] **Step 5 (REVIEW):** Adversarial review agent confirms: same SQL/transactions, no behavior change, pool closed on lifespan shutdown.

### Task A5: Extract retrieval module

**Files:** Create `src/ai-services/agentport/retrieval.py`; modify `main.py`.

- [ ] Move `retrieve_chunks` orchestration + score-threshold/no-answer helpers (`filter_chunks_by_score`, `should_use_no_answer`, `build_rag_answer`, `extractive_answer`, `meaningful_terms`, `singularize`), keeping the dim-mismatch 409 and `DEFAULT_RAG_SCORE_THRESHOLD=0.25` semantics exactly. Re-export; suite green; commit `refactor(ai-services): extract retrieval module`.

### Task A6: Extract providers package (sovereign gate frozen)

**Files:** Create `src/ai-services/agentport/providers/{__init__.py,ollama.py,openai_compatible.py,routing.py}`; modify `main.py`.

- [ ] Move `try_provider_answer`, `try_ollama_answer`, `try_openai_compatible_answer`, `resolve_runtime_route`, `is_ollama_provider`, `provider_enabled_by_env`, `sovereign_mode_enabled`, `estimate_provider_cost`, and the skip-reason taxonomy. **The sovereign short-circuit (skip reason `sovereign_mode`) and `PROVIDER_LIVE_CALLS` gating must remain byte-identical in logic.** Re-export; suite green (incl. `test_provider_answer_skips_cloud_live_calls_when_disabled`); commit `refactor(ai-services): extract providers package`.

### Task A7: Extract ingestion + training modules

**Files:** Create `src/ai-services/agentport/ingestion.py`, `src/ai-services/agentport/training.py`; modify `main.py`.

- [ ] Move `extract_text` (incl. the PDF + new `.docx`/`extract_docx_text` path), the ingest write flow, and `run_training_job` / sklearn TF-IDF logic. Re-export; suite green; commit `refactor(ai-services): extract ingestion and training modules`.

### Task A8: Thin `main.py` + remaining hardening (HARDENING — adversarial review)

**Files:** Modify `src/ai-services/main.py` (now only: app setup, lifespan, route handlers, internal-token middleware, re-exports).

- [ ] **Step 1 (HARDENING):** Wrap the `save_run_trace` call site in try/except so a trace-write failure logs (via the new `logging`) but returns the already-computed chat answer. Guard `UUID(payload.agent_id)` and `UUID(payload.knowledge_base_id)` with try/except → `HTTPException(status_code=400, detail="invalid uuid")`. Remove the dead `fallback_mode == "ollama"` no-answer branch.
- [ ] **Step 2 (HARDENING):** Add `logging.getLogger("agentport")` structured logging for provider errors, DB failures, and the embedding-fallback path (replace the two lifespan `print()`s).
- [ ] **Step 3:** Confirm `main.py` < 800 LOC and every `agentport/*` module < 800 LOC (`wc -l`).
- [ ] **Step 4:** Suite green (hash backend) → 26 passed; `py_compile` clean across the package.
- [ ] **Step 5:** Commit: `refactor(ai-services): thin main.py, harden trace/uuid/logging`
- [ ] **Step 6 (REVIEW):** Adversarial review agent confirms: chat still returns an answer when trace write fails; malformed UUID → 400 not 500; no other behavior change; e5/sovereign/redaction logic untouched.

**Track A gate:** 26 pytest pass on hash backend; coverage ≥45%; all modules <800 LOC; cross-runtime dim guard green.

---

## Track B — .NET: extraction + hardening → training backend → integration tests

After each task: `dotnet build src/platform-api` (0 errors) and `dotnet test tests/PlatformApi.Tests` (38 pass).

### Task B1: Extract entity classes to `Models/`

**Files:**
- Create: `src/platform-api/Models/<Aggregate>.cs` (one file per aggregate: `Workspace.cs`, `Project.cs`, `AgentDefinition.cs`, `Dataset.cs`, `KnowledgeBase.cs`, `DocumentAsset.cs`, `DocumentChunk.cs`, `ApiKey.cs`, `AgentRun.cs`, `TraceRecord.cs`, `ModelProvider.cs`, `ModelRoute.cs`, training/eval entities, etc.)
- Modify: `src/platform-api/Data/AgentPortDbContext.cs` (keep `DbSet<>` declarations + `OnModelCreating` config only; remove inline entity class bodies)

- [ ] **Step 1:** Confirm baseline: `dotnet build src/platform-api` clean; `dotnet test tests/PlatformApi.Tests` → 38 passed.
- [ ] **Step 2:** Move each `public class <Entity>` body out of `AgentPortDbContext.cs` into `Models/<Aggregate>.cs` under namespace `AgentPort.PlatformApi.Models` (or the existing entity namespace — match what `AgentPortDbContext` currently uses so `using` stays minimal). Add the `using` to the context.
- [ ] **Step 3:** Confirm `AgentPortDbContext.cs` < 800 LOC; build clean; 38 tests pass; **no new migration is generated** (`dotnet ef migrations has-pending-model-changes` style check, or simply confirm the model snapshot is unchanged — moving class definitions must not alter the model).
- [ ] **Step 4:** Commit: `refactor(platform-api): extract EF entities into Models/`

### Task B2: Endpoint hardening (HARDENING — adversarial review)

**Files:** Modify `src/platform-api/Endpoints/Phase0Endpoints.cs` (+ Phase11/12/2 where the proxy/parse patterns recur), `src/platform-api/Program.cs` (logger already available via DI).

- [ ] **Step 1:** In `EnrichRunTraceAuthAsync` (`Phase0Endpoints.cs:~955`), wrap `JsonDocument.Parse(...)` in try/catch (`JsonException`) → log + skip enrichment (do not throw); the chat response already returned must be unaffected.
- [ ] **Step 2:** Inject `ILogger` into the endpoint groups and add structured logging on non-2xx ai-services proxy responses (chat + ingest) and on the JSON-parse failure path.
- [ ] **Step 3:** Build clean; 38 tests pass.
- [ ] **Step 4:** Commit: `fix(platform-api): guard trace-enrichment JSON parse, add structured logging`
- [ ] **Step 5 (REVIEW):** Adversarial review agent confirms: malformed ai-services body no longer throws; auth/scope/proxy behavior otherwise unchanged.

### Task B3: WS-1 training backend — carry `TargetColumn`

**Files:** Modify `src/platform-api/Contracts/Phase2Dtos.cs`, `src/platform-api/Endpoints/Phase2Endpoints.cs`, and the training-job→ai-services payload mapping; add a focused xUnit test in `tests/PlatformApi.Tests`.

- [ ] **Step 1: Write the failing test** — assert that a `CreateTrainingJobRequest` with `Kind="regression"`, `TargetColumn="price"` round-trips those values into the persisted job config / outbound payload (test the mapping function directly).

```csharp
[Fact]
public void TrainingJobConfig_Carries_Kind_And_TargetColumn()
{
    var req = new CreateTrainingJobRequest(/* ...existing required fields..., */ Kind: "regression", TargetColumn: "price");
    var cfg = TrainingJobMapping.ToConfig(req); // function under test
    cfg.Task.Should().Be("regression");
    cfg.TargetColumn.Should().Be("price");
}
```

- [ ] **Step 2:** Run → FAIL (no `TargetColumn` on the record / no mapping).
- [ ] **Step 3:** Add `string? TargetColumn` to `CreateTrainingJobRequest`; persist it on the training-job config; map `Kind`→`task` and `TargetColumn`→`target_column` in the ai-services `/v1/train` payload (matching `TrainRequest` fields `task`, `target_column`).
- [ ] **Step 4:** Run → PASS; build clean; full `dotnet test` → green.
- [ ] **Step 5:** Commit: `fix(platform-api): carry training Task/TargetColumn through to /v1/train`

### Task B4: WS-2 — Testcontainers integration tests

**Files:**
- Create: `tests/PlatformApi.IntegrationTests/PlatformApi.IntegrationTests.csproj` (net10.0; refs `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`, `xunit`, `FluentAssertions`), `tests/PlatformApi.IntegrationTests/AuthFloorTests.cs`, `tests/PlatformApi.IntegrationTests/PostgresFixture.cs`
- Modify: `src/platform-api/Program.cs` (add `public partial class Program {}` at end so `WebApplicationFactory<Program>` works), `.github/workflows/ci.yml` (run integration tests in the .NET job)

- [ ] **Step 1:** `PostgresFixture` starts `new PostgreSqlBuilder().WithImage("pgvector/pgvector:pg16").Build()`, exposes the connection string; `WebApplicationFactory<Program>` overrides `ConnectionStrings:DefaultConnection` to it and sets `ASPNETCORE_ENVIRONMENT=Development` WITHOUT `AGENTPORT_ALLOW_DEV_ENDPOINTS` for the gating test.
- [ ] **Step 2: Write failing tests** in `AuthFloorTests.cs`:

```csharp
[Fact] public async Task Mutation_Without_Key_Returns_401() { /* POST /api/v1/datasets, no header → 401 */ }
[Fact] public async Task Wrong_Scope_Returns_403() { /* valid key lacking datasets:write → 403 */ }
[Fact] public async Task Workspace_Mismatch_Returns_403() { /* key for ws A, body workspaceId B → 403 */ }
[Fact] public async Task DevReset_Returns_404_When_Flag_Unset() { /* POST /api/v1/dev/reset → 404 */ }
[Fact] public async Task BootstrapLocal_Returns_404_When_Flag_Unset() { /* POST /api/v1/bootstrap/local → 404 */ }
[Fact] public async Task Valid_Scoped_Key_Returns_2xx() { /* with AGENTPORT_ALLOW_DEV_ENDPOINTS=true factory: bootstrap → use returned key → 2xx */ }
[Fact] public async Task Bootstrap_Key_Is_Random_And_Idempotent() { /* two bootstraps: key != known constant; second is idempotent (no dup) */ }
```

- [ ] **Step 3:** Run → most FAIL until wired; implement the fixture + factory + request helpers.
- [ ] **Step 4:** `dotnet test tests/PlatformApi.IntegrationTests` → all green (requires Docker running).
- [ ] **Step 5:** Add an integration step to the `.NET` CI job (Docker is available on GitHub-hosted runners) running `dotnet test tests/PlatformApi.IntegrationTests`.
- [ ] **Step 6:** Commit: `test(platform-api): Testcontainers auth-floor integration tests + CI wiring`

**Track B gate:** `dotnet build` clean; `dotnet test` (unit + integration) green; `AgentPortDbContext.cs` <800 LOC.

---

## Track C — Web: split monolith → i18n → Playwright

Web gate per stage: `cd src/web && npm run build && npm run lint` (typecheck runs in build). Final stage adds Playwright.

### Task C1: Extract fetch helpers + types to `lib/`

**Files:** Create `src/web/lib/api.ts` (the `requestOptionalJson`/`requestFirstAvailableJson`/`requestOptionalCollection`/`isPendingEndpoint` helpers), `src/web/lib/types.ts` (the domain interfaces), `src/web/lib/workspace-context.tsx` (shared workspace state provider extracted from the component's `useState` cluster). Modify `src/web/app/operator-dashboard-client.tsx` to import from `lib/`.

- [ ] **Step 1:** Baseline: `cd src/web && npm run build` succeeds.
- [ ] **Step 2:** Move the fetch helpers + types verbatim into `lib/`; re-import in the client. No behavior change.
- [ ] **Step 3:** `npm run build && npm run lint` clean.
- [ ] **Step 4:** Commit: `refactor(web): extract fetch helpers and types to lib/`

### Task C2: Extract section views to `components/`

**Files:** Create `src/web/components/<View>.tsx` for each section: `OverviewView`, `DatasetsView`, `PlaygroundView`, `TracesView`, `TrainingView`, `ModelCatalogView`, `DeploymentsView`, `SettingsView`, `EvalsView`, `WalletView`, `StackChooserView`. Modify `operator-dashboard-client.tsx` to a thin shell that renders the active view from `renderMainView()`.

- [ ] **Step 1:** Extract each section's JSX + its local handlers into its own component, receiving workspace state via the `lib/workspace-context` provider (no prop-drilling regressions). Keep each `<View>.tsx` focused (<800 LOC; most far smaller).
- [ ] **Step 2:** Confirm `operator-dashboard-client.tsx` < 800 LOC.
- [ ] **Step 3:** `npm run build && npm run lint` clean.
- [ ] **Step 4:** Commit: `refactor(web): split operator dashboard into per-section components`

### Task C3: WS-1 training-form fix (folded into TrainingView)

**Files:** Modify `src/web/components/TrainingView.tsx`.

- [ ] **Step 1:** In the training submit handler, replace `kind: "classification"` with `kind: trainingTask` and add `targetColumn: trainingTargetColumn` to the POST `/api/platform/training-jobs` payload (the state + selectors already exist).
- [ ] **Step 2:** `npm run build && npm run lint` clean. (End-to-end behavior verified later by Playwright + the B3 backend test.)
- [ ] **Step 3:** Commit: `fix(web): training form submits selected task + target column`

### Task C4: i18n infrastructure (next-intl, no routing)

**Files:**
- Create: `src/web/i18n/request.ts`, `src/web/middleware.ts`, `src/web/messages/en.json`, `src/web/messages/tr.json`, `src/web/components/LocaleToggle.tsx`
- Modify: `src/web/next.config.ts` (wrap with `createNextIntlPlugin('./i18n/request.ts')`), `src/web/app/layout.tsx` (`NextIntlClientProvider`, dynamic `lang`), `src/web/package.json` (`next-intl`)

- [ ] **Step 1:** Add `next-intl` to deps. `middleware.ts` resolves locale: read `NEXT_LOCALE` cookie; else parse `Accept-Language` — if Turkish is preferred → `tr`, else `en`. `i18n/request.ts` (`getRequestConfig`) loads `messages/${locale}.json` and returns `{ locale, messages }`.

```ts
// middleware.ts (sketch — no [locale] routing; sets a request-scoped locale)
import { NextRequest, NextResponse } from 'next/server';
const SUPPORTED = ['en', 'tr'] as const;
function negotiate(req: NextRequest): string {
  const cookie = req.cookies.get('NEXT_LOCALE')?.value;
  if (cookie && SUPPORTED.includes(cookie as any)) return cookie;
  const al = req.headers.get('accept-language') ?? '';
  return /\btr\b/i.test(al.split(',')[0] ?? '') ? 'tr' : 'en';
}
export function middleware(req: NextRequest) {
  const res = NextResponse.next();
  if (!req.cookies.get('NEXT_LOCALE')) res.cookies.set('NEXT_LOCALE', negotiate(req), { path: '/' });
  return res;
}
export const config = { matcher: ['/((?!_next|.*\\..*).*)'] };
```

- [ ] **Step 2:** `layout.tsx`: read locale via `getLocale()`, set `<html lang={locale}>`, wrap children in `NextIntlClientProvider` with messages.
- [ ] **Step 3:** Seed `messages/en.json` + `messages/tr.json` with a small shared namespace (nav + common buttons) to prove wiring. `LocaleToggle.tsx` sets the `NEXT_LOCALE` cookie and calls `router.refresh()`.
- [ ] **Step 4:** `npm run build && npm run lint` clean.
- [ ] **Step 5:** Commit: `feat(web): next-intl locale negotiation (browser tr/en) + toggle`

### Task C5: Translate all operator-console strings (full coverage)

**Files:** Modify every `src/web/components/<View>.tsx` + nav; extend `messages/{en,tr}.json`.

- [ ] **Step 1:** Replace hardcoded strings across all views with `useTranslations(namespace)` `t()` calls; add matching keys to both `en.json` and `tr.json` (full-console coverage). Include DefterPort copy: citation label `Kaynak`, the no-answer message (`Yüklenen belgelerde bu soruyu yanıtlamak için yeterli bilgi bulamadım.`).
- [ ] **Step 2:** Verify no literal user-facing English remains in the views (grep for stray quotes in JSX text); both message files have identical key sets.
- [ ] **Step 3:** `npm run build && npm run lint` clean.
- [ ] **Step 4:** Commit: `feat(web): full operator-console Turkish/English localization`

### Task C6: Playwright smoke (web track gate)

**Files:**
- Create: `src/web/playwright.config.ts`, `src/web/e2e/smoke.spec.ts`
- Modify: `src/web/package.json` (`@playwright/test`, `"test:e2e": "playwright test"`), `.github/workflows/ci.yml` (web e2e job: docker compose up the stack with `EMBEDDING_BACKEND=hash`, then `npm run test:e2e`)

- [ ] **Step 1:** `playwright.config.ts` targets `baseURL` from env (default `http://127.0.0.1:3002`), Chromium project, trace on first retry.
- [ ] **Step 2:** `e2e/smoke.spec.ts`:

```ts
import { test, expect } from '@playwright/test';
test('operator smoke: onboard → cited answer → trace', async ({ page }) => {
  await page.goto('/');
  // run onboarding/bootstrap, open Playground, ask the seeded question, assert a citation renders
  await expect(page.getByRole('heading')).toBeVisible();
  // ... assert a citation chip and a trace row appear
});
test('locale: Accept-Language tr yields Turkish UI', async ({ browser }) => {
  const ctx = await browser.newContext({ locale: 'tr-TR', extraHTTPHeaders: { 'Accept-Language': 'tr' } });
  const page = await ctx.newPage();
  await page.goto('/');
  await expect(page.locator('html')).toHaveAttribute('lang', 'tr');
});
test('locale: default yields English', async ({ page }) => {
  await page.goto('/');
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
});
```

- [ ] **Step 3:** Run locally against the dev stack on the hash backend if available; otherwise validate config + spec compile (`npx playwright test --list`).
- [ ] **Step 4:** Add the CI web-e2e job (compose up stack with `EMBEDDING_BACKEND=hash`, run `test:e2e`).
- [ ] **Step 5:** Commit: `test(web): Playwright smoke + locale checks + CI e2e job`

**Track C gate:** `next build` + `next lint` + typecheck pass; Playwright smoke green (or config/spec validated where no live stack); `operator-dashboard-client.tsx` + each view <800 LOC.

---

## Consolidated verification (after all tracks)

- [ ] Python: `cd src/ai-services && EMBEDDING_BACKEND=hash PYTHONPATH=. python3 -m pytest tests/ -q` → green incl. cross-runtime dim guard.
- [ ] .NET: `dotnet build src/platform-api` clean; `dotnet test tests/PlatformApi.Tests` + `dotnet test tests/PlatformApi.IntegrationTests` green.
- [ ] Web: `cd src/web && npm run build && npm run lint`; `npx playwright test --list` (or full run on live stack).
- [ ] Compose unchanged: `docker compose -f docker-compose.prod.yml --env-file .env.prod.example config -q` and dev `docker compose config -q` both parse.
- [ ] No behavior-frozen contract changed: grep confirms `EMBEDDING_DIM_E5=768`, `DEFAULT_RAG_SCORE_THRESHOLD` default, sovereign gate, `vector(768)` snapshot intact.

## Acceptance criteria (from spec §8)

1. Three monoliths split into focused files <~800 LOC; no behavior change beyond scoped hardening.
2. Python 26 + .NET unit/integration + web build/lint + Playwright all green.
3. Training "Regression" creates a regression job end-to-end (web → .NET `TargetColumn` → ai-services `/v1/train`).
4. Integration tests assert the 401/403/404 + random-bootstrap-key matrix on real Postgres.
5. Console renders Turkish when the browser prefers Turkish, English otherwise; toggle overrides + persists; `Kaynak`/no-answer localized.
6. CI runs .NET (unit+integration), Python (coverage), web (build/lint + Playwright).
7. Earlier auth/embedding/sovereign/KVKK behavior unchanged.
