"""
AgentPort AI Services
Phase 1 local document QA runtime: ingestion, deterministic embeddings,
pgvector retrieval, and extractive chat fallback.
"""

from __future__ import annotations

import json
import logging
import os
import time
from contextlib import asynccontextmanager
from typing import Any, Dict, List, Optional
from uuid import UUID, uuid4

from fastapi import FastAPI, File, Form, HTTPException, Request, UploadFile, status

# Structured application logger. Replaces ad-hoc print() so lifespan, provider
# errors, DB failures, and the embedding-fallback path emit on the standard
# logging pipeline (configurable via the host's logging config / LOG_LEVEL).
logger = logging.getLogger("agentport")

# Pydantic wire models live in agentport.models; re-exported here so existing
# imports from `main` (including tests/test_contracts.py) keep resolving.
from agentport.models import (  # noqa: F401
    DEFAULT_CHAT_MODEL,
    LOCAL_EMBEDDING_MODEL,
    PHASE,
    SERVICE_NAME,
    ChatMessage,
    ChatRequest,
    ChatResponse,
    Citation,
    DeleteDocumentResponse,
    EmbedRequest,
    EmbedResponse,
    HealthResponse,
    IngestResponse,
    ProviderResponse,
    RequestIdentity,
    RetrievalSummary,
    RetrievedChunk,
    RetrieveRequest,
    RetrieveResponse,
    TrainJobConfig,
    TrainMetrics,
    TrainRequest,
    TrainResponse,
)
from agentport.embeddings import (  # noqa: F401
    DEFAULT_E5_MODEL,
    EMBEDDING_DIM_E5,
    EMBEDDING_DIMENSIONS,
    HASH_BACKEND_KEYS,
    active_embedding_dim,
    e5_model_name,
    e5_prefixed,
    embed_passage,
    embed_query,
    embed_text,
    embedding_backend_name,
    hash_embedding,
    sentence_transformer_embedding,
    use_hash_backend,
)
from agentport.chunking import (  # noqa: F401
    chunk_text,
    content_hash,
    ingestion_idempotency_key,
    ingestion_metadata,
    safe_filename,
)

# Postgres persistence (pool + raw-SQL data access) lives in
# agentport.persistence.db; re-exported here so existing imports from `main`
# keep resolving. Connections are leased from a module-level pool opened in
# the lifespan below.
from agentport.persistence.db import (  # noqa: F401
    archive_replaced_documents,
    close_pool,
    connection,
    db_connect,
    estimate_tokens,
    find_existing_document,
    load_agent,
    next_document_version,
    open_pool,
    retrieve_chunks,
    save_run_trace,
    utc_now,
    vector_literal,
    write_document_chunks,
)

# RAG retrieval orchestration (score thresholding, no-answer fallback,
# extractive answer synthesis) lives in agentport.retrieval; re-exported here so
# existing imports from `main` (including tests/test_contracts.py) keep
# resolving. NO_ANSWER_MESSAGE and DEFAULT_RAG_SCORE_THRESHOLD are defined there
# as the single source of truth — the 0.25 score floor is preserved exactly.
from agentport.retrieval import (  # noqa: F401
    DEFAULT_RAG_SCORE_THRESHOLD,
    NO_ANSWER_MESSAGE,
    build_rag_answer,
    extractive_answer,
    filter_chunks_by_score,
    meaningful_terms,
    should_use_no_answer,
    singularize,
)

# Chat provider routing/gating/calls live in agentport.providers; re-exported
# here so existing imports from `main` (including tests/test_contracts.py) keep
# resolving. The sovereign egress short-circuit (skip reason `sovereign_mode`)
# and the `PROVIDER_LIVE_CALLS` gate are preserved byte-identically there.
from agentport.providers import (  # noqa: F401
    DEFAULT_SOVEREIGN_MODE,
    bool_or_default,
    dict_get_any,
    env_flag,
    env_float,
    estimate_provider_cost,
    first_text,
    is_ollama_provider,
    is_openai_compatible_provider,
    openai_compatible_chat_url,
    parse_json_object,
    positive_env_float,
    provider_api_key,
    provider_enabled_by_env,
    provider_env_prefix,
    provider_key,
    provider_skip_result,
    provider_timeout_seconds,
    resolve_runtime_route,
    route_context_metadata,
    sovereign_mode_enabled,
    try_ollama_answer,
    try_openai_compatible_answer,
    try_provider_answer,
)

