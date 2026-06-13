"""OpenAI-compatible chat provider (OpenAI, OpenRouter, vLLM, TGI, etc.).

Extracted verbatim from ``main.py`` (Track A, Task A6). This path is reachable
only when sovereign mode is OFF and ``PROVIDER_LIVE_CALLS`` is enabled — both
gates are enforced upstream in
:func:`agentport.providers.try_provider_answer`, so this module performs the
actual outbound HTTP call. The chat-completions URL normalization, API-key
resolution order, request shape, and timeout/error envelopes are preserved
byte-identically.
"""

from __future__ import annotations

import os
from typing import Any, Dict, List, Optional

import httpx

from agentport.models import DEFAULT_CHAT_MODEL, RetrievedChunk
from agentport.providers.routing import (
    provider_env_prefix,
    provider_skip_result,
    provider_timeout_seconds,
    route_context_metadata,
)


def openai_compatible_chat_url(base_url: str) -> str:
    normalized = base_url.rstrip("/")
    if normalized.endswith("/chat/completions"):
        return normalized
    if normalized.endswith("/v1") or normalized.endswith("/openai/v1"):
        return f"{normalized}/chat/completions"
    return f"{normalized}/v1/chat/completions"


def provider_api_key(route_context: Dict[str, Any]) -> tuple[Optional[str], Optional[str]]:
    provider_name = route_context.get("provider_name")
    prefix = provider_env_prefix(provider_name)
    candidates: List[str] = []
    if prefix:
        candidates.append(f"{prefix}_API_KEY")
    if (provider_name or "").strip().lower() == "openai":
        candidates.append("OPENAI_API_KEY")
    candidates.extend(["OPENAI_COMPATIBLE_API_KEY", "OPENAI_API_KEY"])
    for name in candidates:
        value = os.getenv(name)
        if value:
            return value, name
    return None, None


async def try_openai_compatible_answer(
    question: str,
    chunks: List[RetrievedChunk],
    route_context: Dict[str, Any],
    temperature: float,
) -> Dict[str, Any]:
    timeout_seconds = provider_timeout_seconds(route_context)
    timeout_ms = int(timeout_seconds * 1000)
    provider = route_context.get("provider_name") or "openai-compatible"
    model = route_context.get("model") or os.getenv("OPENAI_MODEL", DEFAULT_CHAT_MODEL)
    base_url = route_context.get("provider_base_url") or os.getenv("OPENAI_COMPATIBLE_BASE_URL") or os.getenv("OPENAI_BASE_URL")
    if not base_url:
        return provider_skip_result(
            provider=provider,
            model=model,
            reason="missing_base_url",
            timeout_ms=timeout_ms,
            metadata={"runtime_route": route_context_metadata(route_context)},
        )

    api_key, key_source = provider_api_key(route_context)
    context = "\n\n".join(f"[{chunk.citation_id}] {chunk.text}" for chunk in chunks)
    headers = {"content-type": "application/json"}
    if api_key:
        headers["authorization"] = f"Bearer {api_key}"

    try:
        async with httpx.AsyncClient(timeout=timeout_seconds) as client:
            response = await client.post(
                openai_compatible_chat_url(base_url),
                headers=headers,
                json={
                    "model": model,
                    "stream": False,
                    "temperature": temperature,
                    "messages": [
                        {"role": "system", "content": "Answer only from the provided context and cite sources."},
                        {"role": "user", "content": f"Context:\n{context}\n\nQuestion: {question}"},
                    ],
                },
            )
            response.raise_for_status()
            data = response.json()
            choices = data.get("choices") or []
            content = None
            if choices:
                content = (choices[0].get("message") or {}).get("content") or choices[0].get("text")
            return {
                "answer": content,
                "provider": provider,
                "model": model,
                "route_health": "ok",
                "timeout_ms": timeout_ms,
                "error_type": None,
                "metadata": {
                    "enabled": True,
                    "base_url": base_url.rstrip("/"),
                    "api_key_source": key_source,
                    "runtime_route": route_context_metadata(route_context),
                    "usage": data.get("usage") or {},
                    "id": data.get("id"),
                    "object": data.get("object"),
                },
            }
    except httpx.TimeoutException:
        return {
            "answer": None,
            "provider": provider,
            "model": model,
            "route_health": "error",
            "timeout_ms": timeout_ms,
            "error_type": "timeout",
            "metadata": {"enabled": True, "base_url": base_url.rstrip("/"), "runtime_route": route_context_metadata(route_context)},
        }
    except httpx.HTTPStatusError as exc:
        return {
            "answer": None,
            "provider": provider,
            "model": model,
            "route_health": "error",
            "timeout_ms": timeout_ms,
            "error_type": f"http_{exc.response.status_code}",
            "metadata": {"enabled": True, "base_url": base_url.rstrip("/"), "runtime_route": route_context_metadata(route_context)},
        }
    except Exception as exc:
        return {
            "answer": None,
            "provider": provider,
            "model": model,
            "route_health": "error",
            "timeout_ms": timeout_ms,
            "error_type": exc.__class__.__name__,
            "metadata": {"enabled": True, "base_url": base_url.rstrip("/"), "runtime_route": route_context_metadata(route_context)},
        }
