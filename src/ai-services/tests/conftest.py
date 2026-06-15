import os
import sys
from pathlib import Path

# Contract tests assume the deterministic hash backend (no model download).
# Production defaults to e5; CI and local pytest inherit this unless overridden.
os.environ.setdefault("EMBEDDING_BACKEND", "hash")

AI_SERVICES_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(AI_SERVICES_ROOT))
