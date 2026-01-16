#!/usr/bin/env bash

# ============================================================
# æž„å»º Docker é•œåƒå¹¶æŽ¨é€åˆ°åŽä¸ºäº‘ SWR ä»“åº“
# ============================================================

set -e

# ============================================================
# é…ç½®åŒºåŸŸ - è¯·æ ¹æ®å®žé™…æƒ…å†µä¿®æ”¹
# ============================================================
# SWR ä»“åº“åœ°å€æ ¼å¼: swr.<region>.myhuaweicloud.com/<organization>
SWR_REGISTRY="${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}"
SWR_ORGANIZATION="${SWR_ORGANIZATION:-go-nomads}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

# é¡¹ç›®æ ¹ç›®å½•
PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

# æœåŠ¡åˆ—è¡¨ - æœåŠ¡å:Dockerfileè·¯å¾„
SERVICES_LIST="
gateway:src/Gateway/Gateway/Dockerfile
user-service:src/Services/UserService/UserService/Dockerfile
city-service:src/Services/CityService/CityService/Dockerfile
coworking-service:src/Services/CoworkingService/CoworkingService/Dockerfile
search-service:src/Services/SearchService/SearchService/Dockerfile
accommodation-service:src/Services/AccommodationService/AccommodationService/Dockerfile
event-service:src/Services/EventService/EventService/Dockerfile
ai-service:src/Services/AIService/AIService/Dockerfile
cache-service:src/Services/CacheService/CacheService/Dockerfile
document-service:src/Services/DocumentService/DocumentService/Dockerfile
innovation-service:src/Services/InnovationService/InnovationService/Dockerfile
message-service:src/Services/MessageService/MessageService/API/Dockerfile
product-service:src/Services/ProductService/ProductService/Dockerfile
"

# èŽ·å–æœåŠ¡çš„ Dockerfile è·¯å¾„
get_dockerfile_path() {
    local service_name=$1
    echo "$SERVICES_LIST" | grep "^${service_name}:" | cut -d: -f2
}

# èŽ·å–æ‰€æœ‰æœåŠ¡å
get_all_services() {
    echo "$SERVICES_LIST" | grep -v '^$' | cut -d: -f1
}

# ============================================================
# å‡½æ•°å®šä¹‰
# ============================================================

# æ‰“å°å¸®åŠ©ä¿¡æ¯
print_help() {
    echo "ç”¨æ³•: $0 [é€‰é¡¹] [æœåŠ¡å...]"
    echo ""
    echo "é€‰é¡¹:"
    echo "  -h, --help              æ˜¾ç¤ºå¸®åŠ©ä¿¡æ¯"
    echo "  -l, --login             ç™»å½•åˆ° SWR ä»“åº“"
    echo "  -b, --build-only        åªæž„å»ºé•œåƒï¼Œä¸æŽ¨é€"
    echo "  -p, --push-only         åªæŽ¨é€é•œåƒï¼Œä¸æž„å»º"
    echo "  -a, --all               æž„å»ºå¹¶æŽ¨é€æ‰€æœ‰æœåŠ¡"
    echo "  -t, --tag <tag>         æŒ‡å®šé•œåƒæ ‡ç­¾ (é»˜è®¤: latest)"
    echo "  --list                  åˆ—å‡ºæ‰€æœ‰å¯ç”¨çš„æœåŠ¡"
    echo ""
    echo "çŽ¯å¢ƒå˜é‡:"
    echo "  SWR_REGISTRY            SWR ä»“åº“åœ°å€ (é»˜è®¤: swr.cn-north-4.myhuaweicloud.com)"
    echo "  SWR_ORGANIZATION        SWR ç»„ç»‡åç§° (é»˜è®¤: go-nomads)"
    echo "  SWR_AK                  åŽä¸ºäº‘ Access Key (ç”¨äºŽç™»å½•)"
    echo "  SWR_SK                  åŽä¸ºäº‘ Secret Key (ç”¨äºŽç™»å½•)"
    echo "  IMAGE_TAG               é•œåƒæ ‡ç­¾ (é»˜è®¤: latest)"
    echo ""
    echo "ç¤ºä¾‹:"
    echo "  $0 --login                          # ç™»å½•åˆ° SWR"
    echo "  $0 event-service                    # æž„å»ºå¹¶æŽ¨é€ event-service"
    echo "  $0 -t v1.0.0 gateway user-service   # ä½¿ç”¨ v1.0.0 æ ‡ç­¾æž„å»ºå¹¶æŽ¨é€å¤šä¸ªæœåŠ¡"
    echo "  $0 -a                               # æž„å»ºå¹¶æŽ¨é€æ‰€æœ‰æœåŠ¡"
    echo "  $0 -b event-service                 # åªæž„å»º event-service"
}

