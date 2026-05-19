#!/usr/bin/env sh
set -eu

BASE_URL="${1:-http://localhost:5081}"
BASE_URL="${BASE_URL%/}"

check_json() {
  endpoint="$1"
  description="$2"

  response="$(curl -fsS "$BASE_URL$endpoint")"
  RESPONSE="$response" python3 - "$description" <<'PY'
import json
import os
import sys

description = sys.argv[1]
payload = json.loads(os.environ["RESPONSE"])
if payload.get("success") is not True:
    raise SystemExit(f"{description}: expected success=true, got {payload!r}")
if payload.get("data") in (None, {}, []):
    raise SystemExit(f"{description}: expected non-empty data, got {payload!r}")
print(f"ok: {description}")
PY
}

check_json "/api/v1/app/config?locale=zh-CN" "app config"
check_json "/api/v1/app/config/version" "app config version"