# Document ingestion (object storage, text extraction incl. the .docx path, and
# the ingest/reingest DB write flow) lives in agentport.ingestion; re-exported
# here so existing imports from `main` keep resolving. SQL/transactions are
# byte-identical to the prior inline route bodies.
from agentport.ingestion import (  # noqa: F401
    extract_docx_text,
    extract_text,
    fetch_from_minio,
    ingest_object_key,
    store_in_minio,
    write_ingested_document,
    write_reingested_document,
)

# Phase 2 sklearn TF-IDF training lives in agentport.training; re-exported here
# so existing imports from `main` keep resolving. Logic is byte-identical.
from agentport.training import run_training_job  # noqa: F401
from agentport.trace_policy import (  # noqa: F401
    DEFAULT_TRACE_REDACT_CONTENT,
    redact_trace_chunks,
    trace_redact_content,
)


SUPPORTED_EXTENSIONS = {".txt", ".md", ".pdf", ".docx"}
ACTIVE_DOCUMENT_STATUSES = {"active", "ready", "succeeded"}
ACTIVE_KNOWLEDGE_BASE_STATUSES = {"active", "ready", "succeeded"}
DELETED_STATUSES = {"archived", "deleted"}


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("AI Services starting")
    # Open the Postgres connection pool lazily at startup. Connections are
    # established in the background (non-blocking), so a transiently
    # unreachable DB does not block app boot — matching the prior per-call
    # connect semantics where failures surfaced on the first request.
    open_pool()
    try:
        yield
    finally:
        close_pool()
        logger.info("AI Services shutting down")


app = FastAPI(
    title="AgentPort AI Services",
    version="0.2.0",
    lifespan=lifespan,
)


def _extract_internal_token(request: Request) -> Optional[str]:
    header = request.headers.get("x-internal-token")
    if header:
        return header.strip()
    authorization = request.headers.get("authorization")
    if authorization and authorization.lower().startswith("bearer "):
        return authorization[7:].strip()
    return None


@app.middleware("http")
async def internal_auth_middleware(request: Request, call_next):
    # When AI_SERVICES_INTERNAL_TOKEN is set, every /v1/* call must carry it
    # (Bearer or x-internal-token). Unset => open, so local dev/tests still work.
    expected = os.getenv("AI_SERVICES_INTERNAL_TOKEN")
    if expected and request.url.path.startswith("/v1/"):
        provided = _extract_internal_token(request)
        if not provided or provided != expected.strip():
            from fastapi.responses import JSONResponse

            return JSONResponse(
                status_code=status.HTTP_401_UNAUTHORIZED,
                content={"detail": "Missing or invalid internal service token."},
            )
    return await call_next(request)


def request_identity(request: Request) -> RequestIdentity:
    request_id = request.headers.get("x-request-id") or str(uuid4())
    trace_id = request.headers.get("x-trace-id") or request_id
    return RequestIdentity(request_id=request_id, trace_id=trace_id)


def health_payload(request: Request) -> HealthResponse:
    identity = request_identity(request)
    return HealthResponse(
        request_id=identity.request_id,
        trace_id=identity.trace_id,
        checks={
            "phase_1_contracts": "ok",
            "embedding_backend": embedding_backend_name(),
        },
    )


def is_deleted_status(status_value: Optional[str]) -> bool:
    return (status_value or "").strip().lower() in DELETED_STATUSES


def is_active_document_status(status_value: Optional[str]) -> bool:
    return (status_value or "").strip().lower() in ACTIVE_DOCUMENT_STATUSES


def configured_rag_score_threshold(value: Optional[float] = None) -> float:
    if value is not None:
        return value
    raw = os.getenv("RAG_SCORE_THRESHOLD")
    if raw is None:
        return DEFAULT_RAG_SCORE_THRESHOLD
    try:
        return float(raw)
    except ValueError:
        return DEFAULT_RAG_SCORE_THRESHOLD


