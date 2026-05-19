#!/usr/bin/env sh
set -eu

BASE_URL="${1:-http://localhost:5081}"
TOKEN="${2:-}"
BASE_URL="${BASE_URL%/}"

if [ -z "$TOKEN" ]; then
  echo "usage: $0 BASE_URL JWT_TOKEN" >&2
  exit 1
fi

check_get() {
  endpoint="$1"
  field="$2"
  description="$3"
  response="$(curl -fsS -H "Authorization: Bearer $TOKEN" "$BASE_URL$endpoint")"
  RESPONSE="$response" python3 - "$field" "$description" <<'PY'
import json
import os
import sys

field = sys.argv[1]
description = sys.argv[2]
payload = json.loads(os.environ["RESPONSE"])
if field not in payload:
    raise SystemExit(f"{description}: missing field {field}: {payload!r}")
print(f"ok: {description}")
PY
}

check_post_batch() {
  endpoint="$1"
  collection="$2"
  description="$3"
  body="$4"
  response="$(curl -fsS -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "$body" "$BASE_URL$endpoint")"
  RESPONSE="$response" python3 - "$collection" "$description" <<'PY'
import json
import os
import sys

collection = sys.argv[1]
description = sys.argv[2]
payload = json.loads(os.environ["RESPONSE"])
items = payload.get(collection)
if not isinstance(items, list) or not items:
    raise SystemExit(f"{description}: expected non-empty {collection}: {payload!r}")
print(f"ok: {description}")
PY
}

check_get "/api/v1/cache/costs/city/city-1" "averageCost" "cache city cost"
check_post_batch "/api/v1/cache/costs/city/batch" "costs" "cache city cost batch" '["city-1","city-2"]'
check_get "/api/v1/cache/scores/city/city-1" "overallScore" "cache city score"
check_post_batch "/api/v1/cache/scores/city/batch" "scores" "cache city score batch" '["city-1","city-2"]'
check_get "/api/v1/cache/scores/coworking/cow-1" "overallScore" "cache coworking score"
check_post_batch "/api/v1/cache/scores/coworking/batch" "scores" "cache coworking score batch" '["cow-1","cow-2"]'