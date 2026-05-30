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

POSTGRES_USER="${POSTGRES_USER:-agentport}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-agentport}"
POSTGRES_DB="${POSTGRES_DB:-agentport}"
export COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-agentport_phase1}"
BACKUP_DIR="${BACKUP_DIR:-$ROOT_DIR/infra/backups/postgres}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUTPUT_FILE="${1:-$BACKUP_DIR/${POSTGRES_DB}_${TIMESTAMP}.dump}"

mkdir -p "$(dirname "$OUTPUT_FILE")"

docker compose -f "$ROOT_DIR/docker-compose.yml" exec -T \
  -e PGPASSWORD="$POSTGRES_PASSWORD" \
  postgres \
  pg_dump \
  --username "$POSTGRES_USER" \
  --dbname "$POSTGRES_DB" \
  --format custom \
  --clean \
  --if-exists \
  --no-owner > "$OUTPUT_FILE"

shasum -a 256 "$OUTPUT_FILE" > "$OUTPUT_FILE.sha256"

echo "Postgres backup written to $OUTPUT_FILE"
echo "Checksum written to $OUTPUT_FILE.sha256"
