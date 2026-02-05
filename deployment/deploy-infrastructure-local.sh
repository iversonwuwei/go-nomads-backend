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

start_consul() {
    header "Deploying Consul"
    remove_container go-nomads-consul
    local consul_dir="${SCRIPT_DIR}/consul"
    mkdir -p "${consul_dir}"
    cat > "${consul_dir}/consul-local.json" <<'EOF'
{
  "datacenter": "dc1",
  "server": true,
  "bootstrap_expect": 1,
  "data_dir": "/consul/data",
  "ui_config": { "enabled": true },
  "log_level": "INFO",
  "ports": {
    "http": 7500,
    "grpc": 7502,
    "dns": 7600
  },
  "addresses": {
    "http": "0.0.0.0",
    "grpc": "0.0.0.0",
    "dns": "0.0.0.0"
  }
}
EOF
    docker run -d \
        --name go-nomads-consul \
        --network "${NETWORK_NAME}" \
        --label "com.docker.compose.project=go-nomads-infras" \
        --label "com.docker.compose.service=consul" \
        -p 7500:7500 \
        -p 7502:7502 \
        -p 7600:7600/udp \
        -v "${consul_dir}/consul-local.json:/consul/config/consul.json:ro" \
        hashicorp/consul:latest agent -config-file=/consul/config/consul.json >/dev/null
    echo "Consul UI available at http://localhost:7500"
}

start_jaeger() {
    header "Deploying Jaeger"
    remove_container go-nomads-jaeger
    docker run -d \
        --name go-nomads-jaeger \
        --network "${NETWORK_NAME}" \
        --label "com.docker.compose.project=go-nomads-infras" \
        --label "com.docker.compose.service=jaeger" \
        -e COLLECTOR_OTLP_ENABLED=true \
        -p 16686:16686 \
        -p 4317:4317 \
        -p 4318:4318 \
        -p 9411:9411 \
        jaegertracing/all-in-one:latest \
        --memory.max-traces=100000 >/dev/null
    echo "Jaeger UI available at http://localhost:16686"
    echo "OTLP gRPC: localhost:4317, OTLP HTTP: localhost:4318"
}

start_prometheus() {
    header "Deploying Prometheus"
    remove_container go-nomads-prometheus
    local prom_dir="${SCRIPT_DIR}/prometheus"
    mkdir -p "${prom_dir}"
    cat > "${prom_dir}/prometheus-local.yml" <<'EOF'
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  external_labels:
    cluster: 'go-nomads'
    env: 'development'

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  # Gateway
  - job_name: 'gateway'
    metrics_path: /metrics
    static_configs:
      - targets: ['host.docker.internal:5000']
        labels:
          service: 'gateway'

  # User Service
  - job_name: 'user-service'
    metrics_path: /metrics
    static_configs:
      - targets: ['host.docker.internal:5001']
        labels:
          service: 'user-service'

  # City Service
  - job_name: 'city-service'
    metrics_path: /metrics
    static_configs:
      - targets: ['host.docker.internal:8002']
        labels:
          service: 'city-service'

  # Accommodation Service
  - job_name: 'accommodation-service'
    metrics_path: /metrics
    static_configs:
      - targets: ['host.docker.internal:8003']
        labels:
          service: 'accommodation-service'

  # Coworking Service
  - job_name: 'coworking-service'
    metrics_path: /metrics
    static_configs:
      - targets: ['host.docker.internal:8004']
        labels:
          service: 'coworking-service'

  # Event Service
  - job_name: 'event-service'
    metrics_path: /metrics
    static_configs:
      - targets: ['host.docker.internal:8005']
        labels:
          service: 'event-service'

  # AI Service
  - job_name: 'ai-service'
    metrics_path: /metrics
    static_configs:
      - targets: ['host.docker.internal:8006']
        labels:
          service: 'ai-service'

  # Message Service
  - job_name: 'message-service'
    metrics_path: /metrics
    static_configs:
      - targets: ['host.docker.internal:8007']
        labels:
          service: 'message-service'

  # Search Service
  - job_name: 'search-service'
    metrics_path: /metrics
    static_configs:
      - targets: ['host.docker.internal:8008']
        labels:
          service: 'search-service'

  # Jaeger
  - job_name: 'jaeger'
    static_configs:
      - targets: ['go-nomads-jaeger:14269']
        labels:
          service: 'jaeger'
EOF
    docker run -d \
        --name go-nomads-prometheus \
        --network "${NETWORK_NAME}" \
        --label "com.docker.compose.project=go-nomads-infras" \
        --label "com.docker.compose.service=prometheus" \
        --add-host host.docker.internal:host-gateway \
        -p 9090:9090 \
        -v "${prom_dir}/prometheus-local.yml:/etc/prometheus/prometheus.yml:ro" \
        prom/prometheus:v2.49.1 \
        --config.file=/etc/prometheus/prometheus.yml \
        --storage.tsdb.path=/prometheus \
        --web.enable-lifecycle >/dev/null
    echo "Prometheus UI available at http://localhost:9090"
}

start_grafana() {
    header "Deploying Grafana"
    remove_container go-nomads-grafana
    local grafana_dir="${SCRIPT_DIR}/grafana"
    local datasources_dir="${grafana_dir}/provisioning/datasources"
    mkdir -p "${datasources_dir}"
    
    # Create datasources config
    cat > "${datasources_dir}/datasources.yml" <<'EOF'
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://go-nomads-prometheus:9090
    isDefault: true
    editable: false

  - name: Jaeger
    type: jaeger
    access: proxy
    url: http://go-nomads-jaeger:16686
    editable: false
    jsonData:
      tracesToLogsV2:
        datasourceUid: ''
      tracesToMetrics:
        datasourceUid: Prometheus
        tags:
          - key: service.name
            value: service
EOF
    
    docker run -d \
        --name go-nomads-grafana \
        --network "${NETWORK_NAME}" \
        --label "com.docker.compose.project=go-nomads-infras" \
        --label "com.docker.compose.service=grafana" \
        --add-host host.docker.internal:host-gateway \
        -p 3000:3000 \
        -e GF_SECURITY_ADMIN_PASSWORD=admin \
        -e GF_INSTALL_PLUGINS=grafana-clock-panel,grafana-simple-json-datasource \
        -v "${grafana_dir}/provisioning:/etc/grafana/provisioning:ro" \
        grafana/grafana:10.3.1 >/dev/null
    echo "Grafana UI available at http://localhost:3000 (admin/admin)"
    echo "Datasources: Prometheus, Jaeger auto-provisioned"
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

start_all() {
    header "Go-Nomads Local Infrastructure"
    require_docker
    create_network
    start_redis
    start_rabbitmq
    start_consul
    start_jaeger
    start_prometheus
    start_grafana
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
        go-nomads-grafana
        go-nomads-prometheus
        go-nomads-jaeger
        go-nomads-consul
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
        go-nomads-grafana
        go-nomads-prometheus
        go-nomads-jaeger
        go-nomads-consul
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
    rm -rf "${SCRIPT_DIR}/consul" "${SCRIPT_DIR}/prometheus" "${SCRIPT_DIR}/grafana"
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
    echo "  Consul:         http://localhost:7500"
    echo "  Jaeger UI:      http://localhost:16686"
    echo "  Jaeger OTLP:    localhost:4317 (gRPC), localhost:4318 (HTTP)"
    echo "  Prometheus:     http://localhost:9090"
    echo "  Grafana:        http://localhost:3000 (admin/admin)"
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
