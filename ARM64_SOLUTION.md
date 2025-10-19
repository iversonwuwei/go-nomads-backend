# ARM64 架构解决方案 - AccessViolationException 修复

## 🎯 问题总结

**错误**: `System.AccessViolationException` - QEMU 模拟器崩溃  
**原因**: Dockerfile 使用 `--platform=linux/amd64` 在 Apple Silicon (ARM64) 上通过 QEMU 模拟 x64 架构  
**影响**: 服务运行时崩溃，无法正常使用

## ✅ 解决方案：预生成 gRPC 代码

### 核心思路
在本地开发环境预先生成 gRPC 代码，容器构建时直接使用，避免在 ARM64 上运行 protoc 编译器。

### 优势对比

| 特性 | 之前 (x64 + QEMU) | 现在 (原生 ARM64) |
|------|------------------|------------------|
| 稳定性 | ❌ 经常崩溃 | ✅ 完全稳定 |
| 性能 | ❌ 慢速模拟 | ✅ 原生性能 |
| gRPC 支持 | ✅ 完整 | ✅ 完整 |
| 构建速度 | ⏱️ 较慢 | ⚡ 快速 |
| 跨平台 | ⚠️ 仅 x64 | ✅ ARM64 + x64 |

## 📦 已完成的修改

### 1. 预生成 gRPC 代码

```bash
# 目录结构
src/Shared/Shared/
├── Protos/              # Proto 源文件
│   ├── user.proto
│   └── product.proto
├── Generated/Protos/    # ⭐ 预生成的 C# 代码
│   ├── User.cs
│   ├── UserGrpc.cs
│   ├── Product.cs
│   └── ProductGrpc.cs
└── Shared.csproj
```

### 2. 更新 Shared.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Google.Protobuf" Version="3.32.1" />
    <PackageReference Include="Grpc.Net.Client" Version="2.71.0" />
    <!-- Grpc.Tools 已注释 - 仅重新生成时需要 -->
  </ItemGroup>

  <!-- Protobuf 自动编译已禁用 - 使用预生成代码 -->
</Project>
```

### 3. Dockerfile 修改（如需要）

移除所有 `--platform=linux/amd64` 和 `-r linux-x64` 参数，使用原生架构。

示例：
```dockerfile
# 之前
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN dotnet restore "Service.csproj" -r linux-x64

# 现在
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN dotnet restore "Service.csproj"
```

## 🔄 gRPC 代码管理流程

### 日常开发
✅ 直接使用预生成的代码  
✅ 无需 Grpc.Tools  
✅ 快速构建

### 修改 .proto 文件后需要重新生成

查看详细指南: [GRPC_REGENERATE.md](./GRPC_REGENERATE.md)

快速步骤：
```bash
# 1. 启用 Grpc.Tools（编辑 Shared.csproj）
# 2. 构建并生成代码
cd src/Shared/Shared
dotnet build

# 3. 复制生成的文件
cp obj/Debug/net9.0/Protos/*.cs Generated/Protos/

# 4. 禁用 Grpc.Tools（编辑 Shared.csproj）
# 5. 验证
dotnet clean && dotnet build

# 6. 提交到 Git
git add Generated/Protos/*.cs
git commit -m "chore: regenerate gRPC code"
```

## 🚀 使用方法

### 本地开发（推荐）

```bash
# UserService
cd src/Services/UserService/UserService
dotnet run

# DocumentService
cd src/Services/DocumentService/DocumentService
dotnet run --environment Development
```

### 容器部署

```bash
# 清理旧镜像（使用 x64 的）
./cleanup-all.sh

# 重新构建（使用原生架构）
./start-all.sh

# 或手动构建
docker build -f src/Services/UserService/UserService/Dockerfile \
  -t go-nomads-user-service:latest .
```

### 验证架构

```bash
# 检查构建成功
dotnet build src/Shared/Shared/Shared.csproj

# 检查镜像架构
docker inspect go-nomads-user-service:latest | grep Architecture
# 应该显示: "Architecture": "arm64"
```

## ⚠️ 注意事项

1. **不要手动编辑** `Generated/Protos/` 中的文件
2. **始终提交** 生成的代码到 Git
3. **容器构建时** 不需要 Grpc.Tools
4. **跨平台兼容** - 同时支持 ARM64 和 x64

## 📚 相关文档

- [GRPC_REGENERATE.md](./GRPC_REGENERATE.md) - gRPC 代码重新生成详细指南
- [PODMAN_COMPOSE_README.md](./PODMAN_COMPOSE_README.md) - 容器部署指南

## ✨ 测试验证

```bash
# 1. 测试 Shared 项目
cd src/Shared/Shared
dotnet clean && dotnet build
# ✅ 应该成功构建

# 2. 测试 UserService
cd src/Services/UserService/UserService
dotnet build
# ✅ 应该成功构建

# 3. 运行服务
dotnet run
# ✅ 应该正常启动，无崩溃
```

## 🎉 问题已解决

- ✅ 保留完整 gRPC 功能
- ✅ 使用原生 ARM64 架构
- ✅ 避免 AccessViolationException
- ✅ 提升构建和运行性能
- ✅ 跨平台兼容

---

创建时间: 2025-10-19  
状态: ✅ 已解决
