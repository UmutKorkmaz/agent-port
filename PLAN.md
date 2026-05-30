# AgentPort Build Plan

AgentPort is an AI systems creation, training, testing, deployment, and operations platform. It should let a team build production AI systems from a web panel or code: LLM agents, RAG applications, tool-using workflows, prompt-based automations, fine-tuned models, rerankers, classifiers, extractors, and other ML models.

The intended position is not "another chatbot wrapper." It is the layer between models, data, tools, and production systems:

- Vercel-like deployment for agents, AI apps, and model endpoints.
- Stripe-like integration surface for models, tools, datasets, channels, and governance.
- Heroku-like developer flow for pushing an AI system config and getting a running service.
- MLflow-like lifecycle visibility for datasets, experiments, model versions, evals, and promotions.

## Founder Fit

This project should lean into Umut Korkmaz's existing strengths:

- Enterprise platform delivery across Digiflow, Pometop, Yazilimci Bul, and high-availability infrastructure.
- Deep full-stack range across React, Next.js, .NET, Node.js, Python, FastAPI, PostgreSQL, Redis, Docker, Azure, and Linux.
- Existing AI work with SLMs, RAG, virtual assistants, and production integration at Digiturk.
- Meridian experience, which maps directly to command/query orchestration, workflow state, and agent runtime patterns.

## Product Principles

- Treat every AI system as versioned infrastructure: prompts, model settings, tools, datasets, retrievers, eval suites, deployment config, and runtime version.
- Make testing and observability part of the builder, not a later dashboard. A user should be able to create, test, inspect, compare, and promote from one panel.
- Keep the first shipped flow narrow, but design the control plane around the full lifecycle: dataset -> experiment -> model or agent version -> eval -> deployment -> trace -> feedback -> improvement.
- Support LLM and non-LLM AI through the same product primitives, while using specialized backends where needed.
- Prefer self-hostable, enterprise-friendly defaults before hosted marketplace features.
- Give users informed choices instead of forcing one AI stack. The web panel should explain what each model, training, RAG, vector store, and deployment choice gives them, plus its advantage, disadvantage, cost, privacy, and operational tradeoff.
- Make "bring your own model" and "bring your own retrieval stack" first-class. Users should be able to connect OpenAI-compatible endpoints, Hugging Face, Ollama, vLLM, TGI, Pinecone, Chroma, FAISS, pgvector, managed RAG services, and future adapters through the same registry model.
- Build a trust layer around every AI action: identity, permissions, provider route, data source, prompt version, tool scope, cost, eval status, and audit trail should be visible and enforceable.
- Keep money movement, provider usage, and customer-facing invoices reconcilable. Billing estimates may be approximate, but ledger entries, price snapshots, provider usage, and corrections must be explainable.

## Choice-First Product Requirement

AgentPort should include an **AI Stack Chooser** in the web panel. It should work like a guided architecture decision assistant:

1. Ask what the user is building: chatbot, RAG app, agent, classifier, extractor, image/audio/pose model, workflow automation, document pipeline, search, recommendation, or batch job.
2. Ask constraints: privacy, latency, budget, expected traffic, data size, data freshness, self-hosting needs, team skill level, compliance, and available hardware.
3. Recommend a stack, but also show alternatives.
4. Explain every choice with:
   - what the option gives the user
   - advantages
   - disadvantages
   - best-fit use cases
   - cost and operations impact
   - lock-in risk
   - data privacy posture
   - required skill level
   - supported deployment targets
5. Save the choice as versioned architecture metadata so experiments can compare stacks, not just prompts.

This is a differentiator. Flowise, Dify, Langflow, and VectorShift prove users like visual builders and fast app creation; Pinecone, Chroma, FAISS, Meilisearch, and Ragie prove retrieval is a stack choice; Hugging Face and Ollama prove model selection should not be tied to one vendor. AgentPort should combine those lessons into a governed, enterprise-friendly chooser.

## Target Users And Primary Workflows

Primary users:

- Developers building AI features into internal or customer-facing products.
- ML engineers managing datasets, training jobs, experiments, and model versions.
- Enterprise operators responsible for security, observability, compliance, and cost.
- Domain teams that need a guided web panel for testing and reviewing AI outputs.

Core workflows:

- Create a RAG or LLM agent from a structured builder or config file.
- Upload documents or tabular data into versioned datasets and knowledge bases.
- Test an AI system in a playground with visible retrieval, tool calls, costs, and traces.
- Save playground runs and production feedback into eval datasets.
- Run prompt, RAG, safety, tool-call, and model evals from the web panel.
- Train or fine-tune models from the web panel, track metrics, and register versions.
- Promote a passing agent or model version to an endpoint, widget, or channel adapter.
- Inspect production traces, failures, costs, safety incidents, and user feedback.

## MVP Boundary

Version 0.1 should prove one complete AI-system lifecycle:

Create a versioned document-QA RAG agent, upload documents, test it in a web playground, inspect full traces, save examples into an eval dataset, run basic evals, and expose the agent through a local web chat/API endpoint.

The MVP must include the product primitives that training and testing need later, even if heavy model training is not fully built in v0.1.

### v0.1 Scope

- One local workspace.
- One project with versioned agent definitions.
- One default cloud model provider through a gateway abstraction.
- One OpenAI-compatible endpoint adapter that can point to OpenAI, Azure OpenAI, Hugging Face router, Ollama, vLLM, TGI, or any compatible local/remote endpoint when configured.
- One RAG pipeline over uploaded documents, with pgvector as the default local store and a retrieval adapter contract for Pinecone, Chroma, FAISS, Qdrant, Weaviate, Meilisearch, Ragie, and future backends.
- One tool integration through the governed tool registry.
- One web chat/test playground.
- One trace inspector for runs, retrieval, model calls, tool calls, latency, tokens, costs, and errors.
- One dataset registry path for uploaded documents and saved eval examples.
- One lightweight eval runner for exact/contains checks, citation presence, latency/cost thresholds, and a placeholder rubric scorer.
- One local deployment endpoint and embeddable chat surface.
- One AI Stack Chooser page that displays model, retrieval, training, and deployment alternatives with clear advantages and disadvantages, even if only the default adapters are executable in v0.1.
- One local identity model with users, service accounts, API keys, roles, project membership, environment labels, and audit events, even if v0.1 runs as a single local workspace.
- Data-model support for `TrainingJob`, `Experiment`, and `ModelVersion`, even if full training arrives in v0.2.
- Data-model support for `ModelRoute`, `ProviderPriceSnapshot`, `UsageEvent`, `WalletAccount`, `WalletTransaction`, `WalletReservation`, `PaymentProvider`, `PaymentIntent`, `Invoice`, and `TaxRecord`, even if real payment capture arrives after v0.1.
- Cost estimates in every trace using a price snapshot: provider cost, AgentPort fee, optional model-owner markup, tax placeholder, and total estimate.
- Payment and provider feature flags in environment configuration so global and Turkey-specific payment methods can be activated without code changes.

### Explicitly Out Of Scope For v0.1

- Production payment capture, public marketplace sales, cash settlement, and revenue-share payouts.
- Full multi-tenant SaaS isolation, SSO, SAML, and production organization billing.
- Broad channel support beyond web chat/API.
- Visual workflow canvas. Start with structured forms plus YAML/JSON editing.
- Open-ended MCP hosting. Start with one governed local tool path.
- GPU-heavy LoRA/QLoRA UI. Define contracts now; implement after eval and registry foundations are stable.

### v0.2 Training Commitment

Version 0.2 should make training and testing from the web panel real:

- A CSV classification/regression training flow using a simple CPU backend such as scikit-learn or LightGBM.
- A provider fine-tuning flow where supported by the selected model provider.
- Training job logs, metrics, artifacts, status, retries, cancellation, and failure diagnostics.
- Model registry entries with aliases such as `candidate`, `staging`, `production`, and `rollback`.
- Required eval gates before promotion.

## Phase-By-Phase Delivery Roadmap

Do not build everything at once. AgentPort should move through gated phases. Each phase should produce a usable product slice and should not start broad expansion until its exit criteria pass.

### Phase 0: Contracts And Skeleton

Target: week 0-1.

Goal:

- Create the repo, schemas, service boundaries, local runtime, and first UI shell before adding breadth.

Build:

- Repository skeleton.
- Docker Compose for PostgreSQL, pgvector, Redis, RabbitMQ, and object storage.
- Initial database migrations for core primitives.
- Local user, service account, API key, environment, secret reference, policy, and audit contracts.
- Agent definition schema.
- Model route and provider price snapshot schema.
- Dataset, knowledge base, trace, eval, deployment, wallet, and payment-provider config schemas.
- Basic web shell with navigation.
- Quickstart docs for running local development.

Exit criteria:

- A developer can start the stack locally.
- The platform can create one local workspace, project, user, API key, model route, and empty agent definition.
- Schema migrations run from a clean database.
- No feature requires raw secrets in committed config.

### Phase 1: MVP Document QA Agent

Target: weeks 1-4.

Goal:

- Ship the first useful MVP: create a document-QA RAG agent, test it, trace it, evaluate it, and expose it through web chat/API.

Build:

- First-run onboarding wizard: create workspace, project, model route, API key, default budget, first document dataset, and first RAG agent.
- One guided template: "Website/document support chatbot."
- File upload for PDF/text/Markdown.
- Ingestion, chunking, embeddings, pgvector retrieval, citations, and failed-file diagnostics.
- One OpenAI-compatible model gateway route plus optional Ollama route.
- AI Stack Chooser with visible alternatives, even if only default routes execute.
- Playground with streaming response, retrieved chunks, provider cost estimate, AgentPort fee estimate, trace, and save-as-test-case.
- Basic eval runner over saved examples.
- Local API endpoint and embeddable chat widget.
- Operator basics: job queue view, failed ingestion view, provider route health, and recent error list.
- Basic backup script for local PostgreSQL and object storage metadata.
- MVP docs: quickstart, first bot tutorial, API key setup, widget install, and local troubleshooting.

Exit criteria:

- A new user can complete onboarding without manual database edits.
- A document upload can become a working RAG bot from the web panel.
- A run produces trace, cost estimate, citations, and eval dataset examples.
- The agent can be called from the web panel and a scoped API key.
- MVP can be restored from a local backup in a clean environment.

### Phase 1.1: MVP Stabilization And Secure Integration

Target: week 4-5.

Goal:

- Turn the working document-QA MVP into a deterministic, repeatable, and safely bounded local product slice before training, SaaS, or broad adapter work starts.

Build:

- Dev-only reset and demo seed flow that clears local demo data, MinIO objects, ingestion jobs, chunks, runs, traces, and wallet placeholders without leaving orphaned rows or duplicate demo records.
- Document lifecycle controls in API and UI: list documents, delete or archive a document, reingest changed content, skip unchanged uploads by content hash, and surface ingestion failures with actionable status.
- Reingest versioning rules: either atomically replace chunks for the active document version or mark old chunks inactive so stale chunks cannot appear in retrieval while historical run citations remain readable.
- Scoped API key enforcement for local chat/API/widget calls. Invalid or missing keys return `401`; insufficient scopes return `403`; every run records the key, workspace, project, agent, and environment context.
- Widget security preparation: local widget config, allowed-origin placeholders, rate-limit placeholders, short-lived widget session token path, and copyable embed snippet that never exposes an admin or long-lived key in browser code.
- Provider gateway preparation: normalized route health, timeout, error, token/cost, and fallback fields for OpenAI-compatible and Ollama/local paths, without adding broad provider fan-out yet.
- RAG quality controls: configurable `topK`, score threshold, no-answer fallback, chunk metadata preservation, citation IDs, and a small golden question set for regression checks.
- Playground and traces polish: show auth mode, provider route, fallback mode, retrieved chunks, score/threshold, citations, latency, token/cost estimate, and trace/run identifiers in one inspectable flow.
- Smoke and Playwright coverage for reset, onboarding/bootstrap, upload, delete, reingest, valid/invalid API key chat, widget snippet loading, playground answer with citations, and trace detail.
- Documentation updates for reset, API key usage, widget local embed, document lifecycle behavior, RAG quality controls, and known Phase 1.1 limitations.

Implementation order:

1. Dev reset and deterministic demo seed.
2. Document delete/reingest API, data rules, and UI.
3. API key enforcement, scoped local widget token path, and deployment snippets.
4. Provider gateway normalization and RAG quality controls.
5. Playwright smoke, API integration tests, docs, and final acceptance gate.

Exit criteria:

- `scripts/dev-up.sh` followed by a single reset/seed path produces the same demo state every time.
- Re-running reset, upload, delete, and reingest does not duplicate documents, chunks, traces, or demo wallet rows.
- Deleting a document removes it from active retrieval and UI listings while preserved historical runs still render their saved citation snapshots.
- Reingesting unchanged content is skipped by content hash; changed content replaces or versions chunks without stale retrieval.
- Local API/widget chat rejects missing or invalid keys, accepts a scoped key/session token, and records the key context in the run/trace.
- The local widget snippet works from the Deployments page and respects the allowed-origin/rate-limit placeholder config.
- RAG tests prove cited answers for known questions and a no-answer response when retrieved scores are below threshold.
- `scripts/smoke.sh`, backend/API tests, AI service tests, web build, and Playwright smoke all pass from a clean local environment.

Explicitly out of scope:

- Real payment capture, invoice settlement, model-owner payouts, or tax filing.
- Real fine-tuning, training jobs, GPU orchestration, or model artifact download.
- Production SaaS hardening, enterprise SSO, full multi-tenant isolation, and public marketplace flows.
- Active Pinecone, Chroma, FAISS, Qdrant, Weaviate, Meilisearch, or Ragie adapters beyond selectable roadmap placeholders.

### Phase 2: Training, Evals, And Model Lifecycle

Target: weeks 5-8.

Goal:

- Make testing and training real enough for model and agent promotion.

Build:

- Dataset cards, split management, PII/license metadata, and golden dataset protection.
- CSV classification/regression training with CPU-friendly scikit-learn or LightGBM.
- Provider fine-tuning where supported by the selected model route.
- Training package or wallet hold before costly training jobs.
- Model registry with aliases: `candidate`, `staging`, `production`, and `rollback`.
- Eval gates for prompts, RAG, tools, training, latency, cost, and safety.
- Human review queues for failed eval cases, bad answers, tool approvals, and golden examples.
- Model/version comparison UI.

Exit criteria:

- A user can train a small classifier from the web panel.
- A provider fine-tune can be configured, budgeted, run, evaluated, and registered where the provider supports it.
- No model or agent reaches production without passing configured gates.
- Human reviewers can approve, reject, or label cases with audit identity.

### Phase 3: Integrations, Templates, API, And Developer Experience

Target: weeks 9-12.

Goal:

- Let users integrate AgentPort into real websites, apps, and business data sources.

Build:

- Connector catalog v1: Google Drive, Notion, Slack, Gmail, Outlook, SharePoint, GitHub, Postgres, MySQL, REST API, S3/Azure Blob, URL crawl, and manual upload.
- Connector permission model, sync state, retry rules, deletion rules, and source ACL metadata.
- Template gallery: support bot, PDF RAG, website chatbot, sales assistant, classifier, extractor, tool-using agent, MCP starter, and document processor.
- Import path for external builders later: Flowise, Dify, Langflow, and OpenAI assistant/agent-style configs.
- Public REST/OpenAPI surface with streaming chat/run APIs.
- SDKs for TypeScript, Python, and .NET.
- Signed webhooks for run/eval/deployment/wallet/provider-status events.
- Docs site: quickstarts, API docs, SDK examples, webhook examples, pricing examples, security guide, and deployment guide.
- Website/app integration example project.

Exit criteria:

- A real external website can embed or call an AgentPort bot.
- At least three connectors can sync data into a knowledge base with traceable permissions.
- Templates can be created, cloned, versioned, and deployed.
- SDK examples pass against the local stack.

### Phase 4: Commercial SaaS, Billing, And Operations

Target: weeks 13-16.

Goal:

