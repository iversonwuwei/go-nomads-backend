#!/bin/bash
# Go-Nomads Infrastructure Deployment Script
# Supports: Linux (Docker/Podman)
# Usage: bash deploy-infrastructure.sh [start|stop|restart|status|clean|help]

set -e

# Configuration
NETWORK_NAME="go-nomads-network"
REDIS_CONFIG_COUNT=21
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Detect container runtime
if command -v podman &> /dev/null; then
    CONTAINER_RUNTIME="podman"
elif command -v docker &> /dev/null; then
    CONTAINER_RUNTIME="docker"
else
    echo -e "${RED}[ERROR] Neither Docker nor Podman found. Please install one of them.${NC}"
    exit 1
fi

echo -e "${CYAN}Using container runtime: $CONTAINER_RUNTIME${NC}"

# Helper Functions
show_header() {
    echo ""
    echo -e "${CYAN}============================================================${NC}"
    echo -e "${WHITE}  $1${NC}"
    echo -e "${CYAN}============================================================${NC}"
    echo ""
}

container_running() {
    local name=$1
    local result=$($CONTAINER_RUNTIME ps --filter "name=$name" --format "{{.Names}}" 2>/dev/null || true)
    [[ "$result" == "$name" ]]
}

remove_container_if_exists() {
    local name=$1
    if $CONTAINER_RUNTIME ps -a --filter "name=$name" --format "{{.Names}}" 2>/dev/null | grep -q "^${name}$"; then
        echo -e "${YELLOW}  Removing existing container: $name${NC}"
        $CONTAINER_RUNTIME rm -f "$name" > /dev/null 2>&1 || true
    fi
}

wait_for_service() {
    local name=$1
    local url=$2
    local max_attempts=${3:-30}
    
    echo -e "${YELLOW}  Waiting for $name to be ready...${NC}"
    for i in $(seq 1 $max_attempts); do
        if curl -sf "$url" > /dev/null 2>&1; then
            echo -e "${GREEN}  $name is ready!${NC}"
            return 0
        fi
        echo -e "${GRAY}    Attempt $i/$max_attempts...${NC}"
        sleep 2
    done
    echo -e "${YELLOW}  [WARNING] $name did not become ready in time${NC}"
    return 1
}

# Create Network
initialize_network() {
    show_header "Creating Network"
    
    if $CONTAINER_RUNTIME network ls --filter "name=$NETWORK_NAME" --format "{{.Name}}" 2>/dev/null | grep -q "^${NETWORK_NAME}$"; then
        echo -e "${GREEN}  Network '$NETWORK_NAME' already exists${NC}"
    else
        echo -e "${YELLOW}  Creating network '$NETWORK_NAME'...${NC}"
        $CONTAINER_RUNTIME network create "$NETWORK_NAME" > /dev/null
        echo -e "${GREEN}  Network created successfully!${NC}"
    fi
}

# Deploy Redis
deploy_redis() {
    show_header "Deploying Redis"
    
    remove_container_if_exists "go-nomads-redis"
    
    echo -e "${YELLOW}  Starting Redis container...${NC}"
    $CONTAINER_RUNTIME run -d \
        --name go-nomads-redis \
        --network "$NETWORK_NAME" \
        -p 6379:6379 \
        redis:latest > /dev/null
    
    if container_running "go-nomads-redis"; then
        echo -e "${GREEN}  Redis deployed successfully!${NC}"
        sleep 3
        import_redis_config
    else
        echo -e "${RED}  [ERROR] Failed to start Redis${NC}"
        exit 1
    fi
}

