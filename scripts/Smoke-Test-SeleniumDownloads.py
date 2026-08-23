#!/usr/bin/env python3
"""Exercise Portable Developer's persistent Selenium download policy without third-party packages."""

from __future__ import annotations

import argparse
import json
import threading
import time
import urllib.error
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


PAYLOAD = b"Portable Developer Selenium download smoke test.\n"


class DownloadHandler(BaseHTTPRequestHandler):
    filename = "portable-developer-download-smoke.txt"

    def do_GET(self) -> None:  # noqa: N802 - BaseHTTPRequestHandler API
        if self.path == "/file":
            self.send_response(200)
            self.send_header("Content-Type", "application/octet-stream")
            self.send_header("Content-Disposition", f'attachment; filename="{self.filename}"')
            self.send_header("Content-Length", str(len(PAYLOAD)))
            self.end_headers()
            self.wfile.write(PAYLOAD)
            return

        page = b'<html><body><a id="download" href="/file">download</a></body></html>'
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(page)))
        self.end_headers()
        self.wfile.write(page)

    def log_message(self, _format: str, *args: object) -> None:
        return


def webdriver(url: str, method: str, path: str, payload: object | None = None) -> dict:
    body = None if payload is None else json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(
        url.rstrip("/") + path,
        data=body,
        method=method,
        headers={"Content-Type": "application/json; charset=utf-8"},
    )
    try:
        with urllib.request.urlopen(request, timeout=20) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as error:
        detail = error.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"WebDriver returned HTTP {error.code}: {detail}") from error


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--grid", default="http://127.0.0.1:55555")
    parser.add_argument("--downloads", required=True, type=Path)
    parser.add_argument("--expect", required=True, choices=("allowed", "blocked"))
    parser.add_argument("--filename", default=DownloadHandler.filename)
    args = parser.parse_args()

    if Path(args.filename).name != args.filename:
        raise ValueError("--filename must be a plain file name.")
    DownloadHandler.filename = args.filename

    downloads = args.downloads.resolve()
    downloads.mkdir(parents=True, exist_ok=True)
    target = downloads / DownloadHandler.filename
    if target.exists():
        raise FileExistsError(f"Refusing to overwrite existing smoke-test file: {target}")

    server = ThreadingHTTPServer(("127.0.0.1", 0), DownloadHandler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    session_id: str | None = None
    try:
        created = webdriver(
            args.grid,
            "POST",
            "/session",
            {
                "capabilities": {
                    "alwaysMatch": {
                        "browserName": "chrome",
                        "goog:chromeOptions": {"args": ["--headless=new"]},
                    }
                }
            },
        )
        session_id = created["value"]["sessionId"]
        try:
            webdriver(
                args.grid,
                "POST",
                f"/session/{session_id}/goog/cdp/execute",
                {
                    "cmd": "Browser.setDownloadBehavior",
                    "params": {
                        "behavior": "allow",
                        "downloadPath": str(downloads.parent / "forbidden-client-downloads"),
                    },
                },
            )
        except RuntimeError as error:
            if "HTTP 403" not in str(error):
                raise
        else:
            raise RuntimeError("The Selenium client was able to override the app-owned download policy.")

        webdriver(
            args.grid,
            "POST",
            f"/session/{session_id}/url",
            {"url": f"http://127.0.0.1:{server.server_port}/"},
        )
        element = webdriver(
            args.grid,
            "POST",
            f"/session/{session_id}/element",
            {"using": "css selector", "value": "#download"},
        )["value"]["element-6066-11e4-a52e-4f735466cecf"]
        webdriver(args.grid, "POST", f"/session/{session_id}/element/{element}/click", {})

        deadline = time.monotonic() + 8
        while time.monotonic() < deadline and not target.exists():
            time.sleep(0.1)

        downloaded = target.exists()
        if args.expect == "allowed":
            if not downloaded or target.read_bytes() != PAYLOAD:
                raise RuntimeError("The expected project download was not preserved with the correct content.")
        elif downloaded:
            raise RuntimeError("A file was written even though Selenium downloads were disabled.")
    finally:
        if session_id is not None:
            try:
                webdriver(args.grid, "DELETE", f"/session/{session_id}")
            except Exception:
                pass
        server.shutdown()
        server.server_close()

    if args.expect == "allowed" and not target.exists():
        raise RuntimeError("The downloaded file disappeared after the Selenium session ended.")
    print(f"PASS: Selenium downloads are {args.expect}; persistent file present={target.exists()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
