"""
AgentPort AI Services
Phase 1 local document QA runtime: ingestion, deterministic embeddings,
pgvector retrieval, and extractive chat fallback.
"""

from __future__ import annotations

import hashlib
import json
import math
import os
import re
import time
from contextlib import asynccontextmanager
from datetime import datetime, timezone
from io import BytesIO
from typing import Any, Dict, List, Literal, Optional
from uuid import UUID, uuid4

import httpx
from fastapi import FastAPI, File, Form, HTTPException, Request, UploadFile, status
from pydantic import BaseModel, Field


SERVICE_NAME = "ai-services"
PHASE = "phase_1"
EMBEDDING_DIMENSIONS = 64
LOCAL_EMBEDDING_MODEL = "agentport-local-hash-64"
SUPPORTED_EXTENSIONS = {".txt", ".md", ".pdf"}
ACTIVE_DOCUMENT_STATUSES = {"active", "ready", "succeeded"}
ACTIVE_KNOWLEDGE_BASE_STATUSES = {"active", "ready", "succeeded"}
DELETED_STATUSES = {"archived", "deleted"}
NO_ANSWER_MESSAGE = "I could not find enough relevant information in the ingested documents to answer that."
DEFAULT_RAG_SCORE_THRESHOLD = 0.0
DEFAULT_CHAT_MODEL = "local-extractive-rag"
_sentence_transformer_model: Any = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    print("AI Services starting...")
    yield
    print("AI Services shutting down...")


app = FastAPI(
    title="AgentPort AI Services",
    version="0.2.0",
    lifespan=lifespan,
)


class RequestIdentity(BaseModel):
    request_id: str
    trace_id: str


class HealthResponse(RequestIdentity):
    status: Literal["ok"] = "ok"
    service: Literal["ai-services"] = SERVICE_NAME
    phase: Literal["phase_1"] = PHASE
    live: bool = True
    ready: bool = True
    checks: Dict[str, str] = Field(default_factory=dict)


class EmbedRequest(BaseModel):
    texts: List[str] = Field(..., min_length=1)
    model: str = Field(default=LOCAL_EMBEDDING_MODEL, min_length=1, max_length=255)
    normalize: bool = True


class EmbedResponse(RequestIdentity):
    status: Literal["ok"] = "ok"
    service: Literal["ai-services"] = SERVICE_NAME
    phase: Literal["phase_1"] = PHASE
    operation: Literal["embed"] = "embed"
    model: str
    input_count: int
    dimensions: int
    embeddings: List[List[float]]


class RetrievedChunk(BaseModel):
    id: str
    citation_id: str
    knowledge_base_id: str
    text: str
    score: float
    metadata: Dict[str, Any] = Field(default_factory=dict)


class RetrieveRequest(BaseModel):
    query: str = Field(..., min_length=1)
    knowledge_base_id: str = Field(..., min_length=1, max_length=128)
    top_k: int = Field(default=5, ge=1, le=50)
    score_threshold: Optional[float] = Field(default=None, ge=-1.0, le=1.0)
    filters: Dict[str, Any] = Field(default_factory=dict)


class RetrieveResponse(RequestIdentity):
    status: Literal["ok"] = "ok"
    service: Literal["ai-services"] = SERVICE_NAME
    phase: Literal["phase_1"] = PHASE
    operation: Literal["retrieve"] = "retrieve"
    query: str
    knowledge_base_id: str
    top_k: int
    score_threshold: Optional[float] = None
    chunks: List[RetrievedChunk]


class ChatMessage(BaseModel):
    role: Literal["system", "user", "assistant", "tool"]
    content: str = Field(..., min_length=1)
    name: Optional[str] = Field(default=None, min_length=1, max_length=128)


class ChatRequest(BaseModel):
    agent_id: Optional[str] = None
    question: Optional[str] = None
    top_k: int = Field(default=4, ge=1, le=12)
    messages: List[ChatMessage] = Field(default_factory=list)
    model: str = Field(default=DEFAULT_CHAT_MODEL, min_length=1, max_length=255)
    model_route_id: Optional[str] = None
    route: Optional[Dict[str, Any]] = Field(default_factory=dict)
    model_route: Optional[Dict[str, Any]] = Field(default_factory=dict)
    route_metadata: Optional[Dict[str, Any]] = Field(default_factory=dict)
    provider_id: Optional[str] = None
    provider: Optional[Any] = None
    provider_name: Optional[str] = None
    provider_kind: Optional[str] = None
    provider_base_url: Optional[str] = None
    model_provider: Optional[Dict[str, Any]] = Field(default_factory=dict)
    provider_metadata: Optional[Dict[str, Any]] = Field(default_factory=dict)
    temperature: float = Field(default=0.0, ge=0.0, le=2.0)
    stream: bool = False
    score_threshold: Optional[float] = Field(default=None, ge=-1.0, le=1.0)
    api_key_id: Optional[str] = None
    environment_id: Optional[str] = None
    auth_mode: str = Field(default="anonymous", min_length=1, max_length=64)
    auth_metadata: Dict[str, Any] = Field(default_factory=dict)


class Citation(BaseModel):
    citation_id: str
    document_asset_id: str
    file_name: str
    chunk_id: str
    score: float


class RetrievalSummary(BaseModel):
    top_k: int
    score_threshold: float
    max_score: Optional[float] = None
    no_answer: bool = False


