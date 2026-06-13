"""Postgres persistence: connection pool + raw-SQL data access.

All SQL and transaction boundaries are identical to the prior per-call
``db_connect()`` implementation; only connection acquisition changed —
connections are now leased from a module-level ``psycopg_pool.ConnectionPool``
opened lazily in the FastAPI lifespan and closed on shutdown.
"""

from __future__ import annotations

import json
import os
from contextlib import contextmanager
from datetime import datetime, timezone
from typing import Any, Dict, Iterator, List, Optional
from uuid import UUID, uuid4

from fastapi import HTTPException, status

from agentport.embeddings import (
    active_embedding_dim,
    embed_passage,
    embed_query,
    embedding_backend_name,
)
from agentport.chunking import safe_filename
from agentport.models import RetrievedChunk

# Default ceiling for the pooled Postgres connections; overridable via env.
DEFAULT_DB_POOL_MAX = 10

# Module-level pool, created lazily in the FastAPI lifespan.
_pool: Optional["ConnectionPool"] = None  # type: ignore[name-defined]  # noqa: F821


def _connection_kwargs() -> Dict[str, Any]:
    """Resolve psycopg connection parameters from env (matches db_connect())."""
    dsn = os.getenv("POSTGRES_DSN")
    if dsn:
        return {"conninfo": dsn}
    return {
        "conninfo": "",
        "kwargs": {
            "host": os.getenv("POSTGRES_HOST", "localhost"),
            "port": int(os.getenv("POSTGRES_PORT", "5432")),
            "dbname": os.getenv("POSTGRES_DB", "agentport"),
            "user": os.getenv("POSTGRES_USER", "agentport"),
            "password": os.getenv("POSTGRES_PASSWORD", "agentport"),
        },
    }


def db_connect():
    """Open a single Postgres connection from env config.

    Retained as the connection factory semantics; the runtime now leases
    connections from the pool via ``connection()`` instead of calling this
    per request, but the env resolution stays byte-identical.
    """
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


def pool_max_size() -> int:
    raw = os.getenv("DB_POOL_MAX")
    if raw is None:
        return DEFAULT_DB_POOL_MAX
    try:
        value = int(raw)
    except ValueError:
        return DEFAULT_DB_POOL_MAX
    return value if value > 0 else DEFAULT_DB_POOL_MAX


def open_pool() -> "ConnectionPool":  # type: ignore[name-defined]  # noqa: F821
    """Create the module-level connection pool (idempotent)."""
    global _pool
    if _pool is not None:
        return _pool
    from psycopg_pool import ConnectionPool

    params = _connection_kwargs()
    conninfo = params["conninfo"]
    kwargs = params.get("kwargs")
    _pool = ConnectionPool(
        conninfo,
        kwargs=kwargs,
        min_size=1,
        max_size=pool_max_size(),
        open=False,
    )
    # wait=False: do not block app boot if Postgres is transiently unreachable;
    # connections are established lazily and errors surface on first use,
    # matching the original per-call db_connect() failure semantics.
    _pool.open(wait=False)
    return _pool


def close_pool() -> None:
    """Close the module-level pool on shutdown."""
    global _pool
    if _pool is not None:
        _pool.close()
        _pool = None


@contextmanager
def connection() -> Iterator[Any]:
    """Lease a connection from the pool, falling back to a direct connect.

    The pool is normally opened in the FastAPI lifespan. If it has not been
    opened (e.g. a unit test importing a single DB helper), fall back to a
    one-off ``db_connect()`` so behavior matches the prior per-call model.
    """
    if _pool is None:
        with db_connect() as conn:
            yield conn
        return
    with _pool.connection() as conn:
        yield conn


def find_existing_document(
    dataset_id: UUID,
    knowledge_base_id: UUID,
    file_name: str,
    payload_hash: str,
    idempotency_key: str,
) -> Optional[Dict[str, Any]]:
    with connection() as conn:
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
    span_start = 0
    for index, chunk in enumerate(chunks):
        chunk_id = uuid4()
        citation_id = f"{safe_filename(file_name)}:{str(document_asset_id)[:8]}:{index + 1}"
        embedding = vector_literal(embed_passage(chunk))
        span_end = span_start + len(chunk)
        chunk_metadata = {
            "file_name": file_name,
            "content_hash": payload_hash,
            "embedding_model": embedding_backend_name(),
            "embedding_dim": active_embedding_dim(),
            "char_start": span_start,
            "char_end": span_end,
            "chunk_index": index,
        }
        span_start = span_end
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
                json.dumps(chunk_metadata),
                now,
                now,
            ),
        )


def retrieve_chunks(
    knowledge_base_id: UUID,
    query: str,
    top_k: int,
    score_threshold: Optional[float] = None,
) -> List[Dict[str, Any]]:
    embedding = vector_literal(embed_query(query))
    query_backend = embedding_backend_name()
    query_dim = active_embedding_dim()
    with connection() as conn:
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
        # Refuse to compare across mismatched embedding backends/dimensions instead
        # of silently scoring query and stored vectors that mean different things.
        stored_dim = metadata.get("embedding_dim")
        stored_model = metadata.get("embedding_model")
        if stored_dim is not None and int(stored_dim) != query_dim:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail=(
                    f"Embedding dimension mismatch: query backend produces {query_dim}-dim "
                    f"vectors ('{query_backend}') but stored chunks are {stored_dim}-dim "
                    f"('{stored_model}'). Re-ingest the knowledge base with the active backend."
                ),
            )
        if stored_model is not None and stored_model != query_backend:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail=(
                    f"Embedding backend mismatch: query backend is '{query_backend}' but stored "
                    f"chunks use '{stored_model}'. Re-ingest the knowledge base with the active backend."
                ),
            )
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
    with connection() as conn:
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
    from agentport.trace_policy import redact_trace_chunks, trace_redact_content

    now = utc_now()
    trace_id = uuid4()
    run_id = uuid4()
    # KVKK: by default persist citation metadata only — never the raw chunk body.
    persisted_chunks = retrieved_chunks if not trace_redact_content() else redact_trace_chunks(retrieved_chunks)
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

    with connection() as conn:
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
                    json.dumps(persisted_chunks),
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


def vector_literal(vector: List[float]) -> str:
    return "[" + ",".join(f"{value:.6f}" for value in vector) + "]"


def estimate_tokens(text: str) -> int:
    import math
    import re

    return max(1, math.ceil(len(re.findall(r"\S+", text)) * 1.25))


def utc_now() -> datetime:
    return datetime.now(timezone.utc)
