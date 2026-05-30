# AgentPort Phase 1.1 Quickstart

Phase 1.1 is the local document-QA demo path: reset a deterministic workspace, ingest a support document, ask the agent through the Platform API, inspect traces, and verify widget/API integration gates.

## Prerequisites

- Docker with Docker Compose
- tmux
- .NET 10 SDK
- Node.js 20+
- Python 3.11+
- curl and jq

## Start The Stack

```bash
scripts/dev-up.sh
```

Default local URLs:

- Web: `http://127.0.0.1:3002`
- Platform API: `http://localhost:5001`
- AI Services: `http://localhost:5002`
- PostgreSQL: `localhost:55432`
- MinIO: `http://localhost:59000`, console `http://localhost:59001`

## Reset And Seed

Run the reset before demos or smoke checks:

```bash
RESET_JSON="$(scripts/dev-reset.sh)"
API_KEY="$(jq -r '.apiKey' <<<"$RESET_JSON")"
AGENT_ID="$(jq -r '.agentDefinitionId' <<<"$RESET_JSON")"
DATASET_ID="$(jq -r '.datasetId' <<<"$RESET_JSON")"
DOCUMENT_ID="$(jq -r '.documentAssetId' <<<"$RESET_JSON")"
```

`scripts/dev-reset.sh` clears the local bootstrap workspace, owner user, cascaded demo rows, and MinIO bucket objects, then calls `POST /api/v1/bootstrap/local` and uploads `examples/phase1/support-policy.md`.

Useful variants:

```bash
scripts/dev-reset.sh --no-seed
scripts/dev-reset.sh --keep-minio
SAMPLE_FILE=examples/phase1/changed-support-policy.md scripts/dev-reset.sh
```

## API Key Chat

The bootstrap response includes the raw local API key only immediately after reset. Use it as `x-agentport-api-key` or as a bearer token:

```bash
curl -fsS -X POST "http://localhost:5001/api/v1/agent-definitions/$AGENT_ID/chat" \
  -H "content-type: application/json" \
  -H "x-agentport-api-key: $API_KEY" \
  -d @examples/phase1/api-chat.json | jq .
```

Expected result: a cited refund answer, a run id, and a trace id. Invalid keys should return `401`; keys without `runs:write` or `workspace:admin` should return `403`. If missing keys still succeed, that is a Phase 1.1 enforcement gap and `scripts/smoke.sh` reports it as a warning unless `STRICT_PHASE11=1`.

## Widget

Open `http://127.0.0.1:3002/deployments` after onboarding/reset. The local widget target is:

```html
<iframe
  src="http://127.0.0.1:3002/playground?agentId=AGENT_ID&mode=embedded&theme=system"
  width="420"
  height="640"
></iframe>
```

Do not place a long-lived admin key in browser code. The Phase 1.1 contract expects a scoped widget/session token, allowed-origin config, and rate-limit placeholders. Until that token endpoint is wired, use the iframe only for local demos and keep API-key calls server-side.

## Delete And Reingest

Current reset is the deterministic cleanup path. When the document lifecycle API is available, the intended local workflow is:

```bash
curl -fsS "http://localhost:5001/api/v1/datasets/$DATASET_ID/documents" | jq .

curl -fsS -X DELETE \
  "http://localhost:5001/api/v1/datasets/$DATASET_ID/documents/$DOCUMENT_ID" | jq .

curl -fsS -X POST "http://localhost:5001/api/v1/datasets/$DATASET_ID/documents" \
  -F "file=@examples/phase1/changed-support-policy.md;type=text/markdown" | jq .
```

Unchanged uploads should be skipped by content hash/idempotency key. Changed content should archive or replace the prior document chunks so deleted documents are excluded from retrieval. If these endpoints return `404` or `405`, use `scripts/dev-reset.sh` for now; smoke records the API gap.

## No-Answer Behavior

Use a high score threshold to force the no-answer path:

```bash
curl -fsS -X POST "http://localhost:5001/api/v1/agent-definitions/$AGENT_ID/chat" \
  -H "content-type: application/json" \
  -H "x-agentport-api-key: $API_KEY" \
  -d @examples/phase1/no-answer-chat.json | jq .
```

Expected result: no citations, no-answer metadata, and a clear fallback answer when the best retrieved score is below threshold. If the Platform API does not pass score-threshold fields through yet, the web playground can still display a local no-answer state from retrieved scores, and smoke reports the backend gap.

## Smoke

```bash
scripts/smoke.sh
```

Smoke resets and reseeds by default, then checks health, bootstrap, upload idempotency, valid/invalid/limited API-key behavior, cited answers, trace detail, delete/reingest endpoint presence, no-answer behavior, widget route loading, and database extensions.

Useful variants:

```bash
SMOKE_RESET=0 AGENTPORT_API_KEY="$API_KEY" scripts/smoke.sh
STRICT_PHASE11=1 scripts/smoke.sh
```

## Stop The Stack

```bash
scripts/dev-down.sh
```
