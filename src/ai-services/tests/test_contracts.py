import asyncio
import math

import pytest
from fastapi.testclient import TestClient

import main as ai_main
from main import (
    ChatRequest,
    EMBEDDING_DIMENSIONS,
    NO_ANSWER_MESSAGE,
    RetrievedChunk,
    app,
    build_rag_answer,
    chunk_text,
    content_hash,
    estimate_provider_cost,
    extractive_answer,
    hash_embedding,
    ingestion_idempotency_key,
    ingestion_metadata,
    is_active_document_status,
    is_deleted_status,
    normalized_provider_response,
    resolve_runtime_route,
    try_provider_answer,
    vector_literal,
)


client = TestClient(app)


def assert_phase1_identity(body: dict) -> None:
    assert body["service"] == "ai-services"
    assert body["phase"] == "phase_1"
    assert body["request_id"] == "req-test"
    assert body["trace_id"] == "trace-test"


@pytest.mark.parametrize("path", ["/health", "/health/live", "/health/ready"])
def test_health_endpoints_return_phase1_status(path: str) -> None:
    response = client.get(
        path,
        headers={"x-request-id": "req-test", "x-trace-id": "trace-test"},
    )

    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "ok"
    assert body["live"] is True
    assert body["ready"] is True
    assert body["checks"]["phase_1_contracts"] == "ok"
    assert body["checks"]["embedding_backend"] == "agentport-local-hash-64"
    assert_phase1_identity(body)


def test_embed_returns_stable_normalized_hash_vectors() -> None:
    response = client.post(
        "/v1/embed",
        headers={"x-request-id": "req-test", "x-trace-id": "trace-test"},
        json={"texts": ["refund policy", "refund policy"]},
    )

    assert response.status_code == 200
    body = response.json()
    assert body["operation"] == "embed"
    assert body["input_count"] == 2
    assert body["dimensions"] == EMBEDDING_DIMENSIONS
    assert body["embeddings"][0] == body["embeddings"][1]
    assert math.isclose(sum(value * value for value in body["embeddings"][0]), 1.0, abs_tol=1e-5)
    assert_phase1_identity(body)


def test_hash_embedding_handles_empty_text_as_zero_vector() -> None:
    vector = hash_embedding("")

    assert len(vector) == EMBEDDING_DIMENSIONS
    assert all(value == 0.0 for value in vector)
    assert vector_literal(vector).startswith("[0.000000,")


def test_hash_embedding_is_stable_across_case_and_spacing() -> None:
    first = hash_embedding("Refund   Policy")
    second = hash_embedding("refund policy")

    assert first == second
    assert math.isclose(sum(value * value for value in first), 1.0, abs_tol=1e-5)


def test_chunk_text_keeps_small_markdown_together() -> None:
    text = "# Refunds\n\nRefunds are available within 14 days.\n\nContact support."

    chunks = chunk_text(text, max_chars=120, overlap=20)

    assert chunks == [text]


def test_chunk_text_splits_large_paragraph_with_overlap() -> None:
    text = "alpha " * 260

    chunks = chunk_text(text, max_chars=200, overlap=40)

    assert len(chunks) > 1
    assert all(len(chunk) <= 200 for chunk in chunks)


def test_extractive_answer_returns_cited_sentence() -> None:
    chunk = type(
        "Chunk",
        (),
        {
            "citation_id": "policy.md:abc12345:1",
            "text": "Refunds are available within 14 days. Billing questions go to support.",
        },
    )()

    answer = extractive_answer("What about refunds?", [chunk])

    assert answer == "Refunds are available within 14 days. [policy.md:abc12345:1]"


def test_build_rag_answer_returns_no_answer_when_scores_are_below_threshold() -> None:
    chunks = [
        RetrievedChunk(
            id="chunk-1",
            citation_id="policy.md:abc12345:1",
            knowledge_base_id="kb-1",
            text="Office hours are 9 to 5.",
            score=0.05,
        )
    ]

    answer, relevant_chunks, no_answer = build_rag_answer("How do refunds work?", chunks, score_threshold=0.2)

    assert answer == NO_ANSWER_MESSAGE
    assert relevant_chunks == []
    assert no_answer is True


