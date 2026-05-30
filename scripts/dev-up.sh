#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SESSION_NAME="${SESSION_NAME:-agentport-phase1}"

export COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-agentport_phase1}"
export POSTGRES_PORT="${POSTGRES_PORT:-55432}"
export REDIS_PORT="${REDIS_PORT:-56379}"
export RABBITMQ_PORT="${RABBITMQ_PORT:-55672}"
export RABBITMQ_MANAGEMENT_PORT="${RABBITMQ_MANAGEMENT_PORT:-15673}"
export MINIO_API_PORT="${MINIO_API_PORT:-59000}"
export MINIO_CONSOLE_PORT="${MINIO_CONSOLE_PORT:-59001}"
export MINIO_BUCKET="${MINIO_BUCKET:-agentport-phase1}"

PLATFORM_API_URL="${PLATFORM_API_URL:-http://localhost:5001}"
AI_SERVICES_URL="${AI_SERVICES_URL:-http://localhost:5002}"
WEB_URL="${WEB_URL:-http://127.0.0.1:3002}"
POSTGRES_DSN="host=localhost port=$POSTGRES_PORT dbname=${POSTGRES_DB:-agentport} user=${POSTGRES_USER:-agentport} password=${POSTGRES_PASSWORD:-agentport}"
DOTNET_CONNECTION="Host=localhost;Port=$POSTGRES_PORT;Database=${POSTGRES_DB:-agentport};Username=${POSTGRES_USER:-agentport};Password=${POSTGRES_PASSWORD:-agentport}"

if ! command -v tmux >/dev/null 2>&1; then
  echo "tmux is required to run the local AgentPort stack." >&2
  exit 1
fi

cd "$ROOT_DIR"
if [ -f "$ROOT_DIR/.env" ]; then
  set -a
  # shellcheck source=/dev/null
  source "$ROOT_DIR/.env"
  set +a
fi

echo "Preparing local dependencies..."
dotnet restore src/AgentPort.sln >/dev/null
dotnet build src/AgentPort.sln --no-restore >/dev/null
python3 -m venv .venv
".venv/bin/python" -m pip install --upgrade pip >/dev/null
".venv/bin/python" -m pip install -r src/ai-services/requirements.txt >/dev/null
if [ ! -d "src/web/node_modules" ]; then
  npm --prefix src/web install >/dev/null
fi
rm -rf src/web/.next

docker compose up -d --wait postgres redis rabbitmq minio
docker compose up minio-bootstrap >/dev/null

tmux kill-session -t "$SESSION_NAME" 2>/dev/null || true
tmux new-session -d -s "$SESSION_NAME" -n platform-api \
  "cd '$ROOT_DIR' && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=$PLATFORM_API_URL ConnectionStrings__DefaultConnection='$DOTNET_CONNECTION' ConnectionStrings__Redis='localhost:$REDIS_PORT' AI_SERVICES_URL='$AI_SERVICES_URL' dotnet run --project src/platform-api/AgentPort.PlatformApi.csproj --no-build"
tmux new-window -t "$SESSION_NAME" -n ai-services \
  "cd '$ROOT_DIR/src/ai-services' && POSTGRES_DSN='$POSTGRES_DSN' MINIO_ENDPOINT='http://localhost:$MINIO_API_PORT' MINIO_BUCKET='$MINIO_BUCKET' MINIO_ROOT_USER='${MINIO_ROOT_USER:-agentport}' MINIO_ROOT_PASSWORD='${MINIO_ROOT_PASSWORD:-agentportagentport}' '$ROOT_DIR/.venv/bin/python' -m uvicorn main:app --host 0.0.0.0 --port 5002"
tmux new-window -t "$SESSION_NAME" -n web \
  "cd '$ROOT_DIR/src/web' && npm run dev -- --hostname 127.0.0.1 --port 3002"

echo "AgentPort Phase 1 stack is starting in tmux session '$SESSION_NAME'."
echo "Web: $WEB_URL"
echo "Platform API: $PLATFORM_API_URL"
echo "AI Services: $AI_SERVICES_URL"
echo "Attach: tmux attach -t $SESSION_NAME"