# Import Redis Configuration
import_redis_config() {
    echo -e "${YELLOW}  Importing configuration to Redis...${NC}"
    
    declare -A configs=(
        ["go-nomads:config:app-version"]="1.0.0"
        ["go-nomads:config:environment"]="development"
        ["go-nomads:config:gateway:endpoint"]="http://go-nomads-gateway:8080"
        ["go-nomads:config:product-service:endpoint"]="http://go-nomads-product-service:8080"
        ["go-nomads:config:user-service:endpoint"]="http://go-nomads-user-service:8080"
        ["go-nomads:config:redis:endpoint"]="go-nomads-redis:6379"
        ["go-nomads:config:consul:endpoint"]="http://go-nomads-consul:8500"
        ["go-nomads:config:zipkin:endpoint"]="http://go-nomads-zipkin:9411"
        ["go-nomads:config:prometheus:endpoint"]="http://go-nomads-prometheus:9090"
        ["go-nomads:config:grafana:endpoint"]="http://go-nomads-grafana:3000"
        ["go-nomads:config:logging:level"]="info"
        ["go-nomads:config:logging:format"]="json"
        ["go-nomads:config:tracing:enabled"]="true"
        ["go-nomads:config:tracing:sample-rate"]="1.0"
        ["go-nomads:config:metrics:enabled"]="true"
        ["go-nomads:config:features:user-registration"]="true"
        ["go-nomads:config:features:product-reviews"]="true"
        ["go-nomads:config:features:recommendations"]="false"
        ["go-nomads:config:business:max-cart-items"]="50"
        ["go-nomads:config:business:session-timeout"]="3600"
        ["go-nomads:config:business:price-precision"]="2"
    )
    
    local count=0
    for key in "${!configs[@]}"; do
        $CONTAINER_RUNTIME exec go-nomads-redis redis-cli SET "$key" "${configs[$key]}" > /dev/null
        ((count++))
    done
    
    echo -e "${GREEN}  Imported $count configuration items${NC}"
}

# Deploy Consul
deploy_consul() {
    show_header "Deploying Consul"
    
    remove_container_if_exists "go-nomads-consul"
    
    # Create Consul config directory
    local consul_config_dir="$SCRIPT_DIR/consul"
    mkdir -p "$consul_config_dir"
    
    # Create Consul configuration
    cat > "$consul_config_dir/consul-config.json" << 'EOF'
{
  "datacenter": "dc1",
  "data_dir": "/consul/data",
  "log_level": "INFO",
  "server": true,
  "ui_config": {
    "enabled": true
  },
  "ports": {
    "http": 8500,
    "dns": 8600,
    "grpc": 8502
  },
  "addresses": {
    "http": "0.0.0.0",
    "dns": "0.0.0.0",
    "grpc": "0.0.0.0"
  },
  "client_addr": "0.0.0.0",
  "bootstrap_expect": 1
}
EOF
    
    echo -e "${YELLOW}  Starting Consul container...${NC}"
    $CONTAINER_RUNTIME run -d \
        --name go-nomads-consul \
        --network "$NETWORK_NAME" \
        -p 8500:8500 \
        -p 8502:8502 \
        -p 8600:8600/udp \
        -e CONSUL_BIND_INTERFACE=eth0 \
        consul:latest agent -server -ui -bootstrap-expect=1 -client=0.0.0.0 > /dev/null
    
    if container_running "go-nomads-consul"; then
        echo -e "${GREEN}  Consul deployed successfully!${NC}"
        wait_for_service "Consul" "http://localhost:8500/v1/status/leader" || true
        register_consul_services
    else
        echo -e "${RED}  [ERROR] Failed to start Consul${NC}"
        exit 1
    fi
}

# Register Services to Consul
register_consul_services() {
    echo -e "${YELLOW}  Registering services to Consul...${NC}"
    
    sleep 3
    
    declare -a services=(
        "gateway:go-nomads-gateway:8080"
        "product-service:go-nomads-product-service:8080"
        "user-service:go-nomads-user-service:8080"
    )
    
    for svc in "${services[@]}"; do
        IFS=':' read -r name address port <<< "$svc"
        $CONTAINER_RUNTIME exec go-nomads-consul consul services register \
            -name="$name" \
            -address="$address" \
            -port="$port" \
            -tag=dapr > /dev/null 2>&1 || true
    done
    
    echo -e "${GREEN}  Registered ${#services[@]} services to Consul${NC}"
}

# Deploy Zipkin
deploy_zipkin() {
    show_header "Deploying Zipkin"
    
    remove_container_if_exists "go-nomads-zipkin"
    
    echo -e "${YELLOW}  Starting Zipkin container...${NC}"
    $CONTAINER_RUNTIME run -d \
        --name go-nomads-zipkin \
        --network "$NETWORK_NAME" \
        -p 9411:9411 \
        openzipkin/zipkin:latest > /dev/null
    
    if container_running "go-nomads-zipkin"; then
        echo -e "${GREEN}  Zipkin deployed successfully!${NC}"
    else
        echo -e "${RED}  [ERROR] Failed to start Zipkin${NC}"
        exit 1
    fi
}

