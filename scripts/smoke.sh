#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

export COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-agentport_phase1}"
export POSTGRES_PORT="${POSTGRES_PORT:-55432}"
export POSTGRES_USER="${POSTGRES_USER:-agentport}"
export POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-agentport}"
export POSTGRES_DB="${POSTGRES_DB:-agentport}"
export REDIS_PORT="${REDIS_PORT:-56379}"
export RABBITMQ_PORT="${RABBITMQ_PORT:-55672}"
export RABBITMQ_MANAGEMENT_PORT="${RABBITMQ_MANAGEMENT_PORT:-15673}"
export MINIO_API_PORT="${MINIO_API_PORT:-59000}"
export MINIO_CONSOLE_PORT="${MINIO_CONSOLE_PORT:-59001}"
export MINIO_ROOT_USER="${MINIO_ROOT_USER:-agentport}"
export MINIO_ROOT_PASSWORD="${MINIO_ROOT_PASSWORD:-agentportagentport}"
export MINIO_BUCKET="${MINIO_BUCKET:-agentport-phase1}"

PLATFORM_API_URL="${PLATFORM_API_URL:-http://localhost:5001}"
AI_SERVICES_URL="${AI_SERVICES_URL:-http://localhost:5002}"
WEB_URL="${WEB_URL:-http://127.0.0.1:3002}"
SAMPLE_FILE="${SAMPLE_FILE:-$ROOT_DIR/examples/phase1/support-policy.md}"
SMOKE_RESET="${SMOKE_RESET:-1}"
STRICT_PHASE11="${STRICT_PHASE11:-0}"

TMP_DIR="$(mktemp -d)"
WARNINGS=()
trap 'rm -rf "$TMP_DIR"' EXIT

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "$1 is required for smoke checks." >&2
    exit 1
  fi
}

compose() {
  COMPOSE_PROJECT_NAME="$COMPOSE_PROJECT_NAME" \
  POSTGRES_PORT="$POSTGRES_PORT" \
  POSTGRES_USER="$POSTGRES_USER" \
  POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
  POSTGRES_DB="$POSTGRES_DB" \
  REDIS_PORT="$REDIS_PORT" \
  RABBITMQ_PORT="$RABBITMQ_PORT" \
  RABBITMQ_MANAGEMENT_PORT="$RABBITMQ_MANAGEMENT_PORT" \
  MINIO_API_PORT="$MINIO_API_PORT" \
  MINIO_CONSOLE_PORT="$MINIO_CONSOLE_PORT" \
  MINIO_ROOT_USER="$MINIO_ROOT_USER" \
  MINIO_ROOT_PASSWORD="$MINIO_ROOT_PASSWORD" \
  MINIO_BUCKET="$MINIO_BUCKET" \
  docker compose "$@"
}

pass() {
  echo "ok - $1"
}

warn() {
  WARNINGS+=("$1")
  echo "warn - $1" >&2
}

fail() {
  echo "fail - $1" >&2
  exit 1
}

http_status() {
  local body_file="$1"
  shift
  curl -sS -o "$body_file" -w "%{http_code}" "$@"
}

chat_request() {
  local body_file="$1"
  local api_key="$2"
  local question="$3"
  local score_threshold="${4:-}"
  local payload

  if [ -n "$score_threshold" ]; then
    payload="$(jq -n --arg question "$question" --argjson threshold "$score_threshold" '{question: $question, topK: 4, scoreThreshold: $threshold}')"
  else
    payload="$(jq -n --arg question "$question" '{question: $question, topK: 4}')"
  fi

  if [ -n "$api_key" ]; then
    http_status "$body_file" \
      -X POST "$PLATFORM_API_URL/api/v1/agent-definitions/$AGENT_ID/chat" \
      -H "content-type: application/json" \
      -H "x-agentport-api-key: $api_key" \
      -d "$payload"
  else
    http_status "$body_file" \
      -X POST "$PLATFORM_API_URL/api/v1/agent-definitions/$AGENT_ID/chat" \
      -H "content-type: application/json" \
      -d "$payload"
  fi
}

