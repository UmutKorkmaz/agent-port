"""Document chunking, content hashing, and ingestion-metadata helpers.

Extracted verbatim from ``main.py`` (Track A, Task A3). These helpers compute the
content hash, the deterministic per-document idempotency key, the ingestion
metadata envelope, and the paragraph-aware chunk spans used during ingestion. The
behavior is preserved exactly — same hashing, same filename normalization, same
chunk boundaries — so existing imports (and ``tests/test_contracts.py``) continue
to resolve these names from ``main``.

``safe_filename`` is colocated here because the idempotency key normalizes the
file name through it; keeping it in this module avoids a circular import with
``main.py`` while preserving the verified normalization behavior.
"""

from __future__ import annotations

import hashlib
import re
from typing import Any, Dict, List
from uuid import UUID


def safe_filename(file_name: str) -> str:
    value = re.sub(r"[^a-zA-Z0-9._-]+", "-", file_name).strip("-")
    return value or "document.txt"


def content_hash(payload: bytes) -> str:
    return hashlib.sha256(payload).hexdigest()


def ingestion_idempotency_key(dataset_id: UUID, knowledge_base_id: UUID, file_name: str, payload_hash: str) -> str:
    fingerprint = ":".join(
        [
            str(dataset_id),
            str(knowledge_base_id),
            safe_filename(file_name).lower(),
            payload_hash,
        ]
    )
    return hashlib.sha256(fingerprint.encode("utf-8")).hexdigest()


def ingestion_metadata(request_id: str, payload_hash: str, idempotency_key: str, source: str = "ingest") -> Dict[str, Any]:
    return {
        "request_id": request_id,
        "sha256": payload_hash,
        "content_hash": payload_hash,
        "idempotency_key": idempotency_key,
        "source": source,
    }


def chunk_text(text: str, max_chars: int = 900, overlap: int = 120) -> List[str]:
    normalized = re.sub(r"\n{3,}", "\n\n", text).strip()
    if not normalized:
        return []
    paragraphs = [part.strip() for part in re.split(r"\n\s*\n", normalized) if part.strip()]
    chunks: List[str] = []
    current = ""
    for paragraph in paragraphs:
        candidate = f"{current}\n\n{paragraph}".strip() if current else paragraph
        if len(candidate) <= max_chars:
            current = candidate
            continue
        if current:
            chunks.append(current)
        if len(paragraph) <= max_chars:
            current = paragraph
            continue
        for start in range(0, len(paragraph), max_chars - overlap):
            piece = paragraph[start : start + max_chars].strip()
            if piece:
                chunks.append(piece)
        current = ""
    if current:
        chunks.append(current)
    return chunks


__all__ = [
    "safe_filename",
    "content_hash",
    "ingestion_idempotency_key",
    "ingestion_metadata",
    "chunk_text",
]
