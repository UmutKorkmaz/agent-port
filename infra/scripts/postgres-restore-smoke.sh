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
BACKUP_FILE="${1:-}"

if [ -z "$BACKUP_FILE" ]; then
  if [ ! -d "$BACKUP_DIR" ]; then
    echo "No Postgres backup found. Run infra/scripts/postgres-backup.sh first." >&2
    exit 1
  fi
  BACKUP_FILE="$(find "$BACKUP_DIR" -maxdepth 1 -type f -name "${POSTGRES_DB}_*.dump" | sort | tail -n 1)"
fi

if [ -z "$BACKUP_FILE" ] || [ ! -f "$BACKUP_FILE" ]; then
  echo "No Postgres backup found. Run infra/scripts/postgres-backup.sh first." >&2
  exit 1
fi

SMOKE_DB="agentport_restore_smoke_$(date -u +%Y%m%d%H%M%S)"

cleanup() {
  docker compose -f "$ROOT_DIR/docker-compose.yml" exec -T \
    -e PGPASSWORD="$POSTGRES_PASSWORD" \
    postgres \
    dropdb --username "$POSTGRES_USER" --if-exists "$SMOKE_DB" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker compose -f "$ROOT_DIR/docker-compose.yml" exec -T \
  -e PGPASSWORD="$POSTGRES_PASSWORD" \
  postgres \
  createdb --username "$POSTGRES_USER" "$SMOKE_DB"

docker compose -f "$ROOT_DIR/docker-compose.yml" exec -T \
  -e PGPASSWORD="$POSTGRES_PASSWORD" \
  postgres \
  pg_restore \
  --username "$POSTGRES_USER" \
  --dbname "$SMOKE_DB" \
  --clean \
  --if-exists \
  --no-owner < "$BACKUP_FILE"

docker compose -f "$ROOT_DIR/docker-compose.yml" exec -T \
  -e PGPASSWORD="$POSTGRES_PASSWORD" \
  postgres \
  psql \
  --username "$POSTGRES_USER" \
  --dbname "$SMOKE_DB" \
  --set ON_ERROR_STOP=1 \
  --command "SELECT extname FROM pg_extension WHERE extname IN ('vector', 'pgcrypto', 'citext') ORDER BY extname;"

echo "Postgres restore smoke passed for $BACKUP_FILE"
