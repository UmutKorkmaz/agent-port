import sys
from pathlib import Path


AI_SERVICES_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(AI_SERVICES_ROOT))
