# DefterPort — Single-Node On-Prem Install Runbook

This runbook installs the full DefterPort stack on **one machine** (a Windows file
server or a Linux box) using Docker Compose. It is built for an SMMM (mali müşavir)
firm with **no DevOps staff**: copy, fill in a few secrets, run two commands.

The stack is **sovereign by default** — no data leaves the box. Only an HTTPS
reverse proxy (Caddy) is exposed on the LAN; every other service lives on a private
Docker network with no published host port.

---

## 1. What gets installed

| Service          | Role                                   | Exposed to LAN? |
|------------------|----------------------------------------|-----------------|
| caddy            | TLS reverse proxy (443/80)             | **Yes — only this** |
| web              | Next.js UI + API proxy                 | No (internal)   |
| platform-api     | .NET 10 control plane + EF migrations  | No (internal)   |
| ai-services      | FastAPI RAG runtime (e5 embeddings)    | No (internal)   |
| postgres         | pgvector 16 — system of record         | No (internal)   |
| redis            | cache / queue (password-protected)     | No (internal)   |
| rabbitmq         | message broker                         | No (internal)   |
| minio            | object storage for documents           | No (internal)   |

Security defaults baked into `docker-compose.prod.yml`:

- `AGENTPORT_SOVEREIGN=true` — outbound egress to any non-local LLM provider is
  locked off and **cannot be re-opened by editing a route**.
- `TRACE_REDACT_CONTENT=true` — traces persist citation metadata only, never raw
  document text (KVKK).
- `PROVIDER_LIVE_CALLS=false` and **Ollama is the only provider** the runtime may
  reach (off by default; opt-in via `.env.prod`).
- Dev/seed endpoints (`/bootstrap/local`, `/dev/reset`) return **404 in prod** —
  they require a temporary, explicit opt-in (see §6).
- `AI_SERVICES_INTERNAL_TOKEN` is required: platform-api forwards it on every call
  and ai-services rejects any `/v1/*` request without it.
- No default-credential fallbacks: compose **fails fast** if any secret is unset.

---

## 2. Prerequisites

### Linux (recommended for an always-on box)
- 64-bit Linux (Ubuntu 22.04+ / Debian 12 / RHEL 9 or similar)
- Docker Engine 24+ and the Compose v2 plugin (`docker compose version`)
- 4 CPU cores, 8 GB RAM minimum (the e5 embedding model needs ~2 GB headroom)
- ~20 GB free disk (images + the ~1.1 GB e5 model + your documents/backups)
- `openssl` (for generating secrets), `jq` and `curl` (for the seed step)

### Windows (Docker Desktop + WSL2)
- Windows 10/11 Pro or a Windows Server with virtualization enabled
- **WSL2** enabled and a Linux distro installed (`wsl --install`)
- **Docker Desktop** with the **WSL2 backend** turned on
  (Settings → General → "Use the WSL 2 based engine")
- Allocate ≥ 8 GB RAM to WSL2 (Docker Desktop → Settings → Resources)
- Run all commands below **inside the WSL2 shell** (e.g. Ubuntu), with the repo
  checked out **on the Linux filesystem** (`~/defterport`), NOT under `/mnt/c/...`
  — bind-mount performance and file permissions are far better there.
- `openssl`, `jq`, `curl` inside WSL2 (`sudo apt install openssl jq curl`)

> Keep the box on a UPS. Postgres and MinIO hold the firm's only copy of ingested
> data until your first off-box backup runs (§7).

---

## 3. Get the code onto the box

```bash
git clone <your-defterport-repo> defterport
cd defterport
```

(Or copy the repo via USB / file share to the server, then `cd` into it.)

---

## 4. Generate secrets (`.env.prod`)

Copy the template and fill in **strong, unique** values. The compose file will
refuse to start if any required secret is blank.

```bash
cp .env.prod.example .env.prod
```

Generate values (Linux or WSL2):

