"""Pydantic request/response models for the AgentPort AI Services runtime.

These models define the Phase 1/2 wire contracts (embed, retrieve, chat,
ingest, delete, train). They are re-exported from ``main.py`` so existing
imports (and ``tests/test_contracts.py``) continue to resolve from ``main``.
"""

from __future__ import annotations

from typing import Any, Dict, List, Literal, Optional

from pydantic import BaseModel, Field


SERVICE_NAME = "ai-services"
PHASE = "phase_1"
LOCAL_EMBEDDING_MODEL = "agentport-local-hash-64"
DEFAULT_CHAT_MODEL = "local-extractive-rag"


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
    # Character span/offset within the source document so a "Kaynak" label can be
    # rendered without persisting the full chunk body.
    char_start: Optional[int] = None
    char_end: Optional[int] = None


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


__all__ = [
    "SERVICE_NAME",
    "PHASE",
    "LOCAL_EMBEDDING_MODEL",
    "DEFAULT_CHAT_MODEL",
    "RequestIdentity",
    "HealthResponse",
    "EmbedRequest",
    "EmbedResponse",
    "RetrievedChunk",
    "RetrieveRequest",
    "RetrieveResponse",
    "ChatMessage",
    "ChatRequest",
    "Citation",
    "RetrievalSummary",
    "ProviderResponse",
    "ChatResponse",
    "IngestResponse",
    "DeleteDocumentResponse",
    "TrainJobConfig",
    "TrainRequest",
    "TrainMetrics",
    "TrainResponse",
]
