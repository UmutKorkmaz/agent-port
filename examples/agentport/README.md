# AgentPort Examples

Sample fixtures for the AgentPort wedge: Turkish tax law seed documents and client-folder ingestion layout for SMMM pilot installs.

## Contents

```text
examples/agentport/
  README.md                 This file
  bootstrap.json            Firm install graph fixture (workspace, datasets, keys)
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

To bootstrap the AgentPort firm install and ingest the seed pack in one step, run
the bootstrap script. It reuses the base bootstrap endpoint, creates the two
AgentPort datasets described in `bootstrap.json`, and uploads every file under
`seed-docs/` (and `client-folder-sample/`) through the Platform API:

```bash
scripts/dev-up.sh
AGENTPORT_JSON="$(scripts/agentport-bootstrap.sh)"
echo "$AGENTPORT_JSON" | jq .
```

The script prints a JSON summary with the ids you need for follow-up calls:

```json
{
  "status": "bootstrapped",
  "workspaceId": "...",
  "taxLawDatasetId": "...",
  "clientFolderDatasetId": "...",
  "agentDefinitionId": "...",
  "apiKey": "..."
}
```

Pull individual values back out with `jq`, e.g. to query the agent:

```bash
API_KEY="$(jq -r '.apiKey' <<<"$AGENTPORT_JSON")"
AGENT_ID="$(jq -r '.agentDefinitionId' <<<"$AGENTPORT_JSON")"

curl -fsS -X POST "http://localhost:5001/api/v1/agent-definitions/$AGENT_ID/chat" \
  -H "content-type: application/json" \
  -H "x-agentport-api-key: $API_KEY" \
  -d '{"question":"KDV istisnası hangi işlemlerde uygulanabilir?","topK":4}' | jq .
```

Skip client-folder ingestion (seed pack only) with `scripts/agentport-bootstrap.sh --no-client`.
Override service endpoints with the same env vars used by `dev-reset.sh`
(`PLATFORM_API_URL`, `AI_SERVICES_URL`, `WEB_URL`).

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

- Product spec: `docs/products/AGENTPORT.md`
- Launch plan: `docs/LAUNCH.md`