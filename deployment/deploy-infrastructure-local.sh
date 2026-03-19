#!/bin/bash
# Go-Nomads Local Infrastructure Deployment (Docker only)
# Usage: bash deploy-infrastructure-local.sh [start|stop|restart|status|clean|help]

set -euo pipefail

NETWORK_NAME="go-nomads-network"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RUNTIME="docker"
SWR_REGISTRY="${SWR_LOGIN_SERVER:-${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}}"
SWR_ORGANIZATION="${SWR_ORGANIZATION:-go-nomads}"
USE_SWR=""
USE_MIRROR=""
MIRROR_PREFIX="${MIRROR_PREFIX:-docker.1ms.run}"
REDIS_IMAGE="${REDIS_IMAGE:-redis:7.2-alpine}"
RABBITMQ_IMAGE="${RABBITMQ_IMAGE:-rabbitmq:3-management-alpine}"
ELASTICSEARCH_IMAGE="${ELASTICSEARCH_IMAGE:-docker.elastic.co/elasticsearch/elasticsearch:8.17.4}"
NGINX_IMAGE="${NGINX_IMAGE:-nginx:1.29.6}"
RABBITMQ_DEFAULT_USER="${RABBITMQ_DEFAULT_USER:-walden}"
RABBITMQ_DEFAULT_PASS="${RABBITMQ_DEFAULT_PASS:-walden}"

set_swr_images() {
    REDIS_IMAGE="${SWR_REGISTRY}/${SWR_ORGANIZATION}/redis:7.2-alpine"
    RABBITMQ_IMAGE="${SWR_REGISTRY}/${SWR_ORGANIZATION}/rabbitmq:3-management-alpine"
    ELASTICSEARCH_IMAGE="${SWR_REGISTRY}/${SWR_ORGANIZATION}/elasticsearch:8.17.4"
    NGINX_IMAGE="${SWR_REGISTRY}/${SWR_ORGANIZATION}/nginx:1.29.6"
}

set_mirror_images() {
    REDIS_IMAGE="${MIRROR_PREFIX}/library/redis:7.2-alpine"
    RABBITMQ_IMAGE="${MIRROR_PREFIX}/library/rabbitmq:3-management-alpine"
    ELASTICSEARCH_IMAGE="${SWR_REGISTRY}/${SWR_ORGANIZATION}/elasticsearch:8.17.4"
    NGINX_IMAGE="${MIRROR_PREFIX}/library/nginx:1.29.6"
}

if [[ "$(uname -s)" == "Linux" ]]; then
    USE_SWR="1"
fi

require_docker() {
    if ! command -v docker >/dev/null 2>&1; then
        echo "[ERROR] Docker is required for this script." >&2
        exit 1
    fi
}

header() {
    echo
    echo "============================================================"
    echo "  $1"
    echo "============================================================"
    echo
}

network_exists() {
    docker network ls --filter "name=${NETWORK_NAME}" --format '{{.Name}}' | grep -q "^${NETWORK_NAME}$"
}

create_network() {
    header "Creating Docker network"
    if network_exists; then
        echo "Network '${NETWORK_NAME}' already exists."
    else
        docker network create "${NETWORK_NAME}" >/dev/null
        echo "Network '${NETWORK_NAME}' created."
    fi
}

container_exists() {
    docker ps -a --filter "name=$1" --format '{{.Names}}' | grep -q "^$1$"
}

container_running() {
    docker ps --filter "name=$1" --format '{{.Names}}' | grep -q "^$1$"
}

remove_container() {
    local name="$1"
    if container_exists "$name"; then
        echo "Removing container ${name}..."
        docker rm -f "$name" >/dev/null
    fi
}

start_redis() {
    header "Deploying Redis"
    remove_container go-nomads-redis
    docker run -d \
        --name go-nomads-redis \
        --network "${NETWORK_NAME}" \
        --label "com.docker.compose.project=go-nomads-infras" \
        --label "com.docker.compose.service=redis" \
        -p 6379:6379 \
        "${REDIS_IMAGE}" redis-server --appendonly yes >/dev/null
    echo "Redis running at redis://localhost:6379"
}

start_elasticsearch() {
    header "Deploying Elasticsearch"
    remove_container go-nomads-elasticsearch
    docker run -d \
        --name go-nomads-elasticsearch \
        --network "${NETWORK_NAME}" \
        --label "com.docker.compose.project=go-nomads-infras" \
        --label "com.docker.compose.service=elasticsearch" \
        -p 9200:9200 \
        -p 9300:9300 \
        -e "discovery.type=single-node" \
        -e "xpack.security.enabled=false" \
        -e "ES_JAVA_OPTS=-Xms512m -Xmx512m" \
        "${ELASTICSEARCH_IMAGE}" >/dev/null
    echo "Elasticsearch available at http://localhost:9200"
}

