"""Provider route resolution, env gating, and the skip-reason taxonomy.

Extracted verbatim from ``main.py`` (Track A, Task A6). This module owns the
pieces of the chat provider path that decide *whether* and *how* a route is
used — env-flag/float helpers, route/provider config coalescing, the sovereign
egress lock, cost estimation, and the ``provider_skip_result`` shape that
carries the skip-reason taxonomy.

Behavior is preserved byte-identically. In particular:

* ``sovereign_mode_enabled`` defaults on (:data:`DEFAULT_SOVEREIGN_MODE`) and is
  consulted in :func:`provider_enabled_by_env` to lock egress to any non-local
  provider, regardless of route/provider config or ``PROVIDER_LIVE_CALLS``.
* The skip-reason strings (``empty_context``, ``route_disabled``,
  ``provider_disabled``, ``sovereign_mode``, ``provider_env_disabled``,
  ``live_calls_disabled``, ``unsupported_provider``, ``missing_base_url``) and
  the ``provider_skip_result`` envelope are unchanged.

These names are re-exported from ``main`` so existing imports (including
``tests/test_contracts.py``) keep resolving them from ``main`` unchanged.
"""

from __future__ import annotations

import json
import os
import re
from typing import Any, Dict, Optional

from agentport.models import DEFAULT_CHAT_MODEL, ChatRequest

# Sovereign mode is default-on for DefterPort: egress to any non-local provider
# is locked off regardless of route/provider config.
DEFAULT_SOVEREIGN_MODE = True


def env_flag(name: str, default: bool = False) -> bool:
    raw = os.getenv(name)
    if raw is None:
        return default
    return raw.strip().lower() in {"1", "true", "yes", "on", "enabled"}


def env_float(name: str, default: float) -> float:
    raw = os.getenv(name)
    if raw is None or raw.strip() == "":
        return default
    try:
        return float(raw)
    except ValueError:
        return default


def positive_env_float(name: str) -> float:
    value = env_float(name, 0.0)
    return value if value > 0 else 0.0


def estimate_provider_cost(input_tokens: int, output_tokens: int) -> float:
    fixed_fee = env_float("AGENTPORT_FEE_FIXED_USD", 0.10)
    fixed_fee = fixed_fee if fixed_fee > 0 else 0.0
    input_price = positive_env_float("AGENTPORT_INPUT_TOKEN_PRICE_PER_MILLION_USD")
    output_price = positive_env_float("AGENTPORT_OUTPUT_TOKEN_PRICE_PER_MILLION_USD")
    cost = fixed_fee
    cost += (input_tokens / 1_000_000) * input_price
    cost += (output_tokens / 1_000_000) * output_price
    return round(cost, 6)


def sovereign_mode_enabled() -> bool:
    return env_flag("AGENTPORT_SOVEREIGN", DEFAULT_SOVEREIGN_MODE)


def parse_json_object(value: Any) -> Dict[str, Any]:
    if isinstance(value, dict):
        return value
    if not value:
        return {}
    try:
        parsed = json.loads(value)
    except (TypeError, ValueError):
        return {}
    return parsed if isinstance(parsed, dict) else {}


def first_text(*values: Any) -> Optional[str]:
    for value in values:
        if value is None:
            continue
        text = str(value).strip()
        if text:
            return text
    return None


def bool_or_default(value: Any, default: bool) -> bool:
    if value is None:
        return default
    if isinstance(value, bool):
        return value
    if isinstance(value, (int, float)):
        return bool(value)
    if isinstance(value, str):
        normalized = value.strip().lower()
        if normalized in {"1", "true", "yes", "on", "enabled"}:
            return True
        if normalized in {"0", "false", "no", "off", "disabled"}:
            return False
    return default


def dict_get_any(value: Dict[str, Any], *keys: str) -> Any:
    for key in keys:
        if key in value:
            return value[key]
    return None


def provider_env_prefix(provider_name: Optional[str]) -> Optional[str]:
    if not provider_name:
        return None
    prefix = re.sub(r"[^A-Za-z0-9]+", "_", provider_name).strip("_").upper()
    return prefix or None


def provider_key(route_context: Dict[str, Any]) -> str:
    provider_kind = (route_context.get("provider_kind") or "").strip().lower()
    provider_name = (route_context.get("provider_name") or route_context.get("provider") or "").strip().lower()
    if provider_kind:
        return provider_kind
    return provider_name


def is_ollama_provider(route_context: Dict[str, Any]) -> bool:
    key = provider_key(route_context)
    name = (route_context.get("provider_name") or "").strip().lower()
    return key == "ollama" or name == "ollama"


def is_openai_compatible_provider(route_context: Dict[str, Any]) -> bool:
    key = provider_key(route_context)
    name = (route_context.get("provider_name") or "").strip().lower()
    return key in {"openai-compatible", "openai", "vllm"} or name in {"openai", "openrouter", "deepseek", "groq", "together", "fireworks", "mistral", "perplexity", "vllm", "huggingface_tgi"}


def provider_enabled_by_env(route_context: Dict[str, Any]) -> bool:
    if is_ollama_provider(route_context):
        return env_flag("OLLAMA_ENABLED", True)
    # Sovereign mode locks egress: no non-local provider can be enabled, regardless
    # of route/provider config or PROVIDER_LIVE_CALLS. Cannot be re-opened by a route edit.
    if sovereign_mode_enabled():
        return False
    if is_openai_compatible_provider(route_context):
        prefix = provider_env_prefix(route_context.get("provider_name"))
        if prefix and os.getenv(f"{prefix}_ENABLED") is not None:
            return env_flag(f"{prefix}_ENABLED", False)
        if os.getenv("OPENAI_COMPATIBLE_ENABLED") is not None:
            return env_flag("OPENAI_COMPATIBLE_ENABLED", False)
        if (route_context.get("provider_name") or "").strip().lower() == "openai":
            return env_flag("OPENAI_ENABLED", False)
    return False


