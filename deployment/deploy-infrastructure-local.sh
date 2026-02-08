#!/bin/bash
# Go-Nomads Local Infrastructure Deployment (Docker only)
# Usage: bash deploy-infrastructure-local.sh [start|stop|restart|status|clean|help]

set -euo pipefail

NETWORK_NAME="go-nomads-network"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RUNTIME="docker"

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
        redis:latest redis-server --appendonly yes >/dev/null
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
        docker.elastic.co/elasticsearch/elasticsearch:8.16.1 >/dev/null
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
        -e RABBITMQ_DEFAULT_USER=walden \
        -e RABBITMQ_DEFAULT_PASS=walden \
        rabbitmq:3-management-alpine >/dev/null
    echo "RabbitMQ running at amqp://localhost:5672"
    echo "RabbitMQ Management UI: http://localhost:15672 (walden/walden)"
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
        nginx:alpine >/dev/null
    echo "Nginx running at http://localhost"
}

start_aspire_dashboard() {
    header "Deploying Aspire Dashboard"
    remove_container go-nomads-aspire-dashboard
    docker run -d \
        --name go-nomads-aspire-dashboard \
        --network "${NETWORK_NAME}" \
        --label "com.docker.compose.project=go-nomads-infras" \
        --label "com.docker.compose.service=aspire-dashboard" \
        -p 18888:18888 \
        -p 4317:18889 \
        -e DASHBOARD__FRONTEND__AUTHMODE=Unsecured \
        -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true \
        mcr.microsoft.com/dotnet/aspire-dashboard:9.0 >/dev/null
    echo "Aspire Dashboard running at http://localhost:18888"
    echo "OTLP gRPC endpoint: http://localhost:4317 (container: http://go-nomads-aspire-dashboard:18889)"
}

start_all() {
    header "Go-Nomads Local Infrastructure"
    require_docker
    create_network
    start_redis
    start_rabbitmq
    start_elasticsearch
    start_nginx
    start_aspire_dashboard
    status_all
    echo "Infrastructure ready."
}

stop_all() {
    header "Stopping local infrastructure"
    require_docker
    local containers=(
        go-nomads-aspire-dashboard
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
        go-nomads-aspire-dashboard
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
    echo "  Nginx:            http://localhost"
    echo "  Redis:            redis://localhost:6379"
    echo "  RabbitMQ:         amqp://localhost:5672"
    echo "  RabbitMQ UI:      http://localhost:15672"
    echo "  Elasticsearch:    http://localhost:9200"
    echo "  Aspire Dashboard: http://localhost:18888"
    echo "  OTLP Endpoint:    http://localhost:4317"
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
