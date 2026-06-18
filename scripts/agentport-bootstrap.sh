#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

PLATFORM_API_URL="${PLATFORM_API_URL:-http://localhost:5001}"
AI_SERVICES_URL="${AI_SERVICES_URL:-http://localhost:5002}"
WEB_URL="${WEB_URL:-http://127.0.0.1:3002}"

FIXTURE="${FIXTURE:-$ROOT_DIR/examples/agentport/bootstrap.json}"
SEED_DOCS_DIR="${SEED_DOCS_DIR:-$ROOT_DIR/examples/agentport/seed-docs}"
CLIENT_DOCS_DIR="${CLIENT_DOCS_DIR:-$ROOT_DIR/examples/agentport/client-folder-sample}"
WAIT_SECONDS="${WAIT_SECONDS:-45}"
SEED_CLIENT="${SEED_CLIENT:-1}"

usage() {
  cat <<'EOF'
Usage: scripts/agentport-bootstrap.sh [--no-client]

Bootstraps a AgentPort firm install against a running AgentPort stack:
  1. Reuses POST /api/v1/bootstrap/local for the base workspace/project/agent/api key.
  2. Creates the AgentPort tax-law and client-folder datasets via POST /api/v1/datasets.
  3. Ingests every file under the seed-docs (and client-folder-sample) directories
     through POST /api/v1/datasets/{id}/documents.
  4. Prints a JSON summary of the resulting ids and the bootstrap API key.

Environment overrides:
  PLATFORM_API_URL, AI_SERVICES_URL, WEB_URL
  FIXTURE, SEED_DOCS_DIR, CLIENT_DOCS_DIR, WAIT_SECONDS, SEED_CLIENT
EOF
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --no-client)
      SEED_CLIENT=0
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

# Resolve a content type from a file extension so the ingest pipeline tags it sensibly.
content_type_for() {
  case "${1##*.}" in
    md) echo "text/markdown" ;;
    txt) echo "text/plain" ;;
    pdf) echo "application/pdf" ;;
    docx) echo "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ;;
    *) echo "application/octet-stream" ;;
  esac
}

# Create (or look up by slug) a dataset for this workspace/project, returning its id.
create_dataset() {
  local workspace_id="$1"
  local project_id="$2"
  local agent_id="$3"
  local name="$4"
  local slug="$5"
  local kind="$6"

  local payload
  payload="$(jq -n \
    --arg workspaceId "$workspace_id" \
    --arg projectId "$project_id" \
    --arg agentDefinitionId "$agent_id" \
    --arg name "$name" \
    --arg slug "$slug" \
    --arg kind "$kind" \
    '{workspaceId: $workspaceId, projectId: $projectId, agentDefinitionId: $agentDefinitionId, name: $name, slug: $slug, kind: $kind}')"

  local body status
  body="$(mktemp)"
  status="$(curl -sS -o "$body" -w "%{http_code}" \
    -X POST "$PLATFORM_API_URL/api/v1/datasets" \
    -H "content-type: application/json" \
    -H "x-agentport-api-key: $API_KEY" \
    -d "$payload")"

  if [ "$status" = "201" ] || [ "$status" = "200" ]; then
    jq -r '.id // .Id' "$body"
    rm -f "$body"
    return 0
  fi

  # A slug conflict means the dataset already exists from a prior run; look it up.
  if [ "$status" = "409" ]; then
    local existing
    existing="$(curl -fsS -H "x-agentport-api-key: $API_KEY" \
      "$PLATFORM_API_URL/api/v1/datasets?workspaceId=$workspace_id&projectId=$project_id" \
      | jq -r --arg slug "$slug" 'map(select((.slug // .Slug) == $slug)) | (.[0].id // .[0].Id) // empty')"
    rm -f "$body"
    if [ -n "$existing" ]; then
      echo "$existing"
      return 0
    fi
  fi

  echo "Failed to create dataset '$slug' (HTTP $status):" >&2
  cat "$body" >&2
  rm -f "$body"
  return 1
}

