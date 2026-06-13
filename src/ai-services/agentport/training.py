"""Phase 2 training: CSV-backed sklearn TF-IDF training job.

Extracted verbatim from ``main.py`` (Track A, Task A7). :func:`run_training_job`
loads the latest CSV document asset for a dataset, vectorizes the non-target
columns with TF-IDF, fits a classification or regression model, serializes the
artifact to MinIO, and records metrics on the training job. Object storage goes
through :mod:`agentport.ingestion` so ingestion and training share one MinIO
client path. Logic is byte identical to the prior inline implementation; only
the relocation changed. Re-exported from ``main``.
"""

from __future__ import annotations

import json
from typing import Any, Dict, List
from uuid import UUID

from agentport.ingestion import fetch_from_minio, store_in_minio
from agentport.models import TrainRequest
from agentport.persistence.db import connection, utc_now


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
    with connection() as conn:
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
    with connection() as conn:
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