def test_build_rag_answer_uses_relevant_chunks_at_or_above_threshold() -> None:
    chunks = [
        RetrievedChunk(
            id="chunk-1",
            citation_id="policy.md:abc12345:1",
            knowledge_base_id="kb-1",
            text="Refunds are available within 14 days.",
            score=0.2,
        )
    ]

    answer, relevant_chunks, no_answer = build_rag_answer("How do refunds work?", chunks, score_threshold=0.2)

    assert answer == "Refunds are available within 14 days. [policy.md:abc12345:1]"
    assert relevant_chunks == chunks
    assert no_answer is False


def test_content_hash_and_idempotency_key_are_stable() -> None:
    dataset_id = "113eec5e-2477-4f45-bdcc-bfac3d8a5ff6"
    knowledge_base_id = "4a2c699a-d654-49e8-af03-30c3a622b381"
    payload_hash = content_hash(b"refund policy")

    first = ingestion_idempotency_key(dataset_id, knowledge_base_id, "Support Policy.md", payload_hash)
    second = ingestion_idempotency_key(dataset_id, knowledge_base_id, "support-policy.md", payload_hash)
    changed = ingestion_idempotency_key(dataset_id, knowledge_base_id, "Support Policy.md", content_hash(b"changed"))

    assert payload_hash == content_hash(b"refund policy")
    assert len(payload_hash) == 64
    assert first == second
    assert first != changed


def test_ingestion_metadata_preserves_content_hash_and_idempotency_key() -> None:
    payload_hash = content_hash(b"document")
    key = "idem-key"

    metadata = ingestion_metadata("req-test", payload_hash, key)

    assert metadata["request_id"] == "req-test"
    assert metadata["sha256"] == payload_hash
    assert metadata["content_hash"] == payload_hash
    assert metadata["idempotency_key"] == key
    assert metadata["source"] == "ingest"


def test_document_status_helpers_separate_active_from_deleted() -> None:
    assert is_active_document_status("succeeded") is True
    assert is_active_document_status("deleted") is False
    assert is_deleted_status("archived") is True
    assert is_deleted_status("ready") is False


