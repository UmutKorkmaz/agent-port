"""Document ingestion: object storage, text extraction, and the DB write flow.

Extracted verbatim from ``main.py`` (Track A, Task A7). This module owns:

* MinIO object-storage helpers (:func:`store_in_minio`, :func:`fetch_from_minio`),
  shared with :mod:`agentport.training`.
* Text extraction for the supported file types — ``.txt``/``.md`` decode, the
  ``.pdf`` ``pypdf`` path, and the ``.docx``/:func:`extract_docx_text` path.
* The ingest/reingest write flow — the document-asset upsert, ingestion-job
  row, and chunk write transaction. SQL and transaction boundaries are byte
  identical to the prior inline route bodies; only the relocation changed.

All names are re-exported from ``main`` so existing imports keep resolving.
"""

from __future__ import annotations

import json
import os
from io import BytesIO
from typing import Any, Dict, List, Optional, Tuple
from uuid import UUID, uuid4

from agentport.chunking import ingestion_metadata, safe_filename
from agentport.embeddings import embedding_backend_name
from agentport.persistence.db import (
    archive_replaced_documents,
    connection,
    next_document_version,
    write_document_chunks,
)


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


def extract_text(file_name: str, payload: bytes) -> str:
    extension = os.path.splitext(file_name.lower())[1]
    if extension in {".txt", ".md"}:
        return payload.decode("utf-8", errors="replace")
    if extension == ".pdf":
        from pypdf import PdfReader

        reader = PdfReader(BytesIO(payload))
        return "\n\n".join(page.extract_text() or "" for page in reader.pages)
    if extension == ".docx":
        return extract_docx_text(payload)
    return ""


def extract_docx_text(payload: bytes) -> str:
    from docx import Document

    document = Document(BytesIO(payload))
    return "\n".join(paragraph.text for paragraph in document.paragraphs)


def ingest_object_key(workspace_id: UUID, dataset_id: UUID, file_name: str) -> str:
    """Build the MinIO object key for a freshly ingested document."""
    return f"workspaces/{workspace_id}/datasets/{dataset_id}/{uuid4()}-{safe_filename(file_name)}"


def write_ingested_document(
    *,
    request_id: str,
    workspace_id: UUID,
    project_id: UUID,
    dataset_id: UUID,
    knowledge_base_id: UUID,
    file_name: str,
    content_type: str,
    object_key: str,
    payload: bytes,
    payload_hash: str,
    idempotency_key: str,
    chunks: List[Dict[str, Any]],
    reusable_document: Optional[Dict[str, Any]],
    document_asset_id: UUID,
    ingestion_job_id: UUID,
    now: Any,
) -> Tuple[int, List[str]]:
    """Persist a freshly ingested document, its ingestion job, and chunks.

    Returns ``(document_version, replaced_document_asset_ids)``. The SQL and
    transaction boundaries are byte identical to the prior inline ``/v1/ingest``
    route body.
    """
    replaced_document_asset_ids: List[str] = []
    with connection() as conn:
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
                        content_type,
                        object_key,
                        payload_hash,
                        document_version,
                        now,
                        len(payload),
                        json.dumps(ingestion_metadata(request_id, payload_hash, idempotency_key, source="hydrate-existing")),
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
                        content_type,
                        object_key,
                        payload_hash,
                        document_version,
                        now,
                        len(payload),
                        json.dumps(ingestion_metadata(request_id, payload_hash, idempotency_key)),
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

    return document_version, replaced_document_asset_ids


def write_reingested_document(
    *,
    request_id: str,
    workspace_id: UUID,
    project_id: UUID,
    dataset_id: UUID,
    knowledge_base_id: UUID,
    document_asset_id: UUID,
    file_name: str,
    object_key: str,
    payload: bytes,
    payload_hash: str,
    idempotency_key: str,
    chunks: List[Dict[str, Any]],
    next_version: int,
    ingestion_job_id: UUID,
    now: Any,
) -> None:
    """Persist a reingested document, its ingestion job, and chunks.

    The SQL and transaction boundaries are byte identical to the prior inline
    ``/v1/documents/{id}/reingest`` route body.
    """
    with connection() as conn:
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
                    json.dumps(ingestion_metadata(request_id, payload_hash, idempotency_key, source="reingest")),
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