class ProviderResponse(BaseModel):
    provider: str
    model: str
    route_health: str
    timeout_ms: int
    error_type: Optional[str] = None
    fallback_mode: str
    input_tokens: int
    output_tokens: int
    total_tokens: int
    estimated_cost: float = 0.0
    metadata: Dict[str, Any] = Field(default_factory=dict)


class ChatResponse(RequestIdentity):
    status: Literal["ok"] = "ok"
    service: Literal["ai-services"] = SERVICE_NAME
    phase: Literal["phase_1"] = PHASE
    operation: Literal["chat"] = "chat"
    model: str
    answer: str
    citations: List[Citation]
    retrieved_chunks: List[RetrievedChunk]
    run_id: Optional[str] = None
    trace_id_record: Optional[str] = None
    fallback_mode: str
    retrieval: RetrievalSummary
    provider_response: ProviderResponse
    estimated_cost: float = 0.0


class IngestResponse(RequestIdentity):
    status: Literal["succeeded"] = "succeeded"
    service: Literal["ai-services"] = SERVICE_NAME
    phase: Literal["phase_1"] = PHASE
    operation: Literal["ingest", "reingest"] = "ingest"
    dataset_id: str
    knowledge_base_id: str
    document_asset_id: str
    ingestion_job_id: str
    file_name: str
    chunk_count: int
    object_key: str
    content_hash: str
    idempotency_key: str
    idempotent: bool = False
    ingestion_status: Literal["created", "replaced", "reingested", "skipped"] = "created"
    replaced_document_asset_ids: List[str] = Field(default_factory=list)


class DeleteDocumentResponse(RequestIdentity):
    status: Literal["deleted"] = "deleted"
    service: Literal["ai-services"] = SERVICE_NAME
    phase: Literal["phase_1"] = PHASE
    operation: Literal["delete_document"] = "delete_document"
    document_asset_id: str
    deleted_chunk_count: int


class TrainJobConfig(BaseModel):
    target_column: str = Field(..., min_length=1)
    task: Literal["classification", "regression"] = "classification"
    test_size: float = Field(default=0.2, ge=0.05, le=0.5)
    random_state: int = Field(default=42, ge=0)
    max_features: int = Field(default=1000, ge=10, le=10000)
    model_type: Literal["logistic_regression", "random_forest", "sgd"] = "logistic_regression"


class TrainRequest(BaseModel):
    training_job_id: str = Field(..., min_length=1)
    dataset_id: str = Field(..., min_length=1)
    workspace_id: str = Field(..., min_length=1)
    project_id: str = Field(..., min_length=1)
    config: TrainJobConfig


class TrainMetrics(BaseModel):
    accuracy: Optional[float] = None
    precision: Optional[float] = None
    recall: Optional[float] = None
    f1: Optional[float] = None
    r2: Optional[float] = None
    mse: Optional[float] = None
    train_samples: int = 0
    test_samples: int = 0
    feature_count: int = 0


