#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [ -f "$ROOT_DIR/.env" ]; then
  set -a
  # shellcheck source=/dev/null
  source "$ROOT_DIR/.env"
  set +a
fi

PROFILE="${1:-${OLLAMA_PULL_PROFILE:-core}}"
CORE_MODELS="${OLLAMA_CORE_MODELS:-${OLLAMA_MODEL:-gemma4:e4b}}"
SMALL_MODELS="${OLLAMA_SMALL_MODELS:-$CORE_MODELS gemma4:e2b llama3.2:1b llama3.2:3b phi4-mini}"
EXTENDED_MODELS="${OLLAMA_EXTENDED_MODELS:-$SMALL_MODELS gemma4:26b llama3.1:8b mistral}"

usage() {
  cat <<USAGE
Usage: $0 [core|small|extended]

Profiles:
  core      Pull OLLAMA_MODEL, default: gemma4:e4b
  small     Pull core plus small local chat models
  extended  Pull small plus larger fallback models

Overrides:
  OLLAMA_MODEL, OLLAMA_CORE_MODELS, OLLAMA_SMALL_MODELS, OLLAMA_EXTENDED_MODELS
USAGE
}

case "$PROFILE" in
  core)
    MODELS="$CORE_MODELS"
    ;;
  small)
    MODELS="$SMALL_MODELS"
    ;;
  extended)
    MODELS="$EXTENDED_MODELS"
    ;;
  -h|--help|help)
    usage
    exit 0
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac

if ! command -v ollama >/dev/null 2>&1; then
  echo "ollama CLI is required. Install and start Ollama before pulling models." >&2
  exit 1
fi

INSTALLED="$(ollama list 2>/dev/null | awk 'NR > 1 {print $1}')"

for model in $MODELS; do
  if printf '%s\n' "$INSTALLED" | grep -Fxq "$model"; then
    echo "ok - $model already present"
    continue
  fi

  echo "pull - $model"
  ollama pull "$model"
  INSTALLED="$(printf '%s\n%s\n' "$INSTALLED" "$model" | sed '/^$/d')"
done