# Deploy Prometheus
deploy_prometheus() {
    show_header "Deploying Prometheus"
    
    remove_container_if_exists "go-nomads-prometheus"
    
    # Create Prometheus config directory
    local prometheus_config_dir="$SCRIPT_DIR/prometheus"
    mkdir -p "$prometheus_config_dir"
    
    # Create Prometheus configuration
    cat > "$prometheus_config_dir/prometheus.yml" << 'EOF'
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  external_labels:
    cluster: 'go-nomads'
    environment: 'development'

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']
        labels:
          service: 'prometheus'

  - job_name: 'dapr-services'
    metrics_path: '/metrics'
    consul_sd_configs:
      - server: 'go-nomads-consul:8500'
        services: ['product-service', 'user-service', 'gateway']
        tags: ['dapr']
    relabel_configs:
      - source_labels: [__address__]
        regex: '([^:]+):.*'
        replacement: '${1}:9090'
        target_label: __address__
      - source_labels: [__meta_consul_service]
        target_label: app_id
      - replacement: 'dapr'
        target_label: service_type

  - job_name: 'app-services'
    metrics_path: '/metrics'
    consul_sd_configs:
      - server: 'go-nomads-consul:8500'
        services: ['product-service', 'user-service', 'gateway']
        tags: ['dapr']
    relabel_configs:
      - source_labels: [__address__]
        regex: '([^:]+):.*'
        replacement: '${1}:8080'
        target_label: __address__
      - source_labels: [__meta_consul_service]
        target_label: app
      - replacement: 'application'
        target_label: service_type

  - job_name: 'redis'
    static_configs:
      - targets: ['go-nomads-redis:6379']
        labels:
          service: 'redis'

  - job_name: 'zipkin'
    static_configs:
      - targets: ['go-nomads-zipkin:9411']
        labels:
          service: 'zipkin'
EOF
    
    echo -e "${YELLOW}  Starting Prometheus container...${NC}"
    $CONTAINER_RUNTIME run -d \
        --name go-nomads-prometheus \
        --network "$NETWORK_NAME" \
        -p 9090:9090 \
        -v "$prometheus_config_dir:/etc/prometheus:Z" \
        prom/prometheus:latest \
        --config.file=/etc/prometheus/prometheus.yml > /dev/null
    
    if container_running "go-nomads-prometheus"; then
        echo -e "${GREEN}  Prometheus deployed successfully!${NC}"
    else
        echo -e "${RED}  [ERROR] Failed to start Prometheus${NC}"
        exit 1
    fi
}

# Deploy Grafana
deploy_grafana() {
    show_header "Deploying Grafana"
    
    remove_container_if_exists "go-nomads-grafana"
    
    echo -e "${YELLOW}  Starting Grafana container...${NC}"
    $CONTAINER_RUNTIME run -d \
        --name go-nomads-grafana \
        --network "$NETWORK_NAME" \
        -p 3000:3000 \
        -e GF_SECURITY_ADMIN_PASSWORD=admin \
        grafana/grafana:latest > /dev/null
    
    if container_running "go-nomads-grafana"; then
        echo -e "${GREEN}  Grafana deployed successfully!${NC}"
    else
        echo -e "${RED}  [ERROR] Failed to start Grafana${NC}"
        exit 1
    fi
}

# Show Status
show_status() {
    show_header "Infrastructure Status"
    
    echo -e "${YELLOW}Container Status:${NC}"
    $CONTAINER_RUNTIME ps --filter "name=go-nomads" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    echo ""
    
    if container_running "go-nomads-consul"; then
        echo -e "${YELLOW}Consul Services:${NC}"
        $CONTAINER_RUNTIME exec go-nomads-consul consul catalog services 2>/dev/null | while read -r svc; do
            echo -e "${CYAN}  - $svc${NC}"
        done
        echo ""
    fi
    
    if container_running "go-nomads-redis"; then
        echo -e "${YELLOW}Redis Configuration:${NC}"
        local config_count=$($CONTAINER_RUNTIME exec go-nomads-redis redis-cli KEYS "go-nomads:config:*" 2>/dev/null | wc -l)
        echo -e "${CYAN}  - Config Items: $config_count${NC}"
        echo ""
    fi
    
    echo -e "${YELLOW}Access URLs:${NC}"
    echo -e "${CYAN}  - Consul UI:    http://localhost:8500${NC}"
    echo -e "${CYAN}  - Prometheus:   http://localhost:9090${NC}"
    echo -e "${CYAN}  - Grafana:      http://localhost:3000 (admin/admin)${NC}"
    echo -e "${CYAN}  - Zipkin:       http://localhost:9411${NC}"
    echo ""
}

