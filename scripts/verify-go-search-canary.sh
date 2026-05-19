#!/usr/bin/env sh
set -eu

BASE_URL="${1:-http://localhost:5081}"
BASE_URL="${BASE_URL%/}"

check() {
  endpoint="$1"
  description="$2"
  data_key="$3"
  response="$(curl -fsS "$BASE_URL$endpoint")"
  RESPONSE="$response" python3 - "$description" "$data_key" <<'PY'
import json
import os
import sys

description = sys.argv[1]
data_key = sys.argv[2]
payload = json.loads(os.environ["RESPONSE"])
if payload.get("success") is not True:
    raise SystemExit(f"{description}: expected success=true, got {payload!r}")
data = payload.get("data")
if not isinstance(data, dict) or data_key not in data:
    raise SystemExit(f"{description}: missing data key {data_key}: {payload!r}")
print(f"ok: {description}")
PY
}

check "/api/v1/search?query=nomad&type=all&page=1&pageSize=20" "search all" "totalCount"
check "/api/v1/search/cities?query=lisbon&page=1&pageSize=20" "search cities" "items"
check "/api/v1/search/coworkings?query=hub&page=1&pageSize=20" "search coworkings" "items"
check "/api/v1/search/suggest?prefix=li&type=all&size=10" "search suggest" "suggestions"