#!/bin/bash
# Go-Nomads Infrastructure Deployment Script
# Supports: Linux (Docker/Podman)
# Usage: bash deploy-infrastructure.sh [start|stop|restart|status|clean|help]

set -e

# Configuration
NETWORK_NAME="go-nomads-network"
REDIS_CONFIG_COUNT=21
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SWR_REGISTRY="${SWR_LOGIN_SERVER:-${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}}"
SWR_ORGANIZATION="${SWR_ORGANIZATION:-go-nomads}"
USE_SWR=""
USE_MIRROR=""
MIRROR_PREFIX="${MIRROR_PREFIX:-docker.1ms.run}"
REDIS_IMAGE="${REDIS_IMAGE:-redis:7.4}"
RABBITMQ_IMAGE="${RABBITMQ_IMAGE:-rabbitmq:3-management-alpine}"
ELASTICSEARCH_IMAGE="${ELASTICSEARCH_IMAGE:-docker.elastic.co/elasticsearch/elasticsearch:8.17.4}"
NGINX_IMAGE="${NGINX_IMAGE:-nginx:1.29.6}"

set_swr_images() {
    REDIS_IMAGE="${SWR_REGISTRY}/${SWR_ORGANIZATION}/redis:7.4"
    RABBITMQ_IMAGE="${SWR_REGISTRY}/${SWR_ORGANIZATION}/rabbitmq:3-management-alpine"
    ELASTICSEARCH_IMAGE="${SWR_REGISTRY}/${SWR_ORGANIZATION}/elasticsearch:8.17.4"
    NGINX_IMAGE="${SWR_REGISTRY}/${SWR_ORGANIZATION}/nginx:1.29.6"
}

set_mirror_images() {
    REDIS_IMAGE="${MIRROR_PREFIX}/library/redis:7.4"
    RABBITMQ_IMAGE="${MIRROR_PREFIX}/library/rabbitmq:3-management-alpine"
    ELASTICSEARCH_IMAGE="${SWR_REGISTRY}/${SWR_ORGANIZATION}/elasticsearch:8.17.4"
    NGINX_IMAGE="${MIRROR_PREFIX}/library/nginx:1.29.6"
}

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

if [[ -n "$USE_SWR" ]]; then
    set_swr_images
elif [[ -n "$USE_MIRROR" ]]; then
    set_mirror_images
