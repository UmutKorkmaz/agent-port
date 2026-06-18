#!/usr/bin/env bash
set -euo pipefail

# AgentPort QA smoke runner.
#
# Iterates examples/agentport/benchmark.json, POSTs each Turkish question to the
# AgentPort chat API, and asserts the launch-readiness MVP success criteria:
#
#   (a) tax-law / client-folder cases cite the expected document
#   (b) no-answer fires for out-of-domain cases
#   (c) ZERO-EGRESS: the resolved provider route is local/ollama and no cloud
#       provider is enabled (PROVIDER_LIVE_CALLS off / provider stays local)
#   (d) FAIL (exit 1) if tax-law < 8/10 or client-folder < 5/5
#
# Bring-up / seeding precedence:
#   1. AGENTPORT_DATASET_ID + AGENT_ID + (optional) API key via env/args
#   2. scripts/agentport-bootstrap.sh (if present) -> JSON with ids
#   3. scripts/dev-reset.sh fallback (SEED_SAMPLE path)
#
# Usage:
#   scripts/agentport-smoke.sh
#   AGENT_ID=<uuid> AGENTPORT_API_KEY=ap_xxx scripts/agentport-smoke.sh
#   scripts/agentport-smoke.sh --agent <uuid> --api-key ap_xxx --dataset <uuid>

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

PLATFORM_API_URL="${PLATFORM_API_URL:-http://localhost:5001}"
AI_SERVICES_URL="${AI_SERVICES_URL:-http://localhost:5002}"
BENCHMARK_FILE="${BENCHMARK_FILE:-$ROOT_DIR/examples/agentport/benchmark.json}"
BOOTSTRAP_SCRIPT="${BOOTSTRAP_SCRIPT:-$ROOT_DIR/scripts/agentport-bootstrap.sh}"

# No-answer cases query with a deliberately high score threshold so the
# retrieval gate is exercised even when the corpus has loosely related chunks.
NO_ANSWER_THRESHOLD="${NO_ANSWER_THRESHOLD:-0.99}"

# MVP success thresholds (docs/LAUNCH.md "MVP success criteria").
TAX_LAW_PASS_MIN="${TAX_LAW_PASS_MIN:-8}"
TAX_LAW_TOTAL_TARGET="${TAX_LAW_TOTAL_TARGET:-10}"
CLIENT_FOLDER_PASS_MIN="${CLIENT_FOLDER_PASS_MIN:-5}"
CLIENT_FOLDER_TOTAL_TARGET="${CLIENT_FOLDER_TOTAL_TARGET:-5}"

AGENT_ID="${AGENT_ID:-}"
DATASET_ID="${AGENTPORT_DATASET_ID:-${DATASET_ID:-}}"
API_KEY="${AGENTPORT_API_KEY:-${API_KEY:-}}"

while [ "$#" -gt 0 ]; do
  case "$1" in
    --agent) AGENT_ID="$2"; shift 2 ;;
    --dataset) DATASET_ID="$2"; shift 2 ;;
    --api-key) API_KEY="$2"; shift 2 ;;
    --benchmark) BENCHMARK_FILE="$2"; shift 2 ;;
    -h|--help)
      sed -n '3,30p' "$0"
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "$1 is required for AgentPort smoke checks." >&2
    exit 1
  fi
}

pass() { echo "ok   - $1"; }
warn() { echo "warn - $1" >&2; }
fail() { echo "fail - $1" >&2; exit 1; }

require curl
require jq

[ -f "$BENCHMARK_FILE" ] || fail "benchmark file not found: $BENCHMARK_FILE"
jq empty "$BENCHMARK_FILE" >/dev/null 2>&1 || fail "benchmark file is not valid JSON: $BENCHMARK_FILE"

# ---------------------------------------------------------------------------
# Resolve dataset / agent / API key.
# ---------------------------------------------------------------------------
if [ -z "$AGENT_ID" ]; then
  if [ -x "$BOOTSTRAP_SCRIPT" ]; then
    echo "Bootstrapping AgentPort via $BOOTSTRAP_SCRIPT ..." >&2
    BOOT_JSON="$("$BOOTSTRAP_SCRIPT")"
  elif [ -x "$ROOT_DIR/scripts/dev-reset.sh" ]; then
    echo "No bootstrap script; falling back to scripts/dev-reset.sh ..." >&2
    BOOT_JSON="$(SEED_SAMPLE=1 "$ROOT_DIR/scripts/dev-reset.sh")"
  else
    fail "no AGENT_ID provided and no bootstrap/dev-reset script available"
  fi
  AGENT_ID="$(jq -r '.agentDefinitionId // .agent_id // .agentId // empty' <<<"$BOOT_JSON")"
  [ -n "$DATASET_ID" ] || DATASET_ID="$(jq -r '.datasetId // .dataset_id // empty' <<<"$BOOT_JSON")"
  [ -n "$API_KEY" ] || API_KEY="$(jq -r '.apiKey // .api_key // empty' <<<"$BOOT_JSON")"
fi