class TrainResponse(RequestIdentity):
    status: Literal["ok", "error"] = "ok"
    service: Literal["ai-services"] = SERVICE_NAME
    phase: Literal["phase_2"] = "phase_2"
    operation: Literal["train"] = "train"
    training_job_id: str
    model_type: str
    artifact_object_key: Optional[str] = None
    metrics: TrainMetrics
    message: str = ""


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
        dimensions=EMBEDDING_DIMENSIONS,
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
            detail=f"Unsupported file type '{extension}'. Supported: .txt, .md, .pdf.",
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

    object_key = f"workspaces/{workspace_id}/datasets/{dataset_id}/{uuid4()}-{safe_filename(file_name)}"
    store_in_minio(object_key, payload, file.content_type or "application/octet-stream")

    now = utc_now()
    document_asset_id = reusable_document["document_asset_id"] if reusable_document is not None else uuid4()
    ingestion_job_id = uuid4()
    replaced_document_asset_ids: List[str] = []

    with db_connect() as conn:
        with conn.cursor() as cur:
            if reusable_document is not None:
                document_version = int(reusable_document.get("document_version") or 1) + 1
                cur.execute(
                    """
                    UPDATE document_assets
                    SET "ContentType" = %s,
                        "ObjectKey" = %s,
                        "Status" = 'succeeded',
                        "ContentHash" = %s,
                        "DocumentVersion" = %s,
                        "IsActive" = TRUE,
                        "LastIngestedAt" = %s,
                        "DeletedAt" = NULL,
                        "SizeBytes" = %s,
                        "MetadataJson" = COALESCE("MetadataJson", '{}'::jsonb) || %s::jsonb,
                        "UpdatedAt" = %s
                    WHERE "Id" = %s
                    """,
                    (
                        file.content_type or "application/octet-stream",
                        object_key,
                        payload_hash,
                        document_version,
                        now,
                        len(payload),
                        json.dumps(ingestion_metadata(identity.request_id, payload_hash, idempotency_key, source="hydrate-existing")),
                        now,
                        document_asset_id,
                    ),
                )
            else:
                document_version = next_document_version(cur, dataset_id, knowledge_base_id, file_name)
                replaced_document_asset_ids = [
                    str(document_id)
                    for document_id in archive_replaced_documents(
                        cur,
                        dataset_id,
                        knowledge_base_id,
                        file_name,
                        payload_hash,
                        now,
                    )
                ]
                cur.execute(
                    """
                    INSERT INTO document_assets
                        ("Id", "WorkspaceId", "ProjectId", "DatasetId", "KnowledgeBaseId", "FileName", "ContentType", "ObjectKey", "Status", "ContentHash", "DocumentVersion", "IsActive", "LastIngestedAt", "SizeBytes", "MetadataJson", "CreatedAt", "UpdatedAt")
                    VALUES
                        (%s, %s, %s, %s, %s, %s, %s, %s, 'succeeded', %s, %s, TRUE, %s, %s, %s::jsonb, %s, %s)
                    """,
                    (
                        document_asset_id,
                        workspace_id,
                        project_id,
                        dataset_id,
                        knowledge_base_id,
                        file_name,
                        file.content_type or "application/octet-stream",
                        object_key,
                        payload_hash,
                        document_version,
                        now,
                        len(payload),
                        json.dumps(ingestion_metadata(identity.request_id, payload_hash, idempotency_key)),
                        now,
                        now,
                    ),
                )
            cur.execute(
                """
                INSERT INTO ingestion_jobs
                    ("Id", "WorkspaceId", "ProjectId", "DatasetId", "KnowledgeBaseId", "DocumentAssetId", "Status", "Operation", "ContentHash", "DocumentVersion", "StartedAt", "CompletedAt", "ChunkCount", "MetadataJson", "CreatedAt", "UpdatedAt")
                VALUES
                    (%s, %s, %s, %s, %s, %s, 'succeeded', 'ingest', %s, %s, %s, %s, %s, %s::jsonb, %s, %s)
                """,
                (
                    ingestion_job_id,
                    workspace_id,
                    project_id,
                    dataset_id,
                    knowledge_base_id,
                    document_asset_id,
                    payload_hash,
                    document_version,
                    now,
                    now,
                    len(chunks),
                    json.dumps(
                        {
                            "embedding_model": embedding_backend_name(),
                            "content_hash": payload_hash,
                            "idempotency_key": idempotency_key,
                            "replaced_document_asset_ids": replaced_document_asset_ids,
                        }
                    ),
                    now,
                    now,
                ),
            )
            write_document_chunks(
                cur,
                workspace_id=workspace_id,
                project_id=project_id,
                dataset_id=dataset_id,
                knowledge_base_id=knowledge_base_id,
                document_asset_id=document_asset_id,
                file_name=file_name,
                chunks=chunks,
                payload_hash=payload_hash,
                document_version=document_version,
                now=now,
            )
        conn.commit()

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
    with db_connect() as conn:
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
    with db_connect() as conn:
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
    with db_connect() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                UPDATE document_assets
                SET "Status" = 'succeeded',
                    "ContentHash" = %s,
                    "DocumentVersion" = %s,
                    "IsActive" = TRUE,
                    "LastIngestedAt" = %s,
                    "DeletedAt" = NULL,
                    "SizeBytes" = %s,
                    "MetadataJson" = COALESCE("MetadataJson", '{}'::jsonb) || %s::jsonb,
                    "UpdatedAt" = %s
                WHERE "Id" = %s
                """,
                (
                    payload_hash,
                    next_version,
                    now,
                    len(payload),
                    json.dumps(ingestion_metadata(identity.request_id, payload_hash, idempotency_key, source="reingest")),
                    now,
                    document_asset_id,
                ),
            )
            cur.execute(
                """
                INSERT INTO ingestion_jobs
                    ("Id", "WorkspaceId", "ProjectId", "DatasetId", "KnowledgeBaseId", "DocumentAssetId", "Status", "Operation", "ContentHash", "DocumentVersion", "StartedAt", "CompletedAt", "ChunkCount", "MetadataJson", "CreatedAt", "UpdatedAt")
                VALUES
                    (%s, %s, %s, %s, %s, %s, 'succeeded', 'reingest', %s, %s, %s, %s, %s, %s::jsonb, %s, %s)
                """,
                (
                    ingestion_job_id,
                    workspace_id,
                    project_id,
                    dataset_id,
                    knowledge_base_id,
                    document_asset_id,
                    payload_hash,
                    next_version,
                    now,
                    now,
                    len(chunks),
                    json.dumps(
                        {
                            "embedding_model": embedding_backend_name(),
                            "content_hash": payload_hash,
                            "idempotency_key": idempotency_key,
                            "source": "reingest",
                        }
                    ),
                    now,
                    now,
                ),
            )
            write_document_chunks(
                cur,
                workspace_id=workspace_id,
                project_id=project_id,
                dataset_id=dataset_id,
                knowledge_base_id=knowledge_base_id,
                document_asset_id=document_asset_id,
                file_name=file_name,
                chunks=chunks,
                payload_hash=payload_hash,
                document_version=next_version,
                now=now,
            )
        conn.commit()

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
    chunks = retrieve_chunks(
        UUID(payload.knowledge_base_id),
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
    agent_id = UUID(payload.agent_id)
    agent = load_agent(agent_id)
    if agent is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Agent definition was not found.")

    knowledge_base_id = agent["knowledge_base_id"]
    if knowledge_base_id is None:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Agent does not have a knowledge base.")

    score_threshold = configured_rag_score_threshold(payload.score_threshold)
    retrieved = retrieve_chunks(knowledge_base_id, question, payload.top_k)
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
    elif fallback_mode == "ollama":
        cited_ids = {chunk.citation_id for chunk in answer_chunks}
        cited_chunks = [chunk for chunk in retrieved if chunk["citation_id"] in cited_ids]
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
        )
        for chunk in cited_chunks
    ]
    elapsed_ms = int((time.perf_counter() - started) * 1000)
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

    return ChatResponse(
        request_id=identity.request_id,
        trace_id=identity.trace_id,
        model=provider_model,
        answer=answer,
        citations=citations,
        retrieved_chunks=retrieved_models,
        run_id=str(run_id),
        trace_id_record=str(trace_record_id),
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


def run_training_job(payload: TrainRequest) -> Dict[str, Any]:
    import csv
    import pickle
    import tempfile

    from sklearn.feature_extraction.text import TfidfVectorizer
    from sklearn.linear_model import LogisticRegression, SGDClassifier
    from sklearn.ensemble import RandomForestClassifier
    from sklearn.model_selection import train_test_split
    from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score, r2_score, mean_squared_error

    training_job_id = UUID(payload.training_job_id)
    dataset_id = UUID(payload.dataset_id)
    workspace_id = UUID(payload.workspace_id)
    project_id = UUID(payload.project_id)
    config = payload.config

    # Find the latest CSV document asset in the dataset
    with db_connect() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT "Id", "ObjectKey", "FileName"
                FROM document_assets
                WHERE "DatasetId" = %s AND "Status" = 'succeeded'
                  AND "IsActive" = TRUE
                  AND "DeletedAt" IS NULL
                  AND ("FileName" ILIKE '%.csv' OR "ContentType" ILIKE '%csv%')
                ORDER BY "CreatedAt" DESC
                LIMIT 1
                """,
                (dataset_id,),
            )
            row = cur.fetchone()

    if row is None:
        raise ValueError("No CSV document found in dataset. Upload a .csv file first.")

    document_asset_id, object_key, file_name = row

    # Download from MinIO
    payload_bytes = fetch_from_minio(object_key)
    text = payload_bytes.decode("utf-8", errors="replace")

    # Parse CSV
    reader = csv.DictReader(text.splitlines())
    rows = list(reader)
    if not rows:
        raise ValueError("CSV file is empty or malformed.")

    if config.target_column not in rows[0]:
        available = ", ".join(rows[0].keys())
        raise ValueError(f"Target column '{config.target_column}' not found. Available: {available}")

    # Simple heuristic: if any value in target column is non-numeric, treat as text classification
    texts = []
    labels = []
    for r in rows:
        # Combine all non-target columns into a single text feature
        features = {k: v for k, v in r.items() if k != config.target_column}
        text_parts = [f"{k}: {v}" for k, v in features.items()]
        texts.append(" ".join(text_parts))
        labels.append(r[config.target_column])

    # Encode labels
    label_encoder: Dict[str, int] = {}
    encoded_labels: List[int] = []
    for label in labels:
        if label not in label_encoder:
            label_encoder[label] = len(label_encoder)
        encoded_labels.append(label_encoder[label])

    X_train, X_test, y_train, y_test = train_test_split(
        texts, encoded_labels, test_size=config.test_size, random_state=config.random_state, stratify=encoded_labels if config.task == "classification" else None
    )

    vectorizer = TfidfVectorizer(max_features=config.max_features)
    X_train_vec = vectorizer.fit_transform(X_train)
    X_test_vec = vectorizer.transform(X_test)

    if config.task == "classification":
        if config.model_type == "random_forest":
            model = RandomForestClassifier(n_estimators=100, random_state=config.random_state, n_jobs=-1)
        elif config.model_type == "sgd":
            model = SGDClassifier(random_state=config.random_state, max_iter=1000)
        else:
            model = LogisticRegression(max_iter=1000, random_state=config.random_state, n_jobs=-1)
    else:
        from sklearn.linear_model import Ridge
        model = Ridge(random_state=config.random_state)

    model.fit(X_train_vec, y_train)
    y_pred = model.predict(X_test_vec)

    metrics: Dict[str, Any] = {
        "train_samples": len(X_train),
        "test_samples": len(X_test),
        "feature_count": X_train_vec.shape[1],
    }

    if config.task == "classification":
        metrics["accuracy"] = round(float(accuracy_score(y_test, y_pred)), 4)
        if len(label_encoder) <= 2:
            metrics["precision"] = round(float(precision_score(y_test, y_pred, average="binary", zero_division=0)), 4)
            metrics["recall"] = round(float(recall_score(y_test, y_pred, average="binary", zero_division=0)), 4)
            metrics["f1"] = round(float(f1_score(y_test, y_pred, average="binary", zero_division=0)), 4)
        else:
            metrics["precision"] = round(float(precision_score(y_test, y_pred, average="weighted", zero_division=0)), 4)
            metrics["recall"] = round(float(recall_score(y_test, y_pred, average="weighted", zero_division=0)), 4)
            metrics["f1"] = round(float(f1_score(y_test, y_pred, average="weighted", zero_division=0)), 4)
    else:
        metrics["r2"] = round(float(r2_score(y_test, y_pred)), 4)
        metrics["mse"] = round(float(mean_squared_error(y_test, y_pred)), 4)

    # Serialize artifact
    artifact = {
        "model": model,
        "vectorizer": vectorizer,
        "label_encoder": label_encoder,
        "config": config.model_dump(),
        "metrics": metrics,
    }
    artifact_bytes = pickle.dumps(artifact)
    artifact_object_key = f"workspaces/{workspace_id}/projects/{project_id}/training_jobs/{training_job_id}/model.pkl"
    store_in_minio(artifact_object_key, artifact_bytes, "application/octet-stream")

    # Update training job in DB
    with db_connect() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                UPDATE training_jobs
                SET "Status" = 'completed',
                    "ArtifactsJson" = %s::jsonb,
                    "MetricsJson" = %s::jsonb,
                    "CompletedAt" = %s,
                    "UpdatedAt" = %s
                WHERE "Id" = %s
                """,
                (
                    json.dumps({"artifact_object_key": artifact_object_key, "model_type": config.model_type}),
                    json.dumps(metrics),
                    utc_now(),
                    utc_now(),
                    training_job_id,
                ),
            )
        conn.commit()

    return {
        "artifact_object_key": artifact_object_key,
        "metrics": metrics,
        "message": f"Trained {config.model_type} on {len(rows)} rows with {metrics.get('accuracy') or metrics.get('r2')} test score.",
    }


def fetch_from_minio(object_key: str) -> bytes:
    from minio import Minio

    endpoint = os.getenv("MINIO_ENDPOINT", "http://localhost:9000")
    secure = os.getenv("MINIO_USE_SSL", "false").lower() == "true"
    endpoint = endpoint.replace("http://", "").replace("https://", "")
    access_key = os.getenv("MINIO_ACCESS_KEY") or os.getenv("MINIO_ROOT_USER", "agentport")
    secret_key = os.getenv("MINIO_SECRET_KEY") or os.getenv("MINIO_ROOT_PASSWORD", "agentportagentport")
    bucket = os.getenv("MINIO_BUCKET", "agentport")
    client = Minio(endpoint, access_key=access_key, secret_key=secret_key, secure=secure)
    response = client.get_object(bucket, object_key)
    return response.read()


def db_connect():
    import psycopg

    dsn = os.getenv("POSTGRES_DSN")
    if dsn:
        return psycopg.connect(dsn)
    return psycopg.connect(
        host=os.getenv("POSTGRES_HOST", "localhost"),
        port=int(os.getenv("POSTGRES_PORT", "5432")),
        dbname=os.getenv("POSTGRES_DB", "agentport"),
        user=os.getenv("POSTGRES_USER", "agentport"),
        password=os.getenv("POSTGRES_PASSWORD", "agentport"),
    )


def store_in_minio(object_key: str, payload: bytes, content_type: str) -> None:
    from minio import Minio

    endpoint = os.getenv("MINIO_ENDPOINT", "http://localhost:9000")
    secure = os.getenv("MINIO_USE_SSL", "false").lower() == "true"
    endpoint = endpoint.replace("http://", "").replace("https://", "")
    access_key = os.getenv("MINIO_ACCESS_KEY") or os.getenv("MINIO_ROOT_USER", "agentport")
    secret_key = os.getenv("MINIO_SECRET_KEY") or os.getenv("MINIO_ROOT_PASSWORD", "agentportagentport")
    bucket = os.getenv("MINIO_BUCKET", "agentport")
    client = Minio(endpoint, access_key=access_key, secret_key=secret_key, secure=secure)
    if not client.bucket_exists(bucket):
        client.make_bucket(bucket)
    client.put_object(bucket, object_key, BytesIO(payload), len(payload), content_type=content_type)


def find_existing_document(
    dataset_id: UUID,
    knowledge_base_id: UUID,
    file_name: str,
    payload_hash: str,
    idempotency_key: str,
) -> Optional[Dict[str, Any]]:
    with db_connect() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT
                    d."Id",
                    d."ObjectKey",
                    COALESCE(j."Id", '00000000-0000-0000-0000-000000000000'::uuid) AS ingestion_job_id,
                    d."Status",
                    d."DocumentVersion",
                    COUNT(c."Id")::int AS chunk_count
                FROM document_assets d
                LEFT JOIN LATERAL (
                    SELECT "Id"
                    FROM ingestion_jobs
                    WHERE "DocumentAssetId" = d."Id"
                    ORDER BY "CreatedAt" DESC
                    LIMIT 1
                ) j ON TRUE
                LEFT JOIN document_chunks c ON c."DocumentAssetId" = d."Id" AND c."IsActive" = TRUE
                WHERE d."DatasetId" = %s
                  AND d."KnowledgeBaseId" = %s
                  AND d."FileName" = %s
                  AND d."IsActive" = TRUE
                  AND d."DeletedAt" IS NULL
                  AND LOWER(d."Status") IN ('active', 'ready', 'succeeded', 'queued')
                  AND (
                    d."ContentHash" = %s
                    OR d."MetadataJson"->>'content_hash' = %s
                    OR d."MetadataJson"->>'sha256' = %s
                    OR d."MetadataJson"->>'idempotency_key' = %s
                  )
                GROUP BY d."Id", d."ObjectKey", d."Status", d."DocumentVersion", j."Id"
                ORDER BY d."CreatedAt"
                LIMIT 1
                """,
                (dataset_id, knowledge_base_id, file_name, payload_hash, payload_hash, payload_hash, idempotency_key),
            )
            row = cur.fetchone()
    if row is None:
        return None
    return {
        "document_asset_id": row[0],
        "object_key": row[1],
        "ingestion_job_id": row[2],
        "status": row[3],
        "document_version": row[4],
        "chunk_count": row[5],
    }


