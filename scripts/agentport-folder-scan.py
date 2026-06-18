#!/usr/bin/env python3
"""
AgentPort folder scan / NAS ingestion CLI.

Walks a NAS/SMB watch path laid out like:

    /Mukellefler/{VKN} - {Unvan}/
        Beyannameler/
        Sozlesmeler/
        ...

It derives the VKN (tax number) and client name (unvan) from each top-level
client directory, then uploads new/changed PDF/DOCX/MD/TXT files to a target
AgentPort dataset via the platform-api dataset document upload endpoint:

    POST {api-base}/api/v1/datasets/{dataset-id}/documents

The server enforces content-hash idempotency, so re-uploading an unchanged
file is a cheap no-op (it returns ingestion_status="skipped"). To avoid even
re-POSTing unchanged files this scanner keeps a small local manifest keyed by
relative path -> sha256, stored next to the watch path by default.

Usage:
    python3 scripts/agentport-folder-scan.py \
        --watch-path "/Mukellefler" \
        --dataset-id 11111111-1111-1111-1111-111111111111 \
        --api-key dp_live_xxx \
        [--api-base http://localhost:5001] \
        [--dry-run]
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Optional

try:  # Prefer httpx (already used by src/ai-services); fall back to requests.
    import httpx  # type: ignore

    _HTTP_BACKEND = "httpx"
except ImportError:  # pragma: no cover - environment dependent
    try:
        import requests  # type: ignore

        _HTTP_BACKEND = "requests"
    except ImportError:  # pragma: no cover - environment dependent
        httpx = None  # type: ignore
        requests = None  # type: ignore
        _HTTP_BACKEND = None


SUPPORTED_EXTENSIONS = {".pdf", ".docx", ".md", ".txt"}
MANIFEST_FILENAME = ".agentport-folder-scan.json"
HASH_CHUNK_BYTES = 1024 * 1024
# Top-level dir shape: "{VKN} - {Unvan}". VKN is 10 (VKN) or 11 (TCKN) digits.
_CLIENT_DIR_PATTERN = re.compile(r"^\s*(?P<vkn>\d{10,11})\s*-\s*(?P<unvan>.+?)\s*$")

_CONTENT_TYPES = {
    ".pdf": "application/pdf",
    ".docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    ".md": "text/markdown",
    ".txt": "text/plain",
}


@dataclass(frozen=True)
class ClientFolder:
    vkn: str
    unvan: str
    path: Path


@dataclass(frozen=True)
class ScanFile:
    client: ClientFolder
    path: Path
    relative_key: str
    content_hash: str


def parse_client_folder(path: Path) -> Optional[ClientFolder]:
    match = _CLIENT_DIR_PATTERN.match(path.name)
    if not match:
        return None
    return ClientFolder(vkn=match.group("vkn"), unvan=match.group("unvan"), path=path)


def hash_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(HASH_CHUNK_BYTES), b""):
            digest.update(block)
    return digest.hexdigest()


def content_type_for(path: Path) -> str:
    return _CONTENT_TYPES.get(path.suffix.lower(), "application/octet-stream")


def discover_files(watch_path: Path) -> list[ScanFile]:
    files: list[ScanFile] = []
    for entry in sorted(watch_path.iterdir()):
        if not entry.is_dir():
            continue
        client = parse_client_folder(entry)
        if client is None:
            continue
        for candidate in sorted(entry.rglob("*")):
            if not candidate.is_file():
                continue
            if candidate.suffix.lower() not in SUPPORTED_EXTENSIONS:
                continue
            relative_key = candidate.relative_to(watch_path).as_posix()
            files.append(
                ScanFile(
                    client=client,
                    path=candidate,
                    relative_key=relative_key,
                    content_hash=hash_file(candidate),
                )
            )
    return files


def load_manifest(manifest_path: Path) -> Dict[str, str]:
    if not manifest_path.exists():
        return {}
    try:
        data = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError):
        return {}
    files = data.get("files") if isinstance(data, dict) else None
    if not isinstance(files, dict):
        return {}
    return {str(key): str(value) for key, value in files.items()}


def save_manifest(manifest_path: Path, files: Dict[str, str]) -> None:
    payload = {"version": 1, "files": dict(sorted(files.items()))}
    manifest_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def upload_file(
    api_base: str,
    dataset_id: str,
    api_key: str,
    scan_file: ScanFile,
    timeout: float = 120.0,
) -> tuple[int, str]:
    url = f"{api_base.rstrip('/')}/api/v1/datasets/{dataset_id}/documents"
    headers = {
        "x-agentport-api-key": api_key,
        "Authorization": f"Bearer {api_key}",
    }
    content_type = content_type_for(scan_file.path)
    file_bytes = scan_file.path.read_bytes()

    if _HTTP_BACKEND == "httpx":
        with httpx.Client(timeout=timeout) as client:
            response = client.post(
                url,
                headers=headers,
                files={"file": (scan_file.path.name, file_bytes, content_type)},
            )
        return response.status_code, response.text
    if _HTTP_BACKEND == "requests":
        response = requests.post(
            url,
            headers=headers,
            files={"file": (scan_file.path.name, file_bytes, content_type)},
            timeout=timeout,
        )
        return response.status_code, response.text
    raise RuntimeError("No HTTP backend available; install httpx or requests.")


def summarize_action(action: str, scan_file: ScanFile, detail: str = "") -> str:
    suffix = f" {detail}" if detail else ""
    return (
        f"[{action:<7}] {scan_file.client.vkn} / {scan_file.client.unvan} :: "
        f"{scan_file.relative_key}{suffix}"
    )


def parse_args(argv: Optional[list[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Scan a AgentPort client folder tree and upload new/changed documents.",
    )
    parser.add_argument("--watch-path", required=True, help="Root NAS/SMB watch path (e.g. /Mukellefler).")
    parser.add_argument("--dataset-id", required=True, help="Target AgentPort dataset id (GUID).")
    parser.add_argument("--api-key", required=True, help="AgentPort API key.")
    parser.add_argument(
        "--api-base",
        default="http://localhost:5001",
        help="platform-api base URL (default: http://localhost:5001).",
    )
    parser.add_argument(
        "--manifest",
        default=None,
        help="Path to the local sync manifest (default: <watch-path>/.agentport-folder-scan.json).",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Report planned actions without uploading anything.",
    )
    return parser.parse_args(argv)


def run(args: argparse.Namespace) -> int:
    watch_path = Path(args.watch_path).expanduser()
    if not watch_path.is_dir():
        print(f"error: watch path is not a directory: {watch_path}", file=sys.stderr)
        return 2

    if not args.dry_run and _HTTP_BACKEND is None:
        print("error: no HTTP backend available; install httpx or requests.", file=sys.stderr)
        return 2

    manifest_path = (
        Path(args.manifest).expanduser() if args.manifest else watch_path / MANIFEST_FILENAME
    )
    manifest = load_manifest(manifest_path)

    files = discover_files(watch_path)
    if not files:
        print(f"No supported documents found under {watch_path}.")
        return 0

    uploaded = 0
    skipped = 0
    failed = 0
    updated_manifest = dict(manifest)

    for scan_file in files:
        known_hash = manifest.get(scan_file.relative_key)
        if known_hash == scan_file.content_hash:
            skipped += 1
            print(summarize_action("skip", scan_file, "unchanged (manifest)"))
            continue

        action = "new" if known_hash is None else "changed"

        if args.dry_run:
            uploaded += 1
            print(summarize_action(action, scan_file, "(dry-run)"))
            continue

        try:
            status_code, body = upload_file(
                args.api_base, args.dataset_id, args.api_key, scan_file
            )
        except Exception as exc:  # noqa: BLE001 - surface any transport error per-file
            failed += 1
            print(summarize_action("fail", scan_file, f"-> {exc}"), file=sys.stderr)
            continue

        if 200 <= status_code < 300:
            uploaded += 1
            ingestion_status = _ingestion_status(body)
            updated_manifest[scan_file.relative_key] = scan_file.content_hash
            print(summarize_action(action, scan_file, f"-> HTTP {status_code} {ingestion_status}"))
        else:
            failed += 1
            print(
                summarize_action("fail", scan_file, f"-> HTTP {status_code} {body[:200]}"),
                file=sys.stderr,
            )

    if not args.dry_run:
        save_manifest(manifest_path, updated_manifest)

    print(
        f"\nSummary: {uploaded} uploaded, {skipped} skipped, {failed} failed "
        f"({len(files)} scanned){' [dry-run]' if args.dry_run else ''}."
    )
    return 1 if failed else 0


def _ingestion_status(body: str) -> str:
    try:
        parsed = json.loads(body)
    except (json.JSONDecodeError, TypeError):
        return ""
    if isinstance(parsed, dict):
        return str(parsed.get("ingestion_status", ""))
    return ""


def main(argv: Optional[list[str]] = None) -> int:
    return run(parse_args(argv))


if __name__ == "__main__":
    raise SystemExit(main())
