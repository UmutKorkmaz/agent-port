"""Local Ollama chat provider.

Extracted verbatim from ``main.py`` (Track A, Task A6). Ollama is the only
provider permitted under the sovereign egress lock, so its call path stays
intact and unguarded by ``sovereign_mode_enabled`` (that gate is applied
upstream in :func:`agentport.providers.try_provider_answer`). Behavior — the
``/api/chat`` request shape, timeout/error envelopes, and metadata — is
preserved byte-identically.
"""

from __future__ import annotations

import os
from typing import Any, Dict, List

import httpx

from agentport.models import RetrievedChunk
from agentport.providers.routing import (
    env_flag,
    provider_timeout_seconds,
    route_context_metadata,
)


async def try_ollama_answer(question: str, chunks: List[RetrievedChunk], route_context: Dict[str, Any]) -> Dict[str, Any]:
    timeout_seconds = provider_timeout_seconds(route_context)
    timeout_ms = int(timeout_seconds * 1000)
    base_url = (route_context.get("provider_base_url") or os.getenv("OLLAMA_BASE_URL", "http://localhost:11434")).rstrip("/")
    model = route_context.get("model") or os.getenv("OLLAMA_MODEL", "llama3.2")
    provider = route_context.get("provider_name") or "ollama"
    disabled = {
        "answer": None,
        "provider": provider,
        "model": model,
        "route_health": "skipped",
        "timeout_ms": timeout_ms,
        "error_type": None,
        "metadata": {"enabled": False, "runtime_route": route_context_metadata(route_context)},
    }
    if not env_flag("OLLAMA_ENABLED", True):
        return disabled
    context = "\n\n".join(f"[{chunk.citation_id}] {chunk.text}" for chunk in chunks)
    if not context:
        return {
            **disabled,
            "route_health": "skipped",
            "metadata": {"enabled": True, "reason": "empty_context", "runtime_route": route_context_metadata(route_context)},
        }
    try:
        async with httpx.AsyncClient(timeout=timeout_seconds) as client:
            response = await client.post(
                f"{base_url}/api/chat",
                json={
                    "model": model,
                    "stream": False,
                    "messages": [
                        {"role": "system", "content": "Answer only from the provided context and cite sources."},
                        {"role": "user", "content": f"Context:\n{context}\n\nQuestion: {question}"},
                    ],
                },
            )
            response.raise_for_status()
            data = response.json()
            return {
                "answer": data.get("message", {}).get("content"),
                "provider": provider,
                "model": model,
                "route_health": "ok",
                "timeout_ms": timeout_ms,
                "error_type": None,
                "metadata": {
                    "enabled": True,
                    "base_url": base_url,
                    "runtime_route": route_context_metadata(route_context),
                    "done": data.get("done"),
                    "total_duration": data.get("total_duration"),
                    "load_duration": data.get("load_duration"),
                    "prompt_eval_count": data.get("prompt_eval_count"),
                    "eval_count": data.get("eval_count"),
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
            "metadata": {"enabled": True, "base_url": base_url, "runtime_route": route_context_metadata(route_context)},
        }
    except httpx.HTTPStatusError as exc:
        return {
            "answer": None,
            "provider": provider,
            "model": model,
            "route_health": "error",
            "timeout_ms": timeout_ms,
            "error_type": f"http_{exc.response.status_code}",
            "metadata": {"enabled": True, "base_url": base_url, "runtime_route": route_context_metadata(route_context)},
        }
    except Exception as exc:
        return {
            "answer": None,
            "provider": provider,
            "model": model,
            "route_health": "error",
            "timeout_ms": timeout_ms,
            "error_type": exc.__class__.__name__,
            "metadata": {"enabled": True, "base_url": base_url, "runtime_route": route_context_metadata(route_context)},
        }
