#!/usr/bin/env bash
set -euo pipefail

# Wrapper to deploy using Huawei Cloud SWR registry defaults.
# Usage: ./deploy-swr.sh [env] [action]
#   env: dev|staging|prod (default: dev)
#   action: deploy|delete|status|build|services|infrastructure|monitoring|dapr (default: deploy)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEFAULT_REGISTRY="swr.ap-southeast-3.myhuaweicloud.com/go-nomads"

export DOCKER_REGISTRY="${DOCKER_REGISTRY:-$DEFAULT_REGISTRY}"
export IMAGE_TAG="${IMAGE_TAG:-latest}"

ENVIRONMENT="${1:-dev}"
ACTION="${2:-deploy}"

echo "DOCKER_REGISTRY=${DOCKER_REGISTRY}"
echo "IMAGE_TAG=${IMAGE_TAG}"

exec "$SCRIPT_DIR/deploy.sh" "$ENVIRONMENT" "$ACTION"
