# DefterPort Remaining Work — Design Spec

**Date:** 2026-06-13
**Branch:** `feat/defterport-pilot-hardening`
**Status:** Approved for planning
**Execution vehicle:** dynamic Workflow (parallel tracks, sequential stages, test-gated, adversarial-reviewed)

## 1. Goal

Complete the four deferred items from the AgentPort/DefterPort review without destabilizing the auth, embedding, and sovereign-mode code shipped earlier on this branch. All work is behavior-preserving except explicitly scoped hardening and the training-form correctness fix.

## 2. Scope

In scope (four independent workstreams):

- **WS-1 — Training-form correctness fix.** The operator UI already holds `trainingTask` / `trainingTargetColumn` state and selectors (`src/web/app/operator-dashboard-client.tsx:327-335`) but the submit handler hardcodes `kind: "classification"` (line 1301) and drops them. Python `/v1/train` already accepts `task` + `target_column` (`src/ai-services/main.py:263-264`). The `.NET CreateTrainingJobRequest` currently carries `Kind` but not `TargetColumn`.
- **WS-2 — Auth-floor integration tests.** Real end-to-end 401/403/404 coverage of the API-key auth handler, scope filter, dev-endpoint gating, and random bootstrap key shipped earlier on this branch.
- **WS-3 — Monolith refactors with targeted hardening.** Split `main.py` (2,277 LOC), `operator-dashboard-client.tsx` (2,792 LOC), and `AgentPortDbContext.cs` (1,506 LOC) under the 800-line rule, and fix review-flagged robustness gaps in the same pass.
- **WS-4 — Full operator-console i18n.** Browser-locale-negotiated Turkish/English with a manual TR/EN toggle.

Explicitly out of scope (cannot be completed here):

- **Real mevzuat content** — a licensing/legal decision; see `examples/defterport/seed-docs/SOURCES.md`. No tax-law content is fabricated.
- **p95 latency measurement** — only meaningful on real pilot hardware (~8 vCPU/32 GB CPU). The Playwright smoke verifies UI-flow wiring (with `EMBEDDING_BACKEND=hash` for speed/determinism), not retrieval quality or latency.

## 3. Orchestration strategy

File footprints partition into three disjoint trees: Python (`src/ai-services`), .NET (`src/platform-api`), Web (`src/web`). The **web tree is touched by three workstreams** (WS-1 web part, WS-3b split, WS-4 i18n), so it is internally sequenced.

```
Track A (Python) : 3a refactor+harden main.py ─────────────────────────────► [gate: pytest 26 green]
Track B (.NET)   : 3c DbContext/Models + endpoint harden + WS-1 backend ─► WS-2 integration tests ─► [gate: dotnet test]
Track C (Web)    : 3b split monolith (+ WS-1 web fix) ─► WS-4 i18n ─► Playwright smoke ─► [gate: next build+lint+typecheck+e2e]
```

- Tracks A / B / C run **in parallel**; stages **within** a track run **in sequence**.
- Each stage is **test-gated**; the workflow halts a track whose gate fails.
- Every **hardening diff** gets an **adversarial review** agent confirming no behavior change beyond what is intended.
- Rejected alternatives: fully sequential (safe, too slow); git-worktree-per-item (merge pain on the shared web file).

## 4. Track A — Python: `main.py` refactor + hardening

**Module split** (target < 800 LOC each):
`models.py` (Pydantic contracts), `embeddings.py` (e5 + hash fallback + prefixing + dim guard), `chunking.py`, `retrieval.py`, `providers/` (`ollama.py`, `openai_compatible.py`, `routing.py`, sovereign gate), `ingestion.py`, `persistence/db.py`, `training.py`, and a thin `api.py` wiring FastAPI routes. Preserve all public env flags and behavior (sovereign, redaction, e5, threshold) exactly.

**Targeted hardening:**
- Replace per-call `db_connect()` with a `psycopg_pool.ConnectionPool` reused per request.
- Wrap `save_run_trace` in try/except so a trace-write failure logs but does not fail an already-computed chat answer.
- Introduce stdlib `logging` (structured) for provider errors, DB failures, and the embedding-fallback path.
- Guard `UUID(...)` parsing on `agent_id` / `knowledge_base_id` → HTTP 400 instead of unhandled 500.
- Remove the dead `fallback_mode == "ollama"` no-answer branch.

**Gate:** the 26 pytest tests stay green; `python -m py_compile` clean; coverage ≥ 45% floor holds.

## 5. Track B — .NET: extraction + hardening → integration tests

**3c — DbContext/Models extraction + endpoint hardening:**
- Move the 40+ entity classes out of `AgentPortDbContext.cs` into `src/platform-api/Models/` (one file per aggregate). The context keeps `DbSet`s + `OnModelCreating` config only.
- Add `ILogger` structured logging to endpoints; guard `JsonDocument.Parse` in `EnrichRunTraceAuthAsync` (`Phase0Endpoints.cs:~955`) with try/catch; log non-2xx ai-services proxy responses.

