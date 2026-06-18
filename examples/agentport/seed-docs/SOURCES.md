# Seed Doc Sources & Licensing

This document defines how the AgentPort seed doc pack is sourced, licensed, and
kept fresh. It is the authoritative process reference for the contents of
`examples/agentport/seed-docs/`.

## Current status: ALL DOCUMENTS ARE PLACEHOLDERS

> **Critical:** Every `.md` file currently in this seed pack is a non-binding
> MVP scaffolding placeholder. The `manifest.json` reports
> `placeholder_count == total_count`. None of these files are usable as legal
> reference and **none may be treated as authoritative mevzuat.**

The placeholder files exist only to exercise the ingestion, chunking,
retrieval, and citation pipeline during development. Their text is illustrative
structure, not law. Do not "fill them in" by paraphrasing or generating tax-law
content from memory — fabricated mevzuat is dangerous and must never be shipped.

## Hard requirement before any paying pilot

Before the pack is used in **any paying pilot or production install**, every
placeholder document MUST be replaced with content that is either:

1. **Public-domain official text** — e.g. Resmi Gazete publications and GİB
   (Gelir İdaresi Başkanlığı) public mevzuat texts, used within the terms of
   their public availability; or
2. **Properly licensed content** — text the firm has a written license or
   redistribution right to use.

Sourcing checklist for each replacement document:

- [ ] Source is official (Resmi Gazete / GİB) or explicitly licensed
- [ ] Provenance recorded in the document's `.meta.json` `source` field
      (replace `"placeholder"` with the real source identifier / URL)
- [ ] `effective_date` set to the real effective date (replace `"placeholder"`)
- [ ] `law_code` set where applicable
- [ ] `placeholder` flag set to `false` in the sidecar and `manifest.json`
- [ ] `sha256` recomputed and updated in both the sidecar and `manifest.json`

## Launch gate: ≥ 20 documents

The AgentPort MVP success criteria require ingesting **at least 20** Turkish tax
law seed documents and answering 8/10 benchmark questions with correct citations
(see `docs/LAUNCH.md`). This pack currently contains **6** documents — all
placeholders — so it does **not** meet the launch gate. The gate is met only
when there are ≥ 20 non-placeholder, properly sourced documents and the manifest
reflects `placeholder_count == 0`.

## Freshness & versioning policy

Turkish tax law changes frequently. Beyanname deadlines, e-Fatura ciro
thresholds, stopaj rates, and SGK rules all date quickly, so a stale pack is a
correctness and liability risk.

- The pack is version-stamped in `seed-docs/VERSION`
  (currently `agentport-seed-pack 0.1.0`).
- Pilot firms receive a **quarterly mevzuat delta pack** (manual rsync or
  re-ingest), as described in `docs/products/AGENTPORT.md`.
- Any content change — replacing a placeholder, updating an article, or adding a
  document — MUST bump `seed-docs/VERSION` and update `manifest.json`
  (`version`, `version_updated`, counts, and per-document `sha256`).
- Re-ingestion is hash-aware: unchanged `sha256` → skip; changed `sha256` →
  replace chunks. Keeping the manifest hashes accurate is what makes this safe.

## Selecting the legal content is a human decision

Choosing which mevzuat texts to include, confirming their licensing status, and
verifying their accuracy and currency is a **human business and legal
decision** — not an automated one. Tooling in this repo only manages metadata,
manifests, and process; it does not select, author, paraphrase, or validate
legal content. A qualified human (partner / legal counsel) must sign off on the
real document set before pilot.
