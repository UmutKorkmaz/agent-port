"""Chat provider package: route resolution, env gating, and provider calls.

Extracted verbatim from ``main.py`` (Track A, Task A6). The orchestration entry
point :func:`try_provider_answer` lives here; it walks the skip-reason taxonomy
in order, applies the sovereign egress lock and ``PROVIDER_LIVE_CALLS`` gate
byte-identically, and dispatches to the local Ollama or OpenAI-compatible
provider modules.

Sub-modules:

* :mod:`agentport.providers.routing` — env helpers, route resolution, the
  sovereign gate (:func:`sovereign_mode_enabled`), cost estimation, and the
  ``provider_skip_result`` skip-reason envelope.
* :mod:`agentport.providers.ollama` — local Ollama call path (sovereign-allowed).
* :mod:`agentport.providers.openai_compatible` — outbound OpenAI-compatible call
  path (sovereign-locked unless disabled + live calls enabled).

All public names are re-exported from ``main`` so existing imports (including
``tests/test_contracts.py``) keep resolving them from ``main`` unchanged.
"""

from __future__ import annotations

import sys
from typing import Any, Dict, List

from agentport.models import DEFAULT_CHAT_MODEL, RetrievedChunk
from agentport.providers.ollama import try_ollama_answer
from agentport.providers.openai_compatible import (
    openai_compatible_chat_url,
    provider_api_key,
    try_openai_compatible_answer,
)
from agentport.providers.routing import (
    DEFAULT_SOVEREIGN_MODE,
    bool_or_default,
    dict_get_any,
    env_flag,
    env_float,
    estimate_provider_cost,
    first_text,
    is_ollama_provider,
    is_openai_compatible_provider,
    parse_json_object,
    positive_env_float,
    provider_enabled_by_env,
    provider_env_prefix,
    provider_key,
    provider_skip_result,
    provider_timeout_seconds,
    resolve_runtime_route,
    route_context_metadata,
    sovereign_mode_enabled,
)


def _provider_call(name: str):
    """Resolve a provider call target, preferring a re-export patched on ``main``.

    Before this extraction, ``try_provider_answer`` and the provider call
    functions co-resided in ``main``, so tests that ``monkeypatch.setattr(main,
    "try_ollama_answer", ...)`` patched the exact symbol the dispatcher used. We
    preserve that contract by resolving the call target off ``main`` at call
    time when ``main`` is loaded (the service entry point), falling back to this
    package's own implementation otherwise. Dispatch logic is unchanged.
    """
    main_module = sys.modules.get("main")
    if main_module is not None:
        target = getattr(main_module, name, None)
        if target is not None:
            return target
    return globals()[name]


async def try_provider_answer(
    question: str,
    chunks: List[RetrievedChunk],
    route_context: Dict[str, Any],
    temperature: float,
) -> Dict[str, Any]:
    provider = route_context.get("provider_name") or route_context.get("provider_kind") or "local"
    model = route_context.get("model") or DEFAULT_CHAT_MODEL
    timeout_ms = int(provider_timeout_seconds(route_context) * 1000)
    route_metadata = route_context_metadata(route_context)
    is_local_ollama = is_ollama_provider(route_context)
    live_calls_enabled = env_flag("PROVIDER_LIVE_CALLS", False)

    if not chunks:
        return provider_skip_result(
            provider=provider,
            model=model,
            reason="empty_context",
            timeout_ms=timeout_ms,
            metadata={"runtime_route": route_metadata},
        )
    if not route_context.get("route_enabled", True):
        return provider_skip_result(
            provider=provider,
            model=model,
            reason="route_disabled",
            timeout_ms=timeout_ms,
            metadata={"runtime_route": route_metadata, "live_calls_enabled": live_calls_enabled},
        )
    if not route_context.get("provider_enabled", True):
        return provider_skip_result(
            provider=provider,
            model=model,
            reason="provider_disabled",
            timeout_ms=timeout_ms,
            metadata={"runtime_route": route_metadata, "live_calls_enabled": live_calls_enabled},
        )
    if sovereign_mode_enabled() and not is_local_ollama:
        return provider_skip_result(
            provider=provider,
            model=model,
            reason="sovereign_mode",
            timeout_ms=timeout_ms,
            metadata={"runtime_route": route_metadata, "sovereign": True},
        )
    if not provider_enabled_by_env(route_context):
        return provider_skip_result(
            provider=provider,
            model=model,
            reason="provider_env_disabled",
            timeout_ms=timeout_ms,
            metadata={"runtime_route": route_metadata, "live_calls_enabled": live_calls_enabled},
        )

    if is_local_ollama:
        return await _provider_call("try_ollama_answer")(question, chunks, route_context)
    if not live_calls_enabled:
        return provider_skip_result(
            provider=provider,
            model=model,
            reason="live_calls_disabled",
            timeout_ms=timeout_ms,
            metadata={"runtime_route": route_metadata, "live_calls_enabled": False},
        )
    if is_openai_compatible_provider(route_context):
        return await _provider_call("try_openai_compatible_answer")(question, chunks, route_context, temperature)

    return provider_skip_result(
        provider=provider,
        model=model,
        reason="unsupported_provider",
        timeout_ms=timeout_ms,
        metadata={"runtime_route": route_metadata, "live_calls_enabled": live_calls_enabled},
    )


__all__ = [
    "DEFAULT_SOVEREIGN_MODE",
    "bool_or_default",
    "dict_get_any",
    "env_flag",
    "env_float",
    "estimate_provider_cost",
    "first_text",
    "is_ollama_provider",
    "is_openai_compatible_provider",
    "openai_compatible_chat_url",
    "parse_json_object",
    "positive_env_float",
    "provider_api_key",
    "provider_enabled_by_env",
    "provider_env_prefix",
    "provider_key",
    "provider_skip_result",
    "provider_timeout_seconds",
    "resolve_runtime_route",
    "route_context_metadata",
    "sovereign_mode_enabled",
    "try_ollama_answer",
    "try_openai_compatible_answer",
    "try_provider_answer",
]
