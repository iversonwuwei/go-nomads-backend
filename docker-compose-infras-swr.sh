#!/usr/bin/env bash

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
COMPOSE_FILE="$PROJECT_ROOT/docker-compose-infras-swr.yml"
DEFAULT_SERVICES=(redis rabbitmq elasticsearch)

if [[ ! -f "$COMPOSE_FILE" ]]; then
  echo "Compose file not found: $COMPOSE_FILE" >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required." >&2
  exit 1
fi

echo "Using compose file: $COMPOSE_FILE"
echo "Registry: ${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}"
echo "Org: ${SWR_ORGANIZATION:-go-nomads}"
echo "Default services: ${DEFAULT_SERVICES[*]}"

if [[ $# -eq 0 ]]; then
  set -- up -d "${DEFAULT_SERVICES[@]}"
elif [[ "$1" == "--with-consul" ]]; then
  shift
  if [[ $# -eq 0 ]]; then
    set -- up -d "${DEFAULT_SERVICES[@]}" consul
  fi
fi

docker compose -f "$COMPOSE_FILE" "$@"
