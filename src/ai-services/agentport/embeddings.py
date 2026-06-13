"""Embedding backends for AgentPort.

Extracted verbatim from ``main.py`` (Track A, Task A2). The e5 (multilingual)
backend is the default; the deterministic hash backend is opt-in for CI/tests via
``EMBEDDING_BACKEND=hash``. Prefixing (``query:``/``passage:``), the strict/fallback
behavior, and the active dimension semantics are preserved exactly so the
behavior-frozen embedding contract is unchanged. ``main.py`` re-exports every
public name so existing imports (and ``tests/test_contracts.py``) continue to
resolve from ``main``.
"""

from __future__ import annotations

import hashlib
import logging
import math
import os
import re
from typing import Any, List

from agentport.models import LOCAL_EMBEDDING_MODEL

logger = logging.getLogger("agentport")

# Hash fallback dimension. Kept distinct from the semantic model dimension so the
# two are never confused. The hash backend is only used for CI/tests.
EMBEDDING_DIMENSIONS = 64
# Default semantic embedding: multilingual-e5-base at its native dimension.
DEFAULT_E5_MODEL = "intfloat/multilingual-e5-base"
EMBEDDING_DIM_E5 = 768

HASH_BACKEND_KEYS = {"hash", "local", "agentport-local-hash-64"}

_sentence_transformer_model: Any = None


def use_hash_backend() -> bool:
    """Hash backend is opt-in via EMBEDDING_BACKEND=hash (CI/tests). Default is e5."""
    return os.getenv("EMBEDDING_BACKEND", "e5").strip().lower() in HASH_BACKEND_KEYS


def e5_model_name() -> str:
    return os.getenv("SENTENCE_TRANSFORMERS_MODEL", DEFAULT_E5_MODEL)


def active_embedding_dim() -> int:
    """Dimension of the vectors the active backend actually produces."""
    return EMBEDDING_DIMENSIONS if use_hash_backend() else EMBEDDING_DIM_E5


def hash_embedding(text: str, normalize: bool = True) -> List[float]:
    vector = [0.0] * EMBEDDING_DIMENSIONS
    tokens = re.findall(r"[a-z0-9]+", text.lower())
    for token in tokens:
        digest = hashlib.sha256(token.encode("utf-8")).digest()
        index = int.from_bytes(digest[:2], "big") % EMBEDDING_DIMENSIONS
        sign = 1.0 if digest[2] % 2 == 0 else -1.0
        vector[index] += sign
    if normalize:
        norm = math.sqrt(sum(value * value for value in vector))
        if norm > 0:
            vector = [value / norm for value in vector]
    return [round(value, 6) for value in vector]


def embed_text(text: str, normalize: bool = True, is_query: bool = False) -> List[float]:
    """Embed text. e5 requires distinct query/passage prefixes; the hash backend
    ignores them. ``is_query=True`` for search queries, False for stored chunks."""
    if use_hash_backend():
        return hash_embedding(text, normalize=normalize)
    try:
        return sentence_transformer_embedding(text, normalize=normalize, is_query=is_query)
    except Exception:
        if os.getenv("EMBEDDING_STRICT", "false").strip().lower() == "true":
            raise
        logger.warning(
            "e5 embedding unavailable; falling back to hash backend", exc_info=True
        )
        return hash_embedding(text, normalize=normalize)


def embed_query(text: str, normalize: bool = True) -> List[float]:
    return embed_text(text, normalize=normalize, is_query=True)


def embed_passage(text: str, normalize: bool = True) -> List[float]:
    return embed_text(text, normalize=normalize, is_query=False)


def e5_prefixed(text: str, is_query: bool) -> str:
    # multilingual-e5 expects "query: " for queries and "passage: " for documents.
    return f"{'query' if is_query else 'passage'}: {text}"


def sentence_transformer_embedding(text: str, normalize: bool = True, is_query: bool = False) -> List[float]:
    global _sentence_transformer_model

    if _sentence_transformer_model is None:
        from sentence_transformers import SentenceTransformer

        _sentence_transformer_model = SentenceTransformer(e5_model_name())

    prefixed = e5_prefixed(text, is_query)
    # e5 vectors are stored/compared at their native 768 dimension — no projection.
    raw = _sentence_transformer_model.encode([prefixed], normalize_embeddings=normalize)[0]
    return [round(float(value), 6) for value in raw]


def embedding_backend_name() -> str:
    if use_hash_backend():
        return LOCAL_EMBEDDING_MODEL
    return e5_model_name()


__all__ = [
    "EMBEDDING_DIMENSIONS",
    "EMBEDDING_DIM_E5",
    "DEFAULT_E5_MODEL",
    "HASH_BACKEND_KEYS",
    "use_hash_backend",
    "e5_model_name",
    "active_embedding_dim",
    "hash_embedding",
    "embed_text",
    "embed_query",
    "embed_passage",
    "e5_prefixed",
    "sentence_transformer_embedding",
    "embedding_backend_name",
]
