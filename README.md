# AgentPort

AgentPort is a self-hostable control plane for building, testing, deploying, and operating AI systems.

The current workspace proves a local document-QA path: a .NET Platform API, Python AI services, a Next.js operator UI, PostgreSQL with pgvector, Redis, RabbitMQ, MinIO, and deterministic local smoke checks.

## What Is Here

- Versioned workspace, project, agent, dataset, model route, run, trace, billing, and training/eval data models.
- Local RAG ingestion and retrieval through the AI services layer.
- API-key scoped chat and trace inspection paths in the Platform API.
- Next.js operator dashboard pages for model catalog, playground, deployments, datasets, traces, and settings.
- Docker Compose infrastructure and scripts for local reset, seed, smoke, backup, and restore checks.

## Repository Layout

```text
src/platform-api/   .NET API, EF Core models, migrations, and endpoints
src/ai-services/    FastAPI runtime for ingestion, embeddings, retrieval, and chat
src/web/            Next.js operator interface
docs/               Quickstarts and acceptance checklists
examples/           Local bootstrap, model-route, chat, widget, and support-doc fixtures
infra/              PostgreSQL init and backup/restore scripts
scripts/            Local development and smoke-test scripts
```

## Local Quickstart

Prerequisites:

- Docker with Docker Compose
- tmux
- .NET 10 SDK
- Node.js 20+
- Python 3.11+
- curl and jq

Start the stack:

```bash
scripts/dev-up.sh
```

Default local URLs:

- Web: `http://127.0.0.1:3002`
- Platform API: `http://localhost:5001`
- AI Services: `http://localhost:5002`
- PostgreSQL: `localhost:55432`
- MinIO: `http://localhost:59000`

Reset and seed a deterministic demo workspace:

```bash
RESET_JSON="$(scripts/dev-reset.sh)"
API_KEY="$(jq -r '.apiKey' <<<"$RESET_JSON")"
AGENT_ID="$(jq -r '.agentDefinitionId' <<<"$RESET_JSON")"
```

Run the smoke checks:

```bash
scripts/smoke.sh
```

Stop the stack:

```bash
scripts/dev-down.sh
```

See `docs/quickstart.md` for the full Phase 1.1 flow.

## Configuration

Copy `.env.example` to `.env` for local development. The example file contains local-only defaults and empty provider credential placeholders. Do not commit `.env` or live provider keys.

## License

Apache-2.0.