# Upload every file under a directory tree to a dataset; emit one ingest result JSON per line.
ingest_dir() {
  local dataset_id="$1"
  local dir="$2"
  local results="$3"

  [ -d "$dir" ] || return 0

  local file ctype upload
  while IFS= read -r -d '' file; do
    ctype="$(content_type_for "$file")"
    echo "Ingesting $file -> dataset $dataset_id" >&2
    upload="$(curl -fsS -X POST "$PLATFORM_API_URL/api/v1/datasets/$dataset_id/documents" \
      -H "x-agentport-api-key: $API_KEY" \
      -F "file=@$file;type=$ctype")"
    jq -c \
      --arg file "$file" \
      '{file: $file,
        documentAssetId: (.document_asset_id // .documentAssetId // null),
        ingestionJobId: (.ingestion_job_id // .ingestionJobId // null),
        ingestionStatus: (.ingestion_status // .ingestionStatus // .status // null),
        chunkCount: (.chunk_count // .chunkCount // null)}' \
      <<<"$upload" >>"$results"
  done < <(find "$dir" -type f \( -name '*.md' -o -name '*.txt' -o -name '*.pdf' -o -name '*.docx' \) \
    ! -name 'SOURCES.md' ! -name 'README.md' -print0 | sort -z)
}

require curl
require jq

if [ ! -f "$FIXTURE" ]; then
  echo "AgentPort fixture not found: $FIXTURE" >&2
  exit 1
fi

if [ ! -d "$SEED_DOCS_DIR" ]; then
  echo "Seed docs directory not found: $SEED_DOCS_DIR" >&2
  exit 1
fi

cd "$ROOT_DIR"

echo "Waiting for Platform API and AI Services..." >&2
wait_for_json "Platform API" "$PLATFORM_API_URL/health/ready" '(.status // .Status) == "healthy"'
wait_for_json "AI Services" "$AI_SERVICES_URL/health/ready" '(.status // .Status) == "ok"'

echo "Bootstrapping base workspace through Platform API..." >&2
BOOTSTRAP_JSON="$(curl -fsS -X POST "$PLATFORM_API_URL/api/v1/bootstrap/local")"

WORKSPACE_ID="$(jq -r '.workspaceId // .WorkspaceId // empty' <<<"$BOOTSTRAP_JSON")"
PROJECT_ID="$(jq -r '.projectId // .ProjectId // empty' <<<"$BOOTSTRAP_JSON")"
AGENT_ID="$(jq -r '.agentDefinitionId // .AgentDefinitionId // empty' <<<"$BOOTSTRAP_JSON")"
API_KEY="$(jq -r '.apiKey // .ApiKey // empty' <<<"$BOOTSTRAP_JSON")"

if [ -z "$WORKSPACE_ID" ] || [ -z "$PROJECT_ID" ] || [ -z "$AGENT_ID" ]; then
  echo "Bootstrap response did not include the expected ids." >&2
  jq . <<<"$BOOTSTRAP_JSON" >&2
  exit 1
fi

# Pull the two AgentPort dataset descriptors from the fixture (by role).
TAX_LAW_NAME="$(jq -r '.datasets[] | select(.role == "tax-law-seed") | .name' "$FIXTURE")"
TAX_LAW_SLUG="$(jq -r '.datasets[] | select(.role == "tax-law-seed") | .slug' "$FIXTURE")"
TAX_LAW_KIND="$(jq -r '.datasets[] | select(.role == "tax-law-seed") | .kind' "$FIXTURE")"
CLIENT_NAME="$(jq -r '.datasets[] | select(.role == "client-folder") | .name' "$FIXTURE")"
CLIENT_SLUG="$(jq -r '.datasets[] | select(.role == "client-folder") | .slug' "$FIXTURE")"
CLIENT_KIND="$(jq -r '.datasets[] | select(.role == "client-folder") | .kind' "$FIXTURE")"

echo "Creating AgentPort datasets..." >&2
TAX_LAW_DATASET_ID="$(create_dataset "$WORKSPACE_ID" "$PROJECT_ID" "$AGENT_ID" "$TAX_LAW_NAME" "$TAX_LAW_SLUG" "$TAX_LAW_KIND")"
CLIENT_DATASET_ID="$(create_dataset "$WORKSPACE_ID" "$PROJECT_ID" "$AGENT_ID" "$CLIENT_NAME" "$CLIENT_SLUG" "$CLIENT_KIND")"

INGEST_RESULTS="$(mktemp)"
trap 'rm -f "$INGEST_RESULTS"' EXIT

echo "Ingesting seed-docs into the tax-law dataset..." >&2
ingest_dir "$TAX_LAW_DATASET_ID" "$SEED_DOCS_DIR" "$INGEST_RESULTS"

if [ "$SEED_CLIENT" = "1" ]; then
  echo "Ingesting client-folder-sample into the client-folder dataset..." >&2
  ingest_dir "$CLIENT_DATASET_ID" "$CLIENT_DOCS_DIR" "$INGEST_RESULTS"
fi

INGESTED_JSON="$(jq -s '.' "$INGEST_RESULTS")"

jq -n \
  --arg platformApiUrl "$PLATFORM_API_URL" \
  --arg aiServicesUrl "$AI_SERVICES_URL" \
  --arg webUrl "$WEB_URL" \
  --arg workspaceId "$WORKSPACE_ID" \
  --arg projectId "$PROJECT_ID" \
  --arg taxLawDatasetId "$TAX_LAW_DATASET_ID" \
  --arg clientFolderDatasetId "$CLIENT_DATASET_ID" \
  --arg agentDefinitionId "$AGENT_ID" \
  --arg apiKey "$API_KEY" \
  --argjson ingested "$INGESTED_JSON" \
  '{
    status: "bootstrapped",
    product: "agentport",
    platformApiUrl: $platformApiUrl,
    aiServicesUrl: $aiServicesUrl,
    webUrl: $webUrl,
    workspaceId: $workspaceId,
    projectId: $projectId,
    taxLawDatasetId: $taxLawDatasetId,
    clientFolderDatasetId: $clientFolderDatasetId,
    agentDefinitionId: $agentDefinitionId,
    apiKey: (if $apiKey == "" then null else $apiKey end),
    ingestedDocuments: ($ingested | length),
    ingested: $ingested,
    completedAt: (now | todate)
  }'
