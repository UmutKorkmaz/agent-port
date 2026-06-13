"""KVKK trace-content redaction policy.

Leaf module (depends only on ``env_flag``) so both ``main`` and
``agentport.persistence.db`` can import it without a circular dependency.
"""

from __future__ import annotations

from typing import Any, Dict, List

from agentport.providers.routing import env_flag

# When set, traces persist citation metadata only (no raw chunk body text).
DEFAULT_TRACE_REDACT_CONTENT = True


def trace_redact_content() -> bool:
    return env_flag("TRACE_REDACT_CONTENT", DEFAULT_TRACE_REDACT_CONTENT)


def redact_trace_chunks(retrieved_chunks: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    """KVKK: persist citation metadata only — no raw chunk body text.

    Keeps chunk_id, document_asset_id, score and citation span/offset so a
    "Kaynak" label (file name + span) can still be rendered, but drops the body.
    """
    redacted: List[Dict[str, Any]] = []
    for chunk in retrieved_chunks:
        metadata = chunk.get("metadata") or {}
        redacted.append(
            {
                "id": chunk.get("id"),
                "citation_id": chunk.get("citation_id"),
                "knowledge_base_id": chunk.get("knowledge_base_id"),
                "score": chunk.get("score"),
                "file_name": metadata.get("file_name"),
                "char_start": metadata.get("char_start"),
                "char_end": metadata.get("char_end"),
                "redacted": True,
            }
        )
    return redacted
