#!/usr/bin/env bash
set -euo pipefail

SUGGEST_FIX=false

for arg in "$@"; do
  case "$arg" in
    --suggest-fix)
      SUGGEST_FIX=true
      ;;
    -h|--help)
      echo "用法: $0 [--suggest-fix]"
      echo ""
      echo "  默认模式: 仅检查，发现问题返回非 0"
      echo "  --suggest-fix: 在发现问题时输出可复制的修复建议（不改文件）"
      exit 0
      ;;
    *)
      echo "未知参数: $arg"
      echo "使用 --help 查看用法"
      exit 2
      ;;
  esac
done

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [ "$SUGGEST_FIX" = true ]; then
  echo "🔍 检查 SkiaSharp 与 Docker 运行时依赖一致性（建议模式）..."
else
  echo "🔍 检查 SkiaSharp 与 Docker 运行时依赖一致性..."
fi

# 搜索所有用户项目 csproj（排除构建产物目录）
mapfile -t CSPROJ_FILES < <(find . -type f -name "*.csproj" \
  -not -path "*/bin/*" \
  -not -path "*/obj/*" | sort)

if [ ${#CSPROJ_FILES[@]} -eq 0 ]; then
  echo "ℹ️ 未找到任何 csproj，跳过检查"
  exit 0
fi

FAILED=0
CHECKED=0

print_fix_suggestion() {
  local dockerfile_path="$1"
  cat <<EOF
--- 建议修复片段: ${dockerfile_path} ---
# 安装 SkiaSharp 运行时依赖（图片压缩/转码需要）
RUN apt-get update \\
    && apt-get install -y --no-install-recommends libfontconfig1 \\
    && rm -rf /var/lib/apt/lists/*

# Alpine 基础镜像可使用：
# RUN apk add --no-cache fontconfig
--- 结束 ---
EOF
}

for csproj in "${CSPROJ_FILES[@]}"; do
  if ! grep -Eq 'PackageReference[[:space:]]+Include="SkiaSharp(\.NativeAssets\.[^"]*)?"|PackageReference[[:space:]]+Include="SkiaSharp"' "$csproj"; then
    continue
  fi

  CHECKED=$((CHECKED + 1))
  project_dir="$(dirname "$csproj")"
  dockerfile="$project_dir/Dockerfile"

  echo "➡️ 发现 SkiaSharp 项目: $csproj"

  if [ ! -f "$dockerfile" ]; then
    echo "❌ 未找到同目录 Dockerfile: $dockerfile"
    echo "   请为该服务 Dockerfile 安装 libfontconfig1（Debian）或 fontconfig（Alpine）"
    if [ "$SUGGEST_FIX" = true ]; then
      echo "   建议：在项目目录新增 Dockerfile，并加入上述依赖安装步骤"
    fi
    FAILED=1
    continue
  fi

  if grep -Eiq 'libfontconfig1|fontconfig' "$dockerfile"; then
    echo "✅ Dockerfile 依赖检查通过: $dockerfile"
  else
    echo "❌ Dockerfile 缺少 SkiaSharp 运行时依赖: $dockerfile"
    echo "   请添加: apt-get install -y --no-install-recommends libfontconfig1"
    if [ "$SUGGEST_FIX" = true ]; then
      print_fix_suggestion "$dockerfile"
    fi
    FAILED=1
  fi

done

if [ "$CHECKED" -eq 0 ]; then
  echo "ℹ️ 未发现引用 SkiaSharp 的项目，检查通过"
  exit 0
fi

if [ "$FAILED" -ne 0 ]; then
  if [ "$SUGGEST_FIX" = true ]; then
    echo "❌ SkiaSharp Docker 依赖检查失败（已输出修复建议）"
  else
    echo "❌ SkiaSharp Docker 依赖检查失败"
  fi
  exit 1
fi

echo "✅ SkiaSharp Docker 依赖检查通过"
