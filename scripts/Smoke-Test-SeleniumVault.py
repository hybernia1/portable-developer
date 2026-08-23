#!/usr/bin/env python3
"""Smoke-test a Portable Developer Selenium cookie vault without exposing cookie data."""

from __future__ import annotations

import argparse
import json
import sys
import time
import urllib.error
import urllib.parse
import urllib.request


def webdriver_request(base_url: str, method: str, path: str, payload: object | None = None) -> dict:
    body = None if payload is None else json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(
        f"{base_url.rstrip('/')}{path}",
        data=body,
        method=method,
        headers={"Content-Type": "application/json; charset=utf-8"},
    )
    try:
        with urllib.request.urlopen(request, timeout=45) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as error:
        raise RuntimeError(f"WebDriver request failed with HTTP {error.code}.") from None
    except (urllib.error.URLError, TimeoutError) as error:
        raise RuntimeError(f"WebDriver is unavailable: {error.reason}.") from None


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--vault-id", required=True)
    parser.add_argument("--url", required=True, help="HTTPS page covered by at least one vault cookie.")
    parser.add_argument("--expected-host", help="Defaults to the hostname from --url.")
    parser.add_argument("--grid", default="http://127.0.0.1:4444")
    arguments = parser.parse_args()

    parsed_url = urllib.parse.urlparse(arguments.url)
    if parsed_url.scheme != "https" or not parsed_url.hostname:
        raise ValueError("--url must be an absolute HTTPS URL.")
    expected_host = (arguments.expected_host or parsed_url.hostname).lower()

    session_id = None
    try:
        created = webdriver_request(
            arguments.grid,
            "POST",
            "/session",
            {
                "capabilities": {
                    "alwaysMatch": {
                        "browserName": "chrome",
                        "portable:vault": arguments.vault_id,
                        "goog:chromeOptions": {
                            "args": ["--headless=new", "--disable-gpu", "--window-size=1280,900"]
                        },
                    }
                }
            },
        )
        value = created.get("value") or {}
        session_id = value.get("sessionId") or created.get("sessionId")
        if not session_id:
            raise RuntimeError("WebDriver did not return a session identifier.")

        webdriver_request(
            arguments.grid,
            "POST",
            f"/session/{session_id}/url",
            {"url": arguments.url},
        )
        time.sleep(2)
        cookies = webdriver_request(
            arguments.grid,
            "GET",
            f"/session/{session_id}/cookie",
        ).get("value") or []
        cookie_count = len(cookies) if isinstance(cookies, list) else 0
        cookies = None
        page = webdriver_request(
            arguments.grid,
            "POST",
            f"/session/{session_id}/execute/sync",
            {
                "script": "return { host: location.hostname, readyState: document.readyState };",
                "args": [],
            },
        ).get("value") or {}

        actual_host = str(page.get("host") or "").lower()
        host_matches = actual_host == expected_host or actual_host.endswith(f".{expected_host}")
        result = {
            "sessionCreatedWithVault": True,
            "hostMatches": host_matches,
            "pageLoaded": page.get("readyState") == "complete",
            "browserCookieCount": cookie_count,
        }
        print(json.dumps(result, indent=2))
        return 0 if host_matches and result["pageLoaded"] and cookie_count > 0 else 2
    except RuntimeError as error:
        print(f"Smoke test failed: {error}", file=sys.stderr)
        return 1
    finally:
        if session_id:
            try:
                webdriver_request(arguments.grid, "DELETE", f"/session/{session_id}")
            except RuntimeError:
                print("Warning: the test session could not be closed cleanly.", file=sys.stderr)


if __name__ == "__main__":
    raise SystemExit(main())
