#!/usr/bin/env sh
set -eu

BASE_URL="${1:-http://localhost:5081}"
BASE_URL="${BASE_URL%/}"

response="$(curl -fsS "$BASE_URL/api/v1/cities/region-tabs")"
RESPONSE="$response" python3 - <<'PY'
import json
import os

payload = json.loads(os.environ["RESPONSE"])
if payload.get("success") is not True:
    raise SystemExit(f"city region tabs: expected success=true, got {payload!r}")
data = payload.get("data")
if not isinstance(data, list) or not data:
    raise SystemExit(f"city region tabs: expected non-empty list, got {payload!r}")
first = data[0]
if not isinstance(first, dict) or "key" not in first or "cityCount" not in first:
    raise SystemExit(f"city region tabs: missing expected fields, got {payload!r}")
print("ok: city region tabs")
PY