def test_provider_cost_uses_fixed_fee_env(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("AGENTPORT_FEE_FIXED_USD", "0.10")
    monkeypatch.delenv("AGENTPORT_INPUT_TOKEN_PRICE_PER_MILLION_USD", raising=False)
    monkeypatch.delenv("AGENTPORT_OUTPUT_TOKEN_PRICE_PER_MILLION_USD", raising=False)

    assert estimate_provider_cost(12, 8) == 0.1
    provider_response = normalized_provider_response(
        provider="local",
        model="local-extractive-rag",
        route_health="ok",
        timeout_ms=0,
        fallback_mode="extractive",
        question="What is the refund policy?",
        answer="Refunds are available within 14 days.",
    )

    assert provider_response.estimated_cost == 0.1


def test_runtime_route_explicit_request_route_overrides_saved_db_route() -> None:
    payload = ChatRequest(
        model="request-model",
        provider_name="openai",
        provider_base_url="https://api.openai.com/v1",
        model_route={"model": "request-route-model"},
        provider_metadata={"kind": "openai-compatible"},
    )
    agent = {
        "model_route_id": "route-1",
        "model_route": {
            "name": "Default Ollama Route",
            "slug": "ollama-default",
            "model_name": "db-route-model",
            "route_type": "chat",
            "is_enabled": True,
            "parameters": "{}",
            "metadata": '{"bootstrap":"local"}',
        },
        "model_provider": {
            "id": "provider-1",
            "name": "ollama",
            "kind": "ollama",
            "base_url": "http://localhost:11434",
            "is_enabled": True,
            "metadata": '{"storesProviderKeys":false}',
        },
    }

    runtime_route = resolve_runtime_route(payload, agent)

    assert runtime_route["source"] == "request_route"
    assert runtime_route["model"] == "request-route-model"
    assert runtime_route["provider_name"] == "openai"
    assert runtime_route["provider_kind"] == "openai-compatible"
    assert runtime_route["provider_base_url"] == "https://api.openai.com/v1"
    assert runtime_route["metadata"] == {"bootstrap": "local"}


def test_ollama_request_route_uses_env_model_when_no_db_route(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("OLLAMA_MODEL", "gemma4:e4b")
    payload = ChatRequest(provider={"name": "ollama", "kind": "ollama"})
    agent = {"model_route_id": None, "model_route": {}, "model_provider": {}}

    runtime_route = resolve_runtime_route(payload, agent)

    assert runtime_route["source"] == "request"
    assert runtime_route["model"] == "gemma4:e4b"
    assert runtime_route["provider_name"] == "ollama"


def test_provider_answer_allows_local_ollama_when_cloud_live_calls_are_disabled(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("PROVIDER_LIVE_CALLS", "false")
    monkeypatch.setenv("OLLAMA_ENABLED", "true")

    async def fake_ollama_answer(question, chunks, route_context):
        return {
            "answer": "Refunds are available within 14 days. [policy.md:abc12345:1]",
            "provider": "ollama",
            "model": route_context["model"],
            "route_health": "ok",
            "timeout_ms": 15000,
            "error_type": None,
            "metadata": {"enabled": True},
        }

    monkeypatch.setattr(ai_main, "try_ollama_answer", fake_ollama_answer)
    chunk = RetrievedChunk(
        id="chunk-1",
        citation_id="policy.md:abc12345:1",
        knowledge_base_id="kb-1",
        text="Refunds are available within 14 days.",
        score=0.9,
    )

    result = asyncio.run(
        try_provider_answer(
            "How do refunds work?",
            [chunk],
            {
                "provider_name": "ollama",
                "provider_kind": "ollama",
                "provider_base_url": "http://localhost:11434",
                "provider_enabled": True,
                "route_enabled": True,
                "model": "gemma4:e4b",
            },
            0.0,
        )
    )

    assert result["route_health"] == "ok"
    assert result["provider"] == "ollama"
    assert result["model"] == "gemma4:e4b"


def test_provider_answer_skips_cloud_live_calls_when_disabled(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("PROVIDER_LIVE_CALLS", "false")
    monkeypatch.setenv("OPENAI_ENABLED", "true")
    chunk = RetrievedChunk(
        id="chunk-1",
        citation_id="policy.md:abc12345:1",
        knowledge_base_id="kb-1",
        text="Refunds are available within 14 days.",
        score=0.9,
    )

    result = asyncio.run(
        try_provider_answer(
            "How do refunds work?",
            [chunk],
            {
                "provider_name": "openai",
                "provider_kind": "openai-compatible",
                "provider_base_url": "https://api.openai.com/v1",
                "provider_enabled": True,
                "route_enabled": True,
                "model": "gpt-4.1-mini",
            },
            0.0,
        )
    )

    assert result["route_health"] == "skipped"
    assert result["metadata"]["reason"] == "live_calls_disabled"
    assert result["model"] == "gpt-4.1-mini"


def test_chat_requires_agent_id_for_phase1() -> None:
    response = client.post(
        "/v1/chat",
        headers={"x-request-id": "req-test", "x-trace-id": "trace-test"},
        json={"question": "hello"},
    )

    assert response.status_code == 400
    assert response.json()["detail"] == "agent_id is required for Phase 1 chat."


def test_contract_validation_rejects_empty_embed_texts() -> None:
    response = client.post(
        "/v1/embed",
        headers={"x-request-id": "req-test", "x-trace-id": "trace-test"},
        json={"texts": []},
    )

    assert response.status_code == 422