- Turn the local platform into a billable SaaS-ready control plane without losing cost transparency.

Build:

- Production wallet ledger, reservations, high-precision usage math, rounding rules, invoices, refunds, and reconciliation jobs.
- Stripe global payments.
- One Turkey payment route: iyzico or Craftgate first.
- Manual bank transfer: wire plus Havale/EFT/FAST.
- Cost forecasting by model route, expected traffic, training job, endpoint hosting, storage, and connector sync.
- Provider price sync and provider status page.
- Admin/operator console: failed jobs, stuck queues, provider outages, price-sync failures, webhook failures, payment reconciliation issues, tenant spend, dangerous tools, and abuse alerts.
- Legal/business document set prepared for legal review: Terms of Service, Acceptable Use Policy, Privacy Policy, Data Processing Agreement, refund policy, service terms, provider terms mapping, and payment/credit terms.
- Abuse moderation: endpoint abuse detection, spam detection, public widget throttling, card-testing protection, API-key leak response, and unsafe-content flags.

Exit criteria:

- A wallet top-up, request reservation, actual debit, invoice line, and refund path work in sandbox.
- Provider price changes can be reviewed before billing impact.
- Operators can see and act on outages, stuck jobs, payment issues, and abuse signals.
- Public endpoints have rate limits, budgets, and abuse controls.

### Phase 5: Enterprise, Self-Hosted, And Reliability

Target: weeks 17-24.

Goal:

- Make AgentPort credible for enterprise and self-hosted customers.

Build:

- SSO/SAML/OIDC and organization-level RBAC/ABAC.
- Tenant isolation, data residency, audit export, and retention controls.
- Model/provider compliance matrix: data retention, training use, region, zero-retention options, HIPAA/GDPR/SOC2 posture, enterprise-only flags, and BYOK support.
- Customer-managed keys later.
- Self-hosted packaging: license key, offline mode, private model routes, private container registry support, upgrade/migration path, and environment health checks.
- Backup, restore, and disaster recovery: database backups, object storage backups, wallet ledger restore, provider key restore, model artifact recovery, audit retention, and restore drills.
- Provider outage playbooks and fail-closed/fallback policies.
- Version migration framework for agent configs, prompt schemas, model route schemas, dataset schemas, and deployment packages.

Exit criteria:

- An enterprise workspace can enforce provider allowlists, data retention, budgets, SSO, and audit export.
- A self-hosted install can be installed, upgraded, backed up, restored, and health-checked.
- Old deployed versions keep running after schema migrations.

### Phase 6: Marketplace And Ecosystem

Target: after Phase 5 gates pass.

Goal:

- Let users share and sell templates, tools, agents, model endpoints, datasets, and eval packs safely.

Build:

- Private organization marketplace first.
- Public marketplace later.
- Marketplace moderation: malware scanning, prompt/tool review, license checks, provider-term checks, dataset rights checks, unsafe-use screening, takedown flow, rating abuse controls, and version review.
- Revenue share and payout ledger.
- Public template and connector submission workflow.
- Verified publisher and enterprise-approved package channels.

Exit criteria:

- A private organization can publish and reuse internal templates safely.
- Public listings cannot be published without review, license metadata, and moderation status.
- Revenue-share events reconcile with usage and payout records.

### Phase 7: Advanced Differentiation

Target: after the platform can reliably build, test, train, deploy, bill, and operate real AI systems.

Build:

- Cost optimization engine.
- Automatic route selection by quality, latency, cost, privacy, and eval history.
- AI system composer for mixed agent/retriever/classifier/evaluator workflows.
- Multimodal voice, image, audio, video, and OCR systems.
- Federated agent networks.
- GraphRAG/KAG, semantic cache, and prompt-cache strategies.
- Advanced simulation, replay, and production A/B testing.

Exit criteria:

- Optimization decisions are based on measured eval and production outcomes, not static routing rules alone.
- Advanced features preserve the same identity, trace, billing, policy, and rollback guarantees as the MVP.

## Product Name

Working name: AgentPort

Reasoning:

- "Agent" is the main user-facing primitive for LLM systems.
- "Port" maps to connectors, adapters, deployment ports, and enterprise integration.
- The name leaves room for runtime, marketplace, SDKs, training, registry, and hosted infrastructure without sounding limited to chat.

## Core Domain Model

AgentPort should model these primitives explicitly:

| Primitive | Purpose |
| --- | --- |
| Workspace | Local or organization boundary. v0.1 has one local workspace. |
| Organization | Commercial, security, and billing owner. v0.1 can map organization to the local workspace, but the schema should not block SaaS later. |
| User | Human account with role assignments, billing permissions, project access, and audit identity. |
| Service Account | Non-human identity for CI/CD, deployed endpoints, connectors, and automation. |
| Membership / Role | Workspace, organization, and project-level authorization binding. |
| Project | Product or use-case container for agents, models, datasets, evals, and deployments. |
| Environment | Development, staging, production, or custom label with secrets, budgets, deployments, and policy gates. |
| API Key | Scoped key for SDKs, public endpoints, widgets, and server-to-server calls with rate limits, budget limits, expiration, and rotation. |
| AI System | A deployable unit: agent, RAG app, workflow, model endpoint, classifier, extractor, or batch job. |
| Agent Definition | Versioned instructions, model settings, tools, memory, retrieval, guardrails, and output schema. |
| Dataset | Versioned input data from uploads, traces, feedback, synthetic generation, or external imports. |
| Data Source Connector | File upload, database, API, SaaS connector, object store, Git repo, URL crawl, or manual entry with sync state, permissions, and deletion rules. |
| Connector Definition | Versioned connector adapter with auth mode, scopes, sync strategy, rate limits, deletion behavior, and source ACL support. |
| Connector Sync Job | Incremental or full sync run with cursor, status, retries, failures, deleted-source handling, and trace links. |
| Dataset Card | Source, owner, schema, license, PII classification, consent/usage rights, quality notes, intended use, and retention policy. |
| Knowledge Base | Document collection, chunks, embeddings, retriever config, citations, and permissions. |
| Prompt Version | Managed prompt/template with variables, owner, tests, and linked eval results. |
| Template | Reusable agent, RAG, workflow, tool, dataset, eval, or deployment starter with inputs, assumptions, owner, version, license, and moderation state. |
| Import Job | Conversion attempt from Flowise, Dify, Langflow, OpenAI assistant/agent-style config, YAML, JSON, or SDK package into AgentPort primitives. |
| Tool | Local, API, MCP, database, filesystem, or custom tool with schema, scopes, secrets, approvals, and audit. |
| Secret Reference | Pointer to local env, vault, cloud secret manager, or customer-provided key without exposing raw secret values to configs or traces. |
| Policy | Versioned rule for provider allowlists, tool scopes, data residency, retention, budgets, human approvals, redaction, and deployment gates. |
| Model | Provider model, hosted model, fine-tuned model, LoRA adapter, embedding model, reranker, or classical ML model. |
| Model Route | A specific way to access a model: first-party API, Azure, Vertex, Bedrock, OpenRouter, Hugging Face Inference Provider, Fireworks, Groq, Together, Replicate, Ollama, vLLM, TGI, or private endpoint. Same model through different routes has different price, policy, region, latency, and availability. |
| Model Provider Account | BYOK customer key, AgentPort-managed key, enterprise cloud account, or local runtime credential reference. |
| Model Endpoint Product | Non-downloadable hosted model or agent endpoint with versioned price, owner, usage policy, and deployment binding. |
| Experiment | Reproducible run linking dataset version, prompt, model, retriever, tool config, runtime, metrics, and artifacts. |
| Training Job | Long-running job for fine-tuning, distillation, adapter training, or non-LLM model training. |
| Evaluation Suite | Dataset plus graders, metrics, thresholds, and release gates. |
| Evaluation Run | Versioned results with per-case scores, failure reasons, traces, cost, latency, and artifacts. |
| Trace | Span tree of one request or job: retrieval, model calls, tool calls, guardrails, approvals, and errors. |
| Deployment | Active endpoint, widget, package, channel, or worker binding to a specific AI system version. |
| Usage Event | Normalized billable event for inference, embeddings, reranking, image, audio, tools, storage, training, evals, batch jobs, and endpoint hosting. |
| Provider Price Snapshot | Immutable price row used for a request or job, including provider, route, model, region, unit prices, source URL, effective time, AgentPort fee, markup, and currency. |
| Wallet Account | Organization or user credit balance by currency with available, reserved, pending, refunded, and disputed states. |
| Wallet Transaction | Append-only credit/debit/reservation/release/refund/adjustment ledger row with idempotency key and source object. |
| Wallet Reservation | Temporary hold before actual usage cost is known; finalized, released, expired, or corrected later. |
| Payment Provider | Stripe, PayPal/Braintree, Adyen, Checkout.com, Paddle, Lemon Squeezy, iyzico, PayTR, Craftgate, Param, Sipay, Paynet, Shopier, Papara, Paycell, Hepsipay, bank VPOS, or bank transfer adapter. |
| Payment Intent And Transaction | Checkout/payment lifecycle records with provider IDs, status, amount, currency, payment method, webhook state, and reconciliation status. |
| Invoice And Tax Record | Customer-facing billing summary, tax/KDV/VAT breakdown, credit note/refund links, and accounting export. |
| Cost Forecast | Estimated spend by project, model route, endpoint, traffic, training plan, connector sync, storage, and time period. |
| Provider Status | Health, latency, error rate, price-sync state, outage status, fallback eligibility, and operator notes for a provider route. |
| Operator Alert | Internal alert for stuck jobs, provider failures, payment reconciliation, abuse, suspicious spend, unsafe outputs, or backup failures. |
| Backup And Restore Point | Database, object storage, model artifacts, wallet ledger, audit log, and config backup metadata with restore-test status. |
| Legal Document | Versioned Terms of Service, Acceptable Use Policy, Privacy Policy, DPA, refund policy, service terms, provider terms mapping, and payment/credit terms. |
| Audit Event | Immutable security and compliance record, separate from sampled observability traces. |

## AI Stack Choice Matrices

The web panel should expose these matrices as interactive choices. Users should not need to already know whether they need Hugging Face, Ollama, Pinecone, FAISS, RAG, CAG, KAG, fine-tuning, or a no-code classifier. AgentPort should explain the practical tradeoff and let them choose.

### Builder And Workflow Options

| Choice | What It Gives Users | Advantages | Disadvantages | Best When |
| --- | --- | --- | --- | --- |
| AgentPort structured builder | Forms plus YAML/JSON for agents, RAG, tools, evals, deployments, and training jobs | Easier to validate, version, diff, test, and deploy | Less visually expressive than drag-and-drop | v0.1 and enterprise-safe workflows |
| Visual flow canvas | Drag-and-drop blocks like Flowise, Dify, Langflow, and VectorShift | Fast for non-code builders; good for explaining workflows | Can become messy; harder to review and version | Phase 3 after contracts are stable |
| Code SDK | C#, TypeScript, and Python SDKs for repeatable systems | Best for developers, CI/CD, tests, and source control | Less accessible to non-technical users | Production teams and custom systems |
| Template gallery | Prebuilt recipes for support bots, document QA, extractors, classifiers, sales agents, and reports | Faster onboarding and demos | Templates can hide important assumptions | Marketplace and internal accelerators |
| Teachable Machine-style simple trainer | Browser-style guided model creation for image, audio, pose, or simple classification | Very easy for education, prototypes, and domain users | Limited model types, evaluation depth, and production controls | No-code prototypes and edge/interactive demos |
| Automation builder | Trigger/action workflows with connectors like email, Slack, CRM, Drive, Notion, and databases | Useful for business process automation | High security and approval needs | Back-office AI workflows |

### AI System Types Users Can Build

| System Type | What It Gives Users | Required Platform Pieces | First Useful Version |
| --- | --- | --- | --- |
| Chatbot | Web chat, API chat, or embeddable support assistant | Agent definition, model route, trace, eval, widget/API key | v0.1 |
| RAG assistant | Answers over uploaded docs or connected knowledge sources | Ingestion, chunks, embeddings, retriever, citations, evals | v0.1 |
| Tool-using agent | Calls APIs, local tools, MCP tools, databases, or workflows with approvals | Tool registry, policy, approval, audit, trace replay | v0.1 narrow; broader later |
| MCP integration | Governed MCP client/server usage for approved tools | OAuth/resource binding later, tool schemas, scopes, sandboxing | v0.2+ |
| Workflow automation | Trigger/action flows for CRM, email, Slack, Drive, databases, and custom APIs | Connector registry, secrets, retries, human approval | Phase 3 |
| Model endpoint | Hosted fine-tune, LoRA adapter, classifier, reranker, extractor, or agent package | Model registry, deployment, API key, usage billing | v0.2+ |
| Classifier / router | Routes requests, tags records, moderates content, detects intent, or scores risk | Dataset, CPU training, evals, deployment endpoint | v0.2 |
| Extractor / document processor | Structured output from PDFs, forms, emails, or OCR documents | Schema validation, OCR later, evals, human review queue | v0.2+ |
| Multimodal app | Image/audio/video understanding or generation | Model catalog flags, media storage, modality-specific cost/evals | Phase 5 |
| AI platform for a customer app | Full API, SDK, widget, model catalog, billing, evals, and governance surface | Public API, webhooks, org/project roles, billing, docs | Phase 3+ |

### Model Provider And Serving Options

| Choice | What It Gives Users | Advantages | Disadvantages | Best When |
| --- | --- | --- | --- | --- |
| OpenAI / Azure OpenAI | Frontier LLMs, embeddings, structured output, tool calling, strong production APIs | High quality, reliable agent behavior, low ops burden | Cost, vendor dependency, data-policy review needed | First production demo and enterprise deployments |
| Anthropic Claude | Strong reasoning, coding, writing, tool use, and cloud routes through first-party, Bedrock, and Vertex | Excellent quality and enterprise cloud options | Route-specific pricing and data residency differences | Agents, coding, support, analysis, regulated enterprise routes |
| Google Gemini / Vertex AI | Gemini models through developer API or enterprise Google Cloud route | Multimodal support, Google Cloud IAM/region controls, broad ecosystem | Pricing and model behavior differ between API and Vertex routes | Multimodal apps and Google Cloud customers |
| xAI Grok | Grok models where customers want xAI access | Adds another frontier-family option | Public pricing/model metadata may change quickly | Customers specifically requesting Grok |
| Z.ai GLM | GLM models through first-party or routed hosts | Competitive coding/reasoning options and regional diversity | Provider maturity and route availability vary | Teams testing GLM against OpenAI/Claude/Gemini |
| Moonshot Kimi | Kimi long-context and agent/coding models where available | Strong long-context choice and China/global ecosystem coverage | Pricing and availability can be region/account constrained | Long documents, coding, multilingual use cases |
| Mistral / Cohere / DeepSeek | Specialized hosted models, embeddings, rerankers, coding, and lower-cost alternatives | Model diversity, cost leverage, rerank/embedding strengths | Different APIs, pricing dimensions, and eval profiles | Avoiding single-provider dependency and optimizing cost/quality |
| Groq / Together / Fireworks / Replicate | Hosted open-model inference and model marketplace routes | Fast access to open/proprietary models without operating GPUs | Model availability, cold starts, and pricing can vary | Open-model serving, image/video/audio, and custom endpoints |
| OpenRouter | Model-router marketplace across many providers | Broadest quick access, fallback, provider filtering, price-aware routing | Additional routing layer and provider policy complexity | Rapid model choice and fallback experiments |
| Hugging Face Inference Providers | One Hugging Face API across many providers and tasks | Broad model catalog, provider routing, chat/image/embedding/classification tasks | Some OpenAI-compatible paths are chat-specific; provider behavior varies | Users want many open and hosted model choices quickly |
| AWS Bedrock / Vertex AI / Azure AI Foundry | Enterprise cloud brokers for provider and open models | IAM, private networking, compliance, regional controls | Pricing, quota, and region behavior differ from first-party APIs | Enterprise customers already standardized on cloud platforms |
| Hugging Face Inference Endpoints / TGI | Managed or self-hosted open-weight serving | Good for open models, streaming, batching, OTel/Prometheus, tool/schema guidance | Requires model/infra knowledge and GPU planning | Production open-weight LLM serving |
| Ollama | Local models behind a simple local API | Private, cheap, offline, great for developer mode and customer demos | Not ideal as the main high-scale serving layer | Local development, demos, air-gapped trials |
| vLLM | High-throughput OpenAI-compatible open-weight serving | Strong production path, batching, GPU efficiency, model flexibility | Needs GPU/Linux ops and model-serving skill | Self-hosted production inference |
| llama.cpp / LM Studio / LocalAI | Lightweight local or edge model serving | CPU/GPU flexibility, good for laptops and edge devices | Lower throughput and more compatibility variance | Desktop, edge, and constrained environments |
| LiteLLM or custom gateway | Unified routing across providers and compatible endpoints | One policy layer for keys, fallback, cost, logs, and routing | Gateway becomes critical infrastructure | AgentPort default abstraction |
| Cloud AutoML / Vertex AI / Azure AI | Managed training and deployment for specialized models | Less training infrastructure to build | Cloud lock-in and pricing | Image/object/tabular workflows where managed tooling saves time |