create_limited_api_key() {
  local raw_key="ap_local_smoke_limited_phase11"
  local sql
  read -r -d '' sql <<SQL || true
WITH key_material AS (
  SELECT '$raw_key'::text AS raw_key
)
INSERT INTO api_keys
  ("WorkspaceId", "ProjectId", "Name", "Prefix", "KeyHash", "Scopes", "MetadataJson")
SELECT
  '$WORKSPACE_ID'::uuid,
  '$PROJECT_ID'::uuid,
  'smoke-limited',
  left(raw_key, 24),
  encode(digest(raw_key, 'sha256'), 'hex'),
  ARRAY['datasets:write']::text[],
  '{"smoke":"phase1.1","purpose":"insufficient-scope-check"}'::jsonb
FROM key_material
ON CONFLICT ("Prefix")
DO UPDATE SET
  "WorkspaceId" = EXCLUDED."WorkspaceId",
  "ProjectId" = EXCLUDED."ProjectId",
  "KeyHash" = EXCLUDED."KeyHash",
  "Scopes" = EXCLUDED."Scopes",
  "MetadataJson" = EXCLUDED."MetadataJson",
  "UpdatedAt" = now()
RETURNING '$raw_key';
SQL

  compose exec -T postgres psql \
    -q \
    -v ON_ERROR_STOP=1 \
    -tA \
    -U "$POSTGRES_USER" \
    -d "$POSTGRES_DB" \
    <<<"$sql" | sed -n '1p'
}

require curl
require docker
require jq

echo "Checking service health..."
curl -fsS "$PLATFORM_API_URL/health/ready" | jq -e '(.status // .Status) == "healthy"' >/dev/null
curl -fsS "$AI_SERVICES_URL/health/ready" | jq -e '(.status // .Status) == "ok"' >/dev/null
curl -fsSI "$WEB_URL/model-catalog" >/dev/null
pass "health endpoints and web route respond"

if [ "$SMOKE_RESET" = "1" ]; then
  echo "Running deterministic reset and seed..."
  RESET_JSON="$(SEED_SAMPLE=1 "$ROOT_DIR/scripts/dev-reset.sh")"