def archive_replaced_documents(
    cur: Any,
    dataset_id: UUID,
    knowledge_base_id: UUID,
    file_name: str,
    payload_hash: str,
    archived_at: datetime,
) -> List[UUID]:
    cur.execute(
        """
        SELECT "Id"
        FROM document_assets
        WHERE "DatasetId" = %s
          AND "KnowledgeBaseId" = %s
          AND "FileName" = %s
          AND "IsActive" = TRUE
          AND "DeletedAt" IS NULL
          AND LOWER("Status") IN ('active', 'ready', 'succeeded')
          AND COALESCE("ContentHash", "MetadataJson"->>'content_hash', "MetadataJson"->>'sha256', '') <> %s
        """,
        (dataset_id, knowledge_base_id, file_name, payload_hash),
    )
    document_ids = [row[0] for row in cur.fetchall()]
    for document_id in document_ids:
        cur.execute(
            """
            UPDATE document_assets
            SET "Status" = 'archived',
                "IsActive" = FALSE,
                "ArchivedAt" = %s,
                "MetadataJson" = COALESCE("MetadataJson", '{}'::jsonb) || %s::jsonb,
                "UpdatedAt" = %s
            WHERE "Id" = %s
            """,
            (
                archived_at,
                json.dumps(
                    {
                        "archived_at": archived_at.isoformat(),
                        "archived_reason": "replaced_by_content_hash",
                    }
                ),
                archived_at,
                document_id,
            ),
        )
        cur.execute(
            """
            UPDATE document_chunks
            SET "IsActive" = FALSE,
                "InactiveAt" = %s,
                "UpdatedAt" = %s
            WHERE "DocumentAssetId" = %s
              AND "IsActive" = TRUE
            """,
            (archived_at, archived_at, document_id),
        )
    return document_ids