Policy:

- Hugging Face should be treated as a model, dataset, task, and training ecosystem, not just another LLM provider.
- Ollama should be a first-class local/private option, especially for developer mode and self-hosted trials.
- vLLM and TGI should be the recommended production paths for open-weight models.
- The model gateway should normalize chat, responses, embeddings, reranking, images, audio, batch jobs, and structured outputs where possible, but must preserve provider-specific capability flags.
- The catalog must separate `model` from `route`: for example, Claude through Anthropic, Bedrock, Vertex, OpenRouter, or a customer cloud account is five selectable routes with different price, data policy, region, quota, and latency.
- The user-facing choice must show provider cost, AgentPort fee, optional owner markup, estimated total, key mode, data policy, and best-fit guidance before a route is selected.

### Training Options

| Choice | What It Gives Users | Advantages | Disadvantages | Best When |
| --- | --- | --- | --- | --- |
| No training, prompt/RAG only | Build with instructions, retrieval, tools, and evals | Fastest, cheapest, safest first step | May not learn style or specialized decisions | Most MVPs and knowledge assistants |
| Teachable Machine-style browser training | Simple image/audio/pose classifiers with export path | Very approachable, good for demos and education | Narrow model scope and weak enterprise lifecycle | Prototyping recognition tasks |
| CPU classical ML | scikit-learn, LightGBM, XGBoost-style tabular/text classifiers | Cheap, fast, explainable, production-friendly | Not generative and requires labeled data | Routing, triage, risk scoring, classification |
| Provider fine-tuning | Fine-tune hosted models through provider APIs | Lower ops, easy deployment path | Provider lock-in and task/model limits | Style, format, domain behavior improvements |
| Hugging Face AutoTrain | No-code/low-code training across LLM and non-LLM tasks | Broad task coverage and open ecosystem | Needs validation before enterprise production | Fast experiments and open-model fine-tuning |
| LoRA / QLoRA | Adapter training for open-weight LLMs | Lower GPU cost than full fine-tuning; portable adapters | Still needs GPU skill and serving compatibility | Custom SLM economics and private models |
| Distillation | Train smaller model from teacher outputs | Can lower cost/latency while preserving behavior | Requires high-quality reviewed data and evals | Scaling a proven expensive workflow |
| Synthetic data generation | Generate examples, questions, edge cases, labels, and red-team tests | Fills data gaps quickly | Can amplify model bias or bad assumptions | Bootstrapping evals and training sets |
| Human labeling/review | SME labels, golden answers, preference pairs, and safety labels | Highest trust for domain quality | Slow and expensive | Regulated or domain-specific systems |

Training rule:

- The panel should always ask: "Can this be solved with prompt, RAG, CAG, KAG, code, or a smaller classifier before training a model?"
- Training should require a dataset card, validation split, baseline eval, cost estimate, rollback path, and post-training eval comparison.

### Retrieval, RAG, And Context Options

| Choice | What It Gives Users | Advantages | Disadvantages | Best When |
| --- | --- | --- | --- | --- |
| Basic RAG | Retrieve chunks, inject context, answer with citations | General-purpose and easy to understand | Can fail on chunking, ranking, or missing context | v0.1 document QA |
| Hybrid RAG | Dense vector plus keyword/BM25 search | Better precision on names, IDs, rare terms | More tuning and ranking complexity | Enterprise docs, legal, support, product catalogs |
| Reranked RAG | First retrieve broad set, then rerank | Better answer quality and fewer irrelevant chunks | Extra latency and model cost | High-quality RAG with larger corpora |
| Agentic RAG | Agent plans multi-step retrieval and tool calls | Better for complex questions | More latency, cost, and safety risk | Research, analytics, multi-hop tasks |
| GraphRAG / KAG | Structured entities, relationships, and reasoning over domain knowledge | Better for professional domains and explainability | Higher ingestion/modeling complexity | Legal, medical, finance, compliance, engineering knowledge |
| CAG / prompt-cache approach | Preload or cache stable context for long-context models | Lower retrieval latency for small stable corpora | Context-window and cache invalidation limits | Stable manuals, policies, small knowledge packs |
| Fine-tuning instead of RAG | Model internalizes style or repeated behavior | Lower runtime retrieval complexity | Not good for fresh factual knowledge; retraining needed | Format, style, classification, domain behavior |
| Managed context engine such as Ragie | Connectors, parsing, chunking, indexing, and retrieval as a service | Fastest path for production RAG without pipeline ops | Vendor dependency and less low-level control | Teams that want to ship RAG quickly |

### Vector Store And Search Options

| Choice | What It Gives Users | Advantages | Disadvantages | Best When |
| --- | --- | --- | --- | --- |
| PostgreSQL + pgvector | Vectors beside relational metadata | Simple local MVP, fewer moving parts, SQL filters | May need tuning or migration at scale | v0.1 and smaller enterprise installs |
| Pinecone | Managed vector DB for AI workloads | Low ops, namespaces, scaling, metadata filtering, fast production start | Usage cost and vendor lock-in | SaaS-scale RAG and agent memory |
| Chroma | Open-source AI search with local and cloud options | Great developer UX, vector/full-text/metadata search, local start | Enterprise scaling depends on deployment model | Local-first builders and teams wanting OSS search |
| FAISS | Library for efficient dense-vector similarity search | Very fast, mature, GPU options, no service dependency | Not a full database; you build persistence/API/metadata | Embedded retrieval, research, custom infra |
| Meilisearch | Fast search with hybrid keyword/vector strengths | Excellent UX for lexical + semantic search | Less specialized than pure vector DBs for all workloads | Product search, typo-tolerant RAG, hybrid retrieval |
| Qdrant | Open-source vector database with filtering | Good self-hosted production option | Extra service to operate | Teams wanting OSS vector DB control |
| Weaviate | Vector DB with hybrid search and schemas | Good RAG ecosystem and managed/self-hosted options | More platform complexity than pgvector | Knowledge apps with schema-rich search |
| Milvus / Zilliz | Large-scale vector database | Strong at scale and ANN workloads | Operational complexity if self-hosted | Large retrieval datasets |
| Elasticsearch / OpenSearch | Enterprise search plus vector search | Existing ops footprint, hybrid search, observability | Vector search may lag specialized DBs | Enterprises already running search clusters |
| MongoDB Atlas Vector Search | Vector search inside MongoDB data model | Fewer moving parts for MongoDB teams | Stack lock-in and tuning complexity | Existing MongoDB applications |
| Vespa | Search, vector retrieval, and ML ranking in one system | Very powerful ranking and massive scale | Steep learning curve and ops complexity | Advanced search/ranking teams |

Default path:

- v0.1 default: pgvector.
- Local developer alternative: Chroma or FAISS.
- Managed scale alternative: Pinecone.
- Hybrid product-search alternative: Meilisearch, Elasticsearch/OpenSearch, or Vespa.
- Managed RAG alternative: Ragie.

### RAG Framework And Orchestration Options

| Choice | What It Gives Users | Advantages | Disadvantages | Best When |
| --- | --- | --- | --- | --- |
| AgentPort native runtime | One governed contract across agents, tools, evals, deployments, and training | Versionable and enterprise-controlled | More initial platform work | Core product |
| LangChain / LangGraph | Rich agent and workflow ecosystem | Huge integrations and rapid prototyping | Can become complex without discipline | Complex agents and experiments |
| LlamaIndex | Data connectors, indexing, retrieval orchestration | Strong RAG-specific abstractions | Less broad as an agent platform | Knowledge-intensive RAG |
| Haystack | Structured production RAG pipelines | Stable, testable, enterprise-friendly | Python-centric and less agent-experimental | Regulated or production RAG |
| Semantic Kernel | Microsoft-oriented orchestration and planners | Fits .NET/Azure enterprise stack | Smaller open ecosystem than LangChain | Microsoft-heavy customers |
| Flowise | Open-source visual agent/RAG builder | Fast visual iteration, multi-agent, HITL, traces, APIs, SDKs, embedded chat | Visual flows can be harder to govern | Inspiration and import/export compatibility |
| Dify | Production-ready agentic workflow and RAG platform | Strong model switching, workflow, MCP, tools, marketplace ideas | Competes directly; may be too opinionated to embed | Feature benchmark and migration target |
| Langflow | Low-code agents/RAG/MCP server builder | Python under the hood, deploy/share/collaborate | Primarily Python ecosystem | Visual builder benchmark |
| VectorShift | No-code plus code SDK automation platform | Strong template, connector, automation, and deployment UX | Hosted-platform dependency | Product UX benchmark |

AgentPort should not copy these tools blindly. It should learn from them and differentiate through enterprise governance, model/training/retrieval choice transparency, versioned contracts, and .NET-friendly production runtime.

## Architecture

```text
AgentPort
|
+-- Platform API / Control Plane (.NET + ASP.NET Core)
|   +-- Projects, workspaces, and AI system definitions
|   +-- Users, roles, service accounts, environments, API keys, and quotas
|   +-- Dataset, prompt, model, experiment, and deployment metadata
|   +-- Runs, traces, evals, feedback, audit events, and approvals
|   +-- AI Stack Chooser metadata and recommendation rules
|   +-- Secrets references and provider configuration
|   +-- Wallet, usage, pricing, payment, invoice, and tax metadata
|
+-- Identity, Policy, And Trust Service
|   +-- Authentication, role checks, API key scopes, service accounts, and key rotation
|   +-- Provider/model/tool allowlists, data residency, retention, and budget policies
|   +-- Audit events, policy evaluation records, and approval workflows
|
+-- Agent Runtime (.NET + ASP.NET Core)
|   +-- Durable run state machine
|   +-- Tool registry and execution policy
|   +-- Memory and context manager
|   +-- Model gateway client
|   +-- Retrieval client
|   +-- Context strategy selector: RAG, hybrid RAG, reranked RAG, CAG, GraphRAG/KAG
|   +-- Guardrail and approval hooks
|   +-- OpenTelemetry-compatible trace emitter
|
+-- AI / ML Services (Python + FastAPI)
|   +-- Document ingestion and parsing
|   +-- Chunking, embeddings, retrieval, and reranking
|   +-- Provider adapters for Hugging Face, Ollama, vLLM, TGI, and cloud APIs
|   +-- Prompt, RAG, safety, and model evaluation
|   +-- Fine-tuning and distillation adapters
|   +-- Non-LLM training backends
|
+-- Training And Evaluation Workers
|   +-- Dataset validation and split creation
|   +-- Eval job execution
|   +-- Training job orchestration
|   +-- Checkpoint, artifact, and metrics logging
|   +-- Retry, cancellation, and failure diagnostics
|
+-- Model Lifecycle Service
|   +-- Model registry
|   +-- Model catalog and capability matrix
|   +-- Model aliases and promotion
|   +-- Deployment gates
|   +-- Rollback targets
|   +-- Provider and serving compatibility checks
|
+-- Billing And Payment Service
|   +-- Provider price catalog and price-sync jobs
|   +-- Cost estimator for requests, training jobs, evals, tools, and deployments
|   +-- Wallet reservations, immutable ledger, invoices, refunds, and reconciliation
|   +-- Payment provider adapters activated by environment flags
|   +-- Idempotency, webhook signature verification, PCI-safe checkout boundaries, and fraud controls
|
+-- Connector And Template Service
|   +-- Connector catalog, sync jobs, source ACL metadata, deletion handling, and retries
|   +-- Template gallery, import jobs, versioned recipes, and moderation state
|
+-- Operations And Reliability Service
|   +-- Operator alerts, provider status, backup/restore jobs, cost forecasts, and incident notes
|
+-- Web App (Next.js + TypeScript)
|   +-- AI Stack Chooser
|   +-- AI system builder
|   +-- Onboarding wizard, template gallery, and connector catalog
|   +-- Playground and chat/test panel
|   +-- Datasets and knowledge bases
|   +-- Training jobs and experiments
|   +-- Evaluation suites and result comparison
|   +-- Trace inspector and feedback queue
|   +-- Deployments and governance settings
|   +-- Wallet, usage, invoices, payments, budgets, and provider price explorer
|
+-- Infrastructure
    +-- PostgreSQL + pgvector
    +-- Object storage for datasets, models, checkpoints, and artifacts
    +-- Redis
    +-- RabbitMQ
    +-- Docker Compose first
    +-- Kubernetes later
```

## Detailed Build Area: MVP Foundation

Target: weeks 1-4.

### Agent Runtime

Build the first runtime in .NET because it fits enterprise deployment, high-throughput orchestration, and existing Meridian-style patterns.

Core components:

- Durable run state machine: queued, running, waiting_for_tool, waiting_for_approval, completed, failed, cancelled.
- Agent definition loader: JSON or YAML config with versioning and schema validation.
- Tool registry: built-in tools first, MCP-compatible concepts from the beginning.
- Memory manager: session context, retrieved context, persistent references, and trace-linked memory decisions.
- Model router: one provider first, gateway contract designed for provider routing and fallback later.
- Run tracing: every model call, retrieval, tool call, approval, guardrail decision, latency, token count, cost estimate, and error.
- Replay/debug support: rerun with same inputs and config version where possible.

First milestone:

Run one versioned agent with one tool, one model, and one trace from a local config file.

### Model Gateway And Model Catalog

The model gateway should support both configured execution and informed choice.

Build first:

- OpenAI-compatible adapter with configurable base URL, API key reference, model ID, timeout, streaming, and capability flags.
- Hugging Face adapter for model discovery, Inference Providers, hosted endpoints, embeddings, image/audio tasks, and future AutoTrain integration.
- Ollama adapter for local model discovery, local chat, local embeddings where supported, and private/offline development.
- Capability matrix fields: chat, responses, tool calling, structured output, embeddings, reranking, vision, audio, image generation, fine-tuning, batch, streaming, context length, cost, latency, license, data policy, deployment type, and required hardware.
- Side-by-side model playground: same prompt, multiple models, compare answer quality, latency, tokens, cost, trace, and eval score.
- Normalized model catalog fields: `provider`, `model_id`, `route_type`, `route_provider`, `region`, `capabilities`, `context_window`, `billing_owner`, `key_mode`, `availability`, `data_policy`, `price_source_url`, `price_version_hash`, `last_checked_at`, and `confidence`.
- Billing modes per route:
  - `BYOK`: customer supplies the provider key; AgentPort meters usage and may charge only platform fees.
  - `Platform Wallet`: AgentPort owns the provider key; prepaid credits are debited for provider cost plus AgentPort fee and optional owner markup.
  - `Hybrid`: enterprise routes use BYOK while public routers/open-model hosts use the AgentPort wallet.
