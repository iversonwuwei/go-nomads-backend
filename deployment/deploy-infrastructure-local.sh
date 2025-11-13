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
        -p 6379:6379 \
        redis:7-alpine >/dev/null
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
        -p 7500:7500 \
        -p 7502:7502 \
        -p 7600:7600/udp \
        -v "${consul_dir}/consul-local.json:/consul/config/consul.json:ro" \
        hashicorp/consul:latest agent -config-file=/consul/config/consul.json >/dev/null
    echo "Consul UI available at http://localhost:7500"
}

start_zipkin() {
    header "Deploying Zipkin"
    remove_container go-nomads-zipkin
    docker run -d \
        --name go-nomads-zipkin \
        --network "${NETWORK_NAME}" \
        -p 9811:9411 \
        openzipkin/zipkin:latest >/dev/null
    echo "Zipkin UI available at http://localhost:9811"
}

start_prometheus() {
    header "Deploying Prometheus"
    remove_container go-nomads-prometheus
    local prom_dir="${SCRIPT_DIR}/prometheus"
    mkdir -p "${prom_dir}"
    cat > "${prom_dir}/prometheus-local.yml" <<'EOF'
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']
  
  # 完全依赖 Consul 自动服务发现 - 无需手动配置服务列表
  - job_name: 'consul-services'
    metrics_path: /metrics
    consul_sd_configs:
      - server: 'go-nomads-consul:7500'
        # 不指定 services，自动发现所有已注册的服务
    relabel_configs:
      # 只抓取有 metrics_path 元数据的服务
      - source_labels: [__meta_consul_service_metadata_metrics_path]
        action: keep
        regex: /.+
      
      # 使用自定义 metrics 路径（如果有）
      - source_labels: [__meta_consul_service_metadata_metrics_path]
        target_label: __metrics_path__
        regex: (.+)
        replacement: $1
      
      # 服务名称标签
      - source_labels: [__meta_consul_service]
        target_label: service
      
      # 版本标签
      - source_labels: [__meta_consul_service_metadata_version]
        target_label: version
      
      # 协议标签
      - source_labels: [__meta_consul_service_metadata_protocol]
        target_label: protocol
      
      # 实例标签
      - source_labels: [__address__]
        target_label: instance
EOF
    docker run -d \
        --name go-nomads-prometheus \
        --network "${NETWORK_NAME}" \
        -p 9090:9090 \
        -v "${prom_dir}/prometheus-local.yml:/etc/prometheus/prometheus.yml:ro" \
        prom/prometheus:latest >/dev/null
    echo "Prometheus UI available at http://localhost:9090"
}

start_grafana() {
    header "Deploying Grafana"
    remove_container go-nomads-grafana
    local grafana_dir="${SCRIPT_DIR}/grafana"
    docker run -d \
        --name go-nomads-grafana \
        --network "${NETWORK_NAME}" \
        -p 3000:3000 \
        -e GF_SECURITY_ADMIN_PASSWORD=admin \
        -v "${grafana_dir}/provisioning:/etc/grafana/provisioning:ro" \
        grafana/grafana:latest >/dev/null
    echo "Grafana UI available at http://localhost:3000 (admin/admin)"
    echo "Dashboards will be automatically provisioned"
}

start_elasticsearch() {
    header "Deploying Elasticsearch"
    remove_container go-nomads-elasticsearch
    docker run -d \
        --name go-nomads-elasticsearch \
        --network "${NETWORK_NAME}" \
        -p 10200:9200 \
        -p 10300:9300 \
        -e "discovery.type=single-node" \
        -e "xpack.security.enabled=false" \
        -e "ES_JAVA_OPTS=-Xms512m -Xmx512m" \
        docker.elastic.co/elasticsearch/elasticsearch:8.11.0 >/dev/null
    echo "Elasticsearch available at http://localhost:10200"
}

start_rabbitmq() {
    header "Deploying RabbitMQ"
    remove_container go-nomads-rabbitmq
    docker run -d \
        --name go-nomads-rabbitmq \
        --network "${NETWORK_NAME}" \
        -p 5672:5672 \
        -p 15672:15672 \
        -e RABBITMQ_DEFAULT_USER=guest \
        -e RABBITMQ_DEFAULT_PASS=guest \
        rabbitmq:3-management-alpine >/dev/null
    echo "RabbitMQ running at amqp://localhost:5672"
    echo "RabbitMQ Management UI: http://localhost:15672 (guest/guest)"
}

start_nginx() {
    header "Deploying Nginx"
    remove_container go-nomads-nginx
    docker run -d \
        --name go-nomads-nginx \
        --network "${NETWORK_NAME}" \
        -p 80:80 \
        -p 443:443 \
        nginx:latest >/dev/null
    echo "Nginx running at http://localhost"
}

start_all() {
    header "Go-Nomads Local Infrastructure"
    require_docker
    create_network
    start_redis
    start_rabbitmq
    start_consul
    start_zipkin
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
        go-nomads-zipkin
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
        go-nomads-zipkin
        go-nomads-consul
        go-nomads-redis
    )
    for c in "${containers[@]}"; do
        remove_container "$c"
    done
    if network_exists; then
        echo "Removing network ${NETWORK_NAME}..."
        docker network rm "${NETWORK_NAME}" >/dev/null
    fi
    rm -rf "${SCRIPT_DIR}/consul" "${SCRIPT_DIR}/prometheus"
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
    echo "  Zipkin:         http://localhost:9811"
    echo "  Prometheus:     http://localhost:9090"
    echo "  Grafana:        http://localhost:3000 (admin/admin)"
    echo "  Elasticsearch:  http://localhost:10200"
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