def next_document_version(cur: Any, dataset_id: UUID, knowledge_base_id: UUID, file_name: str) -> int:
    cur.execute(
        """
        SELECT COALESCE(MAX("DocumentVersion"), 0) + 1
        FROM document_assets
        WHERE "DatasetId" = %s
          AND "KnowledgeBaseId" = %s
          AND "FileName" = %s
        """,
        (dataset_id, knowledge_base_id, file_name),
    )
    row = cur.fetchone()
    return int(row[0] or 1)


def write_document_chunks(
    cur: Any,
    *,
    workspace_id: UUID,
    project_id: UUID,
    dataset_id: UUID,
    knowledge_base_id: UUID,
    document_asset_id: UUID,
    file_name: str,
    chunks: List[str],
    payload_hash: str,
    document_version: int,
    now: datetime,
) -> None:
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
    for index, chunk in enumerate(chunks):
        chunk_id = uuid4()
        citation_id = f"{safe_filename(file_name)}:{str(document_asset_id)[:8]}:{index + 1}"
        embedding = vector_literal(embed_text(chunk))
        cur.execute(
            """
            INSERT INTO document_chunks
                ("Id", "WorkspaceId", "ProjectId", "DatasetId", "KnowledgeBaseId", "DocumentAssetId", "ChunkIndex", "CitationId", "Text", "TokenEstimate", "PageNumber", "Section", "Embedding", "ContentHash", "DocumentVersion", "IsActive", "MetadataJson", "CreatedAt", "UpdatedAt")
            VALUES
                (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NULL, NULL, %s::vector, %s, %s, TRUE, %s::jsonb, %s, %s)
            """,
            (
                chunk_id,
                workspace_id,
                project_id,
                dataset_id,
                knowledge_base_id,
                document_asset_id,
                index,
                citation_id,
                chunk,
                estimate_tokens(chunk),
                embedding,
                payload_hash,
                document_version,
                json.dumps({"file_name": file_name, "content_hash": payload_hash}),
                now,
                now,
            ),
        )


