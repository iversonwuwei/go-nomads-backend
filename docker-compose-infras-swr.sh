#!/usr/bin/env bash

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
COMPOSE_FILE="$PROJECT_ROOT/docker-compose-infras-swr.yml"
DEFAULT_SERVICES=(redis rabbitmq elasticsearch)
USE_SWR=""

if [[ ! -f "$COMPOSE_FILE" ]]; then
  echo "Compose file not found: $COMPOSE_FILE" >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required." >&2
  exit 1
fi

set_swr_images() {
  local registry="${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}"
  local organization="${SWR_ORGANIZATION:-go-nomads}"
  export REDIS_IMAGE="$registry/$organization/redis:7-alpine"
  export RABBITMQ_IMAGE="$registry/$organization/rabbitmq:3-management-alpine"
  export ELASTICSEARCH_IMAGE="$registry/$organization/elasticsearch:8.11.0"
  export CONSUL_IMAGE="$registry/$organization/consul:latest"
}

if [[ "$(uname -s)" == "Linux" ]]; then
  USE_SWR="1"
fi

while [[ $# -gt 0 ]]; do
  case "$1" in
    --use-swr)
      USE_SWR="1"
      shift
      ;;
    --use-official)
      USE_SWR=""
      shift
      ;;
    *)
      break
      ;;
  esac
done

IMAGE_SOURCE="official"

if [[ -n "$USE_SWR" ]]; then
  set_swr_images
  IMAGE_SOURCE="swr"
fi

echo "Using compose file: $COMPOSE_FILE"
echo "Image source: $IMAGE_SOURCE"
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
