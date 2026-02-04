#!/bin/bash
# ============================================================
# Go-Nomads 可观测性测试脚本
# 用于快速验证 OpenTelemetry + Jaeger + Prometheus 配置
# ============================================================

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

header() {
    echo
    echo -e "${CYAN}============================================================${NC}"
    echo -e "${CYAN}  $1${NC}"
    echo -e "${CYAN}============================================================${NC}"
    echo
}

success() {
    echo -e "${GREEN}[✓] $1${NC}"
}

fail() {
    echo -e "${RED}[✗] $1${NC}"
}

info() {
    echo -e "${YELLOW}[i] $1${NC}"
}

# Test Jaeger availability
test_jaeger() {
    header "Testing Jaeger"
    
    if curl -s -o /dev/null -w "%{http_code}" "http://localhost:16686" | grep -q "200"; then
        success "Jaeger UI is accessible at http://localhost:16686"
    else
        fail "Jaeger UI is not accessible"
        return 1
    fi
    
    # Test OTLP endpoint
    info "Jaeger OTLP endpoints: gRPC=4317, HTTP=4318"
    
    return 0
}

# Test Prometheus availability
test_prometheus() {
    header "Testing Prometheus"
    
    if curl -s -o /dev/null -w "%{http_code}" "http://localhost:9090/-/healthy" | grep -q "200"; then
        success "Prometheus is healthy at http://localhost:9090"
    else
        fail "Prometheus is not accessible"
        return 1
    fi
    
    # Check targets
    info "Fetching targets..."
    local targets=$(curl -s "http://localhost:9090/api/v1/targets" 2>/dev/null)
    if [ -n "$targets" ]; then
        local active=$(echo "$targets" | jq -r '.data.activeTargets | length' 2>/dev/null || echo "0")
        local up=$(echo "$targets" | jq -r '[.data.activeTargets[] | select(.health == "up")] | length' 2>/dev/null || echo "0")
        info "Active targets: $active, Up: $up"
        
        # List targets
        echo "$targets" | jq -r '.data.activeTargets[] | "\(.health | ascii_upcase) \(.labels.job) - \(.scrapeUrl)"' 2>/dev/null | while read line; do
            if echo "$line" | grep -q "^UP"; then
                echo -e "  ${GREEN}$line${NC}"
            else
                echo -e "  ${RED}$line${NC}"
            fi
        done
    fi
    
    return 0
}

# Test Grafana availability
test_grafana() {
    header "Testing Grafana"
    
    if curl -s -o /dev/null -w "%{http_code}" "http://localhost:3000/api/health" | grep -q "200"; then
        success "Grafana is healthy at http://localhost:3000"
    else
        fail "Grafana is not accessible"
        return 1
    fi
    
    # Check datasources
    info "Fetching datasources..."
    local datasources=$(curl -s -u admin:admin "http://localhost:3000/api/datasources" 2>/dev/null)
    if [ -n "$datasources" ]; then
        echo "$datasources" | jq -r '.[] | "  - \(.name) (\(.type))"' 2>/dev/null || true
    fi
    
    return 0
}

# Test service metrics endpoint
test_service_metrics() {
    local name="$1"
    local port="$2"
    
    if curl -s -o /dev/null -w "%{http_code}" "http://localhost:$port/metrics" --connect-timeout 2 | grep -q "200"; then
        success "$name metrics available at http://localhost:$port/metrics"
        local count=$(curl -s "http://localhost:$port/metrics" 2>/dev/null | grep -v "^#" | grep -v "^$" | wc -l)
        info "  $count metric samples"
        return 0
    else
        fail "$name metrics not available at http://localhost:$port/metrics"
        return 1
    fi
}

# Test all services
test_all_services() {
    header "Testing Service Metrics"
    
    local services=(
        "Gateway:5000"
        "UserService:5001"
        "CityService:8002"
        "AccommodationService:8003"
        "CoworkingService:8004"
        "EventService:8005"
        "AIService:8006"
        "MessageService:8007"
        "SearchService:8008"
    )
    
    local running=0
    for service in "${services[@]}"; do
        local name="${service%%:*}"
        local port="${service##*:}"
        if test_service_metrics "$name" "$port"; then
            ((running++)) || true
        fi
    done
    
    info "$running of ${#services[@]} services are exposing metrics"
}

# Generate test traffic
generate_traffic() {
    header "Generating Test Traffic"
    
    info "Sending requests to Gateway..."
    
    local endpoints=(
        "http://localhost:5000/health"
        "http://localhost:5000/api/v1/cities"
        "http://localhost:5000/api/v1/users/profile"
    )
    
    for i in {1..10}; do
        for endpoint in "${endpoints[@]}"; do
            if curl -s -o /dev/null -w "" "$endpoint" --connect-timeout 2 2>/dev/null; then
                echo -n -e "${GREEN}.${NC}"
            else
                echo -n -e "${RED}x${NC}"
            fi
        done
    done
    echo
    success "Test traffic generated. Check Jaeger for traces."
}

# Show summary
show_summary() {
    header "Observability Stack Summary"
    
    echo
    echo -e "${CYAN}URLs:${NC}"
    echo -e "  ${GREEN}Jaeger UI:        http://localhost:16686${NC}"
    echo "  Prometheus:       http://localhost:9090"
    echo "  Grafana:          http://localhost:3000 (admin/admin)"
    echo
    echo -e "${CYAN}Endpoints:${NC}"
    echo "  OTLP gRPC:        localhost:4317"
    echo "  OTLP HTTP:        localhost:4318"
    echo "  Zipkin compat:    localhost:9411"
    echo
    echo -e "${CYAN}Quick Actions:${NC}"
    echo "  1. Open Jaeger UI to view traces"
    echo "  2. Open Prometheus to query metrics"
    echo "  3. Open Grafana to view dashboards"
    echo
}

# Help
show_help() {
    cat <<'EOF'
Go-Nomads Observability Test Script

Usage:
  bash test-observability.sh [command]

Commands:
  test       Run all observability tests (default)
  jaeger     Test Jaeger only
  prometheus Test Prometheus only
  grafana    Test Grafana only
  services   Test all service metrics endpoints
  traffic    Generate test traffic for tracing
  summary    Show stack URLs and info
  help       Show this help message
EOF
}

# Main
ACTION="${1:-test}"
case "$ACTION" in
    test)
        test_jaeger
        test_prometheus
        test_grafana
        test_all_services
        show_summary
        ;;
    jaeger)
        test_jaeger
        ;;
    prometheus)
        test_prometheus
        ;;
    grafana)
        test_grafana
        ;;
    services)
        test_all_services
        ;;
    traffic)
        generate_traffic
        ;;
    summary)
        show_summary
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
