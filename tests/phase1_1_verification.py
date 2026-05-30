#!/usr/bin/env python3
"""
Phase 1.1 verification harness for the local AgentPort stack.

The script intentionally uses only Python's standard library so it can run
without adding Playwright, pytest, curl, or jq dependencies.
"""

from __future__ import annotations

import argparse
import json
import os
import socket
import subprocess
import sys
import tempfile
import urllib.error
import urllib.request
import uuid
from dataclasses import dataclass
from pathlib import Path
from typing import Any


ROOT_DIR = Path(__file__).resolve().parents[1]
DEFAULT_SAMPLE_FILE = ROOT_DIR / "examples" / "phase1" / "support-policy.md"
NO_ANSWER_MARKERS = (
    "could not find",
    "cannot answer",
    "not enough information",
    "no answer",
    "do not know",
    "don't know",
)


@dataclass
class HttpResponse:
    status: int
    headers: dict[str, str]
    text: str

    def json(self) -> dict[str, Any] | list[Any] | None:
        if not self.text:
            return None
        try:
            return json.loads(self.text)
        except json.JSONDecodeError:
            return None


@dataclass
class CheckResult:
    name: str
    status: str
    detail: str


class Harness:
    def __init__(self, args: argparse.Namespace):
        self.args = args
        self.platform_url = args.platform_url.rstrip("/")
        self.ai_url = args.ai_url.rstrip("/")
        self.web_url = args.web_url.rstrip("/")
        self.results: list[CheckResult] = []
        self.bootstrap: dict[str, Any] = {}
        self.api_key: str | None = args.api_key
        self.uploaded_document_id: str | None = None

    def record(self, name: str, status: str, detail: str) -> None:
        self.results.append(CheckResult(name=name, status=status, detail=detail))

    def request(
        self,
        method: str,
        url: str,
        *,
        body: bytes | None = None,
        headers: dict[str, str] | None = None,
        timeout: float | None = None,
    ) -> HttpResponse:
        req = urllib.request.Request(url, data=body, method=method, headers=headers or {})
        try:
            with urllib.request.urlopen(req, timeout=timeout or self.args.timeout) as response:
                text = response.read().decode("utf-8", errors="replace")
                return HttpResponse(response.status, dict(response.headers), text)
        except urllib.error.HTTPError as exc:
            text = exc.read().decode("utf-8", errors="replace")
            return HttpResponse(exc.code, dict(exc.headers), text)
        except (urllib.error.URLError, TimeoutError, socket.timeout) as exc:
            return HttpResponse(0, {}, str(exc))

    def get(self, url: str) -> HttpResponse:
        return self.request("GET", url)

    def post_json(
        self,
        url: str,
        payload: dict[str, Any],
        *,
        headers: dict[str, str] | None = None,
    ) -> HttpResponse:
        request_headers = {"content-type": "application/json"}
        if headers:
            request_headers.update(headers)
        return self.request(
            "POST",
            url,
            body=json.dumps(payload).encode("utf-8"),
            headers=request_headers,
        )

    def post_empty(self, url: str, *, headers: dict[str, str] | None = None) -> HttpResponse:
        return self.request("POST", url, body=b"", headers=headers or {})

    def post_multipart_file(
        self,
        url: str,
        field_name: str,
        file_path: Path,
        *,
        filename: str | None = None,
        content_type: str = "text/markdown",
    ) -> HttpResponse:
        boundary = f"----agentport-phase11-{uuid.uuid4().hex}"
        file_name = filename or file_path.name
        payload = file_path.read_bytes()
        body = b"".join(
            [
                f"--{boundary}\r\n".encode("utf-8"),
                (
                    f'Content-Disposition: form-data; name="{field_name}"; '
                    f'filename="{file_name}"\r\n'
                ).encode("utf-8"),
                f"Content-Type: {content_type}\r\n\r\n".encode("utf-8"),
                payload,
                b"\r\n",
                f"--{boundary}--\r\n".encode("utf-8"),
            ]
        )
        return self.request(
            "POST",
            url,
            body=body,
            headers={"content-type": f"multipart/form-data; boundary={boundary}"},
            timeout=max(self.args.timeout, 30),
        )

    def bootstrap_local(self) -> dict[str, Any] | None:
        response = self.post_empty(f"{self.platform_url}/api/v1/bootstrap/local")
        body = response.json()
        if response.status != 200 or not isinstance(body, dict):
            self.record(
                "bootstrap setup",
                "FAIL",
                f"POST /api/v1/bootstrap/local returned {response.status}: {response.text[:240]}",
            )
            return None
        if body.get("apiKey") and not self.api_key:
            self.api_key = str(body["apiKey"])
        self.bootstrap = body
        return body

    def auth_headers(self) -> dict[str, str]:
        return {"x-agentport-api-key": self.api_key} if self.api_key else {}

    def chat(self, question: str, *, headers: dict[str, str] | None = None) -> HttpResponse | None:
        agent_id = self.bootstrap.get("agentDefinitionId")
        if not agent_id:
            self.record("chat setup", "GAP", "Bootstrap did not return agentDefinitionId.")
            return None
        return self.post_json(
            f"{self.platform_url}/api/v1/agent-definitions/{agent_id}/chat",
            {"question": question, "topK": 4},
            headers=headers,
        )

    def run(self) -> int:
        self.check_services()
        if not self.bootstrap_local():
            self.print_results()
            return self.exit_code()

        self.check_reset_and_bootstrap_idempotency()
        self.check_document_lifecycle()
        self.check_api_key_rejection_acceptance()
        self.check_widget_snippet()
        self.check_rag_no_answer_fallback()
        self.print_results()
        return self.exit_code()

    def check_services(self) -> None:
        platform = self.get(f"{self.platform_url}/health/ready")
        platform_body = platform.json()
        if platform.status == 200 and isinstance(platform_body, dict):
            self.record("platform readiness", "PASS", "Platform API readiness endpoint responded.")
        else:
            self.record(
                "platform readiness",
                "FAIL",
                f"Expected 200 from /health/ready, got {platform.status}.",
            )

        ai = self.get(f"{self.ai_url}/health")
        ai_body = ai.json()
        if ai.status == 200 and isinstance(ai_body, dict) and ai_body.get("status") == "ok":
            self.record("ai service readiness", "PASS", "AI service health endpoint responded.")
        else:
            self.record("ai service readiness", "FAIL", f"Expected AI status ok, got {ai.status}.")

    def check_reset_and_bootstrap_idempotency(self) -> None:
        if self.args.reset_command:
            first = self.run_reset_command()
            second = self.run_reset_command()
            if first and second:
                self.record("reset command idempotency", "PASS", "Configured reset command completed twice.")
        else:
            self.record(
                "reset command idempotency",
                "GAP",
                "No reset command is configured; set AGENTPORT_RESET_COMMAND to verify the real reset path.",
            )

        first = dict(self.bootstrap)
        second = self.bootstrap_local()
        if not second:
            return

        stable_fields = (
            "workspaceId",
            "projectId",
            "modelRouteId",
            "agentDefinitionId",
            "datasetId",
            "knowledgeBaseId",
            "walletAccountId",
        )
        mismatches = [field for field in stable_fields if first.get(field) != second.get(field)]
        if mismatches:
            self.record(
                "bootstrap idempotency",
                "FAIL",
                f"Repeated bootstrap changed stable fields: {', '.join(mismatches)}.",
            )
        else:
            self.record("bootstrap idempotency", "PASS", "Repeated bootstrap returned stable local IDs.")

    def run_reset_command(self) -> bool:
        completed = subprocess.run(
            self.args.reset_command,
            cwd=ROOT_DIR,
            shell=True,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=self.args.reset_timeout,
        )
        if completed.returncode == 0:
            reset_body = extract_last_json_object(completed.stdout)
            if isinstance(reset_body, dict):
                self.bootstrap = reset_body
                if reset_body.get("apiKey"):
                    self.api_key = str(reset_body["apiKey"])
            return True
        self.record(
            "reset command idempotency",
            "FAIL",
            f"Reset command exited {completed.returncode}: {completed.stdout[-500:]}",
        )
        return False

    def check_document_lifecycle(self) -> None:
        dataset_id = self.bootstrap.get("datasetId")
        if not dataset_id:
            self.record("document upload setup", "GAP", "Bootstrap did not return datasetId.")
            return

        sample_file = Path(self.args.sample_file)
        if not sample_file.exists():
            self.record("document upload setup", "FAIL", f"Sample file is missing: {sample_file}")
            return

        upload_url = f"{self.platform_url}/api/v1/datasets/{dataset_id}/documents"
        first = self.post_multipart_file(upload_url, "file", sample_file)
        first_body = first.json()
        if not self.assert_ingest_success("document upload", first, first_body):
            return
        self.uploaded_document_id = str(first_body.get("document_asset_id") or first_body.get("documentAssetId"))

        second = self.post_multipart_file(upload_url, "file", sample_file)
        second_body = second.json()
        if not self.assert_ingest_success("unchanged document reingest", second, second_body):
            return

        second_document_id = str(second_body.get("document_asset_id") or second_body.get("documentAssetId"))
        if self.uploaded_document_id == second_document_id:
            self.record(
                "unchanged document reingest",
                "PASS",
                "Reuploading unchanged content returned the existing document asset.",
            )
        else:
            self.record(
                "unchanged document reingest",
                "FAIL",
                "Reuploading unchanged content created a different document asset.",
            )

        changed_file = self.write_changed_sample(sample_file)
        changed = self.post_multipart_file(
            upload_url,
            "file",
            changed_file,
            filename=sample_file.name,
        )
        changed_body = changed.json()
        if self.assert_ingest_success("changed document reingest", changed, changed_body):
            changed_document_id = str(changed_body.get("document_asset_id") or changed_body.get("documentAssetId"))
            if changed_document_id and changed_document_id != self.uploaded_document_id:
                self.record(
                    "changed document reingest",
                    "PASS",
                    "Changed content with the same filename created a new document asset.",
                )
            else:
                self.record(
                    "changed document reingest",
                    "FAIL",
                    "Changed content did not create a replacement/versioned document asset.",
                )

        delete_target = self.uploaded_document_id or second_document_id
        delete_response = self.request("DELETE", f"{self.platform_url}/api/v1/documents/{delete_target}")
        if delete_response.status in (200, 202, 204):
            self.record("document delete/archive endpoint", "PASS", "Document delete/archive endpoint accepted the request.")
        elif delete_response.status in (404, 405):
            self.record(
                "document delete/archive endpoint",
                "GAP",
                f"DELETE /documents/{{documentId}} returned {delete_response.status}.",
            )
        else:
            self.record(
                "document delete/archive endpoint",
                "FAIL",
                f"DELETE returned unexpected status {delete_response.status}: {delete_response.text[:240]}",
            )

    def write_changed_sample(self, sample_file: Path) -> Path:
        changed_text = sample_file.read_text(encoding="utf-8")
        changed_text += f"\n\nPhase 1.1 verification nonce: {uuid.uuid4()}\n"
        handle = tempfile.NamedTemporaryFile("w", suffix=".md", prefix="agentport-phase11-", delete=False)
        with handle:
            handle.write(changed_text)
        return Path(handle.name)

    def assert_ingest_success(
        self,
        name: str,
        response: HttpResponse,
        body: dict[str, Any] | list[Any] | None,
    ) -> bool:
        if response.status != 200 or not isinstance(body, dict):
            self.record(name, "FAIL", f"Expected 200 JSON ingest response, got {response.status}.")
            return False

        status_value = body.get("status")
        chunk_count = body.get("chunk_count", body.get("chunkCount"))
        document_id = body.get("document_asset_id", body.get("documentAssetId"))
        if status_value == "succeeded" and document_id and isinstance(chunk_count, int) and chunk_count > 0:
            self.record(name, "PASS", f"Ingest succeeded with {chunk_count} chunks.")
            return True

        self.record(name, "FAIL", f"Unexpected ingest payload: {json.dumps(body)[:300]}")
        return False

    def check_api_key_rejection_acceptance(self) -> None:
        missing_key = self.chat("What does this document say about refunds?", headers={})
        if missing_key is None:
            return
        if missing_key.status == 401:
            self.record("missing API key rejection", "PASS", "Chat rejected a request without an API key.")
        elif missing_key.status == 200:
            self.record("missing API key rejection", "FAIL", "Chat accepted a request without an API key.")
        else:
            self.record("missing API key rejection", "FAIL", f"Expected 401, got {missing_key.status}.")

        invalid_key = self.chat(
            "What does this document say about refunds?",
            headers={"x-agentport-api-key": "ap_invalid_phase11_verification_key"},
        )
        if invalid_key is None:
            return
        if invalid_key.status == 401:
            self.record("invalid API key rejection", "PASS", "Chat rejected an invalid API key.")
        else:
            self.record("invalid API key rejection", "FAIL", f"Expected 401, got {invalid_key.status}.")

        if not self.api_key:
            self.record(
                "valid API key acceptance",
                "GAP",
                "No raw API key is available. Set AGENTPORT_API_KEY or run against a freshly reset database.",
            )
            return

        valid_key = self.chat(
            "What does this document say about refunds?",
            headers={"x-agentport-api-key": self.api_key},
        )
        if valid_key is None:
            return
        valid_body = valid_key.json()
        if valid_key.status == 200 and isinstance(valid_body, dict):
            citations = valid_body.get("citations") or []
            run_id = valid_body.get("run_id") or valid_body.get("runId")
            trace_id = valid_body.get("trace_id_record") or valid_body.get("traceIdRecord")
            if citations and run_id and trace_id:
                self.record(
                    "valid API key acceptance",
                    "PASS",
                    "Scoped API key chat returned citations, run id, and trace id.",
                )
            else:
                self.record(
                    "valid API key acceptance",
                    "FAIL",
                    "Valid-key chat response missed citations, run id, or trace id.",
                )
        else:
            self.record("valid API key acceptance", "FAIL", f"Expected 200, got {valid_key.status}.")

    def check_widget_snippet(self) -> None:
        if self.args.skip_web:
            self.record("widget snippet presence", "GAP", "Web check skipped by --skip-web.")
            return

        response = self.get(f"{self.web_url}/deployments")
        if response.status != 200:
            self.record(
                "widget snippet presence",
                "GAP",
                f"Web deployments page unavailable at {self.web_url}/deployments: {response.status}.",
            )
            return

        html = response.text.lower()
        has_iframe = "<iframe" in html or "&lt;iframe" in html
        has_playground = "playground?agentid=" in html
        has_api_example = "api/v1/agent-definitions" in html
        if has_iframe and has_playground and has_api_example:
            self.record("widget snippet presence", "PASS", "Deployments page includes API and iframe snippets.")
        else:
            missing = []
            if not has_iframe:
                missing.append("iframe markup")
            if not has_playground:
                missing.append("playground URL")
            if not has_api_example:
                missing.append("local API example")
            self.record("widget snippet presence", "FAIL", f"Missing: {', '.join(missing)}.")

        has_origin_placeholder = "allowed-origin" in html or "allowed origin" in html or "allowlist" in html
        has_rate_placeholder = "rate-limit" in html or "rate limit" in html
        if has_origin_placeholder and has_rate_placeholder:
            self.record("widget guardrail placeholders", "PASS", "Origin and rate-limit placeholders are visible.")
        else:
            self.record(
                "widget guardrail placeholders",
                "GAP",
                "Deployments snippet does not expose both allowed-origin and rate-limit placeholders.",
            )

    def check_rag_no_answer_fallback(self) -> None:
        if not self.uploaded_document_id:
            self.record("RAG no-answer fallback", "GAP", "No uploaded document is available for RAG verification.")
            return

        headers = self.auth_headers()
        response = self.chat(
            "Which lunar basalt refinery runway lighting procedure is approved for orbital tugs?",
            headers=headers,
        )
        if response is None:
            return
        body = response.json()
        if response.status == 401 and not self.api_key:
            self.record(
                "RAG no-answer fallback",
                "GAP",
                "Chat now requires an API key; set AGENTPORT_API_KEY to verify RAG fallback.",
            )
            return
        if response.status != 200 or not isinstance(body, dict):
            self.record("RAG no-answer fallback", "FAIL", f"Expected 200 JSON chat response, got {response.status}.")
            return

        answer = str(body.get("answer") or "")
        citations = body.get("citations") or []
        is_no_answer = any(marker in answer.lower() for marker in NO_ANSWER_MARKERS)
        if is_no_answer and not citations:
            self.record("RAG no-answer fallback", "PASS", "Unanswerable question returned a no-answer response without citations.")
        else:
            detail = f"Answer was {answer[:180]!r}; citation count={len(citations)}."
            self.record("RAG no-answer fallback", "FAIL", detail)

    def print_results(self) -> None:
        if self.args.json:
            payload = {
                "summary": self.summary(),
                "results": [result.__dict__ for result in self.results],
            }
            print(json.dumps(payload, indent=2, sort_keys=True))
            return

        print("Phase 1.1 verification")
        for result in self.results:
            print(f"{result.status:4}  {result.name} - {result.detail}")
        summary = self.summary()
        print(
            "Summary: "
            f"{summary['PASS']} passed, {summary['FAIL']} failed, "
            f"{summary['GAP']} gaps, {summary['SKIP']} skipped."
        )

    def summary(self) -> dict[str, int]:
        counts = {"PASS": 0, "FAIL": 0, "GAP": 0, "SKIP": 0}
        for result in self.results:
            counts[result.status] = counts.get(result.status, 0) + 1
        return counts

    def exit_code(self) -> int:
        if self.args.report_only:
            return 0
        summary = self.summary()
        if summary["FAIL"] > 0:
            return 1
        if summary["GAP"] > 0 and not self.args.allow_gaps:
            return 2
        return 0


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run Phase 1.1 local verification checks.")
    parser.add_argument("--platform-url", default=os.getenv("PLATFORM_API_URL", "http://localhost:5001"))
    parser.add_argument("--ai-url", default=os.getenv("AI_SERVICES_URL", "http://localhost:5002"))
    parser.add_argument("--web-url", default=os.getenv("WEB_URL", "http://127.0.0.1:3002"))
    parser.add_argument("--sample-file", default=os.getenv("SAMPLE_FILE", str(DEFAULT_SAMPLE_FILE)))
    parser.add_argument("--api-key", default=os.getenv("AGENTPORT_API_KEY"))
    parser.add_argument("--reset-command", default=os.getenv("AGENTPORT_RESET_COMMAND"))
    parser.add_argument("--timeout", type=float, default=float(os.getenv("AGENTPORT_VERIFY_TIMEOUT", "15")))
    parser.add_argument("--reset-timeout", type=float, default=float(os.getenv("AGENTPORT_RESET_TIMEOUT", "120")))
    parser.add_argument("--skip-web", action="store_true", help="Skip the deployments page check.")
    parser.add_argument("--allow-gaps", action="store_true", help="Return success when only GAP checks remain.")
    parser.add_argument("--report-only", action="store_true", help="Always exit 0 after printing results.")
    parser.add_argument("--json", action="store_true", help="Print machine-readable JSON.")
    return parser.parse_args(argv)


def extract_last_json_object(text: str) -> dict[str, Any] | None:
    decoder = json.JSONDecoder()
    parsed: dict[str, Any] | None = None
    for index, char in enumerate(text):
        if char != "{":
            continue
        try:
            value, _ = decoder.raw_decode(text[index:])
        except json.JSONDecodeError:
            continue
        if isinstance(value, dict):
            parsed = value
    return parsed


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    return Harness(args).run()


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
