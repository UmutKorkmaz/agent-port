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
SEED_SAMPLE="${SEED_SAMPLE:-1}"
RESET_MINIO="${RESET_MINIO:-1}"
WAIT_SECONDS="${WAIT_SECONDS:-45}"

usage() {
  cat <<'EOF'
Usage: scripts/dev-reset.sh [--no-seed] [--keep-minio]

Resets the local AgentPort demo state, then reseeds through the running
Platform API and AI Services when seeding is enabled.

Environment overrides:
  PLATFORM_API_URL, AI_SERVICES_URL, WEB_URL
  COMPOSE_PROJECT_NAME, POSTGRES_PORT, POSTGRES_USER, POSTGRES_PASSWORD, POSTGRES_DB
  MINIO_API_PORT, MINIO_ROOT_USER, MINIO_ROOT_PASSWORD, MINIO_BUCKET
  SAMPLE_FILE, WAIT_SECONDS
EOF
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --no-seed)
      SEED_SAMPLE=0
      ;;
    --keep-minio)
      RESET_MINIO=0
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
  shift
done

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "$1 is required." >&2
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

wait_for_json() {
  local label="$1"
  local url="$2"
  local jq_filter="$3"
  local started
  started="$(date +%s)"

  while true; do
    local body
    if body="$(curl -fsS "$url" 2>/dev/null)" && jq -e "$jq_filter" >/dev/null <<<"$body"; then
      return 0
    fi

    if [ "$(( $(date +%s) - started ))" -ge "$WAIT_SECONDS" ]; then
      echo "$label did not become ready at $url within ${WAIT_SECONDS}s." >&2
      return 1
    fi

    sleep 1
  done
}

reset_database() {
  local schema_ready
  schema_ready="$(compose exec -T postgres psql \
    -v ON_ERROR_STOP=1 \
    -tA \
    -U "$POSTGRES_USER" \
    -d "$POSTGRES_DB" \
    -c "SELECT to_regclass('public.workspaces') IS NOT NULL;")"

  if [ "$schema_ready" != "t" ]; then
    jq -n '{clearedWorkspaces: 0, clearedUsers: 0}'
    return 0
  fi

  local reset_sql
  read -r -d '' reset_sql <<'SQL' || true
WITH deleted_workspaces AS (
  DELETE FROM workspaces
  WHERE "Slug" = 'local'
  RETURNING 1
),
deleted_users AS (
  DELETE FROM users
  WHERE "Email" = 'owner@agentport.local'
  RETURNING 1
)
SELECT json_build_object(
  'clearedWorkspaces', (SELECT count(*) FROM deleted_workspaces),
  'clearedUsers', (SELECT count(*) FROM deleted_users)
);
SQL

  compose exec -T postgres psql \
    -v ON_ERROR_STOP=1 \
    -tA \
    -U "$POSTGRES_USER" \
    -d "$POSTGRES_DB" \
    <<<"$reset_sql"
}

reset_minio_bucket() {
  compose run --rm --entrypoint /bin/sh minio-bootstrap -s <<'EOF' >/dev/null
set -eu
mc alias set local http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null
mc mb --ignore-existing "local/$MINIO_BUCKET" >/dev/null
mc rm --recursive --force "local/$MINIO_BUCKET" >/dev/null 2>&1 || true
mc mb --ignore-existing "local/$MINIO_BUCKET" >/dev/null
mc anonymous set none "local/$MINIO_BUCKET" >/dev/null
EOF
}

require curl
require docker
require jq

if [ "$SEED_SAMPLE" = "1" ] && [ ! -f "$SAMPLE_FILE" ]; then
  echo "Sample file not found: $SAMPLE_FILE" >&2
  exit 1
fi

cd "$ROOT_DIR"

echo "Starting local Postgres and MinIO dependencies..." >&2
compose up -d --wait postgres minio >/dev/null

echo "Clearing local bootstrap workspace and owner user..." >&2
RESET_DB_JSON="$(reset_database)"

if [ "$RESET_MINIO" = "1" ]; then
  echo "Clearing local MinIO bucket '$MINIO_BUCKET'..." >&2
  reset_minio_bucket
fi

BOOTSTRAP_JSON="null"
UPLOAD_JSON="null"

if [ "$SEED_SAMPLE" = "1" ]; then
  echo "Waiting for Platform API and AI Services..." >&2
  wait_for_json "Platform API" "$PLATFORM_API_URL/health/ready" '(.status // .Status) == "healthy"'
  wait_for_json "AI Services" "$AI_SERVICES_URL/health/ready" '(.status // .Status) == "ok"'

  echo "Bootstrapping local workspace through Platform API..." >&2
  BOOTSTRAP_JSON="$(curl -fsS -X POST "$PLATFORM_API_URL/api/v1/bootstrap/local")"

  DATASET_ID="$(jq -r '.datasetId // .DatasetId // empty' <<<"$BOOTSTRAP_JSON")"
  if [ -z "$DATASET_ID" ]; then
    echo "Bootstrap response did not include datasetId." >&2
    echo "$BOOTSTRAP_JSON" | jq . >&2
    exit 1
  fi

  echo "Uploading deterministic support policy seed document..." >&2
  UPLOAD_JSON="$(curl -fsS -X POST "$PLATFORM_API_URL/api/v1/datasets/$DATASET_ID/documents" \
    -F "file=@$SAMPLE_FILE;type=text/markdown")"
fi

jq -n \
  --arg platformApiUrl "$PLATFORM_API_URL" \
  --arg aiServicesUrl "$AI_SERVICES_URL" \
  --arg webUrl "$WEB_URL" \
  --arg sampleFile "$SAMPLE_FILE" \
  --arg minioBucket "$MINIO_BUCKET" \
  --argjson reset "$RESET_DB_JSON" \
  --argjson seed "$BOOTSTRAP_JSON" \
  --argjson upload "$UPLOAD_JSON" \
  '{
    status: "reset",
    platformApiUrl: $platformApiUrl,
    aiServicesUrl: $aiServicesUrl,
    webUrl: $webUrl,
    sampleFile: $sampleFile,
    minioBucket: $minioBucket,
    clearedWorkspaces: ($reset.clearedWorkspaces // 0),
    clearedUsers: ($reset.clearedUsers // 0),
    seeded: ($seed != null),
    workspaceId: ($seed.workspaceId // $seed.WorkspaceId // null),
    projectId: ($seed.projectId // $seed.ProjectId // null),
    modelRouteId: ($seed.modelRouteId // $seed.ModelRouteId // null),
    agentDefinitionId: ($seed.agentDefinitionId // $seed.AgentDefinitionId // null),
    datasetId: ($seed.datasetId // $seed.DatasetId // null),
    knowledgeBaseId: ($seed.knowledgeBaseId // $seed.KnowledgeBaseId // null),
    walletAccountId: ($seed.walletAccountId // $seed.WalletAccountId // null),
    apiKey: ($seed.apiKey // $seed.ApiKey // null),
    apiKeyMessage: ($seed.apiKeyMessage // $seed.ApiKeyMessage // null),
    documentAssetId: ($upload.document_asset_id // $upload.documentAssetId // null),
    ingestionJobId: ($upload.ingestion_job_id // $upload.ingestionJobId // null),
    ingestionStatus: ($upload.ingestion_status // $upload.ingestionStatus // $upload.status // null),
    contentHash: ($upload.content_hash // $upload.contentHash // null),
    idempotencyKey: ($upload.idempotency_key // $upload.idempotencyKey // null),
    chunkCount: ($upload.chunk_count // $upload.chunkCount // null),
    completedAt: (now | todate)
  }'