def extract_text(file_name: str, payload: bytes) -> str:
    extension = os.path.splitext(file_name.lower())[1]
    if extension in {".txt", ".md"}:
        return payload.decode("utf-8", errors="replace")
    if extension == ".pdf":
        from pypdf import PdfReader

        reader = PdfReader(BytesIO(payload))
        return "\n\n".join(page.extract_text() or "" for page in reader.pages)
    return ""


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


def embed_text(text: str, normalize: bool = True) -> List[float]:
    if os.getenv("EMBEDDING_BACKEND", "hash").lower() not in {"sentence-transformers", "sentence_transformers", "st"}:
        return hash_embedding(text, normalize=normalize)
    try:
        return sentence_transformer_embedding(text, normalize=normalize)
    except Exception:
        if os.getenv("EMBEDDING_STRICT", "false").lower() == "true":
            raise
        return hash_embedding(text, normalize=normalize)


def sentence_transformer_embedding(text: str, normalize: bool = True) -> List[float]:
    global _sentence_transformer_model

    if _sentence_transformer_model is None:
        from sentence_transformers import SentenceTransformer

        model_name = os.getenv("SENTENCE_TRANSFORMERS_MODEL", "sentence-transformers/all-MiniLM-L6-v2")
        _sentence_transformer_model = SentenceTransformer(model_name)

    raw = _sentence_transformer_model.encode([text], normalize_embeddings=False)[0]
    vector = [0.0] * EMBEDDING_DIMENSIONS
    for index, value in enumerate(raw):
        digest = hashlib.sha256(str(index).encode("utf-8")).digest()
        target = int.from_bytes(digest[:2], "big") % EMBEDDING_DIMENSIONS
        sign = 1.0 if digest[2] % 2 == 0 else -1.0
        vector[target] += float(value) * sign
    if normalize:
        norm = math.sqrt(sum(value * value for value in vector))
        if norm > 0:
            vector = [value / norm for value in vector]
    return [round(value, 6) for value in vector]