```bash
# Passwords (DB, Redis, RabbitMQ, MinIO) — 32-char URL-safe each:
openssl rand -base64 32 | tr -d '/+=' | cut -c1-32

# Internal service token (64 hex chars):
openssl rand -hex 32
```

Edit `.env.prod` and replace every `CHANGE_ME_*` placeholder:

- `POSTGRES_PASSWORD`, `REDIS_PASSWORD`, `RABBITMQ_PASS`, `MINIO_ROOT_PASSWORD`
  → a **different** 32-char value each.
- `AI_SERVICES_INTERNAL_TOKEN` → one 64-hex value (used by both platform-api and
  ai-services; compose wires the same variable into both).
- `DEFTERPORT_SITE_ADDRESS` → `https://localhost` for a LAN box, or a real FQDN
  for an automatic public certificate (see §8).

> **Back these up offline immediately** (password manager / sealed envelope).
> Losing `POSTGRES_PASSWORD` or `MINIO_ROOT_PASSWORD` means losing access to the
> firm's data. `.env.prod` is gitignored and must never be committed.

Verify the file parses before going further:

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod config -q && echo OK
```

---

## 5. First bring-up (build + start)

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

The first run:
- builds all three app images (a few minutes; .NET + torch are the slow parts),
- starts infra and waits on healthchecks,
- platform-api runs EF Core migrations automatically on startup,
- ai-services downloads the multilingual-e5-base model (~1.1 GB) into the
  `hf_cache` volume on its first embedding call (one-time; persists across restarts).

Watch progress:

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod ps
docker compose -f docker-compose.prod.yml --env-file .env.prod logs -f platform-api ai-services
```

Once `platform-api` and `ai-services` are `healthy`, open `https://localhost`
(or your site address) in a browser on the box or LAN. You will see a TLS warning
on a self-signed/internal cert — that is expected (§8).

---

## 6. First-run bootstrap & seed (the "bootstrap dance")

The seed endpoints are **disabled in prod**. To seed the firm's base workspace,
agent, API key, and the DefterPort tax-law + client-folder datasets, temporarily
enable them with the bootstrap override, run the seed, then turn them back off.

```bash
# 6.1 — bring the stack up WITH the bootstrap override.
#       This sets platform-api to Development + AGENTPORT_ALLOW_DEV_ENDPOINTS=true
#       and binds platform-api:5001 / ai-services:5002 to 127.0.0.1 (loopback only).
docker compose -f docker-compose.prod.yml -f docker-compose.prod.bootstrap.yml \
  --env-file .env.prod up -d

# 6.2 — run the seed (talks to 127.0.0.1:5001 and 127.0.0.1:5002).
scripts/defterport-bootstrap.sh        # add --no-client to skip the sample client folder

# The script prints a JSON summary INCLUDING the bootstrap API key — copy it now.

# 6.3 — recreate WITHOUT the override. Dev endpoints go back to 404 and the
#        loopback ports disappear. THIS IS REQUIRED before going live.
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d
```

Confirm the dev endpoints are locked again (should return 404):

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod exec platform-api \
  curl -s -o /dev/null -w "%{http_code}\n" -X POST http://127.0.0.1:5001/api/v1/bootstrap/local