fi

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
        -p 5300:6379 \
        "$REDIS_IMAGE" > /dev/null
    
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
    
    # Define configuration items as key-value pairs
    local configs=(
        "go-nomads:config:app-version" "1.0.0"
        "go-nomads:config:environment" "development"
        "go-nomads:config:gateway:endpoint" "http://go-nomads-gateway:5000"
        "go-nomads:config:product-service:endpoint" "http://go-nomads-product-service:5002"
        "go-nomads:config:user-service:endpoint" "http://go-nomads-user-service:5001"
        "go-nomads:config:redis:endpoint" "go-nomads-redis:6379"
        "go-nomads:config:logging:level" "info"
        "go-nomads:config:logging:format" "json"
        "go-nomads:config:tracing:enabled" "false"
        "go-nomads:config:tracing:sample-rate" "1.0"
        "go-nomads:config:metrics:enabled" "false"
        "go-nomads:config:features:user-registration" "true"
        "go-nomads:config:features:product-reviews" "true"
        "go-nomads:config:features:recommendations" "false"
        "go-nomads:config:business:max-cart-items" "50"
        "go-nomads:config:business:session-timeout" "3600"
        "go-nomads:config:business:price-precision" "2"
    )
    
    local count=0
    for ((i=0; i<${#configs[@]}; i+=2)); do
        local key="${configs[i]}"
        local value="${configs[i+1]}"
        $CONTAINER_RUNTIME exec go-nomads-redis redis-cli SET "$key" "$value" > /dev/null
        ((count++))
    done
    
    echo -e "${GREEN}  Imported $count configuration items${NC}"
}

# Deploy RabbitMQ
deploy_rabbitmq() {
    show_header "Deploying RabbitMQ"
    
    remove_container_if_exists "go-nomads-rabbitmq"
    
    echo -e "${YELLOW}  Starting RabbitMQ container...${NC}"
    $CONTAINER_RUNTIME run -d \
        --name go-nomads-rabbitmq \
        --network "$NETWORK_NAME" \
        -p 5301:5672 \
        -p 5302:15672 \
        -e RABBITMQ_DEFAULT_USER=walden \
        -e RABBITMQ_DEFAULT_PASS=walden \
        "$RABBITMQ_IMAGE" > /dev/null
    
    if container_running "go-nomads-rabbitmq"; then
        echo -e "${GREEN}  RabbitMQ deployed successfully!${NC}"
        wait_for_service "RabbitMQ" "http://localhost:5302" 30 || true
    else
        echo -e "${RED}  [ERROR] Failed to start RabbitMQ${NC}"
        exit 1
    fi
}

# Deploy PostgreSQL
deploy_postgres() {
    show_header "Deploying PostgreSQL"
    
    remove_container_if_exists "go-nomads-postgres"
    
    echo -e "${YELLOW}  Starting PostgreSQL container...${NC}"
    $CONTAINER_RUNTIME run -d \
        --name go-nomads-postgres \
        --network "$NETWORK_NAME" \
        -p 5307:5432 \
        -e POSTGRES_USER=postgres \
        -e POSTGRES_PASSWORD=postgres \
        -e POSTGRES_DB=gonomads \
        postgres:16-alpine > /dev/null
    
    if container_running "go-nomads-postgres"; then
        echo -e "${GREEN}  PostgreSQL deployed successfully!${NC}"
        wait_for_service "PostgreSQL" "http://localhost:5307" 10 || true
    else
        echo -e "${RED}  [ERROR] Failed to start PostgreSQL${NC}"
        exit 1
    fi
}

# Deploy Elasticsearch
deploy_elasticsearch() {
    show_header "Deploying Elasticsearch"
    
    remove_container_if_exists "go-nomads-elasticsearch"
    
    echo -e "${YELLOW}  Starting Elasticsearch container...${NC}"
    $CONTAINER_RUNTIME run -d \
        --name go-nomads-elasticsearch \
        --network "$NETWORK_NAME" \
        -p 5303:9200 \
        -p 5304:9300 \
        -e "discovery.type=single-node" \
        -e "xpack.security.enabled=false" \
        -e "ES_JAVA_OPTS=-Xms512m -Xmx512m" \
        "$ELASTICSEARCH_IMAGE" > /dev/null
    
    if container_running "go-nomads-elasticsearch"; then
        echo -e "${GREEN}  Elasticsearch deployed successfully!${NC}"
        wait_for_service "Elasticsearch" "http://localhost:5303" 30 || true
    else
        echo -e "${RED}  [ERROR] Failed to start Elasticsearch${NC}"
        exit 1
    fi
}

# Deploy Nginx
deploy_nginx() {
    show_header "Deploying Nginx"
    
    remove_container_if_exists "go-nomads-nginx"
    local nginx_conf="$SCRIPT_DIR/nginx/nginx.conf"

    if [[ ! -f "$nginx_conf" ]]; then
        echo -e "${RED}  [ERROR] Nginx config not found: $nginx_conf${NC}"
        exit 1
    fi
    
    echo -e "${YELLOW}  Starting Nginx container...${NC}"
    $CONTAINER_RUNTIME run -d \
        --name go-nomads-nginx \
        --network "$NETWORK_NAME" \
        -p 5305:80 \
        -p 5343:443 \
        -v "$nginx_conf:/etc/nginx/conf.d/default.conf:ro" \
        "$NGINX_IMAGE" > /dev/null
    
    if container_running "go-nomads-nginx"; then
        echo -e "${GREEN}  Nginx deployed successfully!${NC}"
    else
        echo -e "${RED}  [ERROR] Failed to start Nginx${NC}"
        exit 1
    fi
}

# Show Status
show_status() {
    show_header "Infrastructure Status"
    
    echo -e "${YELLOW}Container Status:${NC}"
    $CONTAINER_RUNTIME ps --filter "name=go-nomads" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    echo ""
    
    if container_running "go-nomads-redis"; then
        echo -e "${YELLOW}Redis Configuration:${NC}"
        local config_count=$($CONTAINER_RUNTIME exec go-nomads-redis redis-cli KEYS "go-nomads:config:*" 2>/dev/null | wc -l)
        echo -e "${CYAN}  - Config Items: $config_count${NC}"
        echo ""
    fi
    
    echo -e "${YELLOW}Access URLs:${NC}"
    echo -e "${CYAN}  - Nginx:          http://localhost:5305${NC}"
    echo -e "${CYAN}  - RabbitMQ UI:    http://localhost:5302 (walden/walden)${NC}"
    echo -e "${CYAN}  - PostgreSQL:     localhost:5307 (postgres/postgres)${NC}"
    echo -e "${CYAN}  - Elasticsearch:  http://localhost:5303${NC}"
    echo ""
}

# Stop All Containers
stop_infrastructure() {
    show_header "Stopping Infrastructure"
    
    local containers=(
        "go-nomads-nginx"
        "go-nomads-rabbitmq"
        "go-nomads-redis"
        "go-nomads-postgres"
        "go-nomads-elasticsearch"
    )
    
    for container in "${containers[@]}"; do
        remove_container_if_exists "$container"
    done
    
    echo -e "${YELLOW}  Removing network...${NC}"
    $CONTAINER_RUNTIME network rm "$NETWORK_NAME" > /dev/null 2>&1 || true
    
    echo -e "${YELLOW}  Removing configuration directories...${NC}"
    
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
    echo -e "${WHITE}  - RabbitMQ (Message Queue)${NC}"
    echo -e "${WHITE}  - PostgreSQL (Relational Database)${NC}"
    echo -e "${WHITE}  - Elasticsearch (Search Engine)${NC}"
    echo -e "${WHITE}  - Nginx (Reverse Proxy)${NC}"
    echo ""
}

# Main Execution
ACTION=${1:-start}

case "$ACTION" in
    start)
        show_header "Go-Nomads Infrastructure Deployment"
        initialize_network
        deploy_redis
        deploy_rabbitmq
        deploy_postgres
        deploy_elasticsearch
        deploy_nginx
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