# åˆ—å‡ºæ‰€æœ‰æœåŠ¡
list_services() {
    echo "å¯ç”¨çš„æœåŠ¡åˆ—è¡¨:"
    echo "==============="
    for service in $(get_all_services); do
        echo "  - $service"
    done
}

# ç™»å½•åˆ° SWR
login_swr() {
    echo "================================================"
    echo "ç™»å½•åˆ°åŽä¸ºäº‘ SWR: $SWR_REGISTRY"
    echo "================================================"
    
    if [ -n "$SWR_AK" ] && [ -n "$SWR_SK" ]; then
        # ä½¿ç”¨ AK/SK ç™»å½•
        echo "ä½¿ç”¨ AK/SK è¿›è¡Œç™»å½•..."
        # åŽä¸ºäº‘ SWR ç™»å½•å‘½ä»¤
        # å¯†ç æ ¼å¼: åŒºåŸŸé¡¹ç›®å@AK@SK æˆ–ç›´æŽ¥ä½¿ç”¨ä¸´æ—¶ç™»å½•æŒ‡ä»¤
        docker login -u "${SWR_REGION:-cn-north-4}@${SWR_AK}" -p "${SWR_SK}" "$SWR_REGISTRY"
    else
        echo "è¯·è®¾ç½® SWR_AK å’Œ SWR_SK çŽ¯å¢ƒå˜é‡ï¼Œæˆ–æ‰‹åŠ¨æ‰§è¡Œç™»å½•å‘½ä»¤ã€‚"
        echo ""
        echo "æ–¹æ³•1: ä½¿ç”¨ AK/SK ç™»å½•"
        echo "  export SWR_AK=<your-access-key>"
        echo "  export SWR_SK=<your-secret-key>"
        echo "  $0 --login"
        echo ""
        echo "æ–¹æ³•2: ä½¿ç”¨åŽä¸ºäº‘ CLI èŽ·å–ä¸´æ—¶ç™»å½•æŒ‡ä»¤"
        echo "  åœ¨åŽä¸ºäº‘æŽ§åˆ¶å° -> å®¹å™¨é•œåƒæœåŠ¡ -> æˆ‘çš„é•œåƒ -> å®¢æˆ·ç«¯ä¸Šä¼ "
        echo "  å¤åˆ¶å¹¶æ‰§è¡Œç™»å½•æŒ‡ä»¤"
        echo ""
        echo "æ–¹æ³•3: æ‰‹åŠ¨ docker login"
        echo "  docker login $SWR_REGISTRY"
        exit 1
    fi
}

# æž„å»ºå•ä¸ªæœåŠ¡é•œåƒ
build_service() {
    local service_name=$1
    local dockerfile_path=$(get_dockerfile_path "$service_name")
    
    if [ -z "$dockerfile_path" ]; then
        echo "é”™è¯¯: æœªçŸ¥çš„æœåŠ¡ '$service_name'"
        echo "ä½¿ç”¨ --list æŸ¥çœ‹å¯ç”¨çš„æœåŠ¡åˆ—è¡¨"
        return 1
    fi
    
    local full_image_name="$SWR_REGISTRY/$SWR_ORGANIZATION/$service_name:$IMAGE_TAG"
    
    echo "================================================"
    echo "æž„å»ºé•œåƒ: $service_name"
    echo "Dockerfile: $dockerfile_path"
    echo "é•œåƒåç§°: $full_image_name"
    echo "================================================"
    
    cd "$PROJECT_ROOT"
    
    # ä½¿ç”¨ --platform linux/amd64 ç¡®ä¿é•œåƒå…¼å®¹ x86_64 æœåŠ¡å™¨
    # ä½¿ç”¨ --provenance=false å’Œ --sbom=false é¿å…ç”Ÿæˆå¤šå¹³å° manifest
    docker build \
        --platform linux/amd64 \
        --provenance=false \
        --sbom=false \
        -t "$full_image_name" \
        -f "$dockerfile_path" \
        .
    
    echo "âœ… é•œåƒæž„å»ºæˆåŠŸ: $full_image_name"
}

