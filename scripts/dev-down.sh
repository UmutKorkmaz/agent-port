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

tmux kill-session -t "$SESSION_NAME" 2>/dev/null || true

cd "$ROOT_DIR"
docker compose down

echo "AgentPort Phase 1 stack stopped."
