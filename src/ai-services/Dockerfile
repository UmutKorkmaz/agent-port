# syntax=docker/dockerfile:1
# AgentPort / AgentPort — ai-services production image.
#
# Image size note: the dependency weight is torch + sentence-transformers.
#   - CPU-only torch wheel (installed below via the PyTorch CPU index) is ~190 MB
#     vs. ~800 MB+ for the default CUDA wheel. We force the CPU index so the image
#     stays lean on a CPU-only on-prem box (~1.4-1.8 GB total).
#   - The multilingual-e5-base model (~1.1 GB) is NOT baked into the image. It is
#     downloaded at first run into the HuggingFace cache, which docker-compose.prod.yml
#     mounts as a named volume (hf_cache -> /home/appuser/.cache/huggingface) so it
#     persists across restarts and is fetched only once.
#
# Build context MUST be the repo root, e.g.:
#   docker build -f src/ai-services/Dockerfile -t agentport/ai-services .

# ---------- build (compile wheels) ----------
FROM python:3.11-slim AS build
WORKDIR /build

# Build deps for any source-only wheels (psycopg/sentence-transformers pull prebuilt
# wheels, but keep gcc available so a no-wheel fallback still installs).
RUN apt-get update \
    && apt-get install -y --no-install-recommends build-essential \
    && rm -rf /var/lib/apt/lists/*

COPY src/ai-services/requirements.txt .

# Install into a relocatable prefix. torch is pulled from the CPU wheel index to
# avoid dragging in CUDA; everything else resolves from PyPI.
RUN pip install --no-cache-dir --prefix=/install \
        --extra-index-url https://download.pytorch.org/whl/cpu \
        -r requirements.txt

# ---------- runtime ----------
FROM python:3.11-slim AS runtime

# Non-root runtime user with a stable home for the HF cache mount.
RUN groupadd --gid 10001 appuser \
    && useradd --uid 10001 --gid 10001 --create-home --home-dir /home/appuser appuser

WORKDIR /app

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    HF_HOME=/home/appuser/.cache/huggingface \
    SENTENCE_TRANSFORMERS_HOME=/home/appuser/.cache/huggingface \
    PATH=/usr/local/bin:$PATH

# Copy the installed site-packages from the build stage.
COPY --from=build /install /usr/local

# App source (main.py + any siblings under src/ai-services).
COPY src/ai-services/ /app/

# Ensure the cache dir exists and is owned by the runtime user before the volume mounts.
RUN mkdir -p /home/appuser/.cache/huggingface \
    && chown -R appuser:appuser /home/appuser /app

EXPOSE 5002
USER appuser

# uvicorn serves FastAPI; /health, /health/live, /health/ready are defined in main.py.
ENTRYPOINT ["python", "-m", "uvicorn", "main:app", "--host", "0.0.0.0", "--port", "5002"]
