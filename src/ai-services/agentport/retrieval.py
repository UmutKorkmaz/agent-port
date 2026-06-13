"""RAG retrieval orchestration: score thresholding, no-answer fallback, and
extractive answer synthesis.

Extracted verbatim from ``main.py`` (Track A, Task A5). The raw-SQL
``retrieve_chunks`` data-access function (including the cross-runtime embedding
dimension-mismatch ``409``) lives in :mod:`agentport.persistence.db`; this module
holds the orchestration that sits on top of it — filtering retrieved chunks by
score, deciding when to emit the no-answer message, and building the cited
extractive answer.

The score floor (:data:`DEFAULT_RAG_SCORE_THRESHOLD` = ``0.25``) and the
no-answer message are defined here as the single source of truth and re-exported
from ``main`` so existing imports (including ``tests/test_contracts.py``) keep
resolving these names from ``main`` unchanged. Behavior is preserved exactly —
same threshold semantics, same sentence-scoring heuristic, same citation
formatting.
"""

from __future__ import annotations

import re
from typing import List

from agentport.models import RetrievedChunk

NO_ANSWER_MESSAGE = "I could not find enough relevant information in the ingested documents to answer that."
# e5 cosine similarity needs a meaningful floor so the no-answer fallback fires.
DEFAULT_RAG_SCORE_THRESHOLD = 0.25


def filter_chunks_by_score(chunks: List[RetrievedChunk], score_threshold: float) -> List[RetrievedChunk]:
    return [chunk for chunk in chunks if chunk.score >= score_threshold]


def should_use_no_answer(chunks: List[RetrievedChunk], score_threshold: float) -> bool:
    if not chunks:
        return True
    return max(chunk.score for chunk in chunks) < score_threshold


def build_rag_answer(question: str, chunks: List[RetrievedChunk], score_threshold: float) -> tuple[str, List[RetrievedChunk], bool]:
    relevant_chunks = filter_chunks_by_score(chunks, score_threshold)
    if not relevant_chunks:
        return NO_ANSWER_MESSAGE, [], True
    return extractive_answer(question, relevant_chunks), relevant_chunks, False


def extractive_answer(question: str, chunks: List[RetrievedChunk]) -> str:
    if not chunks:
        return NO_ANSWER_MESSAGE
    query_terms = meaningful_terms(question)
    best_sentence = ""
    best_score = -1
    best_citation = chunks[0].citation_id
    for chunk in chunks:
        sentences = re.split(r"(?<=[.!?])\s+", chunk.text.strip())
        for sentence in sentences:
            terms = meaningful_terms(sentence)
            score = len(query_terms & terms) + len({singularize(term) for term in query_terms} & {singularize(term) for term in terms})
            if score > best_score:
                best_sentence = sentence.strip()
                best_score = score
                best_citation = chunk.citation_id
    if not best_sentence:
        best_sentence = chunks[0].text.strip().split("\n")[0]
    return f"{best_sentence} [{best_citation}]"


def meaningful_terms(text: str) -> set[str]:
    stop_words = {
        "a",
        "an",
        "and",
        "about",
        "does",
        "document",
        "is",
        "it",
        "of",
        "say",
        "says",
        "the",
        "this",
        "to",
        "what",
    }
    return {term for term in re.findall(r"[a-z0-9]+", text.lower()) if term not in stop_words}


def singularize(term: str) -> str:
    return term[:-1] if len(term) > 3 and term.endswith("s") else term