- Price display per request: provider input/output/cache/tool/media cost, AgentPort per-request or percentage fee, optional model-owner markup, taxes if applicable, estimated total, and confidence level.
- Provider coverage seed list: OpenAI, Azure OpenAI, Anthropic Claude, Google Gemini API, Google Vertex AI, xAI Grok, Z.ai GLM, Moonshot Kimi, Baidu Qianfan/ERNIE, Mistral, Cohere, DeepSeek, Groq, Together AI, Fireworks, Replicate, Hugging Face Inference Providers, OpenRouter, Perplexity, AWS Bedrock, Ollama, vLLM, and Hugging Face TGI.
- Provider price sync service that pulls official APIs or docs where available, stores historical price snapshots, and requires manual approval before large price deltas affect billing.

Build later:

- vLLM and TGI managed/self-hosted serving adapters.
- Provider-specific first-party adapters beyond the initial gateway: Anthropic, Gemini, Mistral, Cohere, DeepSeek, xAI, Z.ai, Kimi, Perplexity, Groq, Together, Fireworks, Replicate, cloud brokers, and local OpenAI-compatible servers.
- Automatic routing by quality, cost, latency, privacy, and eval history.
- Model recommendation engine based on task type and constraints.
- Managed-route product where AgentPort can route to cheaper or higher-availability providers while transparently showing billing policy and data-policy implications.

Default rule:

- Use the cloud frontier provider for the first production-quality demo.
- Use Ollama for local/private development and offline demos.
- Use Hugging Face for model discovery, open models, datasets, and broad AI task coverage.
- Use vLLM or TGI for production open-weight serving when the user has GPU infrastructure.
- Use OpenRouter and Hugging Face Inference Providers as broad catalog accelerators, not as the only route to every model.
- Use Azure OpenAI, Vertex AI, AWS Bedrock, and similar cloud brokers for enterprise governance, private networking, region controls, and committed-spend customers.

### MCP And Tool Governance Layer

MCP should become a first-class integration surface, but the first implementation must be governed.

Build first:

- Tool registry abstraction compatible with MCP concepts.
- One local document/filesystem tool with strict path controls.
- Tool schemas with typed inputs, output schema, validation, and timeout.
- Secret references instead of raw secrets in configs.
- Per-tool policy: allowed agents, required approval, rate limit, max runtime, and audit level.
- Test-in-panel tool invocation with trace output.

Build later:

- Postgres query tool.
- API connector tool.
- MCP host support for approved servers only.
- Signed or approved tool packages.
- OAuth 2.1 / MCP authorization support with protected resource metadata, resource-bound tokens, PKCE, exact redirect URI validation, short-lived tokens, and token audience validation.
- No token passthrough between MCP servers or third-party APIs. The platform should issue and validate scoped tokens for the intended resource only.

The long-term moat is a registry of reusable MCP tools and adapters, but security and audit must be part of the registry contract.

### Identity, API Access, And Trust Boundaries

Do not postpone identity completely. v0.1 can be single-workspace, but the contracts should already know who acted, which key was used, which project/environment was affected, and which policy allowed it.

Build first:

- Local user and service-account records.
- Project membership with roles: owner, admin, developer, evaluator, billing admin, viewer.
- API keys with scopes, project/environment binding, expiration, last-used timestamp, rate limits, budget limits, and rotation/revocation.
- Environment labels: `development`, `staging`, `production`, and custom labels.
- Secret references with provider, scope, owner, created/rotated timestamps, and redaction policy.
- Audit events for login, key creation, provider key changes, deployment changes, model promotion, payment config changes, policy overrides, and tool approvals.
- Idempotency keys for external API calls, payment callbacks, wallet changes, training job creation, and deployment promotions.

Build later:

- SSO/SAML/OIDC.
- SCIM provisioning.
- Organization-level RBAC/ABAC.
- Customer-managed keys.
- Per-tenant data residency and private networking.

Hard rule:

- No deployed endpoint, widget, SDK call, training job, payment mutation, or tool invocation should execute without an identity, scope, environment, trace id, and policy decision record.

### Memory And RAG System

Use a layered memory model:

- Short-term: Redis for session context and fast expiration.
- Mid-term: PostgreSQL + pgvector for conversation history, chunks, embeddings, and semantic retrieval.
- Long-term: graph memory later with Neo4j or Zep Graphiti.

v0.1 should use PostgreSQL + pgvector before adding graph complexity.

The user should be able to choose a context strategy:

- **Basic RAG** for normal document QA.
- **Hybrid RAG** when exact terms, product codes, names, legal clauses, or multilingual keyword matching matter.
- **Reranked RAG** when answer quality matters more than lowest latency.
- **Agentic RAG** when the system must ask several retrieval questions or use tools before answering.
- **CAG / prompt-cache strategy** when a small, stable knowledge pack fits in context and retrieval overhead is unnecessary.
- **GraphRAG / KAG** when domain entities, relationships, citations, and professional reasoning matter.
- **Fine-tuning or classification** when the problem is behavior, style, routing, or structured prediction rather than fresh knowledge lookup.

Retrieval flow:

```text
User question
-> create trace
-> embed query
-> vector search in pgvector
-> optional rerank
-> select chunks with source metadata
-> inject context with untrusted-content boundaries
-> model response
-> cite sources
-> score basic eval checks when configured
-> trace all steps
```

### Dataset And Knowledge Registry

Do not treat uploads as one-off files. The web panel should create durable, versioned data assets:

- Document datasets for RAG.
- Eval datasets from manual examples, saved playground runs, traces, production feedback, and synthetic generation later.
- CSV/JSONL datasets for non-LLM model training and provider fine-tuning later.
- Dataset versions with source, owner, schema, split, license/source notes, PII flag, language, domain, quality notes, and intended use.
- Ingestion status, failed-file diagnostics, chunk browser, search test, citation preview, reindex action, and delete/quarantine action.

### Data Governance, Privacy, And Dataset Quality

Data governance is part of the product, not just enterprise paperwork. The platform should prevent users from accidentally training on private, unlicensed, stale, or low-quality data.

Required data controls:

- Dataset cards for every dataset and knowledge base.
- PII and secret detection before ingestion, embedding, training, or export.
- License and usage-rights fields for uploaded, generated, scraped, purchased, and customer-owned data.
- Data retention policy per workspace, project, dataset, trace, and provider route.
- Delete, quarantine, reindex, and rebuild-embeddings workflows.
- Source permission and ACL metadata for knowledge-base documents. Future connectors must preserve source access controls where possible.
- Data residency and provider data-use posture on every model route.
- Training consent flag: whether data may be used for provider fine-tuning, internal fine-tuning, synthetic data generation, evals, or only retrieval.
- Golden dataset flag: trusted eval/training examples protected from casual edits.
- Dataset quality checks: duplicates, empty rows, schema drift, label imbalance, missing citations, language mismatch, and unsafe content.
- Data export policy: chunks, embeddings, traces, evals, model artifacts, and invoices each need separate export/delete rules.

Provider-boundary rule:

- Before a run, the platform should know whether the selected provider route may receive the selected prompt, files, retrieved chunks, tool outputs, and user metadata. If not, the AI Stack Chooser should recommend a safer route such as BYOK, private cloud, local/Ollama, vLLM/TGI, or no external model call.

### Observability And Evaluation In v0.1

Trace every run and make evals first-class early.

Required trace fields:

- Input.
- Identity, API key or service account, project, environment, policy decision id, and request id.
- Agent, prompt, model route, provider account, tool, retriever, and deployment versions.
- Retrieved context and source IDs.
- Model calls.
- Tool calls and approvals.
- Guardrail decisions.
- Output.
- Latency.
- Token usage.
- Estimated cost, price snapshot id, provider cost, AgentPort fee, and billing confidence.
- Error state.
- Human feedback.
- Redaction status for sensitive fields.
- Provider status, fallback route, retry count, and idempotency key where applicable.

v0.1 eval runner:

- Dataset-backed eval runs from the web panel.
- Exact match, contains, regex, JSON/schema validation, citation-present checks, and custom threshold checks.
- RAG-focused debug view: query, retrieved chunks, ranks, scores, final answer, citations, and evaluator notes.
- Baseline comparison against the previous agent version.
- Failed-case drilldown linked to traces.
- Evaluation thresholds as release gates, not just charts.
- Human review labels and reviewer identity for golden examples.
- LLM-as-judge rubric support later, with calibration against human-labeled examples before using it as a release blocker.

Use OpenTelemetry concepts, but store early MVP traces in PostgreSQL to keep the system simple. Keep the trace shape exportable to OpenTelemetry, MLflow, LangSmith, or Azure AI Foundry later.

## Detailed Build Area: Training, Testing, And Model Lifecycle

Target: starts in Phase 2, weeks 5-8, and expands after MVP gates pass.

AgentPort should treat datasets, evals, experiments, and model versions as first-class product primitives rather than optional observability metadata.

### Web Panel Training Lab

The training lab should support:

- Training job wizard: dataset, target column or output field, split, base model, method, hyperparameters, budget estimate, and runtime target.
- Job lifecycle: queued, validating_data, running, checkpointing, evaluating, completed, failed, cancelled.
- Logs, metrics, artifacts, checkpoint list, failure reason, retry action, and cancel action.
- CPU/GPU resource request, estimated cost, and max runtime.
- Reproducibility fields: dataset version, code/runtime version, seed, config, base model, package versions, and environment.

### Non-LLM Model Training

Start with useful, low-cost model types before broad AutoML:

- Tabular classification and regression using scikit-learn or LightGBM.
- Text classifiers for routing, moderation, triage, and intent classification.
- Rerankers and embedding models for RAG quality.
- Extractors for structured document fields.
- Document classifiers for OCR/document pipelines.
- Time-series and forecasting later.

All non-LLM training should reuse the same dataset registry, experiment tracking, model registry, eval suites, and deployment gates.

### Fine-Tuning And Distillation

Provider fine-tuning should come before local GPU-heavy fine-tuning where possible:

- Fine-tuning job creation with dataset validation, holdout split, base model, method, hyperparameters, suffix/name, budget estimate, and provider compatibility checks.
- Eval-before/after comparison against base model and previous production version.
- Distillation workflow: collect teacher outputs, build reviewed dataset, train a smaller student model, compare against teacher and base, then promote only if gates pass.
- Require an eval baseline before starting fine-tuning.

### LoRA And QLoRA Training

Add adapter training after the registry, eval, and job systems are stable:

- Adapter jobs for open-source models using PEFT/TRL/Axolotl/Unsloth-style backends.
- Track base model, adapter config, quantization, GPU requirements, checkpoints, merged artifacts, and serving compatibility.
- Registry support for adapters separately from full models.
- Export/merge options with clear storage and cost implications.

### Experiment Tracking

Experiment records should link:

- Dataset version.
- Prompt version.
- Model or base model version.
- Retriever and chunking config.
- Tool and agent config.
- Runtime version.
- Metrics and artifacts.
- Cost, latency, trace IDs, and failure examples.

The web UI should compare experiments side by side and promote successful outputs into model registry candidates.

### Model Registry And Release Gates

The model registry should handle:

- Provider models.
- Fine-tuned models.
- LoRA adapters.
- Embedding models.
- Rerankers.
- Classifiers and regressors.
- Prompt versions.
- Agent packages.

Use aliases such as `candidate`, `staging`, `production`, and `rollback`.

Deployment gates:

- Required eval suites.
- Minimum quality score.
- No critical safety failures.
- Max hallucination or ungrounded-answer rate.
- Max p95 latency.
- Max cost per run.
- Regression threshold versus production.
- Required human approval for high-risk systems.
- Rollback target exists.

### Commercial Model, Wallet, And Payments

AgentPort should treat billing as part of the runtime contract, not as an afterthought. Users must be able to select a model route, choose RAG/fine-tuning/no-training/other methods, see the cost impact, fund a wallet, and run agents through AgentPort-hosted endpoints without downloading provider or custom model artifacts.

Commercial principles:

- Use prepaid wallet credits plus optional subscriptions.
- Store customer-facing balances in minor currency units where possible, but store raw provider cost and internal calculations in high-precision decimal units because token pricing can create fractional cents.
- Apply explicit rounding rules only at reservation, display, invoice, settlement, and refund boundaries.
- Use immutable append-only wallet transactions with idempotency keys. Do not mutate historical usage rows.
- Reserve estimated maximum cost before expensive requests or training jobs start.
- Finalize actual cost from raw usage and a stored price snapshot, then release unused reservation.
- Show `provider_cost`, `agentport_fee`, `model_owner_markup`, `tax`, `discount`, and `total` separately.
- Keep BYOK and platform-wallet billing separate. BYOK customers may pay only platform/orchestration fees, while AgentPort-wallet customers pay provider cost plus AgentPort fees.
- Treat created models as hosted endpoint products by default. They can be used from AgentPort with versioned pricing, but users cannot download artifacts unless an enterprise/self-hosted contract explicitly permits export.

Pricing formula:

```text
total_debit =
  provider_input_cost
  + provider_output_cost
  + cached_token_cost
  + tool_or_media_cost
  + training_or_hosting_cost
  + model_owner_markup
  + agentport_fee
  + tax
  - discount
```

Example:

```text
wallet_start = 10.00 USD
provider_cost = 0.032 USD
agentport_fee = 0.10 USD
model_owner_markup = 0.00 USD
tax = 0.00 USD for estimate
final_debit = 0.132 USD
wallet_end = 9.868 USD
```

Wallet and usage entities:

| Entity | Required Fields |
| --- | --- |
| `wallet_account` | owner type, owner id, currency, available balance, reserved balance, status |
| `wallet_transaction` | idempotency key, high-precision amount, display amount, currency, rounding mode, direction, credit type, source object, tax treatment, created at |
| `wallet_reservation` | estimated max amount, rounding rule, source request/job, expiry, finalized amount, released amount, status |
| `usage_event` | project, endpoint, model route, raw billable dimensions, trace id, status, confidence |
| `provider_cost_event` | provider usage id, input/output/cache tokens, image/audio units, tool calls, GPU seconds, provider amount |
| `price_snapshot` | provider, model route, region, unit prices, AgentPort fee rule, owner markup rule, source URL, effective time |
| `invoice_line` | period, customer, usage type, project/model tags, subtotal, tax, credits applied, final amount |
| `refund_event` | original transaction, refund amount, reason, provider refund id, reconciliation status |

Billable dimensions:

- Text/chat: input tokens, cached input tokens, output tokens, reasoning tokens where exposed, request count.
- Embeddings: tokens, characters, or records depending on provider.
- Reranking/classification: documents ranked, tokens, requests, or provider-specific units.
- Image generation/editing: model, input/output image units, size, quality, and count.
- Audio/STT/TTS/realtime: text tokens, audio tokens, duration, or streaming session time.
- Tools: web search calls, browser actions, code/container runtime, external API calls, MCP tool calls.
- Batch jobs: provider-discounted async processing where supported.
- Training/fine-tuning: training tokens, examples, epochs, eval tokens, GPU/runtime seconds, checkpoints, and artifact storage.
- Hosted deployments: endpoint uptime, reserved CPU/GPU/RAM, storage, bandwidth, cold starts, and request count.

Budgets and safety:

- Block a run when `available_balance - reserved_balance < estimated_max_cost`.
- Require budget confirmation for training, public endpoint exposure, and high-cost model routes.
- Support hard and soft budgets by organization, project, endpoint, model route, API key, user, and environment.
- Add alerts at 50%, 80%, 90%, and 100% of budget.
- Add auto-recharge rules with daily/monthly caps and admin approval thresholds.
- Add kill switches for organization, project, endpoint, model route, provider, API key, training queue, and payment method.
- Reconcile provider invoices/usage reports against AgentPort usage events and create transparent adjustment events for drift.

Training packages:

- Training should require wallet balance, training package credits, or subscription entitlement before a job starts.
- Package credits can be restricted to `training_job`, `training_eval`, and `training_artifact_storage`.
- The wizard should show min/expected/max estimate, confidence level, max runtime, cancellation policy, and refund/release policy.
- Failed jobs should release holds when no provider cost occurred; partially billed jobs should debit only actual unrecoverable provider cost plus the configured AgentPort policy.

