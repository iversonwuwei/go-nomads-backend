#!/usr/bin/env bash

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
COMPOSE_FILE="$PROJECT_ROOT/docker-compose-infras-swr.yml"
DEFAULT_SERVICES=(redis rabbitmq elasticsearch nginx)
USE_SWR=""
USE_MIRROR=""
MIRROR_PREFIX="${MIRROR_PREFIX:-docker.1ms.run}"

if [[ ! -f "$COMPOSE_FILE" ]]; then
  echo "Compose file not found: $COMPOSE_FILE" >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required." >&2
  exit 1
fi

set_swr_images() {
  local registry="${SWR_LOGIN_SERVER:-${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}}"
  local organization="${SWR_ORGANIZATION:-go-nomads}"
  export REDIS_IMAGE="$registry/$organization/redis:7.4"
  export RABBITMQ_IMAGE="$registry/$organization/rabbitmq:3-management-alpine"
  export ELASTICSEARCH_IMAGE="$registry/$organization/elasticsearch:8.17.4"
  export NGINX_IMAGE="$registry/$organization/nginx:1.29.6"
}

set_mirror_images() {
  export REDIS_IMAGE="$MIRROR_PREFIX/library/redis:7.4"
  export RABBITMQ_IMAGE="$MIRROR_PREFIX/library/rabbitmq:3-management-alpine"
  export ELASTICSEARCH_IMAGE="${SWR_LOGIN_SERVER:-${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}}/${SWR_ORGANIZATION:-go-nomads}/elasticsearch:8.17.4"
  export NGINX_IMAGE="$MIRROR_PREFIX/library/nginx:1.29.6"
}

if [[ "$(uname -s)" == "Linux" ]]; then
  USE_SWR="1"
fi

while [[ $# -gt 0 ]]; do
  case "$1" in
    --use-swr)
      USE_SWR="1"
      USE_MIRROR=""
      shift
      ;;
    --use-mirror)
      USE_MIRROR="1"
      USE_SWR=""
      shift
      ;;
    --use-official)
      USE_SWR=""
      USE_MIRROR=""
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
elif [[ -n "$USE_MIRROR" ]]; then
  set_mirror_images
  IMAGE_SOURCE="mirror"
fi

echo "Using compose file: $COMPOSE_FILE"
echo "Image source: $IMAGE_SOURCE"
echo "Registry: ${SWR_LOGIN_SERVER:-${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}}"
echo "Mirror prefix: $MIRROR_PREFIX"
echo "Org: ${SWR_ORGANIZATION:-go-nomads}"
echo "Default services: ${DEFAULT_SERVICES[*]}"

if [[ $# -eq 0 ]]; then
  set -- up -d "${DEFAULT_SERVICES[@]}"
fi

docker compose -f "$COMPOSE_FILE" "$@"