def retrieval_summary(top_k: int, score_threshold: float, chunks: List[RetrievedChunk], no_answer: bool) -> RetrievalSummary:
    return RetrievalSummary(
        top_k=top_k,
        score_threshold=score_threshold,
        max_score=max((chunk.score for chunk in chunks), default=None),
        no_answer=no_answer,
    )


def normalized_provider_response(
    *,
    provider: str,
    model: str,
    route_health: str,
    timeout_ms: int,
    fallback_mode: str,
    question: str,
    answer: str,
    error_type: Optional[str] = None,
    metadata: Optional[Dict[str, Any]] = None,
) -> ProviderResponse:
    input_tokens = estimate_tokens(question)
    output_tokens = estimate_tokens(answer)
    estimated_cost = estimate_provider_cost(input_tokens, output_tokens)
    return ProviderResponse(
        provider=provider,
        model=model,
        route_health=route_health,
        timeout_ms=timeout_ms,
        error_type=error_type,
        fallback_mode=fallback_mode,
        input_tokens=input_tokens,
        output_tokens=output_tokens,
        total_tokens=input_tokens + output_tokens,
        estimated_cost=estimated_cost,
        metadata=metadata or {},
    )


@app.get("/health", response_model=HealthResponse)
async def health(request: Request):
    return health_payload(request)


@app.get("/health/live", response_model=HealthResponse)
async def health_live(request: Request):
    return health_payload(request)


@app.get("/health/ready", response_model=HealthResponse)
async def health_ready(request: Request):
    return health_payload(request)


@app.post("/v1/embed", response_model=EmbedResponse)
async def embed(payload: EmbedRequest, request: Request):
    identity = request_identity(request)
    embeddings = [embed_text(text, normalize=payload.normalize) for text in payload.texts]
    return EmbedResponse(
        request_id=identity.request_id,
        trace_id=identity.trace_id,
        model=payload.model,
        input_count=len(payload.texts),
        dimensions=active_embedding_dim(),
        embeddings=embeddings,
    )


@app.post("/v1/ingest", response_model=IngestResponse)
async def ingest_document(
    request: Request,
    workspace_id: UUID = Form(...),
    project_id: UUID = Form(...),
    dataset_id: UUID = Form(...),
    knowledge_base_id: UUID = Form(...),
    agent_definition_id: Optional[UUID] = Form(default=None),
    file: UploadFile = File(...),
):
    identity = request_identity(request)
    file_name = file.filename or "document.txt"
    extension = os.path.splitext(file_name.lower())[1]
    if extension not in SUPPORTED_EXTENSIONS:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Unsupported file type '{extension}'. Supported: .txt, .md, .pdf, .docx.",
        )

    payload = await file.read()
    if not payload:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="File is empty.")
    payload_hash = content_hash(payload)
    idempotency_key = ingestion_idempotency_key(dataset_id, knowledge_base_id, file_name, payload_hash)

    text = extract_text(file_name, payload)
    chunks = chunk_text(text)
    if not chunks:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="No text could be extracted from the file.")

    existing_document = find_existing_document(dataset_id, knowledge_base_id, file_name, payload_hash, idempotency_key)
    if existing_document and existing_document["chunk_count"] > 0:
        return IngestResponse(
            request_id=identity.request_id,
            trace_id=identity.trace_id,
            dataset_id=str(dataset_id),
            knowledge_base_id=str(knowledge_base_id),
            document_asset_id=str(existing_document["document_asset_id"]),
            ingestion_job_id=str(existing_document["ingestion_job_id"]),
            file_name=file_name,
            chunk_count=existing_document["chunk_count"],
            object_key=existing_document["object_key"],
            content_hash=payload_hash,
            idempotency_key=idempotency_key,
            idempotent=True,
            ingestion_status="skipped",
        )
    reusable_document = existing_document

    object_key = ingest_object_key(workspace_id, dataset_id, file_name)
    content_type = file.content_type or "application/octet-stream"
    store_in_minio(object_key, payload, content_type)

    now = utc_now()
    document_asset_id = reusable_document["document_asset_id"] if reusable_document is not None else uuid4()
    ingestion_job_id = uuid4()

    document_version, replaced_document_asset_ids = write_ingested_document(
        request_id=identity.request_id,
        workspace_id=workspace_id,
        project_id=project_id,
        dataset_id=dataset_id,
        knowledge_base_id=knowledge_base_id,
        file_name=file_name,
        content_type=content_type,
        object_key=object_key,
        payload=payload,
        payload_hash=payload_hash,
        idempotency_key=idempotency_key,
        chunks=chunks,
        reusable_document=reusable_document,
        document_asset_id=document_asset_id,
        ingestion_job_id=ingestion_job_id,
        now=now,
    )

    return IngestResponse(
        request_id=identity.request_id,
        trace_id=identity.trace_id,
        dataset_id=str(dataset_id),
        knowledge_base_id=str(knowledge_base_id),
        document_asset_id=str(document_asset_id),
        ingestion_job_id=str(ingestion_job_id),
        file_name=file_name,
        chunk_count=len(chunks),
        object_key=object_key,
        content_hash=payload_hash,
        idempotency_key=idempotency_key,
        ingestion_status="reingested" if reusable_document is not None else "replaced" if replaced_document_asset_ids else "created",
        replaced_document_asset_ids=replaced_document_asset_ids,
    )