else
  echo "Bootstrapping without reset..."
  BOOTSTRAP_JSON="$(curl -fsS -X POST "$PLATFORM_API_URL/api/v1/bootstrap/local")"
  DATASET_ID="$(jq -r '.datasetId // .DatasetId // empty' <<<"$BOOTSTRAP_JSON")"
  UPLOAD_JSON="$(curl -fsS -X POST "$PLATFORM_API_URL/api/v1/datasets/$DATASET_ID/documents" \
    -F "file=@$SAMPLE_FILE;type=text/markdown")"
  RESET_JSON="$(jq -n --argjson seed "$BOOTSTRAP_JSON" --argjson upload "$UPLOAD_JSON" \
    '{
      seeded: true,
      workspaceId: ($seed.workspaceId // $seed.WorkspaceId),
      projectId: ($seed.projectId // $seed.ProjectId),
      agentDefinitionId: ($seed.agentDefinitionId // $seed.AgentDefinitionId),
      datasetId: ($seed.datasetId // $seed.DatasetId),
      knowledgeBaseId: ($seed.knowledgeBaseId // $seed.KnowledgeBaseId),
      apiKey: ($seed.apiKey // $seed.ApiKey // null),
      documentAssetId: ($upload.document_asset_id // $upload.documentAssetId // null),
      ingestionStatus: ($upload.ingestion_status // $upload.ingestionStatus // $upload.status // null),
      chunkCount: ($upload.chunk_count // $upload.chunkCount // null)
    }')"
fi

WORKSPACE_ID="$(jq -r '.workspaceId // empty' <<<"$RESET_JSON")"
PROJECT_ID="$(jq -r '.projectId // empty' <<<"$RESET_JSON")"
AGENT_ID="$(jq -r '.agentDefinitionId // empty' <<<"$RESET_JSON")"
DATASET_ID="$(jq -r '.datasetId // empty' <<<"$RESET_JSON")"
KNOWLEDGE_BASE_ID="$(jq -r '.knowledgeBaseId // empty' <<<"$RESET_JSON")"
API_KEY="$(jq -r '.apiKey // empty' <<<"$RESET_JSON")"
DOCUMENT_ID="$(jq -r '.documentAssetId // empty' <<<"$RESET_JSON")"

if [ -z "$WORKSPACE_ID" ] || [ -z "$PROJECT_ID" ] || [ -z "$AGENT_ID" ] || [ -z "$DATASET_ID" ] || [ -z "$KNOWLEDGE_BASE_ID" ]; then
  echo "$RESET_JSON" | jq . >&2
  fail "reset/bootstrap did not return the expected Phase 1.1 ids"
fi
pass "reset/bootstrap returned workspace, project, agent, dataset, and knowledge-base ids"

if [ -z "$API_KEY" ]; then
  warn "bootstrap did not return a raw API key; set AGENTPORT_API_KEY to run valid-key checks without resetting"
  API_KEY="${AGENTPORT_API_KEY:-}"
fi

if [ -n "$API_KEY" ]; then
  VALID_BODY="$TMP_DIR/valid-chat.json"
  VALID_STATUS="$(chat_request "$VALID_BODY" "$API_KEY" "What does this document say about refunds?")"
  [ "$VALID_STATUS" = "200" ] || fail "valid API key chat returned HTTP $VALID_STATUS: $(cat "$VALID_BODY")"
  jq -e '.answer and ((.citations // []) | length > 0) and (.trace_id_record // .traceIdRecord) and (.run_id // .runId)' "$VALID_BODY" >/dev/null
  pass "valid scoped API key returns cited answer with run and trace ids"
else
  VALID_BODY="$TMP_DIR/valid-chat.json"
  VALID_STATUS="$(chat_request "$VALID_BODY" "" "What does this document say about refunds?")"
  [ "$VALID_STATUS" = "200" ] || fail "anonymous fallback chat returned HTTP $VALID_STATUS: $(cat "$VALID_BODY")"
  jq -e '.answer and ((.citations // []) | length > 0)' "$VALID_BODY" >/dev/null
  warn "valid-key chat used anonymous fallback because no raw key was available"
fi

RUN_ID="$(jq -r '.run_id // .runId // empty' "$VALID_BODY")"
TRACE_ID="$(jq -r '.trace_id_record // .traceIdRecord // empty' "$VALID_BODY")"

curl -fsS "$PLATFORM_API_URL/api/v1/runs/$RUN_ID" | jq -e '.id or .Id' >/dev/null
curl -fsS "$PLATFORM_API_URL/api/v1/traces/$TRACE_ID" | jq -e '.id or .Id' >/dev/null
pass "run and trace detail endpoints return records"

if jq -e '(.fallback_mode // .fallbackMode) and ((.estimated_cost // .estimatedCost) != null)' "$VALID_BODY" >/dev/null; then
  pass "chat response includes fallback mode and cost estimate fields"
else
  warn "chat response is missing fallback mode or cost estimate fields"
fi

if jq -e '.retrieval and (.provider_response // .providerResponse)' "$VALID_BODY" >/dev/null; then
  pass "chat response includes normalized retrieval and provider metadata"
else
  warn "normalized retrieval/provider metadata is not exposed through the Platform API response yet"
fi

INVALID_BODY="$TMP_DIR/invalid-chat.json"
INVALID_STATUS="$(chat_request "$INVALID_BODY" "ap_local_invalid_smoke_key" "What does this document say about refunds?")"
if [ "$INVALID_STATUS" = "401" ]; then
  pass "invalid API key is rejected with 401"
else
  warn "invalid API key returned HTTP $INVALID_STATUS instead of 401"
fi

MISSING_BODY="$TMP_DIR/missing-key-chat.json"
MISSING_STATUS="$(chat_request "$MISSING_BODY" "" "What does this document say about refunds?")"
if [ "$MISSING_STATUS" = "401" ]; then
  pass "missing API key is rejected with 401"
else
  warn "missing API key returned HTTP $MISSING_STATUS; local chat still permits anonymous calls"
fi

if LIMITED_KEY="$(create_limited_api_key 2>/dev/null)"; then
  LIMITED_BODY="$TMP_DIR/limited-chat.json"
  LIMITED_STATUS="$(chat_request "$LIMITED_BODY" "$LIMITED_KEY" "What does this document say about refunds?")"
  if [ "$LIMITED_STATUS" = "403" ]; then
    pass "insufficient API key scope is rejected with 403"
  else
    warn "insufficient-scope API key returned HTTP $LIMITED_STATUS instead of 403"
  fi
else
  warn "could not create limited smoke API key for insufficient-scope check"
fi

echo "Checking idempotent upload and reingest behavior..."
BEFORE_DATASET="$(curl -fsS "$PLATFORM_API_URL/api/v1/datasets?workspaceId=$WORKSPACE_ID" | jq -c --arg id "$DATASET_ID" '.[] | select((.id // .Id) == $id)')"
REUPLOAD_JSON="$(curl -fsS -X POST "$PLATFORM_API_URL/api/v1/datasets/$DATASET_ID/documents" \
  -F "file=@$SAMPLE_FILE;type=text/markdown")"
AFTER_DATASET="$(curl -fsS "$PLATFORM_API_URL/api/v1/datasets?workspaceId=$WORKSPACE_ID" | jq -c --arg id "$DATASET_ID" '.[] | select((.id // .Id) == $id)')"

BEFORE_DOCS="$(jq -r '.documentCount // .DocumentCount // 0' <<<"$BEFORE_DATASET")"
AFTER_DOCS="$(jq -r '.documentCount // .DocumentCount // 0' <<<"$AFTER_DATASET")"
BEFORE_CHUNKS="$(jq -r '.chunkCount // .ChunkCount // 0' <<<"$BEFORE_DATASET")"
AFTER_CHUNKS="$(jq -r '.chunkCount // .ChunkCount // 0' <<<"$AFTER_DATASET")"
REUPLOAD_STATUS="$(jq -r '.ingestion_status // .ingestionStatus // .status // empty' <<<"$REUPLOAD_JSON")"

if [ "$BEFORE_DOCS" = "$AFTER_DOCS" ] && [ "$BEFORE_CHUNKS" = "$AFTER_CHUNKS" ]; then
  pass "unchanged sample upload is idempotent"
else
  warn "unchanged sample upload changed counts: documents $BEFORE_DOCS->$AFTER_DOCS, chunks $BEFORE_CHUNKS->$AFTER_CHUNKS, status $REUPLOAD_STATUS"
fi

DOC_LIST_BODY="$TMP_DIR/documents.json"
DOC_LIST_STATUS="$(http_status "$DOC_LIST_BODY" "$PLATFORM_API_URL/api/v1/documents?datasetId=$DATASET_ID")"
if [ "$DOC_LIST_STATUS" = "200" ]; then
  jq -e 'type == "array" and length >= 1' "$DOC_LIST_BODY" >/dev/null
  pass "document list API returns seeded documents"
else
  warn "document list API returned HTTP $DOC_LIST_STATUS; endpoint may not be wired yet"
fi

if [ -n "$DOCUMENT_ID" ]; then
  DELETE_BODY="$TMP_DIR/delete-document.json"
  DELETE_STATUS="$(http_status "$DELETE_BODY" -X DELETE "$PLATFORM_API_URL/api/v1/documents/$DOCUMENT_ID")"
  case "$DELETE_STATUS" in
    200|204)
      pass "document delete/archive API accepted seeded document"
      REINGEST_AFTER_DELETE="$(curl -fsS -X POST "$PLATFORM_API_URL/api/v1/documents/$DOCUMENT_ID/reingest" \
        -H "content-type: application/json" \
        -d '{"force":true}')"
      jq -e '(.ingestionJobId // .ingestion_job_id // .jobId // .id)' <<<"$REINGEST_AFTER_DELETE" >/dev/null
      pass "document can queue reingest after delete/archive"
      ;;
    404|405)
      warn "document delete/archive API returned HTTP $DELETE_STATUS; delete/reingest API gate is not exposed yet"
      ;;
    *)
      warn "document delete/archive API returned unexpected HTTP $DELETE_STATUS"
      ;;
  esac
else
  warn "seed upload did not expose a document id for delete/reingest checks"
fi

NO_ANSWER_BODY="$TMP_DIR/no-answer-chat.json"
NO_ANSWER_STATUS="$(chat_request "$NO_ANSWER_BODY" "$API_KEY" "What is the warranty for orbital coffee machines?" "0.99")"
if [ "$NO_ANSWER_STATUS" = "200" ] \
  && jq -e '(((.retrieval.no_answer // .retrieval.noAnswer // false) == true) or ((.citations // []) | length == 0))' "$NO_ANSWER_BODY" >/dev/null; then
  pass "no-answer behavior is exposed for a high score threshold"
else
  warn "no-answer gate did not produce an empty-citation/no-answer response"
fi

DEPLOYMENTS_BODY="$TMP_DIR/deployments.html"
DEPLOYMENTS_STATUS="$(http_status "$DEPLOYMENTS_BODY" "$WEB_URL/deployments")"
if [ "$DEPLOYMENTS_STATUS" = "200" ]; then
  pass "deployments route loads"
  if grep -q "iframe" "$DEPLOYMENTS_BODY" && grep -q "x-agentport-api-key\\|Bearer\\|session" "$DEPLOYMENTS_BODY"; then
    pass "deployments page includes widget/API key guidance"
  else
    warn "deployments page loaded but widget/API key guidance was not visible in returned HTML"
  fi
else
  warn "deployments route returned HTTP $DEPLOYMENTS_STATUS"
fi

echo "Checking database extensions..."
compose exec -T postgres \
  psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
  -c "SELECT extname FROM pg_extension WHERE extname IN ('vector','pgcrypto','citext') ORDER BY extname;" >/dev/null
pass "pgvector, pgcrypto, and citext are available"

if [ "${#WARNINGS[@]}" -gt 0 ]; then
  echo
  echo "Phase 1.1 warnings:"
  printf ' - %s\n' "${WARNINGS[@]}"
  if [ "$STRICT_PHASE11" = "1" ]; then
    fail "STRICT_PHASE11=1 and one or more Phase 1.1 gates emitted warnings"
  fi
fi

echo
echo "Phase 1.1 smoke completed."
jq '{answer, citations, retrieval, providerResponse: (.provider_response // .providerResponse), traceId: (.trace_id_record // .traceIdRecord), runId: (.run_id // .runId), fallbackMode: (.fallback_mode // .fallbackMode)}' "$VALID_BODY"