# Stop All Containers
stop_infrastructure() {
    show_header "Stopping Infrastructure"
    
    local containers=(
        "go-nomads-grafana"
        "go-nomads-prometheus"
        "go-nomads-zipkin"
        "go-nomads-consul"
        "go-nomads-redis"
    )
    
    for container in "${containers[@]}"; do
        if container_running "$container"; then
            echo -e "${YELLOW}  Stopping $container...${NC}"
            $CONTAINER_RUNTIME stop "$container" > /dev/null
        fi
    done
    
    echo ""
    echo -e "${GREEN}All infrastructure containers stopped!${NC}"
}

# Clean All Resources
clean_infrastructure() {
    show_header "Cleaning Infrastructure"
    
    local containers=(
        "go-nomads-grafana"
        "go-nomads-prometheus"
        "go-nomads-zipkin"
        "go-nomads-consul"
        "go-nomads-redis"
    )
    
    for container in "${containers[@]}"; do
        remove_container_if_exists "$container"
    done
    
    echo -e "${YELLOW}  Removing network...${NC}"
    $CONTAINER_RUNTIME network rm "$NETWORK_NAME" > /dev/null 2>&1 || true
    
    echo -e "${YELLOW}  Removing configuration directories...${NC}"
    rm -rf "$SCRIPT_DIR/consul" "$SCRIPT_DIR/prometheus"
    
    echo ""
    echo -e "${GREEN}All infrastructure resources cleaned!${NC}"
}

# Show Help
show_help() {
    echo ""
    echo -e "${CYAN}Go-Nomads Infrastructure Deployment${NC}"
    echo ""
    echo -e "${YELLOW}Usage:${NC}"
    echo -e "${WHITE}  bash deploy-infrastructure.sh [command]${NC}"
    echo ""
    echo -e "${YELLOW}Commands:${NC}"
    echo -e "${WHITE}  start    - Deploy all infrastructure components (default)${NC}"
    echo -e "${WHITE}  stop     - Stop all infrastructure containers${NC}"
    echo -e "${WHITE}  restart  - Restart all infrastructure containers${NC}"
    echo -e "${WHITE}  status   - Show current infrastructure status${NC}"
    echo -e "${WHITE}  clean    - Remove all infrastructure resources${NC}"
    echo -e "${WHITE}  help     - Show this help message${NC}"
    echo ""
    echo -e "${YELLOW}Infrastructure Components:${NC}"
    echo -e "${WHITE}  - Redis (Configuration & State Store)${NC}"
    echo -e "${WHITE}  - Consul (Service Registry)${NC}"
    echo -e "${WHITE}  - Zipkin (Distributed Tracing)${NC}"
    echo -e "${WHITE}  - Prometheus (Metrics Collection)${NC}"
    echo -e "${WHITE}  - Grafana (Metrics Visualization)${NC}"
    echo ""
}

# Main Execution
ACTION=${1:-start}

case "$ACTION" in
    start)
        show_header "Go-Nomads Infrastructure Deployment"
        initialize_network
        deploy_redis
        deploy_consul
        deploy_zipkin
        deploy_prometheus
        deploy_grafana
        show_status
        
        echo ""
        echo -e "${GREEN}============================================================${NC}"
        echo -e "${GREEN}  Infrastructure Deployment Completed Successfully!${NC}"
        echo -e "${GREEN}============================================================${NC}"
        echo ""
        ;;
    
    stop)
        stop_infrastructure
        ;;
    
    restart)
        stop_infrastructure
        sleep 2
        "$0" start
        ;;
    
    status)
        show_status
        ;;
    
    clean)
        echo ""
        echo -e "${YELLOW}[WARNING] This will remove all infrastructure containers and data!${NC}"
        read -p "Are you sure? (yes/no): " confirm
        if [[ "$confirm" == "yes" ]]; then
            clean_infrastructure
        else
            echo -e "${CYAN}Operation cancelled.${NC}"
        fi
        ;;
    
    help|--help|-h)
        show_help
        ;;
    
    *)
        echo -e "${RED}Unknown command: $ACTION${NC}"
        show_help
        exit 1
        ;;
esac
