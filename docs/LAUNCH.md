# AgentPort Launch: DefterPort Wedge

**Strategy:** Narrow & Launch — one vertical, one workflow, one price band.

AgentPort ships as the self-hostable control plane. **DefterPort** is the first commercial wedge: on-prem document QA for Turkish accounting firms (SMMM offices) that need KVKK-compliant answers over tax law and client files.

## Wedge Summary

| Dimension | Choice |
|-----------|--------|
| **Product name** | DefterPort (powered by AgentPort) |
| **ICP** | SMMM firms, 3–15 staff, 50–500 active clients |
| **Buyer** | Office owner / lead müşavir |
| **Champion** | Senior accountant who answers the same GİB / KDV / stopaj questions daily |
| **Deployment** | On-prem or private VPC — data never leaves firm infrastructure |
| **Core promise** | Cited answers from Turkish tax law + firm document folders in under 10 seconds |
| **SKU** | AgentPort Govern (audit, API keys, trace retention, workspace isolation) |

## Ideal Customer Profile (ICP)

### Firm profile

- **Type:** Serbest Muhasebeci Mali Müşavir (SMMM) office or small mali müşavirlik bürosu
- **Size:** 3–15 employees, 1–3 partners
- **Client load:** 50–500 aktif mükellef
- **Tech posture:** Windows file server or NAS with client folders; Excel + Luca / Logo / Mikro; no in-house DevOps
- **Trigger events:**
  - New junior staff asking repeat questions about KDV, stopaj, SGK, e-Fatura
  - Client folder sprawl (`/Mükellefler/...`) with no searchable knowledge layer
  - KVKK pressure to avoid sending client PDFs to public SaaS chat tools
  - Partner wants "one brain" for mevzuat + office precedents

### Disqualifiers (not ICP for v1)

- Big-4 or 50+ staff firms needing multi-tenant SaaS
- Firms with no on-prem or private cloud option
- Teams wanting general-purpose ChatGPT replacement without document grounding
- Firms that need live GİB portal integration in week one

### Jobs to be done

1. **Mevzuat lookup:** "Bu işlemde KDV oranı ve istisna şartları nedir?" with article citations.
2. **Client context:** "X Ltd. için son beyanname notlarında stopaj nasıl işlendi?" from ingested folder docs.
3. **Onboarding:** Junior accountant gets cited answers instead of interrupting senior staff.
4. **Audit trail:** Partner reviews what was asked, what sources were retrieved, and what was answered.

## Positioning

```text
Public SaaS chat  →  client data leaves the office, KVKK risk
Generic RAG SaaS  →  no Turkish tax law pack, no SMMM workflow
DefterPort        →  on-prem RAG, Turkish tax doc pack, client folder ingestion, cited answers
AgentPort Govern  →  control plane: traces, API keys, workspace isolation, retention policy
```

**One-liner (TR):** *Mükellef dosyalarınız ve Türk vergi mevzuatı üzerinde, KVKK uyumlu, kaynak gösteren yapay zekâ asistanı — kendi sunucunuzda.*

**One-liner (EN):** *Cited, on-prem document QA for Turkish accounting firms — tax law pack plus client folder ingestion.*

## Pricing

All prices TRY, billed monthly. Annual prepay: 2 months free.

| Tier | Price | Includes |
|------|-------|----------|
| **DefterPort Starter** | ₺3,500 / mo | 1 workspace, 1 knowledge base, Turkish tax law seed pack, 3 users, 10 GB indexed docs, email support |
| **DefterPort Office** | ₺6,000 / mo | 3 workspaces (e.g. KDV / SGK / özel mükellef), 5 users, 50 GB, client folder watch path, priority setup |
| **AgentPort Govern add-on** | +₺1,500 / mo | Extended trace retention (90d), exportable audit log, API key scopes, domain allowlist for internal widget |

### What's included in setup (one-time, bundled in month 1)

- Docker Compose or single-node install on firm hardware
- Turkish tax law seed doc ingestion (`examples/defterport/seed-docs/`)
- One client folder ingestion path (Office tier: up to 3 paths)
- 2-hour remote onboarding with lead müşavir

### Expansion revenue (post-MVP)

- Extra indexed GB
- Additional workspaces / agents
- Hosted private VPC management (if firm has no IT)
- Custom mevzuat packs (sektör tebliğleri, iç yönetmelik)

## AgentPort Govern SKU

**Govern** is the compliance and operations layer sold with DefterPort. It maps to existing AgentPort primitives:

| Govern capability | AgentPort primitive |
|-------------------|---------------------|
| Workspace isolation | `Workspace` + scoped API keys |
| Who asked what | Chat `Trace` + retrieval log |
| Source citations | RAG `Citation` + `RetrievedChunk` |
| Document lineage | `Dataset` → `DocumentAsset` → ingestion job |
| Access control | API key scopes, future RBAC hooks |
| Retention policy | Trace / eval retention config per workspace |
| No training on client data | On-prem inference; no outbound fine-tuning |

Govern is not a separate codebase in MVP — it is a **packaged policy + retention + onboarding** bundle on top of the open AgentPort control plane.

## 2-Week MVP Cut

Goal: a paying SMMM pilot can ingest tax law + one client folder, ask Turkish questions, and receive cited answers on their own machine.

### Week 1 — Prove the path

| Day | Deliverable | Repo / surface |
|-----|-------------|----------------|
| 1–2 | DefterPort seed doc pack structure + sample ingestion | `examples/defterport/seed-docs/` |
| 2–3 | Bootstrap script: DefterPort workspace, agent, dataset | `scripts/` or `examples/defterport/bootstrap.json` |
| 3–4 | Turkish QA smoke: 10 fixed mevzuat questions with citation assertions | `scripts/smoke.sh` extension |
| 4–5 | Client folder ingestion docs (watch folder → dataset) | `docs/products/DEFTERPORT.md` |
| 5 | Internal demo on Docker Compose stack | `docker-compose.yml` + `scripts/dev-up.sh` |

### Week 2 — Pilot-ready

| Day | Deliverable | Repo / surface |
|-----|-------------|----------------|
| 6–7 | "No answer" behavior for out-of-corpus questions (already in AI services) | `src/ai-services/` |
| 7–8 | Operator quickstart page for DefterPort (TR) | `docs/products/DEFTERPORT.md` |
| 8–9 | Govern: trace export JSON + 90d retention documented | `docs/LAUNCH.md` + existing trace endpoints |
| 9–10 | Install runbook: single-node Linux, backup/restore | `infra/backups/`, `docs/quickstart.md` link |
| 10 | Pilot LOI template + pricing one-pager | sales collateral (out of repo) |
| 10–14 | First pilot install + feedback loop | — |

### Explicitly out of MVP

- GİB / e-Fatura live API integration
- Multi-firm SaaS hosting
- Fine-tuned Turkish legal LLM
- Mobile app
- Automatic OCR for scanned arşiv
- Billing / payment integration in product
- Full Turkish web UI localization

### MVP success criteria

1. Ingest ≥ 20 Turkish tax law seed documents and answer 8/10 benchmark questions with correct citations.
2. Ingest one client folder (≥ 50 PDF/DOCX files) and answer 5/5 folder-specific test questions.
3. End-to-end query latency p95 < 10s on pilot hardware (CPU inference + pgvector).
4. Zero document bytes sent to external APIs when `OLLAMA_*` or local route is configured.
5. Partner can open trace UI and see question, chunks, citations, and timestamp.

## Go-To-Market (first 30 days)

1. **List** 20 SMMM offices in network; prioritize firms already using NAS client folders.
2. **Offer** free 14-day pilot — Starter tier, on-prem install, tax law pack pre-loaded.
3. **Convert** pilot → ₺3,500/mo Starter or ₺6,000/mo Office; Govern add-on for firms with KVKK audit requests.
4. **Content** 3 LinkedIn posts (TR): KVKK + ChatGPT risk, "kaynak gösteren cevap", junior onboarding time saved.
5. **Proof** 1 case study: questions/week, time saved, sample cited answer screenshot (redacted).

## Technical baseline (existing stack)

DefterPort MVP reuses the current AgentPort path without new microservices:

```text
docker-compose.yml     PostgreSQL + pgvector, Redis, RabbitMQ, MinIO
src/platform-api/      Workspace, dataset, agent, chat, trace APIs
src/ai-services/       Ingest, embed, retrieve, cited extractive answers
src/web/               Operator dashboard (playground, traces, datasets)
examples/defterport/   Seed docs + bootstrap fixtures
```

Local bring-up:

```bash
scripts/dev-up.sh
RESET_JSON="$(scripts/dev-reset.sh)"
scripts/smoke.sh
```

See `docs/products/DEFTERPORT.md` for the full DefterPort product specification.

## Repository

Existing repo: [github.com/UmutKorkmaz/agent-port](https://github.com/UmutKorkmaz/agent-port)

Do not fork to a new product repo for MVP. DefterPort ships as `docs/`, `examples/defterport/`, and pilot scripts inside AgentPort.