# expect: 404
```

> On Windows/WSL2 run all three steps inside the WSL2 shell. `scripts/defterport-bootstrap.sh`
> needs `curl` and `jq` available there.

### Optional: enable Ollama for generative answers
By default the runtime serves local extractive RAG (no LLM). To use a local LLM,
run Ollama on the host (or a LAN box), then in `.env.prod` set:

```env
OLLAMA_ENABLED=true
OLLAMA_BASE_URL=http://host.docker.internal:11434   # or http://<lan-ip>:11434
OLLAMA_MODEL=llama3.2
```

and `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d` to apply.
Sovereign mode still blocks every non-Ollama provider.

---

## 7. Backup & restore

Postgres and MinIO are backed up **independently** by the scripts already in the
repo. The compose project name is `defterport-prod`, so point the backup scripts
at it via `COMPOSE_PROJECT_NAME` and the prod compose file.

> ⚠️ **Consistency caveat:** the DB dump and the MinIO mirror are taken
> separately and are **not** a single point-in-time snapshot. A document ingested
> between the two runs can land in one backup but not the other. For a clean pair,
> run both back-to-back during a quiet window (ideally pause ingestion), or briefly
> stop `web` first: `docker compose -f docker-compose.prod.yml --env-file .env.prod stop web`.

### 7.1 Manual backup

The scripts read creds from a `.env` file in the repo root and default to the dev
compose project. For prod, export the prod values and project name first. The
simplest reliable approach is a small wrapper that sources `.env.prod`:

```bash
# Linux/WSL2 — run from the repo root
set -a; . ./.env.prod; set +a
export COMPOSE_PROJECT_NAME=defterport-prod

# Postgres -> infra/backups/postgres/<db>_<timestamp>.dump (+ .sha256)
COMPOSE_FILE=docker-compose.prod.yml infra/scripts/postgres-backup.sh

# MinIO   -> infra/backups/minio/<timestamp>/<bucket>/
COMPOSE_FILE=docker-compose.prod.yml infra/scripts/minio-backup.sh
```

> The backup scripts hard-code `docker-compose.yml` in their `docker compose -f`
> calls. If you keep only the prod stack on the box, either (a) symlink
> `docker-compose.yml -> docker-compose.prod.yml`, or (b) copy the two backup
> scripts and swap the `-f` path to `docker-compose.prod.yml`. Both target the
> same `postgres` / `minio` service names, so the dump/mirror logic is unchanged.

### 7.2 Scheduled backups

**Linux (cron)** — nightly at 02:30, e.g. `crontab -e`:

```cron
30 2 * * * cd /home/defterport/defterport && set -a && . ./.env.prod && set +a && \
  COMPOSE_PROJECT_NAME=defterport-prod COMPOSE_FILE=docker-compose.prod.yml \
  ./infra/scripts/postgres-backup.sh >> /var/log/defterport-backup.log 2>&1
35 2 * * * cd /home/defterport/defterport && set -a && . ./.env.prod && set +a && \
  COMPOSE_PROJECT_NAME=defterport-prod COMPOSE_FILE=docker-compose.prod.yml \
  ./infra/scripts/minio-backup.sh >> /var/log/defterport-backup.log 2>&1
```

(The 5-minute gap keeps the two backups close together; pause `web` in the window
if you need them tighter.)

**Windows (Task Scheduler via WSL2)** — create two daily tasks that invoke the
WSL2 shell. From an elevated PowerShell:

```powershell
schtasks /Create /SC DAILY /ST 02:30 /TN "DefterPort-PgBackup" /TR `
  "wsl.exe -d Ubuntu -- bash -lc 'cd ~/defterport && set -a && . ./.env.prod && set +a && COMPOSE_PROJECT_NAME=defterport-prod COMPOSE_FILE=docker-compose.prod.yml ./infra/scripts/postgres-backup.sh'"

schtasks /Create /SC DAILY /ST 02:35 /TN "DefterPort-MinioBackup" /TR `
  "wsl.exe -d Ubuntu -- bash -lc 'cd ~/defterport && set -a && . ./.env.prod && set +a && COMPOSE_PROJECT_NAME=defterport-prod COMPOSE_FILE=docker-compose.prod.yml ./infra/scripts/minio-backup.sh'"
