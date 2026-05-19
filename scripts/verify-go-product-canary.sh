#!/usr/bin/env sh
set -eu

BASE_URL="${1:-http://localhost:5081}"
BASE_URL="${BASE_URL%/}"

check_json() {
  endpoint="$1"
  description="$2"
  mode="$3"

  response="$(curl -fsS "$BASE_URL$endpoint")"
  RESPONSE="$response" python3 - "$description" "$mode" <<'PY'
import json
import os
import sys

description = sys.argv[1]
mode = sys.argv[2]
payload = json.loads(os.environ["RESPONSE"])
if payload.get("success") is not True:
    raise SystemExit(f"{description}: expected success=true, got {payload!r}")
data = payload.get("data")
if mode == "items":
    if not isinstance(data, dict) or not data.get("items"):
        raise SystemExit(f"{description}: expected non-empty items, got {payload!r}")
elif mode == "object":
    if not isinstance(data, dict) or not data:
        raise SystemExit(f"{description}: expected object data, got {payload!r}")
print(f"ok: {description}")
PY
}

check_json "/api/v1/products?page=1&pageSize=10" "product list" "items"
check_json "/api/v1/products/1" "product detail" "object"
check_json "/api/v1/products/user/1?page=1&pageSize=10" "product user list" "items"