@app.delete("/v1/documents/{document_asset_id}", response_model=DeleteDocumentResponse)
async def delete_document(document_asset_id: UUID, request: Request):
    identity = request_identity(request)
    now = utc_now()
    deleted_chunk_count = 0
    with connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                UPDATE document_assets
                SET "Status" = 'deleted',
                    "IsActive" = FALSE,
                    "DeletedAt" = %s,
                    "MetadataJson" = COALESCE("MetadataJson", '{}'::jsonb) || %s::jsonb,
                    "UpdatedAt" = %s
                WHERE "Id" = %s
                RETURNING "Id"
                """,
                (
                    now,
                    json.dumps(
                        {
                            "deleted_at": now.isoformat(),
                            "deleted_by_request_id": identity.request_id,
                        }
                    ),
                    now,
                    document_asset_id,
                ),
            )
            row = cur.fetchone()
            if row is None:
                raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Document asset was not found.")

            cur.execute(
                """
                UPDATE document_chunks
                SET "IsActive" = FALSE,
                    "InactiveAt" = %s,
                    "UpdatedAt" = %s
                WHERE "DocumentAssetId" = %s
                  AND "IsActive" = TRUE
                """,
                (now, now, document_asset_id),
            )
            deleted_chunk_count = cur.rowcount
        conn.commit()

    return DeleteDocumentResponse(
        request_id=identity.request_id,
        trace_id=identity.trace_id,
        document_asset_id=str(document_asset_id),
        deleted_chunk_count=deleted_chunk_count,
    )


@app.post("/v1/documents/{document_asset_id}/reingest", response_model=IngestResponse)
async def reingest_document(document_asset_id: UUID, request: Request):
    identity = request_identity(request)
    with connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "WorkspaceId", "ProjectId", "DatasetId", "KnowledgeBaseId", "FileName", "ContentType", "ObjectKey", "DocumentVersion"
                FROM document_assets
                WHERE "Id" = %s
                """,
                (document_asset_id,),
            )
            row = cur.fetchone()
    if row is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Document asset was not found.")

    workspace_id, project_id, dataset_id, knowledge_base_id, file_name, content_type, object_key, document_version = row
    payload = fetch_from_minio(object_key)
    payload_hash = content_hash(payload)
    idempotency_key = ingestion_idempotency_key(dataset_id, knowledge_base_id, file_name, payload_hash)
    text = extract_text(file_name, payload)
    chunks = chunk_text(text)
    if not chunks:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="No text could be extracted from the file.")

    now = utc_now()
    ingestion_job_id = uuid4()
    next_version = int(document_version or 1) + 1
    write_reingested_document(
        request_id=identity.request_id,
        workspace_id=workspace_id,
        project_id=project_id,
        dataset_id=dataset_id,
        knowledge_base_id=knowledge_base_id,
        document_asset_id=document_asset_id,
        file_name=file_name,
        object_key=object_key,
        payload=payload,
        payload_hash=payload_hash,
        idempotency_key=idempotency_key,
        chunks=chunks,
        next_version=next_version,
        ingestion_job_id=ingestion_job_id,
        now=now,
    )

    return IngestResponse(
        request_id=identity.request_id,
        trace_id=identity.trace_id,
        operation="reingest",
        dataset_id=str(dataset_id),
        knowledge_base_id=str(knowledge_base_id),
        document_asset_id=str(document_asset_id),
        ingestion_job_id=str(ingestion_job_id),
        file_name=file_name,
        chunk_count=len(chunks),
        object_key=object_key,
        content_hash=payload_hash,
        idempotency_key=idempotency_key,
        ingestion_status="reingested",
    )