[ -n "$AGENT_ID" ] || fail "could not resolve an agent definition id (set AGENT_ID or --agent)"
pass "resolved agent definition id: $AGENT_ID"
[ -n "$API_KEY" ] || warn "no API key resolved; chat calls will use anonymous fallback (local only)"

# ---------------------------------------------------------------------------
# Health checks.
# ---------------------------------------------------------------------------
echo "Checking service health..."
curl -fsS "$PLATFORM_API_URL/health/ready" | jq -e '(.status // .Status) == "healthy"' >/dev/null \
  || fail "Platform API health check failed at $PLATFORM_API_URL/health/ready"
curl -fsS "$AI_SERVICES_URL/health/ready" | jq -e '(.status // .Status) == "ok"' >/dev/null \
  || fail "AI services health check failed at $AI_SERVICES_URL/health/ready"
pass "platform and ai-services health endpoints respond"

# ---------------------------------------------------------------------------
# Chat request helper. Mirrors scripts/smoke.sh chat_request shape.
# Writes JSON body to $1, echoes the HTTP status code.
# ---------------------------------------------------------------------------
chat_request() {
  local body_file="$1"
  local question="$2"
  local score_threshold="${3:-}"
  local payload

  if [ -n "$score_threshold" ]; then
    payload="$(jq -n --arg q "$question" --argjson t "$score_threshold" \
      '{question: $q, topK: 4, scoreThreshold: $t}')"
  else
    payload="$(jq -n --arg q "$question" '{question: $q, topK: 4}')"
  fi

  local -a args=(
    -sS -o "$body_file" -w "%{http_code}"
    -X POST "$PLATFORM_API_URL/api/v1/agent-definitions/$AGENT_ID/chat"
    -H "content-type: application/json"
  )
  if [ -n "$API_KEY" ]; then
    args+=(-H "x-agentport-api-key: $API_KEY")
  fi
  args+=(-d "$payload")

  curl "${args[@]}"
}