```

(Adjust `-d Ubuntu` to your WSL distro name and `~/defterport` to your checkout.)

### 7.3 Retention / pruning

Keep ~14 daily Postgres dumps and MinIO snapshots; prune older ones. Add a daily
prune (Linux cron example):

```cron
0 3 * * * find /home/defterport/defterport/infra/backups/postgres -type f -name '*.dump'   -mtime +14 -delete
1 3 * * * find /home/defterport/defterport/infra/backups/postgres -type f -name '*.sha256' -mtime +14 -delete
2 3 * * * find /home/defterport/defterport/infra/backups/minio -mindepth 1 -maxdepth 1 -type d -mtime +14 -exec rm -rf {} +
```

### 7.4 Off-box copy (do NOT skip)

A backup on the same disk does not survive a dead disk or ransomware. After each
backup, copy the latest dump + MinIO snapshot to a second location:

```bash
# Examples — pick what the firm has:
rsync -a --delete infra/backups/ /mnt/nas/defterport-backups/        # LAN NAS
rsync -a infra/backups/ backup-user@10.0.0.9:/srv/defterport/        # second box
# or robocopy from Windows to a mapped network drive / external USB:
#   robocopy "\\wsl$\Ubuntu\home\defterport\defterport\infra\backups" "E:\defterport-backups" /MIR
```

Verify the off-box copy includes the `.sha256` files (used to detect corruption).

### 7.5 Restore drill (test it BEFORE you need it)

`postgres-restore-smoke.sh` restores the latest dump into a throwaway DB and
verifies the extensions, without touching the live database:

```bash
set -a; . ./.env.prod; set +a
export COMPOSE_PROJECT_NAME=defterport-prod
COMPOSE_FILE=docker-compose.prod.yml infra/scripts/postgres-restore-smoke.sh
# expect: "Postgres restore smoke passed for <file>"
```

Run this monthly. To restore MinIO objects, mirror a snapshot back into the bucket
with `mc mirror /backup/<bucket> local/<bucket>` inside an `mc` container on the
prod network (reverse of `minio-backup.sh`). Because the two backups are
independent (§7), after a full restore re-run any document whose ingest may have
fallen between the two backup points (re-upload, or use the reingest endpoint).

---

## 8. TLS notes

Caddy terminates TLS and forwards plain HTTP to `web` on the internal network.

- **LAN box (default):** `DEFTERPORT_SITE_ADDRESS=https://localhost`. Caddy uses
  its **internal CA** and serves a self-signed certificate (`tls internal` in
  `infra/Caddyfile`). Browsers warn once. To remove the warning, install Caddy's
  root CA on client machines — export it from the container:
  ```bash
  docker compose -f docker-compose.prod.yml --env-file .env.prod \
    cp caddy:/data/caddy/pki/authorities/local/root.crt ./defterport-root.crt
  ```
  then import `defterport-root.crt` into the OS/browser trust store on each client.

- **Real certificate (public FQDN):** set `DEFTERPORT_SITE_ADDRESS` to your domain
  (e.g. `https://defterport.example.com`), ensure it resolves to the box and ports
  80/443 are reachable, and **delete the `tls internal` line** in `infra/Caddyfile`.
  Caddy will obtain and renew a Let's Encrypt certificate automatically.

- **Bring-your-own cert:** drop `fullchain.pem` + `privkey.pem` into a folder, mount
  it into the caddy service, and replace `tls internal` with
  `tls /etc/caddy/certs/fullchain.pem /etc/caddy/certs/privkey.pem`.

---

## 9. Day-2 operations

```bash
# Status / logs
docker compose -f docker-compose.prod.yml --env-file .env.prod ps
docker compose -f docker-compose.prod.yml --env-file .env.prod logs -f <service>

# Update to a new release
git pull
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
#   (platform-api applies any new EF migrations automatically on startup —
#    take a Postgres backup first, §7.1)

# Stop / start
docker compose -f docker-compose.prod.yml --env-file .env.prod stop
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d

# Full teardown (KEEPS named volumes / data)
docker compose -f docker-compose.prod.yml --env-file .env.prod down
# Teardown INCLUDING data volumes — destructive, only after a verified backup:
# docker compose -f docker-compose.prod.yml --env-file .env.prod down -v
```

All services use `restart: unless-stopped`, so they come back automatically after
a reboot once the Docker daemon is up.
