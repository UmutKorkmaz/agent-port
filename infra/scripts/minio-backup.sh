#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ENV_FILE="$ROOT_DIR/.env"

if [ -f "$ENV_FILE" ]; then
  set -a
  # shellcheck disable=SC1090
  source "$ENV_FILE"
  set +a
fi

MINIO_ROOT_USER="${MINIO_ROOT_USER:-agentport}"
MINIO_ROOT_PASSWORD="${MINIO_ROOT_PASSWORD:-agentportagentport}"
MINIO_BUCKET="${MINIO_BUCKET:-agentport-phase1}"
export COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-agentport_phase1}"
MINIO_MC_IMAGE="${MINIO_MC_IMAGE:-minio/mc:RELEASE.2024-07-15T17-46-06Z}"
BACKUP_DIR="${BACKUP_DIR:-$ROOT_DIR/infra/backups/minio}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUTPUT_DIR="${1:-$BACKUP_DIR/$TIMESTAMP}"

MINIO_CONTAINER="$(docker compose -f "$ROOT_DIR/docker-compose.yml" ps -q minio)"
if [ -z "$MINIO_CONTAINER" ]; then
  echo "MinIO is not running. Start it with docker compose up -d minio minio-bootstrap." >&2
  exit 1
fi

NETWORK_NAME="$(docker inspect -f '{{range $name, $_ := .NetworkSettings.Networks}}{{println $name}}{{end}}' "$MINIO_CONTAINER" | head -n 1)"
if [ -z "$NETWORK_NAME" ]; then
  echo "Could not determine MinIO Docker network." >&2
  exit 1
fi

mkdir -p "$OUTPUT_DIR"

docker run --rm \
  --network "$NETWORK_NAME" \
  --entrypoint /bin/sh \
  -e MINIO_ROOT_USER="$MINIO_ROOT_USER" \
  -e MINIO_ROOT_PASSWORD="$MINIO_ROOT_PASSWORD" \
  -e MINIO_BUCKET="$MINIO_BUCKET" \
  -v "$OUTPUT_DIR:/backup" \
  "$MINIO_MC_IMAGE" \
  -c 'mc alias set local http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" &&
    mc mirror --overwrite "local/$MINIO_BUCKET" "/backup/$MINIO_BUCKET"'

echo "MinIO bucket backup written to $OUTPUT_DIR/$MINIO_BUCKET"