Custom model endpoint monetization:

- A user-created fine-tuned model, LoRA adapter, classifier, reranker, extractor, or agent package can become a private or marketplace endpoint.
- Endpoint access is through AgentPort API keys, widgets, or channel adapters.
- Artifacts stay in controlled object storage with internal signed access only.
- Endpoint owner sets a versioned price policy: cost-only, cost-plus percentage, fixed per request, fixed per 1k tokens, or marketplace owner markup.
- Final price is `serving_provider_cost + owner_markup + AgentPort fee + tax`.
- Revenue share and payouts belong in the marketplace phase, but the model endpoint pricing contract should exist before public sales.

Payment provider abstraction:

```text
PaymentProviderAdapter
  createCheckoutSession
  createPaymentIntent
  confirmPayment
  capturePayment
  refundPayment
  voidPayment
  getPaymentStatus
  parseWebhook
  verifyWebhookSignature
  reconcileTransaction
  listInstallmentOptions
  createInvoice
  syncDisputeOrChargeback
```

Payment configuration should be env-driven:

```env
PAYMENTS_ENABLED=true
PAYMENTS_DEFAULT_PROVIDER=stripe
PAYMENTS_REGION_MODE=global,tr,mixed
PAYMENTS_ALLOWED_CURRENCIES=USD,EUR,TRY
PAYMENTS_DEFAULT_CURRENCY=USD

PAYMENTS_ENABLE_CARDS=true
PAYMENTS_ENABLE_3DS=true
PAYMENTS_REQUIRE_3DS_FOR_TR=true
PAYMENTS_ENABLE_INSTALLMENTS=true
PAYMENTS_ENABLE_REFUNDS=true
PAYMENTS_ENABLE_PARTIAL_REFUNDS=true
PAYMENTS_ENABLE_CHARGEBACK_SYNC=true
PAYMENTS_ENABLE_BANK_TRANSFER=true
PAYMENTS_ENABLE_WIRE=true
PAYMENTS_ENABLE_CRYPTO=false
PAYMENTS_ENABLE_WALLETS=true
PAYMENTS_ENABLE_TAX_INVOICE=true

STRIPE_ENABLED=true
PAYPAL_BRAINTREE_ENABLED=false
ADYEN_ENABLED=false
CHECKOUT_COM_ENABLED=false
PADDLE_ENABLED=false
LEMON_SQUEEZY_ENABLED=false
WISE_ENABLED=false

IYZICO_ENABLED=false
PAYTR_ENABLED=false
CRAFTGATE_ENABLED=false
PARAM_ENABLED=false
SIPAY_ENABLED=false
PAYNET_ENABLED=false
SHOPIER_ENABLED=false
PAPARA_ENABLED=false
PAYCELL_ENABLED=false
HEPSIPAY_ENABLED=false
BANK_VPOS_ENABLED=false
HAVALE_EFT_FAST_ENABLED=true
```

Global payment methods and providers:

| Provider / Method | What It Gives Users | Advantages | Disadvantages | Best When |
| --- | --- | --- | --- | --- |
| Stripe | Cards, wallets, bank transfers, usage billing, credits, subscriptions, tax, refunds, disputes, webhooks | Best default global developer experience | Not available or ideal in every country/business model | Global SaaS wallet top-ups and subscriptions |
| PayPal / Braintree | PayPal checkout, cards, wallets, subscriptions, disputes, webhooks | Familiar global buyer option | More integration variance than Stripe | Customers prefer PayPal or regions where PayPal converts better |
| Adyen | Enterprise PSP, acquiring, cards, local methods, risk, 3DS, installments in supported markets | Strong global enterprise coverage | More complex onboarding and integration | Large multi-region merchants |
| Checkout.com | Enterprise cards, wallets, local methods, 3DS, disputes | Strong global PSP alternative | Enterprise-oriented onboarding | High-volume card processing |
| Paddle | Merchant of Record for SaaS | Handles tax/VAT/compliance burden | Less control over merchant relationship and payout rules | Fast global SaaS sales without building tax ops |
| Lemon Squeezy | Merchant of Record for SaaS and digital products | Simpler MoR path and many currencies | Less customizable than direct PSP stack | Indie/SaaS global subscriptions |
| Wise | Multi-currency business transfers and payouts | Useful for treasury and cross-border transfers | Not a normal card checkout provider | B2B wire/transfer workflows and payouts |
| Bank transfer / wire | Offline or asynchronous payments | Works for enterprise procurement | Reconciliation delay and manual work | Large invoices, annual contracts, regulated buyers |
| Stablecoin / crypto | Optional digital-currency rail | Some global reach | Legal, tax, AML/KYC, refund, volatility concerns | Future optional, disabled by default |

Turkey payment methods and providers:

| Provider / Method | What It Gives Users | Advantages | Disadvantages | Best When |
| --- | --- | --- | --- | --- |
| iyzico | Turkey cards, 3DS/non-3DS, webhooks, refunds, installments, TROY/Visa/Mastercard support | Strong Turkey PSP default | Provider approval and local compliance required | First Turkey card integration |
| Craftgate | Payment orchestration over bank VPOS, e-money, BKM, 3DS, installments, refunds, settlement | Best if multiple Turkey providers/banks are needed | More configuration and routing complexity | Serious Turkey payment stack |
| PayTR | Turkey virtual POS/payment pages, 3D Secure, callback flow, installments | Common local PSP | Callback/reconciliation details need careful handling | Alternative Turkey PSP |
| Param | ParamPOS, cards, installments, cancel/refund, 3DS/non-3DS | Local provider coverage | Contract and API behavior must be validated | Turkey PSP alternative |
| Sipay | 3D and non-secure payment flows | Useful local option | Gate behind current availability and approval checks | Optional Turkey PSP |
| Paynet | Cards, 3DS, installments, saved cards, refunds/cancel | Local business payment option | Requires provider-specific integration | Turkey PSP alternative |
| Shopier | Lightweight payment links/shop-style payments | Quick simple payments | Less ideal for deep automated SaaS billing | Small merchants or simple payment links |
| Papara | Turkey wallet/APM route where merchant integration exists | Popular local wallet rail | May require aggregator/direct approval | TRY wallet payments |
| Paycell | Mobile wallet/payment method | Telecom ecosystem reach | Merchant API access needs validation | Optional wallet/mobile route |
| Hepsipay | Turkey wallet/payment ecosystem | Installment/status/refund support where available | Ecosystem-specific | Turkey wallet route |
| Bank VPOS | Direct bank virtual POS | Potentially better economics and direct bank relationship | High integration and reconciliation burden | Mature Turkey merchant operations |
| Havale/EFT/FAST | Manual/asynchronous bank transfer | Required for local B2B and conservative buyers | Manual reconciliation until bank import exists | Turkey bank transfer top-ups |
| BKM / TROY / Installments | Local schemes and installment engine | Important for Turkey conversion | BIN, bank, commission, and settlement complexity | Turkey card checkout optimization |

Payment rules:

- Webhooks are the source of truth, not the browser redirect.
- Store raw webhook payloads, verify signatures, make handlers idempotent, and re-query the provider for high-value events.
- Do not credit a wallet until backend confirmation and reconciliation rules pass.
- Do not handle raw card PANs in AgentPort. Prefer hosted checkout or provider-hosted fields to reduce PCI scope.
- Require TLS for live payment pages and webhook endpoints.
- Use idempotency keys on checkout creation, payment confirmation, refunds, wallet credits, wallet debits, and webhook processing.
- Store only non-sensitive card metadata such as brand, last four digits, expiry, provider token, and payment method id where allowed.
- Support full and partial refunds, disputes/chargebacks, tax corrections, and manual bank-transfer refund workflows.
- Store display currency, charge currency, settlement currency, accounting currency, and FX record separately.
- Support Merchant of Record mode through Paddle/Lemon Squeezy and merchant-owned mode through Stripe/Turkey PSPs with later e-invoice/e-archive integration.

### Cost Forecasting And Pricing Simulator

Users should understand likely spend before they deploy, train, or expose a public bot.

Forecasting inputs:

- Model route.
- Prompt/input token estimate.
- Max output token estimate.
- Expected requests per day/month.
- RAG retrieval count, embedding model, and reranker choice.
- Tool call frequency and external API costs.
- Training dataset size, epochs, provider route, GPU/runtime estimate, eval cost, and storage.
- Endpoint hosting mode, uptime, concurrency, and cold-start assumptions.
- Connector sync frequency, changed documents, embedding refresh, and storage growth.
- AgentPort fee mode: fixed per request, percentage, subscription, pass-through, or managed route.

Forecasting outputs:

- Min/expected/max monthly cost.
- Provider cost vs AgentPort fee vs owner markup vs tax.
- Cheapest compatible alternatives.
- Privacy-compatible alternatives.
- Warning when a route is low-cost but fails policy, data, or eval requirements.
- Budget recommendation and auto-recharge recommendation.
- Exportable estimate for procurement.

### Human Review And Feedback Operations

Human review should be a workflow, not a loose comment box.

Review queues:

- Bad answer reports.
- Low-confidence model outputs.
- Failed eval cases.
- Golden dataset candidate examples.
- Training labels and preference pairs.
- Tool approval requests.
- Unsafe output incidents.
- Marketplace submission reviews.
- Data deletion and quarantine reviews.

Review record fields:

- Reviewer identity.
- Source trace or dataset id.
- Label or decision.
- Reason.
- Severity.
- Linked policy/eval gate.
- Follow-up action.
- Whether the example can be reused for training, evals, or only debugging.

### Red-Team And Safety Evaluation

Add adversarial suites for:

- Jailbreak attempts.
- Direct and indirect prompt injection.
- Tool abuse and excessive agency.
- Data exfiltration.
- Unsafe content.
- Policy bypass.
- PII leakage.
- Vector store poisoning and malicious retrieved documents.
- Multi-turn attacks.

Safety dashboards should show category, severity, affected version, blocking status, and linked traces. Red-team results should become blocking gates for production systems.

### Online And Offline A/B Testing

- Offline A/B: compare prompt, model, retriever, and agent versions over frozen datasets before release.
- Online A/B: split live traffic between production candidates after safety gates pass.
- Monitor task success, user feedback, safety incidents, latency, cost, fallback rate, escalation rate, and support burden.
- Auto-stop experiments when safety or quality thresholds fail.

## Detailed Build Area: Platform Layer, Integrations, And Deployment

Target: starts in Phase 3, weeks 9-12, and expands into commercial SaaS.

### AI System Builder

Two interfaces should feed the same runtime:

- No-code builder in Next.js.
- Code SDKs for .NET, TypeScript, and Python.

Start with a structured form/editor before building a full visual workflow canvas.

Builder fields:

- Instructions and prompt variables.
- Model provider, model route, key mode, temperature, context limits, output format, and provider data policy.
- Tool selection, scopes, approval policy, and test calls.
- Knowledge base and retrieval config.
- Memory policy.
- Guardrails and safety checks.
- Eval suite selection.
- Budget limit, max request cost, and billing mode.
- Deployment target, environment, API key policy, and rollback target.

Later builder blocks:

- LLM node.
- Tool node.
- Condition node.
- Loop node.
- Retrieval node.
- Human approval node.
- Classifier/ranker node.
- Output formatter node.
- Evaluation gate node.

### Public API, SDKs, And Webhooks

AgentPort should be usable from existing websites, mobile apps, backends, internal tools, and CI/CD. The web panel should create and test systems, but the platform needs a stable integration surface.

API requirements:

- REST/OpenAPI first, with streaming responses for chat and agent runs.
- SDKs for TypeScript, Python, and .NET.
- Endpoint types: chat, run, batch, embedding, rerank, classify, extract, train, eval, deploy, trace, feedback, billing, and webhook management.
- API key scopes: project, environment, endpoint, model route, tool, dataset, billing-read, billing-write, admin.
- Rate limits by organization, project, endpoint, API key, user, provider route, and wallet budget.
- Idempotency for create-run, create-training-job, deploy, payment, wallet mutation, and webhook registration.
- Webhooks for run completed, run failed, tool approval requested, eval completed, deployment changed, wallet low, payment succeeded, payment failed, provider outage, and safety incident.
- Signed webhook delivery with replay protection and retry policy.
- Error contract with machine-readable code, human-readable message, trace id, policy decision id, and retry guidance.

Integration products:

- Embeddable web chat widget.
- Server-side SDK for existing websites/apps.
- Admin API for creating projects, routes, keys, budgets, and deployments.
- Webhook subscriptions for downstream systems.
- Import/export for agent configs, eval datasets, traces, invoices, and provider price snapshots.

### Docs And Developer Experience

AgentPort needs strong developer experience because users will integrate it into existing websites, apps, and internal systems.

Docs required by phase:

| Phase | Docs |
| --- | --- |
| v0.1 | Local setup, first bot, first document QA, model route config, Ollama/local mode, API key setup, widget embed, trace/eval walkthrough |
| v0.2 | Training dataset prep, provider fine-tuning, non-LLM training, eval gates, model registry, rollback |
| v0.3 | Connector setup, SDK quickstarts, webhook examples, template authoring, import troubleshooting |
| v0.4 | Wallet, billing, payment methods, invoices, refunds, provider pricing, cost forecasts |
| v0.5 | SSO, audit export, self-hosting, backup/restore, compliance matrix, incident response |

Developer tooling:

- OpenAPI spec generated from the API.
- TypeScript, Python, and .NET SDK examples.
- Postman/Bruno-style collection later.
- CLI later for login, project creation, deploy, logs, traces, evals, and backups.
- Example apps: website chatbot, document QA API, classifier endpoint, webhook receiver, and private connector.
- Error catalog with codes, likely causes, and remediation steps.

### Web Panel Map

The web panel should include:

- Onboarding: first workspace, first project, model route, provider key/BYOK choice, budget, first template, first dataset, first API key, first deploy, and quickstart checklist.
- AI Stack Chooser: guided selection for builder style, model provider, model route, key mode, RAG/fine-tuning/no-training path, local/cloud serving, vector/search store, training path, deployment target, and cost/privacy tradeoffs.
- Overview: system status, active version, model route, tools, knowledge, last deploy, recent runs, wallet burn, cost/latency summary, and budget status.
- Builder: structured forms plus YAML/JSON editor with validation.
- Model Catalog: OpenAI, Claude, Gemini, Grok, GLM, Kimi, Mistral, Cohere, DeepSeek, Groq, Together, Fireworks, Replicate, Hugging Face, OpenRouter, Perplexity, Azure, Vertex, Bedrock, Ollama, vLLM, TGI, local servers, embeddings, rerankers, classifiers, capability comparison, route policy, and live/estimated price.
- Templates: support bot, website chatbot, PDF RAG, sales assistant, classifier, extractor, tool-using agent, MCP starter, document processor, and custom organization templates.
- Connectors: file upload, URL crawl, Google Drive, Notion, Slack, Gmail, Outlook, SharePoint, GitHub, Postgres, MySQL, REST API, S3/Azure Blob, sync history, permissions, failures, and deletion behavior.
- Playground: streaming chat/test panel with visible tool calls, retrieved chunks, token/cost estimates, provider cost, AgentPort fee, final answer, citations, and save-as-test-case.
- Datasets: uploads, versions, schema, splits, dataset cards, PII/license/source metadata, and quality notes.
- Knowledge: ingestion jobs, RAG strategy, vector store choice, chunk browser, search test, citation preview, reindexing, and failed file diagnostics.
- Tools: registry, schema, auth/secret references, test invocation, rate limits, approval policy, and MCP/local/API type.
- Training: job wizard, queue, logs, metrics, artifacts, checkpoints, and model registration.
- Experiments: side-by-side comparison of prompts, models, retrievers, datasets, and configs.
- Evaluations: datasets, graders, thresholds, runs, score history, version comparison, and failed-case drilldown.
- Traces: timeline/tree of model calls, retrieval, tools, guardrails, approvals, errors, latency, tokens, cost, raw JSON, and export.
- Deployments: local endpoint, Docker package, environment variables, version history, rollback, health checks, logs, API key, and widget snippet.
- Feedback: thumbs up/down, comments, review queue, approval queue, and trace-linked labels.
- Wallet And Billing: prepaid balance, reserved balance, top-up, credit packages, training packages, invoices, usage export, refunds, payment methods, auto-recharge, and reconciliation status.
- Provider Prices: current and historical price snapshots, source URLs, confidence, pending price deltas, and billing route comparison.
- Cost Forecasting: forecast spend by route, prompt size, traffic, training plan, storage, connector sync, endpoint hosting, and month.
- Operator Console: failed jobs, stuck queues, ingestion failures, provider outages, price-sync failures, webhook failures, payment reconciliation issues, tenant spend, dangerous tools, abuse alerts, and backup status.
- Provider Status: route health, latency, error rate, price-sync status, outage notes, fallback eligibility, and disabled-route controls.
- Legal And Compliance: active legal document versions, provider terms mapping, DPA/privacy links, retention policy, data-processing flags, and compliance matrix.
- Governance: provider keys, model routing defaults, BYOK/platform-wallet policy, budgets, workspace secrets, audit log, retention policy, tax settings, payment provider flags, and OTel/export settings.

