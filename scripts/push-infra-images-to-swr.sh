#!/usr/bin/env bash

# 上传基础设施镜像到华为云 SWR 仓库
set -euo pipefail

# 配置
SWR_REGISTRY="${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}"
SWR_ORGANIZATION="${SWR_ORGANIZATION:-go-nomads}"
MIRROR_REGISTRY="${MIRROR_REGISTRY:-}"
MIRROR_MODE="${MIRROR_MODE:-registry}"   # registry | daemon
MIRROR_STRICT="${MIRROR_STRICT:-0}"       # 1=失败即退出，0=失败回退源镜像
PUSH_RETRIES="${PUSH_RETRIES:-3}"
IMAGE_FILTER=""

# 基础设施镜像列表: 源镜像:源标签:目标名称:目标标签
INFRA_IMAGES="
docker.elastic.co/elasticsearch/elasticsearch:8.16.1:elasticsearch:8.16.1
"

print_help() {
  cat <<'EOF'
用法: push-infra-images-to-swr.sh [选项]
  -h, --help         显示帮助
  -l, --login        登录到 SWR
  --list             列出将要上传的镜像
  -a, --all          上传列表中全部镜像
  -i, --images n1,n2 仅上传指定目标镜像名（dest_name）

环境变量:
  SWR_REGISTRY       SWR 仓库地址 (默认: swr.ap-southeast-3.myhuaweicloud.com)
  SWR_ORGANIZATION   SWR 组织名称 (默认: go-nomads)
  MIRROR_REGISTRY    镜像加速地址 (可选)
  MIRROR_MODE        registry(默认) 或 daemon
  MIRROR_STRICT      1 失败即退出；0 失败回退源镜像(默认)
  PUSH_RETRIES       push 失败重试次数 (默认: 3)
EOF
}

list_images() {
  echo "将要上传的基础设施镜像列表:"
  echo "============================="
  echo "$INFRA_IMAGES" | grep -v '^$' | while IFS=':' read -r src_image src_tag dest_name dest_tag; do
    echo "  ${src_image}:${src_tag} -> ${SWR_REGISTRY}/${SWR_ORGANIZATION}/${dest_name}:${dest_tag}"
  done
}

resolve_pull_image() {
  local image="$1"

  if [ -z "$MIRROR_REGISTRY" ]; then
    echo "$image"; return
  fi

  if [ "$MIRROR_MODE" = "daemon" ]; then
    echo "$image"; return
  fi

  local mirror="${MIRROR_REGISTRY%/}"
  mirror="${mirror#http://}"
  mirror="${mirror#https://}"

  # 如果 image 含显式 registry，则直接前置 mirror
  if [[ "$image" =~ ^[^/]*[.:][^/]*/ ]]; then
    echo "${mirror}/${image}"; return
  fi

  # Docker Hub library 镜像
  echo "${mirror}/library/${image}"
}

login_swr() {
  echo "请手动登录到华为云 SWR: $SWR_REGISTRY"
  echo "示例: docker login -u [区域项目名]@[AK] -p [临时密码] $SWR_REGISTRY"
}

push_all_images() {
  echo "================================================"
  echo "开始上传基础设施镜像到 SWR"
  echo "================================================"

  echo "$INFRA_IMAGES" | grep -v '^$' | while IFS=':' read -r src_image src_tag dest_name dest_tag; do
    if [ -n "$IMAGE_FILTER" ]; then
      local match="false"
      IFS=',' read -ra FILTER_ARR <<< "$IMAGE_FILTER"
      for f in "${FILTER_ARR[@]}"; do
        if [ "$dest_name" = "$f" ]; then match="true"; break; fi
      done
      [ "$match" = "true" ] || continue
    fi

    local src="${src_image}:${src_tag}"
    local pull_src
    pull_src="$(resolve_pull_image "$src")"
    local dest="${SWR_REGISTRY}/${SWR_ORGANIZATION}/${dest_name}:${dest_tag}"

    echo ""
    echo "处理镜像: $src -> $dest"
    echo "----------------------------------------"

    echo "拉取源镜像: $pull_src (linux/amd64)"
    if ! docker pull --platform linux/amd64 "$pull_src"; then
      if [ "$MIRROR_STRICT" = "1" ] || [ "$pull_src" = "$src" ]; then
        exit 1
      fi
      echo "镜像加速失败, 回退到源镜像: $src" >&2
      docker pull --platform linux/amd64 "$src"
      pull_src="$src"
    fi

    echo "打标签: $dest"
    docker tag "$pull_src" "$dest"

    echo "推送到 SWR: $dest"
    for attempt in $(seq 1 "$PUSH_RETRIES"); do
      if docker push "$dest"; then break; fi
      if [ "$attempt" -eq "$PUSH_RETRIES" ]; then
        echo "推送失败, 已达最大重试次数 ($PUSH_RETRIES)" >&2
        exit 1
      fi
      echo "推送失败, 重试第 $((attempt + 1)) 次..." >&2
      sleep $((2 * attempt))
    done

    echo "✓ 完成: $dest"
  done

  echo ""
  echo "================================================"
  echo "所有基础设施镜像上传完成!"
  echo "================================================"
}

# 主程序
case "${1-}" in
  -h|--help)
    print_help;;
  -l|--login)
    login_swr;;
  --list)
    list_images;;
  -i|--images)
    if [ -z "${2-}" ]; then
      echo "请提供镜像名称列表, 例如: --images redis,elasticsearch" >&2
      exit 1
    fi
    IMAGE_FILTER="$2"
    shift 2
    push_all_images;;
  -a|--all)
    push_all_images;;
  "")
    print_help;;
  *)
    print_help; exit 1;;
esac