start_rabbitmq() {
    header "Deploying RabbitMQ"
    remove_container go-nomads-rabbitmq
    docker run -d \
        --name go-nomads-rabbitmq \
        --network "${NETWORK_NAME}" \
        --label "com.docker.compose.project=go-nomads-infras" \
        --label "com.docker.compose.service=rabbitmq" \
        -p 5672:5672 \
        -p 15672:15672 \
        -e RABBITMQ_DEFAULT_USER="${RABBITMQ_DEFAULT_USER}" \
        -e RABBITMQ_DEFAULT_PASS="${RABBITMQ_DEFAULT_PASS}" \
        "${RABBITMQ_IMAGE}" >/dev/null
    echo "RabbitMQ running at amqp://localhost:5672"
    echo "RabbitMQ Management UI: http://localhost:15672 (${RABBITMQ_DEFAULT_USER}/${RABBITMQ_DEFAULT_PASS})"
}

start_nginx() {
    header "Deploying Nginx"
    remove_container go-nomads-nginx
    local nginx_conf="${SCRIPT_DIR}/nginx/nginx.conf"
    if [[ ! -f "$nginx_conf" ]]; then
        echo "[WARNING] Nginx config not found: $nginx_conf"
        echo "Skipping Nginx deployment. Run deploy-services-local.sh to deploy Nginx with gateway."
        return 0
    fi
    docker run -d \
        --name go-nomads-nginx \
        --network "${NETWORK_NAME}" \
        --label "com.docker.compose.project=go-nomads-infras" \
        --label "com.docker.compose.service=nginx" \
        -p 80:80 \
        -p 443:443 \
        -v "${nginx_conf}:/etc/nginx/conf.d/default.conf:ro" \
        --restart unless-stopped \
        "${NGINX_IMAGE}" >/dev/null
    echo "Nginx running at http://localhost"
}

start_all() {
    header "Go-Nomads Local Infrastructure"
    require_docker
    create_network
    start_redis
    start_rabbitmq
    start_elasticsearch
    start_nginx
    status_all
    echo "Infrastructure ready."
}

stop_all() {
    header "Stopping local infrastructure"
    require_docker
    local containers=(
        go-nomads-nginx
        go-nomads-elasticsearch
        go-nomads-rabbitmq
        go-nomads-redis
    )
    for c in "${containers[@]}"; do
        if container_running "$c"; then
            echo "Stopping $c..."
            docker stop "$c" >/dev/null
        fi
    done
}

clean_all() {
    header "Cleaning local infrastructure"
    require_docker
    stop_all
    local containers=(
        go-nomads-nginx
        go-nomads-elasticsearch
        go-nomads-rabbitmq
        go-nomads-redis
    )
    for c in "${containers[@]}"; do
        remove_container "$c"
    done
    if network_exists; then
        echo "Removing network ${NETWORK_NAME}..."
        docker network rm "${NETWORK_NAME}" >/dev/null
    fi
    echo "Clean complete."
}

status_all() {
    header "Infrastructure status"
    require_docker
    docker ps --filter "name=go-nomads" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    echo
    echo "Access URLs:"
    echo "  Nginx:          http://localhost"
    echo "  Redis:          redis://localhost:6379"
    echo "  Elasticsearch:  http://localhost:9200"
}

show_help() {
    cat <<'EOF'
Go-Nomads Local Infrastructure Deployment

Usage:
  bash deploy-infrastructure-local.sh [command]

Commands:
  start    Deploy all infrastructure containers (default)
  stop     Stop infrastructure containers
  restart  Restart infrastructure containers
  status   Show running status
  clean    Remove containers, network, and configs
  help     Show this message
EOF
}

ACTION="${1:-start}"
if [[ "$ACTION" == "--use-swr" || "$ACTION" == "--use-mirror" || "$ACTION" == "--use-official" ]]; then
    ACTION="start"
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

ACTION="${1:-start}"

if [[ -n "$USE_SWR" ]]; then
    set_swr_images
elif [[ -n "$USE_MIRROR" ]]; then
    set_mirror_images
fi

case "$ACTION" in
    start)
        start_all
        ;;
    stop)
        stop_all
        ;;
    restart)
        stop_all
        start_all
        ;;
    status)
        status_all
        ;;
    clean)
        clean_all
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        echo "Unknown command: $ACTION" >&2
        show_help
        exit 1
        ;;
esac