### Onboarding, Templates, And Imports

Onboarding should reduce time-to-first-working-agent. The first run should guide the user through a real deployment, not just empty settings pages.

Onboarding flow:

1. Create workspace or organization.
2. Create first project.
3. Choose build goal: chatbot, RAG assistant, tool agent, classifier, extractor, or custom AI platform endpoint.
4. Choose model route and key mode: BYOK, platform wallet, Ollama/local, or placeholder.
5. Set initial budget and max request cost.
6. Choose a template.
7. Add first dataset or connector.
8. Test in playground.
9. Save a test case.
10. Deploy local API endpoint or widget.

Template requirements:

- Templates must declare assumptions, required connectors, model capabilities, cost range, privacy posture, eval suite, and deployment target.
- Templates should be versioned and diffable.
- Organization templates should be private by default.
- Public templates require marketplace moderation later.

Initial templates:

- Website support chatbot.
- PDF/document QA assistant.
- Internal knowledge bot.
- Sales assistant.
- Email triage classifier.
- Support ticket classifier.
- Structured document extractor.
- Tool-using API agent.
- MCP starter agent.
- Product search RAG.

Import paths:

- YAML/JSON AgentPort config first.
- Flowise, Dify, Langflow, and OpenAI assistant/agent-style imports later.
- Imports should create an `ImportJob` with unmapped fields, warnings, unsupported nodes, required secrets, estimated cost, and migration notes.

### Connector Catalog

Connectors make RAG and automation useful beyond manual uploads.

Connector phases:

| Phase | Connectors | Notes |
| --- | --- | --- |
| v0.1 | File upload, URL crawl/manual URL import | Keep scope narrow and easy to test |
| v0.2 | Postgres, MySQL, REST API, S3/Azure Blob | Developer and enterprise data paths |
| v0.3 | Google Drive, Notion, Slack, Gmail, Outlook, SharePoint, GitHub | High-value SaaS knowledge sources |
| v0.4+ | CRM, helpdesk, ticketing, calendar, warehouse, custom connector SDK | Add after auth, ACL, sync, and deletion contracts are reliable |

Connector contract:

- Auth mode: API key, OAuth, service account, PAT, database credential, signed URL, or no auth.
- Secret reference only; no raw secrets in connector config.
- Sync mode: one-time, manual, scheduled, webhook-driven, or incremental cursor.
- Source ACL capture where possible.
- Deletion handling: tombstone, hard delete, quarantine, or retention-window delete.
- Rate limits, retries, cursor state, error diagnostics, and backfill controls.
- Connector-specific cost forecast for ingestion, embeddings, storage, and refresh frequency.

### Deployment

Build deployment in this order:

1. Local Docker Compose.
2. Local API endpoint per AI system version.
3. Embeddable web chat widget.
4. Generated Docker package per AI system.
5. GitHub Actions integration.
6. Azure-hosted managed deployment.
7. Kubernetes manifests.

AI system package format:

- Agent/model/workflow config.
- Dataset and model version references.
- Tool config references.
- Secret references.
- Environment variables.
- Runtime version.
- Eval gate status.
- Optional Docker image.

Deployment requirements:

- Environments: development, staging, production.
- Immutable deployment version with rollback target.
- Health checks for runtime, model route, retriever, database, queue, object storage, and payment/billing dependencies.
- Canary or percentage rollout later for production endpoints.
- API key and widget domain allowlists.
- Rate limits, concurrency limits, timeout, max tokens, max cost, and max tool calls per endpoint.
- Provider fallback policy: disabled by default for deterministic/high-risk systems; allowed only when data policy, cost, and eval gates are compatible.
- Cold-start and scale settings for hosted endpoints.
- Deployment logs linked to trace ids and audit events.
- Public endpoint abuse controls: CAPTCHA or signed sessions for widgets where needed, request throttling, and anomaly alerts.

### Channels

Start with web chat and API only.

Add adapters later:

- Slack.
- Discord.
- WhatsApp Business API.
- Email.
- Telegram.
- Voice.

Channel adapters should normalize inbound messages into one internal agent message contract and preserve source-channel metadata for traces and evals.

## Detailed Build Area: Enterprise, Governance, Operations, And Ecosystem

Target: Phase 4 for SaaS operations, Phase 5 for enterprise/self-hosted, then marketplace.

### Enterprise Control Plane

Enterprise readiness should include:

- SSO and SAML.
- Azure AD, Okta, Google Workspace.
- RBAC and ABAC.
- Audit logs separate from sampled traces.
- Tenant-aware data model from the beginning, even while v0.1 is single-workspace.
- Data residency controls.
- Per-tenant rate limits, budgets, and model/provider allowlists.
- Secret vault integration.
- Policy-as-code for tool execution, deployment gates, and model/provider access.
- SLA and uptime reporting.
- Private deployments.

### Security And Incident Response

Security controls:

- Store only secret references in configs.
- Use user identity and agent identity for tool authorization.
- Require approvals for destructive, external, or high-cost tool calls.
- Sandbox local tools and MCP servers by default.
- Validate tool inputs and outputs.
- Redact sensitive trace fields.
- Quarantine suspicious datasets and documents.
- Encrypt sensitive data at rest and in transit.
- Keep raw provider keys, payment secrets, webhook secrets, and signing keys out of logs and traces.
- Use least-privilege API keys, scoped service accounts, and rotation reminders.
- Apply CSP and hosted payment boundaries on checkout pages.
- Run dependency, container, and secret scans in CI.
- Threat-model trust boundaries before enabling public endpoints, MCP hosting, marketplace sales, or provider-wallet billing.

Kill switches:

- Disable tenant.
- Disable project.
- Disable AI system.
- Disable tool.
- Disable provider.
- Disable model route.
- Disable API key.
- Disable payment provider.
- Disable wallet spending.
- Revoke secret.
- Quarantine knowledge base.
- Freeze deployment.

AI-specific incidents:

- Prompt injection with tool abuse.
- Cross-tenant leakage.
- Runaway cost.
- Secret exposure.
- Poisoned dataset or retrieved document.
- Unsafe output.
- Compromised MCP server.
- Payment fraud or webhook replay.
- Provider outage or silent pricing drift.
- Unauthorized model artifact access.

### Operator Console, Provider Status, And Abuse Moderation

AgentPort needs an internal operator surface before it becomes a paid SaaS. Users will see AI failures as product failures, so operators need a single place to diagnose and act.

Operator console should show:

- Failed and stuck ingestion jobs.
- Failed and stuck training jobs.
- Queue depth and worker health.
- Provider route health, latency, error rate, outage notes, and disabled/fallback status.
- Price-sync failures and pending price-delta approvals.
- Payment webhooks, payment reconciliation, refunds, disputes, and chargebacks.
- Tenant/project spend spikes.
- Public endpoint abuse, prompt loops, API key leaks, card-testing attempts, and suspicious signup/payment behavior.
- Dangerous tools, high-risk tool calls, approval queues, and policy overrides.
- Backup status and last restore-test result.

Provider status should support:

- Manual route disablement.
- Automatic degraded status from error/latency thresholds.
- Fail-closed mode for high-risk systems.
- Policy-approved fallback for low-risk systems.
- Customer-facing incident notes later.

Abuse moderation should support:

- Endpoint-level rate limits and max-cost limits.
- Unsafe content flags and escalation queue.
- Public widget abuse detection.
- API key leak detection through spend anomalies and impossible traffic patterns.
- Card-testing protection on payment routes.
- Workspace, project, endpoint, route, and wallet kill switches.

### Legal, Compliance, And Business Terms

This section defines product requirements for legal review; it is not a substitute for legal advice.

Required legal/business documents before paid public launch:

- Terms of Service.
- Privacy Policy.
- Acceptable Use Policy.
- Data Processing Agreement.
- Refund Policy.
- Payment and prepaid credit terms.
- Service Level Terms later.
- Marketplace publisher agreement later.
- Revenue-share and payout terms later.
- Provider terms mapping that explains which upstream model/provider terms apply to each route.
- Data retention and deletion policy.
- Security and responsible disclosure policy.

Compliance surfaces:

- Customer location and tax identity collection for billing.
- GDPR/UK GDPR-style export/delete workflows where applicable.
- Turkish KVKK and KDV/e-invoice/e-archive planning where applicable.
- Audit export for enterprise.
- DPA and subprocessors list for cloud providers, payment providers, analytics, support, storage, and email.
- Provider data-use matrix linked from the Model Catalog.

### Model And Provider Compliance Matrix

Every provider route should show compliance and data-handling metadata before selection.

Matrix fields:

- Provider.
- Route type: first-party, cloud broker, model router, hosted open model, local/self-hosted, search API.
- Region and data residency.
- BYOK support.
- Platform-wallet support.
- Data retention period.
- Provider training/data-use policy.
- Zero-data-retention or enterprise privacy option.
- HIPAA/GDPR/SOC2-style posture where relevant.
- Availability: public, approval required, region limited, enterprise only, deprecated.
- Supported modalities and tool capabilities.
- Fine-tuning support and export/download rules.
- Pricing source and last checked timestamp.
- Contract owner: AgentPort, customer, cloud provider, or marketplace route.

### Backup, Restore, And Disaster Recovery

Backup and restore are core platform features because traces, model artifacts, dataset versions, wallet ledgers, and audit logs are business-critical.

Backup scope:

- PostgreSQL.
- Object storage.
- Model artifacts and checkpoints.
- Dataset originals, chunks, and derived embeddings where retention allows.
- Wallet ledger and invoices.
- Audit logs.
- Provider and payment config metadata, excluding raw secrets unless backed by a proper secret manager.
- Agent configs, prompt versions, eval suites, and deployment records.

Restore requirements:

- Restore into a clean local or staging environment.
- Verify wallet ledger totals after restore.
- Verify active deployments can be reconstructed.
- Verify model artifact references remain valid.
- Verify deleted/quarantined datasets stay deleted/quarantined.
- Run scheduled restore drills.

Disaster recovery later:

- RPO/RTO targets by customer tier.
- Offsite backup retention.
- Region-level failover for managed SaaS.
- Incident runbooks for database loss, object storage loss, queue corruption, provider outage, payment outage, and compromised key.

### Self-Hosted And Enterprise Packaging

Self-hosting should be a product, not a one-off Docker Compose dump.

Enterprise package should include:

- License key or entitlement check.
- Offline mode for air-gapped installs.
- Private model route support: Ollama, vLLM, TGI, Azure private networking, Bedrock/Vertex enterprise routes.
- Private container registry support.
- Helm charts or Kubernetes manifests later.
- Upgrade and rollback path.
- Migration runner with dry-run and backup gate.
- Environment health check.
- Admin bootstrap flow.
- Customer-managed secrets and later customer-managed encryption keys.
- Configurable telemetry export or telemetry-off mode.
- Enterprise support bundle export with redaction.

### Version Migration Strategy

AgentPort will version many contracts. Old deployed systems must keep working while new schemas ship.

Versioned contracts:

- Agent definition schema.
- Prompt template schema.
- Tool schema.
- MCP/tool authorization schema.
- Model route schema.
- Provider price snapshot schema.
- Dataset card schema.
- Knowledge chunk schema.
- Eval suite schema.
- Trace schema.
- Deployment package schema.
- Wallet/usage event schema.
- Webhook payload schema.

Migration rules:

- Store schema version on every versioned object.
- Provide read adapters for old versions.
- Provide explicit migration jobs with dry-run output.
- Never mutate historical traces, usage events, wallet transactions, or price snapshots in-place.
- Keep old deployments pinned to their original runtime/schema until explicitly promoted.
- Show migration warnings in the web panel before deployment promotion.

### Marketplace

Build only after core runtime, deployment, evals, and governance are useful.

Marketplace features:

- Agent templates.
- Dataset/eval templates.
- Tool connectors.
- Model and reranker recipes.
- Categories for support, research, coding, sales, document processing, and internal operations.
- Ratings and reviews.
- Organization-private marketplace.
- Revenue share later.

Marketplace moderation:

- Start with private organization marketplace.
- Public marketplace requires review workflow.
- Scan submitted tools/packages for malware, secrets, dangerous permissions, and unexpected network/file access.
- Require license metadata for templates, datasets, eval packs, prompts, and model recipes.
- Require provider-term compatibility checks.
- Require dataset rights and consent confirmation.
- Require safety category and allowed-use declaration.
- Add takedown, dispute, version rollback, and publisher suspension flows.
- Prevent rating/review abuse.
- Separate verified publishers from community listings.

### Extension System

Developer extension points:

- Custom tool SDK.
- Custom model adapter SDK.
- Custom training backend SDK.
- Custom evaluator SDK.
- Custom channel adapter SDK.
- Middleware hooks.
- Pre-run and post-run hooks.
- Policy hooks.

Preferred SDK targets:

- C#.
- TypeScript.
- Python.

## Detailed Build Area: Differentiation

Target: Phase 7, after the platform can build, test, train, deploy, bill, and operate real AI systems.

### Cost Optimization Engine

Route work by complexity, confidence, eval history, and policy:

```text
Simple query + high confidence -> SLM
Complex reasoning -> frontier model
Structured extraction -> specialized extractor
Classification/routing -> small classifier
Retrieval-heavy answer -> best retriever/reranker combo
Code generation -> code-specialized model
Sensitive enterprise task -> approved private model
```

This should be based on measured success, not static routing rules only.

### AI System Composer

Longer-term builder for mixed systems:

- Agent plus retriever plus classifier plus evaluator.
- Router that picks an agent, model, tool set, or non-LLM model.
- Batch workflows for document processing and data extraction.
- Human review loops for high-risk outputs.
- Simulation and replay before production release.

### Federated Agent Networks

Long-term B2B idea:

Agents from different organizations communicate through a controlled protocol for routine business workflows.

Example:

A supplier agent and manufacturer agent coordinate order status, stock checks, and routine purchasing without human handoff.

This requires identity, trust, audit, policy, and revocation before it is safe.

### Voice, Vision, And Multimodal Systems

Add multimodal capabilities after the core text/data platform is stable:

- Speech-to-text.
- Text-to-speech.
- Image understanding.
- Video analysis.
- Document OCR.
- Multimodal eval datasets.
- Real-time voice agent flows.

## Recommended Stack

### Core

| Layer | Choice | Reason |
| --- | --- | --- |
| Runtime | .NET + ASP.NET Core | Enterprise trust, performance, strong fit with Meridian patterns |
| Platform API | .NET + ASP.NET Core | Same operational model as runtime and clean control-plane boundary |
| AI services | Python + FastAPI | Good fit for embeddings, ingestion, evals, and ML tooling |
| Frontend | Next.js + TypeScript | Fast product iteration and strong UI ecosystem |
| Database | PostgreSQL + pgvector | Durable relational data plus vector retrieval |
| Object storage | MinIO locally, Azure Blob/S3 later | Required for datasets, model artifacts, checkpoints, and logs |
| Cache | Redis | Session state and fast context cache |
| Queue | RabbitMQ | Existing operational familiarity and good fit for jobs |
| Search | Qdrant later | Useful if pgvector becomes limiting |
| Graph | Neo4j or Zep Graphiti later | Relational memory after v0.1 |
| Connector sync | Worker jobs with cursor state | Connectors need retries, incremental sync, deletion handling, and diagnostics |
| Backup | PostgreSQL dump plus object storage backup first | MVP needs restore confidence before SaaS reliability work |

