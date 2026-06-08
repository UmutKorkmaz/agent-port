# DefterPort Product Specification

**Version:** 0.1 (MVP)  
**Powered by:** AgentPort control plane  
**Audience:** SMMM firms, mali müşavirlik büroları, KVKK-conscious professional services

## Product Summary

DefterPort is an on-prem document QA assistant for Turkish accounting firms. It answers staff questions using two grounded corpora:

1. **Turkish tax law doc pack** — seed mevzuat, tebliğler, and reference guides maintained in `examples/defterport/seed-docs/`.
2. **Client folder ingestion** — firm-specific files from mükellef directories (beyannameler, sözleşmeler, yazışmalar, iç notlar).

Every answer must include **citations** to retrieved chunks. If retrieval confidence is below threshold, the system returns a controlled **no-answer** response instead of hallucinating.

## Problem

SMMM offices store critical knowledge in:

- Shared drives (`/Mükellefler/{VKN|TCKN} - {Unvan}/...`)
- Email archives and PDF scans
- Partner head knowledge of mevzuat edge cases

Staff repeatedly ask the same questions about KDV, stopaj, SGK, e-Fatura, and beyanname deadlines. Public LLM tools create KVKK exposure. Generic RAG SaaS lacks Turkish tax law coverage and on-prem deployment.

## Solution

```text
┌─────────────────────────────────────────────────────────────┐
│  SMMM office (on-prem / private VPC)                        │
│                                                             │
│  ┌──────────────┐    ┌──────────────┐    ┌─────────────┐ │
│  │ Client       │    │ Turkish tax  │    │ DefterPort  │ │
│  │ folder watch │───▶│ law seed     │───▶│ AgentPort   │ │
│  │ (NAS / SMB)  │    │ doc pack     │    │ RAG stack   │ │
│  └──────────────┘    └──────────────┘    └──────┬──────┘ │
│                                                    │        │
│                     ┌──────────────────────────────▼──────┐ │
│                     │ Web playground + API + trace log   │ │
│                     │ Cited answers · Govern audit trail │ │
│                     └──────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Users & Permissions (MVP)

| Role | Capabilities |
|------|----------------|
| **Partner (Owner)** | Create workspace, manage API keys, view all traces, configure retention |
| **Senior accountant** | Ingest documents, test playground, approve seed pack updates |
| **Junior accountant** | Chat / API query only, scoped to firm workspace |
| **IT installer** | Run Docker Compose, backup/restore, no chat access (optional) |

MVP uses API key scopes from AgentPort Phase 0/1. Full RBAC is post-MVP.

## Core Features

### 1. Turkish tax law doc pack

Pre-built seed corpus for pilot installs. Categories under `examples/defterport/seed-docs/`:

| Folder | Contents (placeholder structure) |
|--------|----------------------------------|
| `kanunlar/` | KDV Kanunu, GVK, Kurumlar Vergisi Kanunu excerpts |
| `tebligler/` | GİB tebliğleri, KDV Genel Uygulama Tebliği sections |
| `sgk/` | SGK prim, işe giriş/çıkış reference sheets |
| `efatura/` | e-Fatura / e-Arşiv process guides |
| `ic-rehberler/` | Office-authored quick reference (markdown) |

**Ingestion rules:**

- Supported formats: `.md`, `.txt`, `.pdf`, `.docx` (PDF/DOCX via existing ingestion pipeline)
- Chunk size: platform default (tunable per dataset in future)
- Language: Turkish primary; UTF-8 required
- Metadata tags: `category`, `source`, `effective_date`, `law_code` (optional JSON sidecar)

**Update cadence:**

- Seed pack versioned in git (`examples/defterport/seed-docs/VERSION`)
- Pilot firms receive quarterly mevzuat delta pack (manual rsync or re-ingest)

### 2. Client folder ingestion

Firms map one or more watch paths to AgentPort datasets.

**Expected folder layout (example):**

```text
/Mükellefler/
  1234567890 - Örnek Ltd. Şti./
    Beyannameler/
    Sözleşmeler/
    Yazışmalar/
    Notlar/
  9876543210 - Demo A.Ş./
    ...