def route_context_metadata(route_context: Dict[str, Any]) -> Dict[str, Any]:
    return {
        "source": route_context.get("source"),
        "route_id": route_context.get("route_id"),
        "route_name": route_context.get("route_name"),
        "route_slug": route_context.get("route_slug"),
        "route_type": route_context.get("route_type"),
        "route_enabled": route_context.get("route_enabled"),
        "provider_id": route_context.get("provider_id"),
        "provider_name": route_context.get("provider_name"),
        "provider_kind": route_context.get("provider_kind"),
        "provider_base_url": route_context.get("provider_base_url"),
        "provider_enabled": route_context.get("provider_enabled"),
        "model": route_context.get("model"),
        "parameters": route_context.get("parameters") or {},
        "metadata": route_context.get("metadata") or {},
        "provider_metadata": route_context.get("provider_metadata") or {},
    }


def resolve_runtime_route(payload: ChatRequest, agent: Dict[str, Any]) -> Dict[str, Any]:
    db_route = agent.get("model_route") or {}
    db_provider = agent.get("model_provider") or {}
    request_route = {**(payload.route or {}), **(payload.model_route or {})}
    payload_provider_object = payload.provider if isinstance(payload.provider, dict) else {}
    payload_provider_name = None if isinstance(payload.provider, dict) else payload.provider
    request_provider = {**payload_provider_object, **(payload.model_provider or {}), **(payload.provider_metadata or {})}
    route_metadata = payload.route_metadata or {}

    provider_name = first_text(
        payload.provider_name,
        payload_provider_name,
        dict_get_any(request_provider, "name", "provider_name", "providerName", "provider"),
        dict_get_any(request_route, "provider_name", "providerName", "provider"),
        db_provider.get("name"),
        os.getenv("DEFAULT_CHAT_PROVIDER"),
    )
    provider_kind = first_text(
        payload.provider_kind,
        dict_get_any(request_provider, "kind", "provider_kind", "providerKind"),
        dict_get_any(request_route, "provider_kind", "providerKind"),
        db_provider.get("kind"),
        provider_name,
    )

    provider_name_key = (provider_name or "").strip().lower()
    route_model = first_text(
        dict_get_any(request_route, "model_name", "modelName", "model"),
        route_metadata.get("model_name"),
        route_metadata.get("modelName"),
        db_route.get("model_name"),
    )
    payload_model = None if payload.model == DEFAULT_CHAT_MODEL and provider_name_key == "ollama" else payload.model
    provider_model = first_text(os.getenv("OLLAMA_MODEL")) if provider_name_key == "ollama" else None
    model = first_text(route_model, payload_model, provider_model, payload.model)

    route_source = "request_route" if dict_get_any(request_route, "model_name", "modelName", "model") else "db_route" if db_route.get("model_name") else "request"
    route_enabled = bool_or_default(
        db_route.get("is_enabled", dict_get_any(request_route, "is_enabled", "isEnabled")),
        True,
    )
    provider_enabled = bool_or_default(
        db_provider.get("is_enabled", dict_get_any(request_provider, "is_enabled", "isEnabled")),
        True,
    )

    return {
        "source": route_source,
        "route_id": first_text(payload.model_route_id, dict_get_any(request_route, "id", "route_id", "routeId"), agent.get("model_route_id")),
        "route_name": first_text(dict_get_any(request_route, "name", "route_name", "routeName"), db_route.get("name")),
        "route_slug": first_text(dict_get_any(request_route, "slug", "route_slug", "routeSlug"), db_route.get("slug")),
        "route_type": first_text(dict_get_any(request_route, "route_type", "routeType"), db_route.get("route_type"), "chat"),
        "route_enabled": route_enabled,
        "provider_id": first_text(payload.provider_id, dict_get_any(request_provider, "id", "provider_id", "providerId"), db_provider.get("id")),
        "provider_name": provider_name,
        "provider_kind": provider_kind,
        "provider_base_url": first_text(
            payload.provider_base_url,
            dict_get_any(request_provider, "base_url", "baseUrl"),
            dict_get_any(request_route, "provider_base_url", "providerBaseUrl", "base_url", "baseUrl"),
            db_provider.get("base_url"),
        ),
        "provider_enabled": provider_enabled,
        "model": model,
        "parameters": parse_json_object(dict_get_any(request_route, "parameters", "parametersJson")) or parse_json_object(db_route.get("parameters")),
        "metadata": route_metadata or parse_json_object(db_route.get("metadata")),
        "provider_metadata": request_provider or parse_json_object(db_provider.get("metadata")),
    }


def provider_skip_result(
    *,
    provider: str,
    model: str,
    reason: str,
    timeout_ms: int = 0,
    metadata: Optional[Dict[str, Any]] = None,
) -> Dict[str, Any]:
    return {
        "answer": None,
        "provider": provider,
        "model": model,
        "route_health": "skipped",
        "timeout_ms": timeout_ms,
        "error_type": None,
        "metadata": {"reason": reason, **(metadata or {})},
    }


def provider_timeout_seconds(route_context: Dict[str, Any]) -> float:
    if is_ollama_provider(route_context):
        return env_float("OLLAMA_TIMEOUT_SECONDS", 15.0)
    return env_float("PROVIDER_TIMEOUT_SECONDS", env_float("OLLAMA_TIMEOUT_SECONDS", 15.0))