### AI Layer

| Component | Choice | Reason |
| --- | --- | --- |
| Model gateway | LiteLLM or custom OpenAI-compatible adapter | Provider routing without hard lock-in across OpenAI, Anthropic, Gemini, Grok, GLM, Kimi, Mistral, Cohere, DeepSeek, Azure, Vertex, Bedrock, OpenRouter, Hugging Face, Ollama, vLLM, TGI, and local servers |
| Model catalog | AgentPort provider-route registry with Hugging Face/OpenRouter/cloud metadata import | Users need to compare model capability, route, license, cost, privacy, latency, hardware, eval history, data policy, key mode, and deployment fit |
| Local model runtime | Ollama first | Best developer/private/offline path with simple local setup and OpenAI-compatible support |
| Production open-weight serving | vLLM or Hugging Face TGI | Better path for high-throughput open models than laptop local runtimes |
| Tool protocol | MCP-compatible, governed registry first | Strong integration story with security boundaries |
| Agent framework | Custom .NET runtime, LangGraph integration for complex flows later | Keep platform contract central while reusing mature graph patterns where useful |
| UI streaming | Vercel AI SDK patterns or custom stream protocol | Next.js chat UX needs streaming, tools, and trace correlation |
| Embeddings | BGE or Nomic first | Open, self-hostable, strong quality |
| Retrieval strategy | Basic RAG first; hybrid/rerank/CAG/KAG later | Users need a choice between speed, accuracy, reasoning depth, and operational complexity |
| RAG evals | Ragas-style metrics later | Useful for faithfulness, groundedness, and retrieval quality |
| LLM evals | Custom runner first, MLflow/OpenAI/LangSmith-compatible concepts | Datasets, graders, traces, and regression history are core |
| Red-team evals | PyRIT-style suites later | Useful for systematic adversarial testing |
| Fine-tuning | Provider APIs and Hugging Face AutoTrain first; Unsloth + Axolotl later | Start operationally simple, then add custom SLM economics |
| Non-LLM training | scikit-learn/LightGBM first | Useful CPU-friendly baseline for panel-based training |
| Model registry | MLflow-compatible concepts | Proven lifecycle model for versions, aliases, metadata, and lineage |

### Retrieval And Search Alternatives

| Component | Default | Alternatives | Reason |
| --- | --- | --- | --- |
| Local vector store | PostgreSQL + pgvector | Chroma, FAISS | pgvector keeps v0.1 simple; Chroma is developer-friendly; FAISS is fast embedded retrieval |
| Managed vector store | None in v0.1 | Pinecone, Weaviate Cloud, Zilliz/Milvus, Qdrant Cloud | Users can choose low-ops scale when production traffic grows |
| Hybrid search | Add after basic RAG | Meilisearch, Elasticsearch/OpenSearch, Vespa | Hybrid search matters for exact terms, product catalogs, legal clauses, and support docs |
| Managed RAG engine | Optional later | Ragie | Useful for teams that want connectors, parsing, indexing, and retrieval without pipeline operations |
| Knowledge graph | Later | GraphRAG, KAG, Neo4j, Graphiti | Useful for domain reasoning and relationship-heavy knowledge |
| Cache/context strategy | Later | CAG, prompt caching, semantic cache | Useful when data is stable and fits long-context/cached workflows |

### Commercial Stack

| Component | Default | Alternatives | Reason |
| --- | --- | --- | --- |
| Wallet ledger | AgentPort internal append-only ledger | Stripe billing credits for invoice-facing credits | Real-time AI calls need immediate holds/debits before provider invoices exist |
| Price catalog | AgentPort snapshots from official provider sources | Manual price approvals, router APIs, enterprise contract prices | Requests and invoices must use the price active at execution time |
| Global payments | Stripe first | PayPal/Braintree, Adyen, Checkout.com, Paddle, Lemon Squeezy, Wise, bank transfer | Stripe is fastest for global SaaS; alternatives cover enterprise, MoR, and regional needs |
| Turkey payments | iyzico or Craftgate first | PayTR, Param, Sipay, Paynet, Shopier, Papara, Paycell, Hepsipay, bank VPOS, Havale/EFT/FAST | Turkey needs local cards, 3DS, installments, TROY, TRY settlement, and bank-transfer paths |
| Billing modes | BYOK plus Platform Wallet | Hybrid and enterprise committed-spend | Users need both trust/control and one-wallet simplicity |
| Platform fee | Configurable fixed per request plus optional percentage | Subscription-only, pass-through, managed-route spread | The UI must show provider cost and AgentPort fee separately |
| Custom model sales | Non-downloadable AgentPort endpoint products | Enterprise export/self-hosting contracts later | Protects artifacts while enabling priced usage from the platform |
| Tax mode | Merchant-owned records first; MoR optional | Paddle/Lemon Squeezy MoR, later Turkish e-invoice/e-archive | Global and Turkey tax treatment differ and must be configurable |

### DevOps

| Component | Choice | Reason |
| --- | --- | --- |
| Containers | Docker + Docker Compose | Best first local runtime |
| CI/CD | GitHub Actions | Standard developer workflow |
| Monitoring | OpenTelemetry concepts + local dashboard | Trace-first agent debugging and future export |
| Hosting | Azure first | Existing cloud familiarity |
| Secrets | Local `.env` references first, Key Vault later | Keep raw secrets out of agent configs |
| Auth | Local auth and API keys first, OIDC/SAML later | v0.1 needs identity and scoped API access without full enterprise SSO |
| Policy engine | In-code policy evaluator first, OPA-style policy later | Start simple while preserving a policy decision record |
| Webhooks | Signed internal webhook dispatcher first | Existing apps need integration events and replay-safe delivery |
| Operator console | Internal admin panel first | Paid AI platforms need provider, payment, queue, abuse, and backup visibility |
| Docs | Markdown docs site first | Quickstarts and integration examples are part of the product |
| Orchestration | Kubernetes later | Add when managed hosting needs it |

## v0.1 Milestones

1. Repository skeleton and contracts.
2. Docker Compose with PostgreSQL, pgvector, Redis, RabbitMQ, and object storage.
3. Core schemas: organization, user, service account, membership, environment, API key, project, AI system, agent definition, dataset, dataset card, knowledge base, data source connector, run, trace, eval suite, eval run, training job, model version, deployment, model route, usage event, price snapshot, wallet account, wallet transaction, payment provider, invoice, tax record, policy, secret reference, and audit event.
4. Platform API CRUD for projects, agents, datasets, knowledge bases, runs, traces, evals, API keys, policies, price snapshots, and wallet read models.
5. Python ingestion service for PDFs and text files.
6. Embedding path and pgvector retrieval.
7. Model gateway adapter for one cloud provider plus OpenAI-compatible base URL support.
8. Ollama local-provider adapter for developer/private mode.
9. Hugging Face model metadata and inference-provider adapter stub.
10. AI Stack Chooser data model and first UI matrix including model-route, RAG/training method, billing mode, and estimated cost.
11. Runtime execution path with one tool, scoped identity, policy decision, cost estimate, and full trace emission.
12. Web panel with AI Stack Chooser, Overview, Builder, Model Catalog, Playground, Datasets, Knowledge, Evaluations, Traces, Deployments, Wallet, and Billing tabs.
13. Basic eval runner over saved examples.
14. Price snapshot and cost-estimate path for model calls, retrieval, tools, evals, and training placeholders.
15. Env-driven payment-provider config skeleton for Stripe, iyzico/Craftgate, and manual bank transfer without production capture.
16. Local endpoint and embeddable chat surface.
17. End-to-end document QA demo with trace, cost estimate, eval, and deployment.
18. First-run onboarding wizard that creates workspace, project, model route, API key, dataset, and first RAG agent.
19. One starter template for website/document support chatbot.
20. Operator basics for failed jobs, provider route health, recent errors, and backup status.
21. Local backup and restore smoke test.
22. MVP quickstart docs for local setup, first bot, API key, widget embed, trace, and eval.

## First Week Plan

1. Create repository skeleton.
2. Define the data contracts before implementing services.
3. Add Docker Compose with PostgreSQL, pgvector, Redis, RabbitMQ, and MinIO.
4. Add database migrations for organization/user/service account/membership/environment/API key primitives, projects, AI systems, datasets, dataset cards, runs, traces, eval suites, training jobs, model versions, deployments, model routes, usage events, price snapshots, wallet ledger, payment providers, invoices, tax records, policies, secret references, and audit events.
5. Add `.NET` platform/runtime service with minimal APIs.
6. Define `agent.json` schema for one document QA agent.
7. Add Python ingestion service for PDF/text-to-chunks.
8. Add one embedding path and store vectors in pgvector.
9. Add OpenAI-compatible model provider adapter through the gateway contract.
10. Add Ollama local adapter config path, even if it is optional when Ollama is not installed.
11. Add model/retrieval/provider capability tables for the AI Stack Chooser, including model route, billing mode, and estimated total cost.
12. Add payment-provider env config with all providers disabled except the chosen local/default placeholders.
13. Add local user/API key setup and a scoped API endpoint for running the document QA agent.
14. Add Next.js web panel shell with tabs for stack chooser, builder, model catalog, playground, datasets, evals, traces, deployments, wallet, and billing.
15. Store every run, step, policy decision, usage event, and price snapshot in PostgreSQL.
16. Add first-run onboarding checklist and one support-chatbot template.
17. Add local backup script and restore-smoke checklist.
18. Write MVP quickstart docs.
19. Verify end-to-end: choose stack, upload document, ask question with scoped identity, inspect trace and cost estimate, save test case, run basic eval.

## Acceptance Tests

### AI Stack Chooser

Given:

- A user wants to build a document QA assistant.
- The user has constraints for privacy, budget, latency, and expected data size.

When:

- The user opens the AI Stack Chooser.

Then:

- The platform shows multiple model-route options: OpenAI/Azure, Claude, Gemini/Vertex, Grok, GLM, Kimi, Mistral, Cohere, DeepSeek, OpenRouter, Hugging Face, Bedrock, and Ollama/local where configured.
- The platform shows at least three retrieval options: pgvector, managed vector DB, and managed RAG engine.
- Each option explains what it gives the user, advantages, disadvantages, privacy posture, cost/ops impact, billing mode, AgentPort fee, data policy, and best-fit use case.
- The selected stack is saved as versioned architecture metadata.

### Model Provider Comparison

Given:

- The same prompt and eval case.
- Two or more configured model providers.

When:

- The user runs a side-by-side model test.

Then:

- The platform records output, trace, latency, token usage, provider cost, AgentPort fee, total estimated cost, price snapshot, model capability flags, and eval score for each provider route.
- The user can promote one provider/model combination to `candidate`.

### MVP Onboarding

Given:

- A new local user opens AgentPort for the first time.

When:

- The user completes onboarding.

Then:

- The platform creates a workspace, project, API key, model route, default budget, dataset placeholder, and first RAG agent.
- The user lands in the playground with a clear next action.
- The onboarding checklist links to first bot docs, API docs, widget docs, and trace/eval walkthrough.

### Template Clone And Deploy

Given:

- The user selects the website/document support chatbot template.

When:

- The user clones the template into a project and adds one document.

Then:

- The platform creates versioned agent, knowledge, eval, deployment, and widget config records.
- The template assumptions, required model capabilities, expected cost range, and privacy posture are visible.
- The cloned template can be deployed without modifying the database manually.

### Connector Sync And Deletion

Given:

- A connector syncs documents from a source system.

When:

- A source document is updated or deleted.

Then:

- The connector records sync cursor, source ACL metadata, changed document state, and deletion behavior.
- The knowledge base updates or removes chunks and embeddings according to retention policy.
- The sync job exposes failures, retries, and diagnostics in the operator console.

### Backup And Restore

Given:

- A local deployment has projects, datasets, traces, wallet ledger rows, and one deployed endpoint.

When:

- A backup is restored into a clean environment.

Then:

- Database records, object storage references, wallet totals, audit events, and deployed endpoint metadata are consistent.
- Deleted or quarantined datasets remain deleted or quarantined.
- Restore verification produces a pass/fail record.

### Operator Console Alert

Given:

- A provider route fails and a payment webhook is retried.

When:

- An operator opens the console.

Then:

- The provider outage, webhook retry, affected projects, affected endpoints, and recommended actions are visible.
- The operator can disable the provider route or pause wallet spending without editing config files.

### Cost Forecast

Given:

- A user expects 20,000 monthly chatbot requests and selects a model route, retrieval strategy, and endpoint hosting mode.

When:

- The user opens cost forecasting.

Then:

- The platform shows min/expected/max monthly cost.
- The forecast separates provider cost, AgentPort fee, owner markup, taxes, storage, connector sync, and hosting assumptions.
- The platform suggests cheaper compatible alternatives and warns about alternatives that violate privacy or eval requirements.

### Wallet Request Billing

Given:

- A user has 10.00 USD in wallet credits.
- The selected OpenAI route has a stored price snapshot.
- AgentPort charges a 0.10 USD per-request fee.

When:

- The user runs a playground request estimated at 0.132 USD total.

Then:

- The platform reserves the estimated amount before execution.
- The trace shows provider cost, AgentPort fee, tax placeholder, total estimate, and confidence.
- The wallet ledger debits the actual finalized amount and releases unused reservation.
- The wallet balance becomes 9.868 USD before rounding/display rules.
- Every reservation, debit, and release has an immutable ledger row and idempotency key.

### Insufficient Balance Block

Given:

- A user has 0.05 USD available and no reserved balance.
- A selected model route has an estimated max request cost of 0.132 USD.

When:

- The user tries to run the request.

Then:

- The platform blocks the request before calling the provider.
- The UI explains the estimate and offers wallet top-up, cheaper model route, lower max output tokens, BYOK, or local/Ollama alternatives.

### Training Package Billing

Given:

- A user starts a provider fine-tuning or GPU training job.
- The estimated training cost is higher than the current wallet balance.

When:

- The training wizard reaches confirmation.

Then:

- The platform requires wallet credits, a training credit package, subscription entitlement, or admin approval.
- The job reserves the max approved amount before queuing.
- Failed jobs release unused holds or debit only actual unrecoverable provider cost according to policy.

### Payment Provider Toggle

Given:

- `STRIPE_ENABLED=true`.
- `IYZICO_ENABLED=false`.
- `CRAFTGATE_ENABLED=false`.
- `HAVALE_EFT_FAST_ENABLED=true`.

When:

- A user opens wallet top-up settings.

Then:

- The UI shows Stripe and manual bank transfer options.
- iyzico and Craftgate are hidden and cannot be called by API.
- Webhook handlers reject disabled providers.

### Turkey Payment Method

Given:

- `PAYMENTS_REGION_MODE=tr`.
- `IYZICO_ENABLED=true` or `CRAFTGATE_ENABLED=true`.
- `PAYMENTS_REQUIRE_3DS_FOR_TR=true`.

When:

- A Turkish customer buys TRY credits.

Then:

- The platform uses a Turkey PSP route with 3DS enabled.
- Installment options are shown only if the provider/BIN supports them.
- Wallet credit is granted only after backend confirmation or webhook reconciliation.

### Billing Precision And Rounding

Given:

- A provider reports a cost below one cent.
- AgentPort has a fixed request fee and a percentage markup.

When:

- The request completes and the invoice line is generated.

Then:

- The usage event stores high-precision raw provider cost.
- The wallet transaction stores the high-precision amount and the customer-facing rounded amount.
- The invoice line explains the rounding rule.
- Reconciliation can reproduce the total from the price snapshot and raw usage dimensions.

