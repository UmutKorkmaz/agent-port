# Phase 1.1 Success Criteria

Phase 1.1 succeeds when the Phase 1 document-QA MVP is repeatable, testable, and safe enough for local demos plus API/widget integration.

## Executable Gate

Run:

```bash
scripts/dev-up.sh
scripts/smoke.sh
```

For CI or release gating, run:

```bash
STRICT_PHASE11=1 scripts/smoke.sh
```

Default smoke mode fails on broken core demo behavior and warns on backend/API gates that are still being wired. `STRICT_PHASE11=1` turns those warnings into failures.

## Criteria

1. **Dev reset is deterministic.** `scripts/dev-reset.sh` clears the local bootstrap workspace, owner user, cascaded demo data, and MinIO demo objects, then recreates the same workspace/project/agent/dataset seed through the running Platform API.
2. **Seed output is usable.** Reset prints JSON with workspace, project, agent, dataset, knowledge base, wallet placeholder, raw local API key, document id, ingestion job id, content hash, idempotency key, and chunk count.
3. **Document lifecycle is explicit.** Users can list documents, delete/archive a document, reingest changed content, and skip unchanged uploads by hash/idempotency key. Until the Platform API exposes every endpoint, reset is the deterministic cleanup fallback and smoke reports missing endpoints.
4. **API access is scoped.** Valid local keys can chat, invalid keys return `401`, insufficient scopes return `403`, and missing keys return `401` once enforcement is complete.
5. **Widget access avoids long-lived browser keys.** The web Deployments view provides local API and iframe examples. Browser embeds should use scoped session-token flow plus allowed-origin and rate-limit placeholders, not admin keys.
6. **Provider response fields are normalized.** Chat responses expose fallback mode, route health, timeout/error fields, token/cost estimates, and provider metadata in one shape for local/Ollama/OpenAI-compatible paths.
7. **RAG quality is gated.** Known sample questions return cited answers. Low-score retrieval returns no-answer metadata and an empty-citation fallback. `topK`, score threshold, max score, and chunk metadata are visible in responses or trace metadata.
8. **Traceability is present.** Every successful chat returns run and trace ids, and `GET /api/v1/runs/{id}` plus `GET /api/v1/traces/{id}` return the stored records.
9. **Acceptance checks cover the local product slice.** Smoke covers reset, bootstrap, upload, idempotent reupload, document lifecycle endpoint presence, valid/invalid/limited API-key behavior, widget route loading, cited answer, no-answer probe, trace detail, and pgvector/pgcrypto/citext availability.
10. **Scope stays controlled.** Real payments, real training/fine-tuning, production SaaS hardening, external vector-store adapters, and public widget abuse controls stay out of Phase 1.1 except for placeholders and schema-safe extension points.

## Current Expected Warning Classes

These warnings are acceptable during source-level wiring but must be gone before strict Phase 1.1 signoff:

- Missing-key chat accepted instead of returning `401`.
- Document list/delete/reingest endpoints return `404` or `405`.
- Platform API does not pass `scoreThreshold` through to AI Services.
- Chat response omits normalized retrieval/provider metadata.
- Deployments HTML loads but does not include visible session-token/API-key guidance in the returned markup.