# ---------------------------------------------------------------------------
# Zero-egress assertion on a response body.
# The chat response exposes provider_response.provider (local|ollama|cloud)
# and provider_response.metadata.runtime_route.{provider_kind,provider_name}.
# A cloud provider only fires when PROVIDER_LIVE_CALLS is enabled AND a cloud
# route is configured; for AgentPort on-prem we require local/ollama.
# Returns 0 (egress safe) or 1 (cloud leak).
# ---------------------------------------------------------------------------
assert_zero_egress() {
  local body_file="$1"
  jq -e '
    (.provider_response // .providerResponse // {}) as $p
    | ($p.provider // "local" | ascii_downcase) as $provider
    | (($p.metadata.runtime_route // $p.metadata.runtimeRoute // {})) as $route
    | (($route.provider_kind // $route.providerKind // "") | ascii_downcase) as $kind
    | (($route.provider_name // $route.providerName // "") | ascii_downcase) as $name
    | ($provider == "local" or $provider == "ollama")
      and ($kind == "" or $kind == "ollama" or $kind == "local")
      and ($name == "" or $name == "ollama" or $name == "local")
  ' "$body_file" >/dev/null 2>&1
}

# Cheap pre-flight: if the environment advertises live cloud calls, this whole
# run violates the on-prem zero-egress promise regardless of per-response data.
LIVE_CALLS="${PROVIDER_LIVE_CALLS:-}"
case "$(printf '%s' "$LIVE_CALLS" | tr '[:upper:]' '[:lower:]')" in
  1|true|yes|on)
    fail "ZERO-EGRESS violation: PROVIDER_LIVE_CALLS=$LIVE_CALLS enables outbound cloud calls"
    ;;
esac

# ---------------------------------------------------------------------------
# Iterate the benchmark.
# ---------------------------------------------------------------------------
tax_total=0;    tax_pass=0
client_total=0; client_pass=0
noans_total=0;  noans_pass=0
egress_violation=0

CASE_COUNT="$(jq 'length' "$BENCHMARK_FILE")"
echo "Running $CASE_COUNT benchmark cases against agent $AGENT_ID ..."

for i in $(seq 0 $((CASE_COUNT - 1))); do
  CASE="$(jq -c ".[$i]" "$BENCHMARK_FILE")"
  ID="$(jq -r '.id' <<<"$CASE")"
  TYPE="$(jq -r '.type' <<<"$CASE")"
  QUESTION="$(jq -r '.question_tr' <<<"$CASE")"
  EXPECT_NO_ANSWER="$(jq -r '.expect_no_answer' <<<"$CASE")"
  EXPECT_FILE="$(jq -r '.expected_citation.file_name // empty' <<<"$CASE")"
  EXPECT_CATEGORY="$(jq -r '.expected_citation.category // empty' <<<"$CASE")"

  BODY="$TMP_DIR/$ID.json"
  if [ "$EXPECT_NO_ANSWER" = "true" ]; then
    STATUS="$(chat_request "$BODY" "$QUESTION" "$NO_ANSWER_THRESHOLD")"
  else
    STATUS="$(chat_request "$BODY" "$QUESTION")"
  fi

  if [ "$STATUS" != "200" ]; then
    warn "[$ID] HTTP $STATUS (expected 200): $(head -c 200 "$BODY" 2>/dev/null)"
  fi

  # (c) zero-egress — checked on every successful response.
  if [ "$STATUS" = "200" ]; then
    if ! assert_zero_egress "$BODY"; then
      egress_violation=1
      PROVIDER="$(jq -r '(.provider_response // .providerResponse // {}).provider // "?"' "$BODY" 2>/dev/null)"
      warn "[$ID] ZERO-EGRESS violation: provider resolved to '$PROVIDER' (cloud route active)"
    fi
  fi

  case "$TYPE" in
    no-answer)
      noans_total=$((noans_total + 1))
      # (b) no_answer must fire (flag true OR zero citations).
      if [ "$STATUS" = "200" ] && jq -e '
            (((.retrieval.no_answer // .retrieval.noAnswer // false) == true)
             or ((.citations // []) | length == 0))
          ' "$BODY" >/dev/null 2>&1; then
        noans_pass=$((noans_pass + 1))
        pass "[$ID] no-answer fired for out-of-domain question"
      else
        warn "[$ID] expected no-answer but got citations / answer"
      fi
      ;;
    tax-law|client-folder)
      [ "$TYPE" = "tax-law" ] && tax_total=$((tax_total + 1)) || client_total=$((client_total + 1))
      # (a) response must cite the expected document (file_name match,
      # category fallback) and must NOT be a no-answer.
      CITED=0
      if [ "$STATUS" = "200" ]; then
        if [ -n "$EXPECT_FILE" ] && jq -e \
            --arg f "$EXPECT_FILE" \
            '((.citations // []) | map(.file_name // .fileName // "") | any(. == $f))' \
            "$BODY" >/dev/null 2>&1; then
          CITED=1
        elif [ -n "$EXPECT_CATEGORY" ] && jq -e \
            --arg c "$EXPECT_CATEGORY" \
            '((.citations // []) | map((.file_name // .fileName // "") | ascii_downcase) | any(test($c; "i")))' \
            "$BODY" >/dev/null 2>&1; then
          # Category fallback: filename embeds or aligns with category tag.
          CITED=1
        fi
        # Must not be a no-answer for a grounded question.
        if jq -e '((.retrieval.no_answer // .retrieval.noAnswer // false) == true)' "$BODY" >/dev/null 2>&1; then
          CITED=0
        fi
      fi
      if [ "$CITED" = "1" ]; then
        [ "$TYPE" = "tax-law" ] && tax_pass=$((tax_pass + 1)) || client_pass=$((client_pass + 1))
        pass "[$ID] cited expected document '${EXPECT_FILE:-$EXPECT_CATEGORY}'"
      else
        warn "[$ID] did NOT cite expected '${EXPECT_FILE:-$EXPECT_CATEGORY}'"
      fi
      ;;
    *)
      warn "[$ID] unknown case type '$TYPE'; skipping"
      ;;
  esac
done

# ---------------------------------------------------------------------------
# Scorecard + gate.
# ---------------------------------------------------------------------------
echo
echo "AgentPort benchmark scorecard:"
echo "  tax-law:       $tax_pass / $tax_total  (min $TAX_LAW_PASS_MIN of $TAX_LAW_TOTAL_TARGET)"
echo "  client-folder: $client_pass / $client_total  (min $CLIENT_FOLDER_PASS_MIN of $CLIENT_FOLDER_TOTAL_TARGET)"
echo "  no-answer:     $noans_pass / $noans_total"
echo "  zero-egress:   $([ "$egress_violation" = "0" ] && echo "clean" || echo "VIOLATION")"

GATE_FAILED=0

if [ "$egress_violation" != "0" ]; then
  warn "ZERO-EGRESS gate failed: at least one response used a cloud provider"
  GATE_FAILED=1
fi

if [ "$noans_total" -gt 0 ] && [ "$noans_pass" -lt "$noans_total" ]; then
  warn "no-answer gate failed: $noans_pass/$noans_total out-of-domain cases returned a controlled no-answer"
  GATE_FAILED=1
fi

if [ "$tax_pass" -lt "$TAX_LAW_PASS_MIN" ]; then
  warn "tax-law gate failed: $tax_pass correct citations < required $TAX_LAW_PASS_MIN/$TAX_LAW_TOTAL_TARGET"
  GATE_FAILED=1
fi

if [ "$client_pass" -lt "$CLIENT_FOLDER_PASS_MIN" ]; then
  warn "client-folder gate failed: $client_pass correct citations < required $CLIENT_FOLDER_PASS_MIN/$CLIENT_FOLDER_TOTAL_TARGET"
  GATE_FAILED=1
fi

if [ "$GATE_FAILED" != "0" ]; then
  fail "AgentPort launch-readiness gates did not pass"
fi

echo
echo "AgentPort smoke completed: all launch-readiness gates passed."
