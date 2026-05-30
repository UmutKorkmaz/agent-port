# Phase 0 Acceptance Checklist

Use this checklist as the local gate before moving Phase 0 operational files forward.

## Infrastructure

- [ ] `docker compose config` validates with no obsolete `version` warning.
- [ ] `docker compose up -d` starts PostgreSQL, Redis, RabbitMQ, MinIO, and `minio-bootstrap`.
- [ ] PostgreSQL image is pinned and initializes `vector`, `pgcrypto`, and `citext`.
- [ ] Redis image is pinned and `redis-cli ping` returns `PONG`.
- [ ] RabbitMQ image is pinned and `rabbitmq-diagnostics -q ping` exits successfully.
- [ ] MinIO image and MinIO client image are pinned.
- [ ] MinIO bucket bootstrap creates or confirms the configured bucket.
- [ ] Ports and credentials come from `.env` with safe local defaults.

## Local App Smoke

- [ ] Platform API starts on `http://localhost:5001`.
- [ ] `curl -fsS http://localhost:5001/api/v1/status` returns `status: operational`.
- [ ] AI Services starts on `http://localhost:5002`.
- [ ] `curl -fsS http://localhost:5002/health` returns `status: ok`.
- [ ] Web starts on `http://localhost:3000`.
- [ ] Web proxy smoke checks pass for `/api/platform/status` and `/api/ai/health`.

## Backups

- [ ] `infra/scripts/postgres-backup.sh` writes a custom-format dump and checksum under `infra/backups/postgres/`.
- [ ] `infra/scripts/postgres-restore-smoke.sh` restores the latest dump into a temporary database and verifies required extensions.
- [ ] `infra/scripts/minio-backup.sh` mirrors the configured local bucket under `infra/backups/minio/`.

## Documentation and Examples

- [ ] `docs/quickstart.md` uses `http://localhost:5001` for the local Platform API.
- [ ] Quickstart commands match the current skeleton and do not require manual migration creation.
- [ ] `examples/phase0/` contains JSON payloads for bootstrap, model route, workspace/project, empty agent definition, and API key scopes.
- [ ] `.env.example` documents local URLs, infrastructure knobs, provider toggles, payment toggles, and Turkiye payment placeholders.
- [ ] `.gitignore` excludes local environment files, build artifacts, dependency directories, Python caches, and local backups.