```

**MVP ingestion modes:**

| Mode | Description |
|------|-------------|
| **Manual upload** | Operator UI or API upload of selected files |
| **Batch folder scan** | CLI script walks SMB path, uploads new/changed files (MVP script) |
| **Re-ingest** | Same file hash → skip; changed hash → replace chunks |

**Isolation:**

- One workspace per firm installation (single-tenant on-prem)
- Optional sub-datasets per mükellef (Office tier) for retrieval filtering
- Client folder docs never mixed with other firms (single-tenant deployment)

**KVKK posture:**

- All embeddings and object storage on firm-controlled MinIO + PostgreSQL
- Local or VPC-hosted inference (Ollama / private endpoint)
- No document content in application logs; traces store chunk ids and citation spans only

### 3. Cited answers

DefterPort uses the AgentPort AI services RAG path:

1. Embed question (deterministic local embeddings in dev; configurable in prod)
2. Retrieve top-k chunks from pgvector
3. Apply score threshold; if below → no-answer message
4. Build extractive answer with citations

**Answer contract (API response fields):**

```json
{
  "answer": "KDV istisnası için ...",
  "citations": [
    {
      "citation_id": "cite-1",
      "document_id": "...",
      "chunk_id": "...",
      "excerpt": "...",
      "score": 0.82
    }
  ],
  "retrieved_chunks": [],
  "retrieval": {
    "top_k": 5,
    "max_score": 0.82,
    "no_answer": false
  }
}
```

**Turkish UX copy:**

- No answer: *"Yüklenen belgelerde bu soruyu yanıtlamak için yeterli bilgi bulamadım."*
- Citation label: *"Kaynak"* + document name + section

### 4. AgentPort Govern (bundled)

| Feature | MVP behavior |
|---------|----------------|
| Audit trail | Full chat trace with retrieval payload |
| Retention | 90 days (Govern); 30 days (base) |
| Export | JSON trace export for partner review |
| API key scopes | `chat`, `ingest`, `trace:read` separation |
| Workspace isolation | Single workspace per install |

## Non-Functional Requirements

| Requirement | Target |
|-------------|--------|
| Query latency (p95) | < 10s on 8 vCPU / 32 GB RAM pilot hardware |
| Ingestion throughput | ≥ 100 documents/hour (mixed PDF/MD) |
| Availability | Single-node acceptable for MVP; backup nightly |
| Languages | Turkish queries; citations may quote Ottoman/legal terms |
| Compliance | KVKK on-prem processing; no third-party training |

## MVP User Flows

### Flow A — First install (IT + Partner)

1. Run `scripts/dev-up.sh` (or production compose overlay).
2. Bootstrap DefterPort workspace from `examples/defterport/bootstrap.json` (planned fixture).
3. Ingest `examples/defterport/seed-docs/`.
4. Run `scripts/smoke.sh` with DefterPort question set.
5. Partner opens web playground, asks 3 live questions, verifies citations.

### Flow B — Add client folder

1. Senior accountant creates dataset `mukellef-ornek-ltd`.
2. Run folder scan script against NAS path (or manual upload).
3. Wait for ingestion job completion in operator UI.
4. Ask: *"Örnek Ltd. 2025 KDV beyannamesi notlarında ne yazıyor?"*
5. Confirm citations point to ingested client files only.

### Flow C — Junior daily use

1. Junior opens internal chat widget (API key scoped).
2. Asks mevzuat question → cited answer from seed pack.
3. Asks client question → cited answer from client dataset (if configured).
4. Partner weekly: export traces, spot-check wrong retrieval.

## Example Queries (acceptance)

**Tax law (seed pack):**

- "KDV Genel Uygulama Tebliği'ne göre istisna sınırı nedir?"
- "Stopaj kesintisi hangi hizmet ödemelerinde uygulanır?"
- "e-Fatura zorunluluğu cirosu 3 milyon TL olan şirket için ne zaman başlar?"

**Client folder:**

- "Örnek Ltd. son SGK bordrosunda eksik gün var mı?"
- "Demo A.Ş. kira sözleşmesinde KDV kim tarafından ödeniyor?"

**Should no-answer:**

- "Bugün hava nasıl?" (out of domain)
- "Almanya VAT rate?" (not in Turkish pack)

## Integration Surface

| Surface | Use |
|---------|-----|
| **Web playground** | Partner / senior testing |
| **REST chat API** | Internal tools, future Luca sidebar |
| **Widget embed** | Intranet iframe with domain allowlist |

Platform API base: `http://localhost:5001` (local)  
AI services: `http://localhost:5002`

## Data Model Mapping

| DefterPort concept | AgentPort entity |
|--------------------|------------------|
| Firm install | `Workspace` |
| Mükellef corpus | `Dataset` + `KnowledgeBase` |
| Tax law pack | `Dataset` (seed) |
| Single document | `DocumentAsset` |
| Ingestion job | Ingest API job id |
| Q&A session | Chat run + `Trace` |
| Source quote | `Citation` |

## Roadmap (post-MVP)

- Mükellef-level retrieval filters (VKN tag on ingest)
- OCR pipeline for scanned arşiv
- GİB e-Beyanname metadata integration (read-only)
- Turkish-localized operator UI
- Automated mevzuat delta alerts
- Multi-office holding company (multi-workspace SaaS)

## Related Docs

- Launch strategy & pricing: `docs/LAUNCH.md`
- Local stack quickstart: `docs/quickstart.md`
- Example seed structure: `examples/defterport/README.md`