**WS-1 backend:** add `TargetColumn` (and confirm `Task`/`Kind`) to `CreateTrainingJobRequest` + the training-job config/persistence, flowing through to the ai-services `/v1/train` payload (`task`, `target_column`).

**WS-2 — integration tests** (`tests/PlatformApi.IntegrationTests/`):
- WebApplicationFactory + Testcontainers (`pgvector/pgvector:pg16`); requires `public partial class Program {}` in the API.
- Assertions: 401 (missing/invalid key), 403 (wrong scope), 403 (workspace mismatch), 404 (`/dev/reset` + `/bootstrap/local` when `AGENTPORT_ALLOW_DEV_ENDPOINTS` unset), 200 (valid scoped key), bootstrap returns a cryptographically random key and is idempotent.
- Add a Testcontainers-enabled integration step to the `.NET` job in `.github/workflows/ci.yml`.

**Gate:** `dotnet build` clean; `dotnet test` (unit + integration) green.

## 6. Track C — Web: split → i18n → Playwright

**3b — split `operator-dashboard-client.tsx`:** extract each section into `src/web/components/` (`OverviewView`, `DatasetsView`, `PlaygroundView`, `TracesView`, `TrainingView`, `ModelCatalogView`, `DeploymentsView`, `SettingsView`, …); move fetch helpers + domain types into `src/web/lib/`; lift shared workspace state into a context/provider. **WS-1 web fix is folded into `TrainingView` extraction:** submit `kind: trainingTask` + `targetColumn: trainingTargetColumn`.

**WS-4 — i18n (after the split):** next-intl in **"without i18n routing"** mode (no route restructuring):
- `src/web/middleware.ts` negotiates locale from `Accept-Language` — Turkish preference → `tr`, otherwise `en` — with a cookie (`NEXT_LOCALE`) override that the toggle writes.
- `getRequestConfig` + `NextIntlClientProvider` in `layout.tsx`; dynamic `<html lang>`.
- `src/web/messages/en.json` + `src/web/messages/tr.json`; replace hardcoded strings with `t()` across all extracted components (full-console coverage).
- A TR/EN toggle component that sets the cookie and refreshes.
- DefterPort copy: citation label `Kaynak`, the no-answer message, and key SMMM-facing strings.
- `next-intl` added to `package.json`.

**Playwright smoke** (`src/web` — the web track's real gate, closing the Phase-1.1 gap):
- Add `@playwright/test` + `playwright.config.ts`.
- Smoke journey against a live stack with `EMBEDDING_BACKEND=hash` (deterministic, no model download): bootstrap/onboarding → seed a dataset (or use the bootstrap-seeded one) → playground returns a cited answer → trace detail renders. Plus a locale check: `Accept-Language: tr` yields Turkish UI, default/`en` yields English.
- Add a web e2e job to `.github/workflows/ci.yml` (docker compose up the stack, then `playwright test`).

**Gate:** `next build` + `next lint` + typecheck + Playwright smoke pass.

## 7. Testing, gates & risk management

- Per-stage gates as above; a track halts on gate failure.
- Adversarial-review agent per hardening diff (Python pool/trace/logging, .NET logging/parse-guard) asserts behavior preservation.
- Final consolidated verification: all suites green, both compose files still parse, cross-runtime dim guard test still green, prod compose unaffected.

**Risks & mitigations:**
- Refactor + hardening entangles structure and behavior → test gates + adversarial review per diff; sovereign/redaction/e5/auth semantics treated as frozen contracts.
- Large web refactor + i18n with thin coverage → Playwright smoke as the safety net (now in scope).
- Testcontainers needs Docker in CI → added to the existing `.NET` CI job; GitHub Actions provides Docker.
- e5 model weight in CI/e2e → smoke runs on the hash backend to stay fast and deterministic.

## 8. Acceptance criteria

1. `main.py`, `operator-dashboard-client.tsx`, and `AgentPortDbContext.cs` each split into focused modules/files under ~800 LOC; no public behavior change beyond the scoped hardening.
2. Python 26 tests green; .NET unit + new integration tests green; `next build`/lint/typecheck + Playwright smoke green.
3. Training form: selecting "Regression" creates a regression job (web sends `kind`+`targetColumn`; .NET carries `TargetColumn`; reaches ai-services `/v1/train`).
4. Integration tests assert the full 401/403/404 + random-bootstrap-key matrix against a real Postgres.
5. Operator console renders Turkish when the browser prefers Turkish and English otherwise; the TR/EN toggle overrides and persists; `Kaynak`/no-answer copy localized.
6. CI runs .NET (unit+integration), Python (coverage), web (build/lint + Playwright).
7. The earlier-shipped auth/embedding/sovereign/KVKK behavior is unchanged (existing tests + cross-runtime dim guard remain green).

## 9. Execution

After spec review: invoke the writing-plans skill to produce the implementation plan, then execute the three tracks as a single dynamic Workflow — parallel tracks, sequential stages, test-gated, adversarial-reviewed — followed by a consolidated verification and logical commits.
