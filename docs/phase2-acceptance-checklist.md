# Phase 2 Acceptance Checklist

Use this checklist as the local gate before moving Phase 2 operational files forward.

## Database Schema

- [ ] `training_jobs` table exists with status, metrics, artifacts, logs, cost, and wallet reservation links.
- [ ] `experiments` table exists with config, metrics, and artifact links.
- [ ] `model_versions` table exists with kind, status, artifact URI, and config.
- [ ] `model_aliases` table exists with candidate/staging/production/rollback aliases.
- [ ] `eval_runs` table exists with score, threshold, passed, results, and failures.
- [ ] `human_review_records` table exists with queue, status, label, severity, reviewer, and reuse flags.
- [ ] `datasets` table has Phase 2 card fields: license, source, pii_classification, is_golden, allows_training, allows_eval, consent_flags, splits, schema, quality_notes.
- [ ] EF Core migration `Phase2TrainingEvalModelLifecycle` applies cleanly from a fresh database.

## Platform API

- [ ] `PATCH /api/v1/datasets/{id}/card` updates dataset card fields.
- [ ] `GET /api/v1/training-jobs` lists training jobs.
- [ ] `POST /api/v1/training-jobs` creates a training job.
- [ ] `GET /api/v1/training-jobs/{id}` returns a training job.
- [ ] `PATCH /api/v1/training-jobs/{id}` updates status, metrics, artifacts, logs, cost.
- [ ] `GET /api/v1/experiments` lists experiments.
- [ ] `POST /api/v1/experiments` creates an experiment.
- [ ] `GET /api/v1/model-versions` lists model versions.
- [ ] `POST /api/v1/model-versions` creates a model version.
- [ ] `GET /api/v1/model-aliases` lists active aliases.
- [ ] `POST /api/v1/model-aliases` sets candidate/staging/production/rollback alias.
- [ ] `GET /api/v1/eval-runs` lists eval runs.
- [ ] `POST /api/v1/eval-runs` creates an eval run.
- [ ] `PATCH /api/v1/eval-runs/{id}` updates score, passed, results, failures.
- [ ] `GET /api/v1/human-review-records` lists review records with queue/status filters.
- [ ] `POST /api/v1/human-review-records` creates a review record.
- [ ] `PATCH /api/v1/human-review-records/{id}` updates label, status, reviewer, reuse flag.

## AI Services

- [ ] `POST /v1/train` accepts a training config and CSV dataset.
- [ ] Classification tasks train logistic regression, random forest, or SGD.
- [ ] Regression tasks train Ridge regression.
- [ ] Training produces accuracy/precision/recall/f1 or r2/mse metrics.
- [ ] Model artifact (pickle) is stored in MinIO.
- [ ] Training job record in PostgreSQL is updated with status `completed`, metrics, and artifact path.

## Web Panel

- [ ] `Training` navigation section exists.
- [ ] Training Lab shows training jobs with status, metrics, and artifact info.
- [ ] Model versions sidebar shows registered versions.
- [ ] `Evals` section shows eval runs with pass/fail status.
- [ ] `Traces` section includes a human review queue sidebar.
- [ ] Builder page shows "Phase 2 training enabled" instead of disabled message.

## End-to-End

- [ ] A CSV dataset can be uploaded to a document dataset.
- [ ] A training job can be created from the web panel.
- [ ] The AI service completes training and updates the job record.
- [ ] A model version can be registered from a completed training job.
- [ ] An alias (candidate/staging/production/rollback) can be assigned to a model version.
- [ ] An eval run can be created and updated with scores.
- [ ] A human review record can be created and approved/rejected with reviewer identity.