# æŽ¨é€å•ä¸ªæœåŠ¡é•œåƒ
push_service() {
    local service_name=$1
    local full_image_name="$SWR_REGISTRY/$SWR_ORGANIZATION/$service_name:$IMAGE_TAG"
    
    echo "================================================"
    echo "æŽ¨é€é•œåƒ: $full_image_name"
    echo "================================================"
    
    docker push "$full_image_name"
    
    echo "âœ… é•œåƒæŽ¨é€æˆåŠŸ: $full_image_name"
}

# æž„å»ºå¹¶æŽ¨é€å•ä¸ªæœåŠ¡
build_and_push_service() {
    local service_name=$1
    
    if [ "$BUILD_ONLY" = true ]; then
        build_service "$service_name"
    elif [ "$PUSH_ONLY" = true ]; then
        push_service "$service_name"
    else
        build_service "$service_name"
        push_service "$service_name"
    fi
}

# ============================================================
# ä¸»é€»è¾‘
# ============================================================

BUILD_ONLY=false
PUSH_ONLY=false
DO_LOGIN=false
BUILD_ALL=false
SERVICES_TO_BUILD=""

# è§£æžå‘½ä»¤è¡Œå‚æ•°
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            print_help
            exit 0
            ;;
        -l|--login)
            DO_LOGIN=true
            shift
            ;;
        -b|--build-only)
            BUILD_ONLY=true
            shift
            ;;
        -p|--push-only)
            PUSH_ONLY=true
            shift
            ;;
        -a|--all)
            BUILD_ALL=true
            shift
            ;;
        -t|--tag)
            IMAGE_TAG="$2"
            shift 2
            ;;
        --list)
            list_services
            exit 0
            ;;
        -*)
            echo "æœªçŸ¥é€‰é¡¹: $1"
            print_help
            exit 1
            ;;
        *)
            SERVICES_TO_BUILD="$SERVICES_TO_BUILD $1"
            shift
            ;;
    esac
done

# æ‰§è¡Œç™»å½•
if [ "$DO_LOGIN" = true ]; then
    login_swr
    if [ -z "$SERVICES_TO_BUILD" ] && [ "$BUILD_ALL" = false ]; then
        exit 0
    fi
fi

# ç¡®å®šè¦æž„å»ºçš„æœåŠ¡
if [ "$BUILD_ALL" = true ]; then
    SERVICES_TO_BUILD=$(get_all_services)
fi

# æ£€æŸ¥æ˜¯å¦æœ‰æœåŠ¡éœ€è¦æž„å»º
if [ -z "$SERVICES_TO_BUILD" ]; then
    echo "é”™è¯¯: è¯·æŒ‡å®šè¦æž„å»ºçš„æœåŠ¡ï¼Œæˆ–ä½¿ç”¨ -a æž„å»ºæ‰€æœ‰æœåŠ¡"
    echo ""
    print_help
    exit 1
fi

# æ˜¾ç¤ºæž„å»ºä¿¡æ¯
echo "================================================"
echo "SWR ä»“åº“é…ç½®"
echo "================================================"
echo "Registry:     $SWR_REGISTRY"
echo "Organization: $SWR_ORGANIZATION"
echo "Tag:          $IMAGE_TAG"
echo "================================================"
echo ""

# æž„å»ºå¹¶æŽ¨é€æ¯ä¸ªæœåŠ¡
for service in $SERVICES_TO_BUILD; do
    build_and_push_service "$service"
    echo ""
done

echo "================================================"
echo "ðŸŽ‰ æ‰€æœ‰æ“ä½œå®Œæˆ!"
echo "================================================"
