# DefterPort Examples

Sample fixtures for the DefterPort wedge: Turkish tax law seed documents and client-folder ingestion layout for SMMM pilot installs.

## Contents

```text
examples/defterport/
  README.md                 This file
  seed-docs/
    VERSION                 Seed pack version stamp
    kanunlar/               Tax laws (KDV, GVK, KVK excerpts)
    tebligler/              GİB circulars and application guides
    sgk/                    SGK payroll and registration references
    efatura/                e-Fatura / e-Arşiv process guides
    ic-rehberler/           Office-authored quick reference notes
  client-folder-sample/     Example mükellef directory layout (no PII)
```

## Quick Start

From the AgentPort repo root, start the stack and run the default smoke path:

```bash
scripts/dev-up.sh
RESET_JSON="$(scripts/dev-reset.sh)"
scripts/smoke.sh
```

To ingest the DefterPort seed pack manually (once bootstrap fixture lands):

```bash
# Placeholder — use Platform API ingest endpoints with API_KEY from dev-reset
API_KEY="$(jq -r '.apiKey' <<<"$RESET_JSON")"
DATASET_ID="<defterport-tax-law-dataset-id>"

for f in examples/defterport/seed-docs/**/*.md; do
  echo "Ingest: $f"
  # curl -X POST .../ingest with file upload
done
```

## Seed Doc Pack

The `seed-docs/` tree uses **placeholder markdown files** for MVP scaffolding. Replace with licensed or public-domain mevzuat excerpts before production pilot.

| Directory | Purpose |
|-----------|---------|
| `kanunlar/` | Core tax law articles referenced in daily SMMM work |
| `tebligler/` | GİB application circulars (KDV GUT sections, etc.) |
| `sgk/` | Prim, işe giriş/çıkış, eksik gün rules |
| `efatura/` | e-Belge processes and thresholds |
| `ic-rehberler/` | Firm-specific cheat sheets (safe to commit; no client PII) |

Check `seed-docs/VERSION` before re-ingesting after a pack update.

## Client Folder Sample

`client-folder-sample/` mirrors a typical NAS layout:

```text
client-folder-sample/
  0000000000 - Ornek Ltd. Sti./
    Beyannameler/
    Sozlesmeler/
    Yazismalar/
    Notlar/
```

Files here are synthetic placeholders for ingestion testing. **Do not commit real mükellef documents.**

## Benchmark Questions

Use these for smoke / eval after ingestion:

**Tax law:**

1. KDV istisnası hangi işlemlerde uygulanabilir?
2. Stopaj kesintisi oranı hizmet alımlarında nasıl belirlenir?
3. e-Fatura zorunluluğu cirosu 3 milyon TL olan şirket için ne zaman başlar?

**Client folder (after ingesting sample):**

1. Örnek Ltd. kira sözleşmesinde KDV kim tarafından ödeniyor?
2. Son beyanname notlarında hangi dönem belirtilmiş?

## Docs

- Product spec: `docs/products/DEFTERPORT.md`
- Launch plan: `docs/LAUNCH.md`