### API Key Scope And Budget

Given:

- An API key is scoped to one project, one environment, and one endpoint.
- The key has a daily budget and rate limit.

When:

- A caller uses the key from an existing website or app.

Then:

- Requests outside the scope are rejected.
- Over-budget or over-rate requests are blocked before provider calls.
- The trace records API key id, environment, rate-limit decision, budget decision, and policy decision id.

### Data Deletion And Provider Boundary

Given:

- A user uploads documents containing PII.
- The selected provider route is not approved for that data class.

When:

- The user tries to build a RAG assistant.

Then:

- The AI Stack Chooser warns that the route is incompatible.
- The platform suggests approved alternatives such as local/Ollama, BYOK, private cloud, vLLM/TGI, or redaction.
- If the user deletes the dataset, source files, chunks, embeddings, and derived eval examples are deleted or marked for deletion according to retention policy.

### Provider Outage Fallback

Given:

- A production endpoint has a primary provider route and an approved fallback route.

When:

- The primary route fails or exceeds latency/error thresholds.

Then:

- The platform uses fallback only if policy allows the fallback for the same data class, budget, eval gate, and output contract.
- The trace records provider status, fallback reason, route change, and cost difference.
- High-risk deployments with fallback disabled fail closed.

### Non-Downloadable Model Endpoint

Given:

- A user creates a fine-tuned model or LoRA adapter in AgentPort.
- The model is deployed as a private endpoint product.

When:

- Another user or API key invokes the endpoint.

Then:

- The caller receives only API output, not model files, checkpoints, or adapter artifacts.
- Artifact storage access is internal and signed.
- Usage is billed using the endpoint's versioned price policy.

### Payment Webhook Idempotency

Given:

- A payment provider sends the same successful payment webhook twice.

When:

- AgentPort processes both webhooks.

Then:

- The wallet is credited exactly once.
- The duplicate event is stored as received but ignored for ledger mutation.
- The payment transaction records provider event id, signature verification result, and reconciliation state.

### Document QA RAG

Given:

- A local PDF.
- A configured model provider.
- A running PostgreSQL + pgvector database.

When:

- A user uploads the PDF.
- A user asks a question about it in the web UI.

Then:

- The document is chunked and embedded.
- Relevant chunks are retrieved.
- The model answers using retrieved context.
- The response includes citations or source references.
- The run trace shows retrieval, model call, latency, tokens, cost estimate, and output.

### Dataset Management

Given:

- A CSV, JSONL, PDF, or text file.

When:

- A user uploads it from the web panel.

Then:

- The platform stores metadata, validates format, creates a dataset version, and shows ingestion status.
- The dataset has source, owner, schema or file type, PII flag, intended use, and split metadata where applicable.

### Evaluation

Given:

- An agent version.
- A saved eval dataset.
- Configured graders and thresholds.

When:

- A user runs evaluation from the web panel.

Then:

- The platform records per-example outputs, scores, failed cases, traces, latency, tokens, cost, and comparison against the previous version.
- Failed cases can be opened directly in the trace inspector.

### Non-LLM Training

Given:

- A CSV classification dataset.

When:

- A user selects a baseline training recipe and clicks Train.

Then:

- A training job is queued.
- Status and logs are visible in the panel.
- Metrics and artifacts are stored.
- A model version is created when the job succeeds.
- The model version can be evaluated and promoted only if gates pass.

### Deployment Promotion And Rollback

Given:

- A passing agent or model version.

When:

- A user promotes it from `candidate` to `production`.

Then:

- The active endpoint points to that version.
- The previous production version remains available as rollback.
- The deployment record links to eval gates, model/agent version, dataset versions, trace examples, and audit event.

### Tool Approval And Audit

Given:

- An agent configured with a tool that requires approval.

When:

- The model attempts to call the tool.

Then:

- The run pauses in `waiting_for_approval`.
- The approval request appears in the web panel with inputs, risk level, and trace context.
- Approval or rejection is recorded as an audit event.

### MCP Token Boundary

Given:

- An approved MCP tool requires OAuth authorization.

When:

- An agent attempts to call the MCP tool.

Then:

- The platform uses a token scoped to the intended MCP resource.
- Tokens are not accepted through query strings or forwarded to unrelated resources.
- Missing or invalid scopes produce a 401 or 403-style tool failure and a trace event.

## Risks

| Risk | Mitigation |
| --- | --- |
| Scope creep | Keep v0.1 to one RAG agent lifecycle, but design schemas for training/eval/model lifecycle early |
| MVP never ships | Enforce phase gates and do not start broad SaaS/marketplace work until MVP onboarding, document QA, trace, eval, deploy, and restore criteria pass |
| Poor onboarding | Add first-run wizard, starter template, quickstart docs, and a time-to-first-agent acceptance test |
| Choice overload | Use the AI Stack Chooser to recommend defaults, then expose advanced alternatives with clear tradeoffs |
| Adapter compatibility drift | Store capability flags per provider/model route and verify them with smoke tests and evals |
| Existing competitors | Differentiate with .NET enterprise runtime, MCP-compatible governed tools, self-hosting, training/eval lifecycle, and compliance |
| Fast AI ecosystem changes | Keep model, tool, dataset, eval, and channel abstractions swappable |
| Training cost | Start with CPU-friendly non-LLM training and provider fine-tuning before local GPU workflows |
| Weak identity boundary | Model users, service accounts, API keys, scopes, environments, and policy decisions from v0.1 |
| Provider price drift | Store price snapshots with source URL, timestamp, hash, confidence, and manual approval for large deltas |
| Incorrect cost calculation | Keep raw usage dimensions separate from calculated amounts, reconcile provider invoices, and correct only through ledger adjustment events |
| Fractional-cent provider pricing | Store high-precision billing amounts internally and round only at display, reservation, invoice, settlement, and refund boundaries |
| Wallet liability and stored-value risk | Treat credits as prepaid service credits, use legal/accounting review, clear terms, expiry policy, and refund rules |
| Runaway spend | Use reservations, hard budgets, per-request max cost, auto-stop rules, anomaly detection, and kill switches |
| PCI scope creep | Use hosted checkout or provider-hosted payment fields, do not process raw card PANs, and document PCI responsibilities |
| Payment fraud and card testing | Require 3DS/risk checks where appropriate, rate-limit checkout attempts, use provider fraud tools, and delay wallet credit until backend confirmation |
| Turkey payment complexity | Gate iyzico/Craftgate/PayTR/bank VPOS methods by env flags, require 3DS when configured, store installment/BIN/settlement data, and reconcile webhooks |
| Tax/KDV/VAT mistakes | Store seller/buyer tax data, tax mode, invoice records, credit notes, and support MoR providers where useful |
| Chargebacks and refunds | Normalize disputes, keep raw webhook payloads, link refunds to ledger rows, and preserve provider reconciliation status |
| Custom model artifact leakage | Keep created models non-downloadable by default, serve only through AgentPort endpoints, and use signed internal storage access |
| Eval metrics create false confidence | Keep per-case drilldown, human review, regression history, and production feedback loops |
| Dataset privacy | Add PII flags, source metadata, retention controls, secret references, and audit events early |
| Dataset/license misuse | Require dataset cards, license/consent fields, source ownership notes, and provider-boundary checks before training or export |
| Connector ACL leakage | Preserve source ACL metadata where possible, sync deletion state, and block retrieval when caller access is not authorized |
| Prompt injection and tool abuse | Use allowlisted tools, scoped parameters, approval gates, untrusted-content boundaries, and red-team suites |
| MCP confused-deputy/token leakage | Bind tokens to intended MCP resources, reject token passthrough, require scoped approvals, and audit every tool call |
| Local model quality variance | Treat Ollama/local models as selectable but eval-gated, not assumed equivalent to frontier models |
| Vector database lock-in | Keep retrieval adapters, exportable embeddings/chunks, and migration jobs |
| Long-running job failures | Add queue state, retries, cancellation, logs, checkpoints, and failure diagnostics |
| Model artifact sprawl | Use object storage lifecycle policy, registry aliases, retention rules, and cleanup jobs |
| Provider outage | Support explicit fail-closed or policy-approved fallback routes with traceable cost and policy differences |
| Backup restore failure | Run restore drills and verify database, object storage, wallet ledger, audit log, and deployment references after restore |
| Marketplace abuse | Require moderation, malware/secret scans, license checks, provider-term checks, takedown flow, and publisher controls before public listings |
| Legal launch risk | Prepare ToS, AUP, privacy policy, DPA, refund/payment terms, provider terms mapping, and legal review before paid public launch |
| Version migration breakage | Version schemas, keep read adapters, dry-run migrations, and pin old deployments until promoted |
| GPU availability | Treat GPU jobs as optional phase 2+ backends; keep initial training CPU/provider-based |
| No users | Dogfood internally and target one real enterprise workflow first |
| Time pressure | Ship thin vertical slices: builder -> playground -> trace -> eval -> deploy |

## Repo Structure Proposal

```text
AgentPort/
|-- PLAN.md
|-- src/
|   |-- runtime/
|   |-- platform-api/
|   |-- billing/
|   |-- payments/
|   |-- connectors/
|   |-- templates/
|   |-- operations/
|   |-- web/
|   |-- ai-services/
|   |-- workers/
|   |-- sdks/
|-- infra/
|   |-- docker-compose.yml
|   |-- postgres/
|   |-- minio/
|-- docs/
|   |-- architecture.md
|   |-- contracts/
|   |-- decisions/
|   |-- security/
|   |-- billing/
|   |-- compliance/
|   |-- api/
|   |-- runbooks/
|   |-- legal/
|-- examples/
|   |-- document-qa-agent/
|   |-- tabular-classifier/
|   |-- website-chatbot-integration/
|   |-- webhook-receiver/
|   |-- connector-template/
|-- tests/
    |-- acceptance/
```

## Decision Log

Initial decisions:

- Build the runtime and platform API around .NET and ASP.NET Core.
- Keep Python for AI service, ingestion, eval, and training boundaries.
- Use Next.js for the platform UI.
- Start with PostgreSQL + pgvector before adding Qdrant or graph memory.
- Add object storage early because datasets, models, checkpoints, and artifacts require it.
- Build web chat/API first; delay Slack, WhatsApp, Telegram, email, and marketplace.
- Make MCP-compatible tooling a first-class direction, but begin with a simple governed local tool contract.
- Treat evals, traces, datasets, and model versions as core product primitives, not optional analytics.
- Keep v0.1 narrow enough to ship, but make v0.2 training/test workflows a committed product milestone.
- Add an AI Stack Chooser so users can compare model, training, retrieval, vector store, and deployment alternatives before committing to a stack.
- Treat Hugging Face as a required ecosystem integration for models, datasets, inference providers, embeddings, AutoTrain, and open-model metadata.
- Treat Ollama as a required local/private model integration, but not as the default high-scale production serving layer.
- Treat vLLM and Hugging Face TGI as the preferred production open-weight serving paths.
- Keep pgvector as the default local RAG store, but make Pinecone, Chroma, FAISS, Meilisearch, Qdrant, Weaviate, Milvus, Ragie, Elasticsearch/OpenSearch, MongoDB Atlas, and Vespa selectable alternatives through adapters.
- Separate `model` from `model route` so every provider, cloud broker, router, hosted open-model route, or local runtime can carry its own price, region, key mode, data policy, and availability.
- Support cloud models across OpenAI, Azure OpenAI, Anthropic Claude, Google Gemini, Vertex AI, AWS Bedrock, xAI Grok, Z.ai GLM, Moonshot Kimi, Mistral, Cohere, DeepSeek, Groq, Together, Fireworks, Replicate, Hugging Face Inference Providers, OpenRouter, Perplexity, and region-specific providers where needed.
- Use prepaid wallet credits plus optional subscriptions as the commercial default. Every paid run should reserve estimated cost, debit actual cost, release unused hold, and show provider cost separately from AgentPort fees.
- Keep user-created models non-downloadable by default and expose them through AgentPort-hosted endpoints with versioned pricing.
- Build payment providers behind env-gated adapters. Stripe is the global default; iyzico or Craftgate are first Turkey candidates; PayTR, Param, Sipay, Paynet, Shopier, Papara, Paycell, Hepsipay, bank VPOS, and Havale/EFT/FAST remain selectable alternatives.
- Store internal billing math in high precision and round only at defined customer-facing boundaries.
- Model identity, API keys, service accounts, environments, scopes, and policy decisions in v0.1 so later SaaS, billing, and public API work does not require a rewrite.
- Require dataset cards, PII/secret detection, license/consent metadata, provider-boundary checks, and deletion workflows before training or broad connector ingestion.
- Use PCI-safe payment integration patterns: hosted checkout or provider-hosted fields first, no raw card PAN handling in AgentPort.
- Use phase gates: MVP document QA and restore must work before training breadth; training/evals before connectors/API breadth; integrations before paid SaaS; SaaS reliability before enterprise; enterprise governance before public marketplace.
- Treat onboarding, templates, docs, operator visibility, backup/restore, and cost forecasting as product features, not secondary cleanup.
- Build connector and template systems before public marketplace so marketplace assets use the same internal contracts.

## Research Basis

The expanded plan is based on current patterns from agent and AI lifecycle platforms:

- OpenAI Agents SDK and trace/eval docs emphasize tracing, graders, datasets, and trace-based improvement.
- LangSmith, MLflow, Flowise, Dify, and Azure AI Foundry all make evaluation, traces, datasets, or deployment lifecycle visible in the product surface.
- MLflow's registry model is a useful reference for model versions, aliases, metadata, lineage, and promotion.
- Hugging Face AutoTrain/TRL/PEFT-style workflows are useful references for later fine-tuning and LoRA/QLoRA support.
- OWASP LLM Top 10, MCP authorization guidance, and NIST AI RMF should shape security, governance, and enterprise controls.
- Stripe's integration security guidance reinforces that AgentPort should avoid raw card data, prefer low-risk hosted/payment-field integrations, verify webhook signatures, use TLS, and document PCI responsibility.
- MCP authorization guidance reinforces resource-bound OAuth, bearer tokens in authorization headers, token audience validation, PKCE, exact redirect URI validation, and avoiding token passthrough.
- NIST AI RMF reinforces that trustworthiness, governance, risk management, and evaluation should be designed into AI systems rather than added only after deployment.
- Prior AgentPort/tooling research points toward a trust layer around permissions, verification evidence, and secret blocking as a product differentiator.
- Teachable Machine is a useful reference for the simple no-code training path: gather examples, train, test, and export.
- Flowise, Dify, Langflow, and VectorShift are useful UX references for visual builders, templates, connectors, embedded chat, APIs, human-in-the-loop review, traces, and agent/RAG workflow deployment.
- Ragie is a useful reference for managed context engines that handle connectors, parsing, chunking, indexing, and agent-ready retrieval.
- Pinecone, Chroma, FAISS, and Meilisearch show that retrieval choice is not one-dimensional: users need managed scale, local developer UX, embedded search, hybrid search, metadata filtering, and operational tradeoffs.
- KAG, GraphRAG, CAG, and prompt caching should be options when plain chunk-based RAG is not the best fit.
- OpenAI, Anthropic, Google, Hugging Face, OpenRouter, and provider docs show pricing differs by input/output/cache tokens, modality, tools, batch mode, route, region, and provider account. AgentPort therefore needs price snapshots and provider-route records instead of one static model price.
- Stripe usage-based billing, Stripe billing credits, and Stripe Tax are useful customer-facing billing references, but AgentPort still needs an internal real-time wallet ledger because AI calls must be authorized before invoice finalization.
- Stripe, PayPal/Braintree, Adyen, Checkout.com, Paddle, Lemon Squeezy, and Wise cover global payment alternatives; iyzico, Craftgate, PayTR, Param, Sipay, Paynet, Shopier, Papara, Paycell, Hepsipay, bank VPOS, and Havale/EFT/FAST cover Turkey-specific payment alternatives.