def embedding_backend_name() -> str:
    if os.getenv("EMBEDDING_BACKEND", "hash").lower() in {"sentence-transformers", "sentence_transformers", "st"}:
        model_name = os.getenv("SENTENCE_TRANSFORMERS_MODEL", "sentence-transformers/all-MiniLM-L6-v2")
        return f"{model_name}:projected-{EMBEDDING_DIMENSIONS}"
    return LOCAL_EMBEDDING_MODEL


def vector_literal(vector: List[float]) -> str:
    return "[" + ",".join(f"{value:.6f}" for value in vector) + "]"


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


def retrieve_chunks(
    knowledge_base_id: UUID,
    query: str,
    top_k: int,
    score_threshold: Optional[float] = None,
) -> List[Dict[str, Any]]:
    embedding = vector_literal(embed_text(query))
    with db_connect() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT
                    c."Id",
                    c."CitationId",
                    c."KnowledgeBaseId",
                    c."DocumentAssetId",
                    c."Text",
                    1 - (c."Embedding" <=> %s::vector) AS score,
                    c."MetadataJson",
                    d."FileName"
                FROM document_chunks c
                JOIN document_assets d ON d."Id" = c."DocumentAssetId"
                JOIN knowledge_bases kb ON kb."Id" = c."KnowledgeBaseId"
                WHERE c."KnowledgeBaseId" = %s
                  AND c."IsActive" = TRUE
                  AND d."IsActive" = TRUE
                  AND d."DeletedAt" IS NULL
                  AND LOWER(d."Status") IN ('active', 'ready', 'succeeded')
                  AND LOWER(kb."Status") IN ('active', 'ready', 'succeeded')
                ORDER BY c."Embedding" <=> %s::vector
                LIMIT %s
                """,
                (embedding, knowledge_base_id, embedding, top_k),
            )
            rows = cur.fetchall()

    chunks = []
    for row in rows:
        metadata = row[6] if isinstance(row[6], dict) else json.loads(row[6] or "{}")
        score = float(row[5] or 0)
        if score_threshold is not None and score < score_threshold:
            continue
        model = RetrievedChunk(
            id=str(row[0]),
            citation_id=row[1],
            knowledge_base_id=str(row[2]),
            text=row[4],
            score=score,
            metadata={**metadata, "file_name": row[7]},
        )
        chunks.append(
            {
                "id": row[0],
                "citation_id": row[1],
                "knowledge_base_id": row[2],
                "document_asset_id": row[3],
                "text": row[4],
                "score": score,
                "file_name": row[7],
                "model": model,
            }
        )
    return chunks


def load_agent(agent_id: UUID) -> Optional[Dict[str, Any]]:
    with db_connect() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT
                    a."Id",
                    a."WorkspaceId",
                    a."ProjectId",
                    a."ModelRouteId",
                    kb."Id" AS knowledge_base_id,
                    mr."Name" AS model_route_name,
                    mr."Slug" AS model_route_slug,
                    mr."ModelName" AS model_route_model_name,
                    mr."RouteType" AS model_route_type,
                    mr."IsEnabled" AS model_route_enabled,
                    mr."ParametersJson" AS model_route_parameters,
                    mr."MetadataJson" AS model_route_metadata,
                    mp."Id" AS model_provider_id,
                    mp."Name" AS model_provider_name,
                    mp."Kind" AS model_provider_kind,
                    mp."BaseUrl" AS model_provider_base_url,
                    mp."IsEnabled" AS model_provider_enabled,
                    mp."MetadataJson" AS model_provider_metadata
                FROM agent_definitions a
                LEFT JOIN model_routes mr
                  ON mr."Id" = a."ModelRouteId"
                LEFT JOIN model_providers mp
                  ON mp."Id" = mr."ProviderId"
                LEFT JOIN knowledge_bases kb
                  ON kb."AgentDefinitionId" = a."Id"
                 AND LOWER(kb."Status") IN ('active', 'ready', 'succeeded')
                WHERE a."Id" = %s
                  AND LOWER(a."Status") NOT IN ('archived', 'deleted')
                ORDER BY kb."CreatedAt" DESC NULLS LAST
                LIMIT 1
                """,
                (agent_id,),
            )
            row = cur.fetchone()
    if row is None:
        return None
    return {
        "id": row[0],
        "workspace_id": row[1],
        "project_id": row[2],
        "model_route_id": row[3],
        "knowledge_base_id": row[4],
        "model_route": {
            "name": row[5],
            "slug": row[6],
            "model_name": row[7],
            "route_type": row[8],
            "is_enabled": row[9],
            "parameters": row[10],
            "metadata": row[11],
        },
        "model_provider": {
            "id": row[12],
            "name": row[13],
            "kind": row[14],
            "base_url": row[15],
            "is_enabled": row[16],
            "metadata": row[17],
        },
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
    if not provider_enabled_by_env(route_context):
        return provider_skip_result(
            provider=provider,
            model=model,
            reason="provider_env_disabled",
            timeout_ms=timeout_ms,
            metadata={"runtime_route": route_metadata, "live_calls_enabled": live_calls_enabled},
        )

    if is_local_ollama:
        return await try_ollama_answer(question, chunks, route_context)
    if not live_calls_enabled:
        return provider_skip_result(
            provider=provider,
            model=model,
            reason="live_calls_disabled",
            timeout_ms=timeout_ms,
            metadata={"runtime_route": route_metadata, "live_calls_enabled": False},
        )
    if is_openai_compatible_provider(route_context):
        return await try_openai_compatible_answer(question, chunks, route_context, temperature)

    return provider_skip_result(
        provider=provider,
        model=model,
        reason="unsupported_provider",
        timeout_ms=timeout_ms,
        metadata={"runtime_route": route_metadata, "live_calls_enabled": live_calls_enabled},
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


def save_run_trace(
    *,
    agent: Dict[str, Any],
    knowledge_base_id: UUID,
    question: str,
    answer: str,
    citations: List[Dict[str, Any]],
    retrieved_chunks: List[Dict[str, Any]],
    latency_ms: int,
    fallback_mode: str,
    request_id: str,
    provider_response: Dict[str, Any],
    retrieval: Dict[str, Any],
    estimated_cost: float,
    api_key_id: Optional[UUID],
    environment_id: Optional[UUID],
    auth_mode: str,
    auth_metadata: Dict[str, Any],
) -> tuple[UUID, UUID]:
    now = utc_now()
    trace_id = uuid4()
    run_id = uuid4()
    input_tokens = estimate_tokens(question)
    output_tokens = estimate_tokens(answer)
    top_k = int(retrieval.get("top_k") or retrieval.get("topK") or 4)
    score_threshold = retrieval.get("score_threshold", retrieval.get("scoreThreshold"))
    best_retrieval_score = retrieval.get("max_score", retrieval.get("maxScore"))
    no_answer = bool(retrieval.get("no_answer") or retrieval.get("noAnswer"))
    quality_status = "no_answer" if no_answer else "passed"
    no_answer_reason = None
    if no_answer:
        no_answer_reason = "no_relevant_chunks" if best_retrieval_score is None else "below_score_threshold"

    with db_connect() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                INSERT INTO trace_records
                    ("Id", "WorkspaceId", "ProjectId", "EnvironmentId", "ApiKeyId", "AgentDefinitionId", "ModelRouteId", "CorrelationId", "TraceType", "Status", "AuthMode", "AuthMetadataJson", "TopK", "ScoreThreshold", "BestRetrievalScore", "QualityStatus", "NoAnswerReason", "InputTokens", "OutputTokens", "CostAmount", "StartedAt", "EndedAt", "MetadataJson", "CreatedAt")
                VALUES
                    (%s, %s, %s, %s, %s, %s, %s, %s, 'rag_chat', 'succeeded', %s, %s::jsonb, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s::jsonb, %s)
                """,
                (
                    trace_id,
                    agent["workspace_id"],
                    agent["project_id"],
                    environment_id,
                    api_key_id,
                    agent["id"],
                    agent["model_route_id"],
                    request_id,
                    auth_mode,
                    json.dumps(auth_metadata),
                    top_k,
                    score_threshold,
                    best_retrieval_score,
                    quality_status,
                    no_answer_reason,
                    input_tokens,
                    output_tokens,
                    estimated_cost,
                    now,
                    now,
                    json.dumps(
                        {
                            "fallback_mode": fallback_mode,
                            "citations": citations,
                            "provider_response": provider_response,
                            "retrieval": retrieval,
                        }
                    ),
                    now,
                ),
            )
            cur.execute(
                """
                INSERT INTO agent_runs
                    ("Id", "WorkspaceId", "ProjectId", "EnvironmentId", "ApiKeyId", "AgentDefinitionId", "KnowledgeBaseId", "TraceRecordId", "Question", "Answer", "Status", "FallbackMode", "AuthMode", "AuthMetadataJson", "TopK", "ScoreThreshold", "BestRetrievalScore", "QualityStatus", "NoAnswerReason", "CitationsJson", "RetrievedChunksJson", "LatencyMs", "EstimatedCost", "MetadataJson", "CreatedAt", "UpdatedAt")
                VALUES
                    (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, 'succeeded', %s, %s, %s::jsonb, %s, %s, %s, %s, %s, %s::jsonb, %s::jsonb, %s, %s, %s::jsonb, %s, %s)
                """,
                (
                    run_id,
                    agent["workspace_id"],
                    agent["project_id"],
                    environment_id,
                    api_key_id,
                    agent["id"],
                    knowledge_base_id,
                    trace_id,
                    question,
                    answer,
                    fallback_mode,
                    auth_mode,
                    json.dumps(auth_metadata),
                    top_k,
                    score_threshold,
                    best_retrieval_score,
                    quality_status,
                    no_answer_reason,
                    json.dumps(citations),
                    json.dumps(retrieved_chunks),
                    latency_ms,
                    estimated_cost,
                    json.dumps(
                        {
                            "request_id": request_id,
                            "provider_response": provider_response,
                            "retrieval": retrieval,
                        }
                    ),
                    now,
                    now,
                ),
            )
        conn.commit()
    return run_id, trace_id


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


def estimate_tokens(text: str) -> int:
    return max(1, math.ceil(len(re.findall(r"\S+", text)) * 1.25))


def safe_filename(file_name: str) -> str:
    value = re.sub(r"[^a-zA-Z0-9._-]+", "-", file_name).strip("-")
    return value or "document.txt"


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=5002)