@app.post("/v1/retrieve", response_model=RetrieveResponse)
async def retrieve(payload: RetrieveRequest, request: Request):
    identity = request_identity(request)
    try:
        knowledge_base_id = UUID(payload.knowledge_base_id)
    except (TypeError, ValueError):
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="invalid uuid")
    chunks = retrieve_chunks(
        knowledge_base_id,
        payload.query,
        payload.top_k,
        score_threshold=payload.score_threshold,
    )
    return RetrieveResponse(
        request_id=identity.request_id,
        trace_id=identity.trace_id,
        query=payload.query,
        knowledge_base_id=payload.knowledge_base_id,
        top_k=payload.top_k,
        score_threshold=payload.score_threshold,
        chunks=[chunk["model"] for chunk in chunks],
    )


@app.post("/v1/chat", response_model=ChatResponse)
async def chat(payload: ChatRequest, request: Request):
    identity = request_identity(request)
    question = payload.question or latest_user_message(payload.messages)
    if not question:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Question is required.")
    if not payload.agent_id:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="agent_id is required for Phase 1 chat.")

    started = time.perf_counter()
    try:
        agent_id = UUID(payload.agent_id)
    except (TypeError, ValueError):
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="invalid uuid")
    agent = load_agent(agent_id)
    if agent is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Agent definition was not found.")

    knowledge_base_ids = agent.get("knowledge_base_ids") or (
        [agent["knowledge_base_id"]] if agent.get("knowledge_base_id") else []
    )
    if not knowledge_base_ids:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Agent does not have a knowledge base.")
    # Primary KB (most recently created) for run/trace attribution.
    knowledge_base_id = knowledge_base_ids[0]

    score_threshold = configured_rag_score_threshold(payload.score_threshold)
    retrieved = retrieve_chunks(knowledge_base_ids, question, payload.top_k)
    retrieved_models = [chunk["model"] for chunk in retrieved]
    answer, answer_chunks, no_answer = build_rag_answer(question, retrieved_models, score_threshold)
    fallback_mode = "no_answer" if no_answer else "extractive"
    runtime_route = resolve_runtime_route(payload, agent)
    route_override_id = parse_optional_uuid(runtime_route.get("route_id"))
    if route_override_id:
        agent["model_route_id"] = route_override_id
    provider_metadata: Dict[str, Any] = {
        "retrieval": retrieval_summary(payload.top_k, score_threshold, retrieved_models, no_answer).model_dump(),
        "embedding_model": embedding_backend_name(),
        "runtime_route": route_context_metadata(runtime_route),
    }
    provider = "local"
    provider_model = runtime_route.get("model") or payload.model
    route_health = "no_answer" if no_answer else "ok"
    timeout_ms = 0
    error_type: Optional[str] = None

    if not no_answer:
        provider_result = await try_provider_answer(question, answer_chunks, runtime_route, payload.temperature)
        provider_metadata["provider_attempt"] = provider_result["metadata"]
        if provider_result["answer"]:
            answer = provider_result["answer"]
            provider = provider_result["provider"]
            provider_model = provider_result["model"]
            route_health = provider_result["route_health"]
            timeout_ms = provider_result["timeout_ms"]
            error_type = provider_result["error_type"]
            fallback_mode = provider_result["provider"]
        elif provider_result["error_type"]:
            route_health = "degraded"
            provider = provider_result["provider"]
            provider_model = provider_result["model"]
            timeout_ms = provider_result["timeout_ms"]
            error_type = provider_result["error_type"]

    provider_response = normalized_provider_response(
        provider=provider,
        model=provider_model,
        route_health=route_health,
        timeout_ms=timeout_ms,
        fallback_mode=fallback_mode,
        question=question,
        answer=answer,
        error_type=error_type,
        metadata=provider_metadata,
    )
    estimated_cost = provider_response.estimated_cost
    retrieval = retrieval_summary(payload.top_k, score_threshold, retrieved_models, no_answer)

    if no_answer:
        cited_chunks: List[Dict[str, Any]] = []
    else:
        cited_ids = {chunk.citation_id for chunk in answer_chunks}
        cited_chunks = [chunk for chunk in retrieved if chunk["citation_id"] in cited_ids]

    citations = [
        Citation(
            citation_id=chunk["citation_id"],
            document_asset_id=str(chunk["document_asset_id"]),
            file_name=chunk["file_name"],
            chunk_id=str(chunk["id"]),
            score=chunk["score"],
            char_start=chunk["model"].metadata.get("char_start"),
            char_end=chunk["model"].metadata.get("char_end"),
        )
        for chunk in cited_chunks
    ]
    elapsed_ms = int((time.perf_counter() - started) * 1000)
    run_id: Optional[Any] = None
    trace_record_id: Optional[Any] = None
    try:
        run_id, trace_record_id = save_run_trace(
            agent=agent,
            knowledge_base_id=knowledge_base_id,
            question=question,
            answer=answer,
            citations=[citation.model_dump() for citation in citations],
            retrieved_chunks=[chunk.model_dump() for chunk in retrieved_models],
            latency_ms=elapsed_ms,
            fallback_mode=fallback_mode,
            request_id=identity.request_id,
            provider_response=provider_response.model_dump(),
            retrieval=retrieval.model_dump(),
            estimated_cost=estimated_cost,
            api_key_id=parse_optional_uuid(payload.api_key_id),
            environment_id=parse_optional_uuid(payload.environment_id),
            auth_mode=payload.auth_mode,
            auth_metadata=payload.auth_metadata,
        )
    except Exception:
        # A trace-write failure must not lose the already-computed answer:
        # log and return the chat response without run/trace ids.
        logger.exception(
            "save_run_trace failed; returning chat answer without trace ids",
        )

    return ChatResponse(
        request_id=identity.request_id,
        trace_id=identity.trace_id,
        model=provider_model,
        answer=answer,
        citations=citations,
        retrieved_chunks=retrieved_models,
        run_id=str(run_id) if run_id is not None else None,
        trace_id_record=str(trace_record_id) if trace_record_id is not None else None,
        fallback_mode=fallback_mode,
        retrieval=retrieval,
        provider_response=provider_response,
        estimated_cost=estimated_cost,
    )


@app.post("/v1/train", response_model=TrainResponse)
async def train_model(payload: TrainRequest, request: Request):
    identity = request_identity(request)
    try:
        result = run_training_job(payload)
        return TrainResponse(
            request_id=identity.request_id,
            trace_id=identity.trace_id,
            training_job_id=payload.training_job_id,
            model_type=payload.config.model_type,
            artifact_object_key=result.get("artifact_object_key"),
            metrics=TrainMetrics(**result.get("metrics", {})),
            message=result.get("message", "Training completed."),
        )
    except Exception as exc:
        return TrainResponse(
            request_id=identity.request_id,
            trace_id=identity.trace_id,
            status="error",
            training_job_id=payload.training_job_id,
            model_type=payload.config.model_type,
            metrics=TrainMetrics(),
            message=str(exc),
        )

def parse_optional_uuid(value: Optional[str]) -> Optional[UUID]:
    if not value:
        return None
    try:
        return UUID(value)
    except (TypeError, ValueError):
        return None


def latest_user_message(messages: List[ChatMessage]) -> Optional[str]:
    for message in reversed(messages):
        if message.role == "user":
            return message.content
    return None


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=5002)
