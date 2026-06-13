# syntax=docker/dockerfile:1
# AgentPort / DefterPort — web (Next.js 15) production image.
# Multi-stage: node:20 builds the standalone output, node:20-slim runs it as non-root.
# Relies on next.config.ts `output: "standalone"`.
#
# Build context MUST be the repo root, e.g.:
#   docker build -f src/web/Dockerfile -t agentport/web .

# ---------- deps ----------
FROM node:20 AS deps
WORKDIR /app
COPY src/web/package.json src/web/package-lock.json ./
RUN npm ci

# ---------- build ----------
FROM node:20 AS build
WORKDIR /app
ENV NEXT_TELEMETRY_DISABLED=1
COPY --from=deps /app/node_modules ./node_modules
COPY src/web/ ./
RUN npm run build

# ---------- runtime ----------
FROM node:20-slim AS runtime
WORKDIR /app

ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0

# node:20-slim ships a "node" user (uid 1000). Run as it.
# Standalone output bundles a minimal server.js + the trimmed node_modules it needs.
COPY --from=build --chown=node:node /app/.next/standalone ./
COPY --from=build --chown=node:node /app/.next/static ./.next/static
COPY --from=build --chown=node:node /app/public ./public

EXPOSE 3000
USER node

# server.js is emitted by the standalone build at the project root.
CMD ["node", "server.js"]
